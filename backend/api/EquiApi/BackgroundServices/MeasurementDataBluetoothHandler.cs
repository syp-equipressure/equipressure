using System.Text;
using EquiApi.Core.Services;
using Microsoft.Extensions.Options;

namespace EquiApi.BackgroundServices;

/// <summary>
///     Stores the data received from the Bluetooth device in the measurement that is currently recorded.
///     Messages are expected to be terminated by a line feed, because a BLE notification only carries
///     (MTU - 3) bytes, so longer messages (e.g. a JSON array) arrive in several chunks.
/// </summary>
/// <param name="scopeFactory">Service scope factory</param>
/// <param name="clock">Clock instance to use</param>
/// <param name="options">Bluetooth options</param>
/// <param name="logger">The logger</param>
internal sealed class MeasurementDataBluetoothHandler(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    IOptions<BluetoothOptions> options,
    ILogger<MeasurementDataBluetoothHandler> logger) : IBluetoothDataHandler
{
    private const int MaxPendingLength = 64 * 1024;

    private readonly string _deviceId = options.Value.MeasurementDeviceId;
    private string _pending = string.Empty;

    /// <inheritdoc />
    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        // called sequentially by the connection service (single reader), so no locking needed
        _pending += Encoding.UTF8.GetString(data.Span);

        int end;
        while ((end = _pending.IndexOf('\n')) >= 0)
        {
            // also removes the '\r' of a "\r\n" terminator
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

    /// <inheritdoc />
    public void Reset()
    {
        // the rest of the message was lost with the previous connection, prepending it would corrupt the next one
        if (_pending.Length > 0)
        {
            logger.LogInformation("Discarding {Length} chars of an incomplete message of the previous connection",
                _pending.Length);
        }

        _pending = string.Empty;
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