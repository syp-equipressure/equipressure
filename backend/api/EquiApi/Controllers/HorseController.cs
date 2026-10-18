using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/horses")]
public sealed class HorseController(IHorseService horseService) : BaseController
{
    /// <summary>
    ///     Gets a horse
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<HorseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<HorseDto>> GetHorseById([FromRoute][ValidId] int id)
    {
        var result = await horseService.GetHorseByIdAsync(id);

        return result.Match<ActionResult<HorseDto>>(
            success => Ok(HorseDto.FromHorse(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Adds a horse
    /// </summary>
    [HttpPost]
    [ProducesResponseType<HorseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<HorseDto>> CreateHorse([FromBody] AddHorseRequest request)
    {
        if (!IsValid<AddHorseRequest.Validator, AddHorseRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await horseService.AddHorseAsync(request.ToData());

        return result.Match<ActionResult<HorseDto>>(
            success => CreatedAtAction(nameof(GetHorseById), new { id = success.Value.Id },
                                       HorseDto.FromHorse(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the saddles of a horse
    /// </summary>
    [HttpGet("{id:int}/saddles")]
    [ProducesResponseType<SaddleListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<SaddleListResponse>> GetSaddles([FromRoute][ValidId] int id)
    {
        var result = await horseService.GetSaddlesOfHorseAsync(id);

        return result.Match<ActionResult<SaddleListResponse>>(
            success => Ok(SaddleListResponse.FromSaddles(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the owner and the visible riders of a horse, the owner is always the first entry
    /// </summary>
    [HttpGet("{id:int}/riders")]
    [ProducesResponseType<PersonListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PersonListResponse>> GetRiders([FromRoute][ValidId] int id)
    {
        var result = await horseService.GetRidersOfHorseAsync(id);

        return result.Match<ActionResult<PersonListResponse>>(
            success => Ok(PersonListResponse.FromPersons(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the hidden riders of a horse
    /// </summary>
    [HttpGet("{id:int}/riders/hidden")]
    [ProducesResponseType<PersonListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PersonListResponse>> GetHiddenRiders([FromRoute][ValidId] int id)
    {
        var result = await horseService.GetHiddenRidersOfHorseAsync(id);

        return result.Match<ActionResult<PersonListResponse>>(
            success => Ok(PersonListResponse.FromPersons(success.Value)),
            _ => NotFound());
    }
}