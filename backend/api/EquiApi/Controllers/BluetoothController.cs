using EquiApi.BackgroundServices;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/bluetooth")]
public sealed class BluetoothController(IBluetoothConnectionManager connectionManager) : BaseController
{
    /// <summary>
    ///     Gets the current state of the Bluetooth connection
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType<BluetoothStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<BluetoothStatusResponse> GetStatus()
    {
        return Ok(BluetoothStatusResponse.FromManager(connectionManager));
    }

    /// <summary>
    ///     Starts connecting to the device. Returns immediately, scanning and connecting run in the background.
    /// </summary>
    [HttpPost("connect")]
    [ProducesResponseType<BluetoothStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<BluetoothStatusResponse>> Connect(
        [FromBody] BluetoothConnectRequest? request)
    {
        var accepted = await connectionManager.ConnectAsync(request?.DeviceName);
        if (!accepted)
        {
            return Problem(
                           detail: $"Already running for device '{connectionManager.DeviceName}', disconnect first.",
                           statusCode: StatusCodes.Status409Conflict);
        }

        return Ok(BluetoothStatusResponse.FromManager(connectionManager));
    }

    /// <summary>
    ///     Stops the connection and returns once it is closed
    /// </summary>
    [HttpPost("disconnect")]
    [ProducesResponseType<BluetoothStatusResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<BluetoothStatusResponse>> Disconnect()
    {
        await connectionManager.DisconnectAsync();

        return Ok(BluetoothStatusResponse.FromManager(connectionManager));
    }
}
