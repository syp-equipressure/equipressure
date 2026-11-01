using System.ComponentModel.DataAnnotations;
using EquiApi.BackgroundServices;

namespace EquiApi.Dtos;

/// <summary>
///     Request to start the Bluetooth connection
/// </summary>
public sealed record BluetoothConnectRequest
{
    /// <summary>
    ///     Advertised name of the device to connect to. If empty, the configured default is used.
    /// </summary>
    [StringLength(248)] // max. length of a Bluetooth device name
    public string? DeviceName { get; init; }
}

/// <summary>
///     Current state of the Bluetooth connection
/// </summary>
/// <param name="Status">Disconnected, Connecting or Connected</param>
/// <param name="DeviceName">Name of the device the connection is (or was last) started for</param>
/// <param name="Error">Last error, null if there is none</param>
public sealed record BluetoothStatusResponse(string Status, string? DeviceName, string? Error)
{
    public static BluetoothStatusResponse FromManager(IBluetoothConnectionManager manager) =>
        new(manager.Status.ToString(), manager.DeviceName, manager.LastError);
}
