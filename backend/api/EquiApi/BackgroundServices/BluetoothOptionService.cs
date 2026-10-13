namespace EquiApi.BackgroundServices;

/// <summary>
///     Configuration for the Bluetooth connection
/// </summary>
internal sealed class BluetoothOptions
{
    /// <summary>
    ///     Gets the configuration section name
    /// </summary>
    public const string SectionName = "Bluetooth";

    /// <summary>
    ///     Gets or sets the name of the device to connect to (case-insensitive)
    /// </summary>
    public string DeviceName { get; set; } = "EquiPressure_Dummy";

    /// <summary>
    ///     Gets or sets the interval in which the connection is checked and re-established if necessary
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     Gets or sets the PIN used to pair the device if it is not paired yet. Leave empty to skip pairing.
    /// </summary>
    public string? PairingPin { get; set; }
}

/// <summary>
///     Handles the data received from the Bluetooth device
/// </summary>
internal interface IBluetoothDataHandler
{
    /// <summary>
    ///     Handle received data
    /// </summary>
    /// <param name="data">Received bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);
}

/// <summary>
///     Default handler which only logs the received data
/// </summary>
/// <param name="logger">The logger</param>
internal sealed class LoggingBluetoothDataHandler(ILogger<LoggingBluetoothDataHandler> logger)
    : IBluetoothDataHandler
{
    /// <inheritdoc />
    public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        logger.LogInformation(
                              "Received {Count} bytes | HEX: {Hex} | ASCII: {Text}",
                              data.Length,
                              Convert.ToHexString(data.Span),
                              System.Text.Encoding.ASCII.GetString(data.Span));
        Console.WriteLine("Received {0} bytes", data.Length);
        return ValueTask.CompletedTask;
    }
}
