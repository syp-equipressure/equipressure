using System.Text;
using EquiApi.Core.Services;
using Microsoft.Extensions.Options;

namespace EquiApi.BackgroundServices;

/// <summary>
///     Stores the data received from the Bluetooth device in the measurement that is currently recorded.
///     Messages are expected to be terminated by a line feed, because a BLE notification only carries
///     (MTU - 3) bytes, so longer messages (e.g. a JSON array) arrive in several chunks.
/// </summary>
internal sealed class MeasurementDataBluetoothHandler(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    IOptions<BluetoothOptions> options,
    ILogger<MeasurementDataBluetoothHandler> logger) : IBluetoothDataHandler
{
    private const int MaxPendingLength = 64 * 1024;

    private readonly string _deviceId = options.Value.MeasurementDeviceId ?? options.Value.DeviceName;
    private string _pending = string.Empty;

    // called sequentially by the connection service (single reader), so no locking needed
    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        _pending += Encoding.UTF8.GetString(data.Span);

        int end;
        while ((end = _pending.IndexOf('\n')) >= 0)
        {
            var message = _pending[..end].Trim();
            _pending = _pending[(end + 1)..];

            if (message.Length > 0)
            {
                await StoreAsync(message);
            }
        }

        if (_pending.Length > MaxPendingLength)
        {
            logger.LogWarning("Discarding {Length} chars without a line terminator", _pending.Length);
            _pending = string.Empty;
        }
    }

    private async ValueTask StoreAsync(string message)
    {
        // the handler is a singleton, the unit of work is scoped
        using var scope = scopeFactory.CreateScope();
        var measurementService = scope.ServiceProvider.GetRequiredService<IMeasurementService>();

        // the service logs why data was rejected
        await measurementService.AddDataToLatestMeasurementOfDeviceAsync(
            _deviceId, message, clock.GetCurrentInstant());
    }
}