using EquiApi.Core.Services;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/horses")] 
public class HorseController(IHorseService service, ILogger<HorseController> logger) : BaseController
{
    [HttpGet("person/{personId:int}")] 
    [ProducesResponseType<DataTransfer.HorseListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<DataTransfer.HorseListResponse>> GetHorsesOfPerson([FromRoute] int personId)
    {
        if (personId <= 0) 
        {
            logger.LogWarning("personId: {id} was not valid", personId);
            return BadRequest();
        }

        var res = await service.GetAllHorsesOfPersonAsync(personId);

        return res.Match<ActionResult<DataTransfer.HorseListResponse>>(success => Ok(DataTransfer.HorseListResponse.FromHorses(success)),
                                                                       notFound => NotFound());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<DataTransfer.HorseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<DataTransfer.HorseDto>> GetById([FromRoute] int id)
    {
        if (id <= 0) 
        {
            logger.LogWarning("id: {id} was not valid", id);
            return BadRequest();
        }

        var res = await service.GetHorseByIdAsync(id);

        return res.Match<ActionResult<DataTransfer.HorseDto>>(ok => DataTransfer.HorseDto.FromHorse(ok),
                                                              _ => NotFound());
    }
}
