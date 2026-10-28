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
    ///     Removes a horse
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult> RemoveHorse([FromRoute][ValidId] int id)
    {
        var result = await horseService.RemoveHorseAsync(id);

        return result.Match<ActionResult>(
                                          _ => NoContent(),
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

    /// <summary>
    ///     Adds a person as rider (Reitbeteiligung) of a horse, the rider measures with the device of the owner
    /// </summary>
    [HttpPost("{id:int}/riders/{personId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> AddRider([FromRoute][ValidId] int id, [FromRoute][ValidId] int personId)
    {
        var result = await horseService.AddRiderToHorseAsync(id, personId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            _ => Conflict());
    }

    /// <summary>
    ///     Hides a rider of a horse, hidden riders cannot be selected for new measurements; the owner cannot be hidden
    /// </summary>
    [HttpPut("{id:int}/riders/{personId:int}/hidden")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> HideRider([FromRoute][ValidId] int id, [FromRoute][ValidId] int personId)
    {
        var result = await horseService.SetRiderHiddenAsync(id, personId, true);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            _ => Conflict());
    }

    /// <summary>
    ///     Shows a hidden rider of a horse again
    /// </summary>
    [HttpDelete("{id:int}/riders/{personId:int}/hidden")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> ShowRider([FromRoute][ValidId] int id, [FromRoute][ValidId] int personId)
    {
        var result = await horseService.SetRiderHiddenAsync(id, personId, false);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            _ => Conflict());
    }

    /// <summary>
    ///     Adds a saddle to a horse
    /// </summary>
    [HttpPost("{id:int}/saddles")]
    [ProducesResponseType<SaddleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<SaddleDto>> AddSaddleToHorse([FromRoute][ValidId] int id,
                                                                     [FromBody] AddSaddleRequest request)
    {
        if (!IsValid<AddSaddleRequest.Validator, AddSaddleRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await horseService.AddSaddleToHorse(request.Name, id, request.CategoryId);

        return result.Match<ActionResult<SaddleDto>>(
                                                     success => CreatedAtAction(nameof(GetSaddles), new { id },
                                                                                SaddleDto.FromSaddle(success.Value)),
                                                     _ => NotFound());
    }

    /// <summary>
    ///     Gets all saddle categories
    /// </summary>
    [HttpGet("saddles/categories")]
    [ProducesResponseType<SaddleCategoryListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<SaddleCategoryListResponse>> GetAllSaddleCategories()
    {
        var categories = await horseService.GetAllSaddleCategories();

        return Ok(SaddleCategoryListResponse.FromCategories(categories));
    }

    /// <summary>
    ///     Changes the given values of a horse
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType<HorseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<HorseDto>> PatchHorse([FromRoute][ValidId] int id,
                                                              [FromBody] PatchHorseRequest request)
    {
        if (!IsValid<PatchHorseRequest.Validator, PatchHorseRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await horseService.PatchHorseAsync(id, request.ToData());

        return result.Match<ActionResult<HorseDto>>(
                                                    success => Ok(HorseDto.FromHorse(success.Value)),
                                                    _ => NotFound());
    }
}