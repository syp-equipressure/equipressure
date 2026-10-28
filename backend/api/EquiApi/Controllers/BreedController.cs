using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/breeds")]
public sealed class BreedController(IHorseService horseService) : BaseController
{
    /// <summary>
    ///     Gets all breeds ordered by name, e.g. for the selection when adding a horse
    /// </summary>
    [HttpGet]
    [ProducesResponseType<BreedListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<BreedListResponse>> GetBreeds()
    {
        var breeds = await horseService.GetAllBreedsAsync();

        return Ok(BreedListResponse.FromBreeds(breeds));
    }
}
