using System.Threading.Channels;
using InTheHand.Bluetooth;
using Microsoft.Extensions.Options;

namespace EquiApi.BackgroundServices;

public enum BluetoothConnectionStatus
{
    /// <summary>Not started (or stopped by the user)</summary>
    Disconnected,

    /// <summary>Started, scanning / connecting / waiting for the next attempt</summary>
    Connecting,

    /// <summary>Connected and receiving notifications</summary>
    Connected
}

/// <summary>
///     Starts and stops the BLE connection on demand
/// </summary>
public interface IBluetoothConnectionManager
{
    BluetoothConnectionStatus Status { get; }

    /// <summary>Name of the device the connection is (or was last) started for</summary>
    string? DeviceName { get; }

    /// <summary>Last error of the current/previous attempt, null if there is none</summary>
    string? LastError { get; }

    /// <summary>
    ///     Starts connecting (and reconnecting) until <see cref="DisconnectAsync" /> is called.
    /// </summary>
    /// <param name="deviceName">Advertised device name, null/empty = default from the configuration</param>
    /// <returns>
    ///     False if it is already running for a different device name, otherwise true
    ///     (also if already running for the same device). Configuration errors are reported via
    ///     <see cref="LastError" />.
    /// </returns>
    Task<bool> ConnectAsync(string? deviceName = null);

    /// <summary>Stops the connection and waits until it is closed. Idempotent.</summary>
    Task DisconnectAsync();
}

