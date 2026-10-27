using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace EquiApi.Controllers;

[Route("api/measurementgroups")]
public sealed class MeasurementController(IMeasurementService measurementService) : BaseController
{
    /// <summary>
    ///     Gets all measurement groups of a horse with minimal data
    /// </summary>
    [HttpGet("horse/{horseId:int}")]
    [ProducesResponseType<MeasurementGroupListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MeasurementGroupListResponse>> GetGroupsOfHorse(
        [FromRoute][ValidId] int horseId)
    {
        if (!await HasAccessAsync((access, me) => access.CanViewHorseAsync(me, horseId)))
        {
            return AccessDenied();
        }

        var result = await measurementService.GetGroupsOfHorseAsync(horseId);

        return result.Match<ActionResult<MeasurementGroupListResponse>>(
            success => Ok(MeasurementGroupListResponse.FromGroups(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets a measurement group with the data of the horse, the rider and the saddle
    /// </summary>
    [HttpGet("{groupId:int}")]
    [ProducesResponseType<MeasurementGroupDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MeasurementGroupDetailDto>> GetGroupById([FromRoute][ValidId] int groupId)
    {
        if (!await HasAccessAsync((access, me) => access.CanViewMeasurementGroupAsync(me, groupId)))
        {
            return AccessDenied();
        }

        var result = await measurementService.GetGroupByIdAsync(groupId);

        return result.Match<ActionResult<MeasurementGroupDetailDto>>(
            success => Ok(MeasurementGroupDetailDto.FromGroup(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Adds a measurement group, the saddle has to belong to the horse
    /// </summary>
    [HttpPost]
    [ProducesResponseType<MeasurementGroupDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MeasurementGroupDetailDto>> CreateGroup(
        [FromBody] AddMeasurementGroupRequest request)
    {
        if (!IsValid<AddMeasurementGroupRequest.Validator, AddMeasurementGroupRequest>(request))
        {
            return ValidationProblem();
        }

        if (!await HasAccessAsync((access, me) => access.CanAddMeasurementGroupAsync(me, request.ToData())))
        {
            return AccessDenied();
        }

        var result = await measurementService.AddGroupAsync(request.ToData());

        return result.Match<ActionResult<MeasurementGroupDetailDto>>(
            success => CreatedAtAction(nameof(GetGroupById), new { groupId = success.Value.Id },
                                       MeasurementGroupDetailDto.FromGroup(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets all measurements of a measurement group
    /// </summary>
    [HttpGet("{groupId:int}/measurements")]
    [ProducesResponseType<MeasurementListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MeasurementListResponse>> GetMeasurementsOfGroup(
        [FromRoute][ValidId] int groupId)
    {
        if (!await HasAccessAsync((access, me) => access.CanViewMeasurementGroupAsync(me, groupId)))
        {
            return AccessDenied();
        }

        var result = await measurementService.GetMeasurementsOfGroupAsync(groupId);

        return result.Match<ActionResult<MeasurementListResponse>>(
            success => Ok(MeasurementListResponse.FromMeasurements(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the measurement of a measurement group that matches the given pace and hand
    /// </summary>
    [HttpGet("{groupId:int}/measurements/filter")]
    [ProducesResponseType<MeasurementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MeasurementDto>> GetMeasurementByFilter(
        [FromRoute][ValidId] int groupId, [FromQuery] string? pace, [FromQuery] string? hand)
    {
        if (!await HasAccessAsync((access, me) => access.CanViewMeasurementGroupAsync(me, groupId)))
        {
            return AccessDenied();
        }

        var result = await measurementService.GetMeasurementByFilterAsync(groupId, pace, hand);

        return result.Match<ActionResult<MeasurementDto>>(
            success => Ok(MeasurementDto.FromMeasurement(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the average of all data values of a measurement group, optionally filtered by pace and hand
    /// </summary>
    [HttpGet("{groupId:int}/measurements/average")]
    [ProducesResponseType<AggregateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AggregateResponse>> GetGroupAverage(
        [FromRoute][ValidId] int groupId, [FromQuery] string? pace, [FromQuery] string? hand) =>
        await AggregateAsync(groupId, () => measurementService.GetGroupAverageAsync(groupId, pace, hand));

    /// <summary>
    ///     Gets the minimum of all data values of a measurement group, optionally filtered by pace and hand
    /// </summary>
    [HttpGet("{groupId:int}/measurements/min")]
    [ProducesResponseType<AggregateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AggregateResponse>> GetGroupMin(
        [FromRoute][ValidId] int groupId, [FromQuery] string? pace, [FromQuery] string? hand) =>
        await AggregateAsync(groupId, () => measurementService.GetGroupMinAsync(groupId, pace, hand));

    /// <summary>
    ///     Gets the maximum of all data values of a measurement group, optionally filtered by pace and hand
    /// </summary>
    [HttpGet("{groupId:int}/measurements/max")]
    [ProducesResponseType<AggregateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AggregateResponse>> GetGroupMax(
        [FromRoute][ValidId] int groupId, [FromQuery] string? pace, [FromQuery] string? hand) =>
        await AggregateAsync(groupId, () => measurementService.GetGroupMaxAsync(groupId, pace, hand));

    /// <summary>
    ///     Gets a measurement of a measurement group
    /// </summary>
    [HttpGet("{groupId:int}/measurements/{measurementId:int}")]
    [ProducesResponseType<MeasurementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MeasurementDto>> GetMeasurementById(
        [FromRoute][ValidId] int groupId, [FromRoute][ValidId] int measurementId)
    {
        if (!await HasAccessAsync((access, me) => access.CanViewMeasurementGroupAsync(me, groupId)))
        {
            return AccessDenied();
        }

        var result = await measurementService.GetMeasurementByIdAsync(groupId, measurementId);

        return result.Match<ActionResult<MeasurementDto>>(
            success => Ok(MeasurementDto.FromMeasurement(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the average of all data values of a measurement
    /// </summary>
    [HttpGet("{groupId:int}/measurements/{measurementId:int}/average")]
    [ProducesResponseType<AggregateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AggregateResponse>> GetMeasurementAverage(
        [FromRoute][ValidId] int groupId, [FromRoute][ValidId] int measurementId) =>
        await AggregateAsync(groupId, () => measurementService.GetMeasurementAverageAsync(groupId, measurementId));

    /// <summary>
    ///     Gets the minimum of all data values of a measurement
    /// </summary>
    [HttpGet("{groupId:int}/measurements/{measurementId:int}/min")]
    [ProducesResponseType<AggregateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AggregateResponse>> GetMeasurementMin(
        [FromRoute][ValidId] int groupId, [FromRoute][ValidId] int measurementId) =>
        await AggregateAsync(groupId, () => measurementService.GetMeasurementMinAsync(groupId, measurementId));

    /// <summary>
    ///     Gets the maximum of all data values of a measurement
    /// </summary>
    [HttpGet("{groupId:int}/measurements/{measurementId:int}/max")]
    [ProducesResponseType<AggregateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AggregateResponse>> GetMeasurementMax(
        [FromRoute][ValidId] int groupId, [FromRoute][ValidId] int measurementId) =>
        await AggregateAsync(groupId, () => measurementService.GetMeasurementMaxAsync(groupId, measurementId));

    /// <summary>
    ///     Gets all data entries of a measurement, ordered by their timestamp
    /// </summary>
    [HttpGet("{groupId:int}/measurements/{measurementId:int}/measurementData")]
    [ProducesResponseType<MeasurementDataListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MeasurementDataListResponse>> GetDataOfMeasurement(
        [FromRoute][ValidId] int groupId, [FromRoute][ValidId] int measurementId)
    {
        if (!await HasAccessAsync((access, me) => access.CanViewMeasurementGroupAsync(me, groupId)))
        {
            return AccessDenied();
        }

        var result = await measurementService.GetDataOfMeasurementAsync(groupId, measurementId);

        return result.Match<ActionResult<MeasurementDataListResponse>>(
            success => Ok(MeasurementDataListResponse.FromData(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets a data entry of a measurement
    /// </summary>
    [HttpGet("{groupId:int}/measurements/{measurementId:int}/measurementData/{dataId:int}")]
    [ProducesResponseType<MeasurementDataDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MeasurementDataDto>> GetDataById(
        [FromRoute][ValidId] int groupId, [FromRoute][ValidId] int measurementId, [FromRoute][ValidId] int dataId)
    {
        if (!await HasAccessAsync((access, me) => access.CanViewMeasurementGroupAsync(me, groupId)))
        {
            return AccessDenied();
        }

        var result = await measurementService.GetDataByIdAsync(groupId, measurementId, dataId);

        return result.Match<ActionResult<MeasurementDataDto>>(
            success => Ok(MeasurementDataDto.FromData(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Adds a measurement to a measurement group, pace and hand have to be unique within the group
    /// </summary>
    [HttpPost("{groupId:int}/measurements")]
    [ProducesResponseType<MeasurementDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<MeasurementDto>> CreateMeasurement([FromRoute][ValidId] int groupId,
                                                                          [FromBody] AddMeasurementRequest request)
    {
        if (!IsValid<AddMeasurementRequest.Validator, AddMeasurementRequest>(request))
        {
            return ValidationProblem();
        }

        if (!await HasAccessAsync((access, me) => access.CanManageMeasurementGroupAsync(me, groupId)))
        {
            return AccessDenied();
        }

        var result = await measurementService.AddMeasurementAsync(groupId, request.Pace, request.Hand,
                                                                  request.Description);

        return result.Match<ActionResult<MeasurementDto>>(
            success => CreatedAtAction(nameof(GetMeasurementById),
                                       new { groupId, measurementId = success.Value.Id },
                                       MeasurementDto.FromMeasurement(success.Value)),
            _ => NotFound(),
            _ => Conflict());
    }

    /// <summary>
    ///     Removes a measurement including its data entries
    /// </summary>
    [HttpDelete("{groupId:int}/measurements/{measurementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeleteMeasurement([FromRoute][ValidId] int groupId,
                                                            [FromRoute][ValidId] int measurementId)
    {
        if (!await HasAccessAsync((access, me) => access.CanManageMeasurementGroupAsync(me, groupId)))
        {
            return AccessDenied();
        }

        var result = await measurementService.DeleteMeasurementAsync(groupId, measurementId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound());
    }

    private async ValueTask<ActionResult<AggregateResponse>> AggregateAsync(
        int groupId,
        Func<ValueTask<OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData>>> aggregate)
    {
        if (!await HasAccessAsync((access, me) => access.CanViewMeasurementGroupAsync(me, groupId)))
        {
            return AccessDenied();
        }

        return ToAggregateResponse(await aggregate());
    }

    private ActionResult<AggregateResponse> ToAggregateResponse(
        OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData> result) =>
        result.Match<ActionResult<AggregateResponse>>(
            success => Ok(new AggregateResponse(success.Value)),
            _ => NotFound(),
            _ => BadRequest());
}