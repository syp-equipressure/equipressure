using System.Threading.Channels;
using InTheHand.Bluetooth;
using Microsoft.Extensions.Options;
using WordClock.BackgroundServices;

namespace EquiApi.BackgroundServices;

/// <summary>
///     A background service that keeps a BLE (GATT) connection to a device with a configured name
///     and subscribes to notifications of a configured characteristic
/// </summary>
/// <param name="clock">Clock instance to use</param>
/// <param name="scopeFactory">Service scope factory</param>
/// <param name="options">Bluetooth options</param>
/// <param name="dataHandler">Handler for the received data</param>
/// <param name="logger">The logger</param>
internal sealed class BluetoothConnectionService(
    IClock clock,
    IServiceScopeFactory scopeFactory,
    IOptions<BluetoothOptions> options,
    IBluetoothDataHandler dataHandler,
    ILogger<BluetoothConnectionService> logger)
    : BackgroundServiceBase<BluetoothConnectionService>(clock, scopeFactory, logger)
{
    private static readonly TimeSpan ScanTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(15);

    private readonly BluetoothOptions _options = options.Value;
    private BluetoothDevice? _device;
    private Task? _sessionTask;

    protected override string ServiceName => nameof(BluetoothConnectionService);

    protected override async ValueTask ExecuteAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.DeviceName))
        {
            Logger.LogError("{Section}:{Property} is not configured", BluetoothOptions.SectionName,
                nameof(BluetoothOptions.DeviceName));
            return;
        }

        // still connected and receiving
        if (_sessionTask is { IsCompleted: false })
        {
            return;
        }

        // clean up the previous connection
        _sessionTask = null;
        DisconnectDevice();

        var device = await FindDeviceAsync(stoppingToken);
        if (device is null)
        {
            Logger.LogInformation("BLE device {DeviceName} not found", _options.DeviceName);
            return;
        }

        var deviceName = device.Name ?? _options.DeviceName;
        GattCharacteristic? characteristic;
        try
        {
            Logger.LogInformation("Connecting to {DeviceName} ({Id})", deviceName, device.Id);

            var gatt = device.Gatt;

            // ConnectAsync has no cancellation/timeout support, so enforce both here
            await gatt.ConnectAsync().WaitAsync(ConnectTimeout, stoppingToken);

            var service = await gatt.GetPrimaryServiceAsync(BluetoothUuid.FromGuid(_options.ServiceUuid))
                .WaitAsync(ConnectTimeout, stoppingToken);
            if (service is null)
            {
                Logger.LogWarning("Service {Service} not found on {DeviceName}", _options.ServiceUuid, deviceName);
                SafeDisconnect(device);
                return;
            }

            characteristic = await service.GetCharacteristicAsync(BluetoothUuid.FromGuid(_options.CharacteristicUuid))
                .WaitAsync(ConnectTimeout, stoppingToken);
            if (characteristic is null)
            {
                Logger.LogWarning("Characteristic {Characteristic} not found on {DeviceName}",
                    _options.CharacteristicUuid, deviceName);
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
            Logger.LogWarning("Connect to {DeviceName} timed out after {Timeout}", deviceName, ConnectTimeout);
            return;
        }
        catch (Exception ex)
        {
            SafeDisconnect(device);
            Logger.LogWarning("Connect to {DeviceName} failed: {Message}", deviceName, ex.Message);

            if (ex.Message.Contains("br-connection", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning(
                    "BlueZ tried a classic (BR/EDR) connection instead of BLE. " +
                    "Set 'ControllerMode = le' in /etc/bluetooth/main.conf, run " +
                    "'bluetoothctl remove {Id}' and restart the bluetooth service",
                    device.Id);
            }

            return;
        }

        _device = device;
        _sessionTask = Task.Run(() => RunSessionAsync(device, characteristic, deviceName, stoppingToken),
            stoppingToken);
    }

    protected override Instant GetFirstTimeToRun() => Clock.GetCurrentInstant();

    protected override Instant GetNextTimeToRun(Instant lastRunTime) =>
        lastRunTime + Duration.FromTimeSpan(_options.CheckInterval);

    /// <inheritdoc />
    public override void Dispose()
    {
        DisconnectDevice();
        base.Dispose();
    }

    private async Task<BluetoothDevice?> FindDeviceAsync(CancellationToken stoppingToken)
    {
        // BLE devices have to be discovered by scanning, there is no SPP-like "paired devices" shortcut
        var scanOptions = new RequestDeviceOptions { AcceptAllDevices = true };

        // safety net so a hanging scan cannot block the service forever
        using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        scanCts.CancelAfter(ScanTimeout);

        try
        {
            var devices = await Bluetooth.ScanForDevicesAsync(scanOptions, scanCts.Token);

            return devices.FirstOrDefault(d =>
                string.Equals(d.Name?.Trim(), _options.DeviceName, StringComparison.OrdinalIgnoreCase));
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            Logger.LogWarning("BLE scan timed out after {Timeout}", ScanTimeout);
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

        characteristic.CharacteristicValueChanged += OnValueChanged;
        device.GattServerDisconnected += OnDisconnected;
            await characteristic.StartNotificationsAsync();
            Logger.LogInformation("Connected to {DeviceName}", deviceName);

            // the connection might have dropped before the handler was attached
            if (!device.Gatt.IsConnected)
            {
                return;
            }

            await foreach (var data in channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await dataHandler.HandleAsync(data, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Logger.LogError(ex, "Handling data from {DeviceName} failed", deviceName);
                }
            }
    }

    private void DisconnectDevice()
    {
        if (_device is null)
        {
            return;
        }

        SafeDisconnect(_device);
        _device = null;
    }

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