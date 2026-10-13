using Microsoft.Extensions.Options;
using WordClock.BackgroundServices;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace EquiApi.BackgroundServices;

/// <summary>
///     A background service that keeps a connection to a Bluetooth device with a configured name
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
    private readonly BluetoothOptions _options = options.Value;
    private BluetoothClient? _client;
    private Task? _readTask;

    protected override string ServiceName => nameof(BluetoothConnectionService);

    protected override async ValueTask ExecuteAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.DeviceName))
        {
            Logger.LogError("{Section}:{Property} is not configured", BluetoothOptions.SectionName,
                nameof(BluetoothOptions.DeviceName));
            return;
        }

        // still connected and reading
        if (_readTask is { IsCompleted: false })
        {
            return;
        }

        // clean up the previous connection
        _readTask = null;
        _client?.Dispose();
        _client = null;

        var device = await FindDeviceAsync(stoppingToken);
        if (device is null)
        {
            Logger.LogInformation("Bluetooth device {DeviceName} not found", _options.DeviceName);
            return;
        }

        if (!device.Authenticated && !string.IsNullOrEmpty(_options.PairingPin))
        {
            Logger.LogInformation("Pairing with {DeviceName}", device.DeviceName);
            if (!BluetoothSecurity.PairRequest(device.DeviceAddress, _options.PairingPin))
            {
                Logger.LogWarning("Pairing with {DeviceName} failed", device.DeviceName);
                return;
            }
        }

        var client = new BluetoothClient();
        try
        {
            Logger.LogInformation("Connecting to {DeviceName} ({Address})", device.DeviceName,
                device.DeviceAddress);

            // serial port profile, change if the device uses a different service
            await Task.Run(() => client.Connect(device.DeviceAddress, BluetoothService.SerialPort),
                stoppingToken);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        _client = client;
        _readTask = Task.Run(() => ReadLoopAsync(client, device.DeviceName, stoppingToken), stoppingToken);
    }

    protected override Instant GetFirstTimeToRun() => Clock.GetCurrentInstant();

    protected override Instant GetNextTimeToRun(Instant lastRunTime) =>
        lastRunTime + Duration.FromTimeSpan(_options.CheckInterval);

    /// <inheritdoc />
    public override void Dispose()
    {
        _client?.Dispose();
        base.Dispose();
    }

    private async Task<BluetoothDeviceInfo?> FindDeviceAsync(CancellationToken stoppingToken)
    {
        // DiscoverDevices is blocking, so move it to the thread pool
        var devices = await Task.Run(() =>
        {
            using var client = new BluetoothClient();
            return client.DiscoverDevices();
        }, stoppingToken);

        return devices.FirstOrDefault(d =>
            string.Equals(d.DeviceName, _options.DeviceName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ReadLoopAsync(BluetoothClient client, string deviceName, CancellationToken stoppingToken)
    {
        try
        {
            Logger.LogInformation("Connected to {DeviceName}", deviceName);

            var stream = client.GetStream();
            var buffer = new byte[1024];

            while (!stoppingToken.IsCancellationRequested && client.Connected)
            {
                var read = await stream.ReadAsync(buffer, stoppingToken);
                if (read == 0)
                {
                    // remote side closed the connection
                    break;
                }

                await dataHandler.HandleAsync(buffer.AsMemory(0, read), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Reading from {DeviceName} failed", deviceName);
        }
        finally
        {
            Logger.LogWarning("Connection to {DeviceName} closed", deviceName);
            client.Dispose();
        }
    }
}