/// <summary>
///     Keeps a BLE (GATT) connection to a device with a given name while it is switched on,
///     and subscribes to notifications of a configured characteristic
/// </summary>
internal sealed class BluetoothConnectionService(
    IOptions<BluetoothOptions> options,
    IBluetoothDataHandler dataHandler,
    ILogger<BluetoothConnectionService> logger)
    : IBluetoothConnectionManager, IHostedService
{
    private static readonly TimeSpan scanTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan connectTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan stopTimeout = TimeSpan.FromSeconds(10);

    private readonly BluetoothOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private volatile BluetoothConnectionStatus _status = BluetoothConnectionStatus.Disconnected;
    private volatile string? _lastError;
    private volatile string _deviceName = string.Empty;

    public BluetoothConnectionStatus Status => _status;

    public string? DeviceName => _deviceName.Length > 0 ? _deviceName : null;

    public string? LastError => _lastError;

    public async Task<bool> ConnectAsync(string? deviceName = null)
    {
        await _gate.WaitAsync();
        try
        {
            var requested = string.IsNullOrWhiteSpace(deviceName) ? _options.DeviceName : deviceName.Trim();

            // already switched on: fine for the same device, conflict for another one
            if (_runTask is { IsCompleted: false })
            {
                return string.Equals(requested, _deviceName, StringComparison.OrdinalIgnoreCase);
            }

            _lastError = null;

            if (string.IsNullOrWhiteSpace(requested))
            {
                SetError("No device name given and " +
                         $"{BluetoothOptions.SectionName}:{nameof(BluetoothOptions.DeviceName)} is not configured");
                return true;
            }

            // without it the received data cannot be assigned to a measurement
            if (string.IsNullOrWhiteSpace(_options.MeasurementDeviceId))
            {
                SetError(
                    $"{BluetoothOptions.SectionName}:{nameof(BluetoothOptions.MeasurementDeviceId)} is not configured");
                return true;
            }

            _deviceName = requested;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _status = BluetoothConnectionStatus.Connecting;
            _runTask = Task.Run(() => RunAsync(token), CancellationToken.None);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_cts is null || _runTask is null)
            {
                return;
            }

            await _cts.CancelAsync();

            try
            {
                // RunAsync never throws; the timeout protects against hanging BLE calls during cleanup
                await _runTask.WaitAsync(stopTimeout);
            }
            catch (TimeoutException)
            {
                logger.LogWarning("Bluetooth session did not stop within {Timeout}", stopTimeout);
            }

            _cts.Dispose();
            _cts = null;
            _runTask = null;
            _status = BluetoothConnectionStatus.Disconnected;
        }
        finally
        {
            _gate.Release();
        }
    }

    // IHostedService: nothing is started automatically, only clean up on shutdown
    Task IHostedService.StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => DisconnectAsync();

    /// <summary>
    ///     Connect loop: runs until cancelled. Never throws.
    /// </summary>
    private async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndReceiveAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Bluetooth connection failed");
                    SetError(ex.Message);
                }

                // attempt or session is over, try again after the interval
                _status = BluetoothConnectionStatus.Connecting;
                await Task.Delay(_options.CheckInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // stopped by the user or shutting down
        }
        finally
        {
            _status = BluetoothConnectionStatus.Disconnected;
        }
    }

    /// <summary>
    ///     One attempt: find the device, connect and receive until the connection ends
    /// </summary>
    private async Task ConnectAndReceiveAsync(CancellationToken stoppingToken)
    {
        var device = await FindDeviceAsync(stoppingToken);
        if (device is null)
        {
            logger.LogInformation("BLE device {DeviceName} not found", _deviceName);
            SetError($"Device {_deviceName} not found");
            return;
        }

        var deviceName = device.Name ?? _deviceName;
        GattCharacteristic? characteristic;
        try
        {
            logger.LogInformation("Connecting to {DeviceName} ({Id})", deviceName, device.Id);

            var gatt = device.Gatt;

            // ConnectAsync has no cancellation/timeout support, so enforce both here
            await gatt.ConnectAsync().WaitAsync(connectTimeout, stoppingToken);

            var service = await gatt.GetPrimaryServiceAsync(BluetoothUuid.FromGuid(_options.ServiceUuid))
                .WaitAsync(connectTimeout, stoppingToken);
            if (service is null)
            {
                logger.LogWarning("Service {Service} not found on {DeviceName}", _options.ServiceUuid, deviceName);
                SetError($"Service {_options.ServiceUuid} not found");
                SafeDisconnect(device);
                return;
            }

            characteristic = await service.GetCharacteristicAsync(BluetoothUuid.FromGuid(_options.CharacteristicUuid))
                .WaitAsync(connectTimeout, stoppingToken);
            if (characteristic is null)
            {
                logger.LogWarning("Characteristic {Characteristic} not found on {DeviceName}",
                    _options.CharacteristicUuid, deviceName);
                SetError($"Characteristic {_options.CharacteristicUuid} not found");
                SafeDisconnect(device);
                return;
            }
        }
        catch (OperationCanceledException)
        {
            SafeDisconnect(device);
            throw;
        }
        catch (TimeoutException)
        {
            SafeDisconnect(device);
            logger.LogWarning("Connect to {DeviceName} timed out after {Timeout}", deviceName, connectTimeout);
            SetError("Connect timed out");
            return;
        }
        catch (Exception ex)
        {
            SafeDisconnect(device);
            logger.LogWarning("Connect to {DeviceName} failed: {Message}", deviceName, ex.Message);
            SetError($"Connect failed: {ex.Message}");

            if (ex.Message.Contains("br-connection", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "BlueZ tried a classic (BR/EDR) connection instead of BLE. " +
                    "Set 'ControllerMode = le' in /etc/bluetooth/main.conf, run " +
                    "'bluetoothctl remove {Id}' and restart the bluetooth service",
                    device.Id);
            }

            return;
        }

        await RunSessionAsync(device, characteristic, deviceName, stoppingToken);
    }

    private async Task<BluetoothDevice?> FindDeviceAsync(CancellationToken stoppingToken)
    {
        // BLE devices have to be discovered by scanning, there is no SPP-like "paired devices" shortcut
        var scanOptions = new RequestDeviceOptions { AcceptAllDevices = true };

        // safety net so a hanging scan cannot block the service forever
        using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        scanCts.CancelAfter(scanTimeout);

        try
        {
            var devices = await Bluetooth.ScanForDevicesAsync(scanOptions, scanCts.Token);

            return devices.FirstOrDefault(d =>
                string.Equals(d.Name?.Trim(), _deviceName, StringComparison.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning("BLE scan timed out after {Timeout}", scanTimeout);
            SetError("Scan timed out");
            return null;
        }
    }

    private async Task RunSessionAsync(
        BluetoothDevice device,
        GattCharacteristic characteristic,
        string deviceName,
        CancellationToken stoppingToken)
    {
        // notifications arrive on a callback thread - queue them so the (async) handler
        // is called sequentially and in order
        var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

        void OnValueChanged(object? sender, GattCharacteristicValueChangedEventArgs e)
        {
            if (e.Value is { Length: > 0 } value)
            {
                channel.Writer.TryWrite(value);
            }
        }

        void OnDisconnected(object? sender, EventArgs e) => channel.Writer.TryComplete();

        // the previous session has completed, so the handler is not called concurrently
        dataHandler.Reset();

        characteristic.CharacteristicValueChanged += OnValueChanged;
        device.GattServerDisconnected += OnDisconnected;
        try
        {
            await characteristic.StartNotificationsAsync();

            // the connection might have dropped before the handler was attached
            if (!device.Gatt.IsConnected)
            {
                return;
            }

            logger.LogInformation("Connected to {DeviceName}", deviceName);
            _lastError = null;
            _status = BluetoothConnectionStatus.Connected;

            await foreach (var data in channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await dataHandler.HandleAsync(data, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Handling data from {DeviceName} failed", deviceName);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // stopped by the user or shutting down
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Receiving from {DeviceName} failed", deviceName);
            SetError($"Receiving failed: {ex.Message}");
        }
        finally
        {
            characteristic.CharacteristicValueChanged -= OnValueChanged;
            device.GattServerDisconnected -= OnDisconnected;
            channel.Writer.TryComplete();

            try
            {
                await characteristic.StopNotificationsAsync();
            }
            catch
            {
                // already disconnected
            }

            SafeDisconnect(device);
            logger.LogWarning("Connection to {DeviceName} closed", deviceName);

            if (!stoppingToken.IsCancellationRequested)
            {
                SetError($"Connection to {deviceName} lost");
            }
        }
    }

    private void SetError(string message) => _lastError = message;

    private static void SafeDisconnect(BluetoothDevice device)
    {
        try
        {
            device.Gatt.Disconnect();
        }
        catch
        {
            // best effort
        }
    }
}