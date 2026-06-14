using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

/// <summary>
/// Handles all endpoints related to measurement groups, measurements, and measurement data.
/// </summary>
[Route("api/measurementgroups")]
public sealed class MeasurementController(
    IMeasurementService measurementService) : BaseController
{
    /// <summary>
    /// Returns all measurement groups for a given horse with minimal data (date, person, saddle).
    /// </summary>
    /// <param name="horseId">The id of the horse.</param>
    /// <returns>List of measurement groups or 404 if the horse does not exist.</returns>
    [HttpGet("horse/{horseId:int}")]
    [ProducesResponseType<DataTransfer.MeasurementGroupListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.MeasurementGroupListResponse>> GetGroupsByHorse([FromRoute] int horseId)
    {
        if (horseId <= 0)
        {
            return BadRequest();
        }

        var result = await measurementService.GetGroupsByHorseAsync(horseId);

        return result.Match<ActionResult<DataTransfer.MeasurementGroupListResponse>>(
            success => Ok(DataTransfer.MeasurementGroupListResponse.FromGroups(success.Value)),
            none    => Ok(DataTransfer.MeasurementGroupListResponse.FromGroups([])),
            notFound => NotFound());
    }
}
