using EquiApi.Core.Services;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Microsoft.AspNetCore.Components.Route("api/horses")]
public class HorseController(IHorseService service, ILogger<HorseController> logger) : BaseController
{
    [HttpGet("{personId:int}")]
    [ProducesResponseType<DataTransfer.HorseListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<DataTransfer.HorseListResponse>> GetHorsesOfPerson([FromRoute] int personId)
    {
        if (personId < 0)
        {
            logger.LogWarning("personId: {id} was not valid", personId);
            return BadRequest();
        }

        var res = await service.GetAllHorsesOfPersonAsync(personId);

        return res.Match<ActionResult<DataTransfer.HorseListResponse>>(success => Ok(success),
                                                                       notFound => NotFound());
    }
}


