using EquiApi.Core.Services;
using EquiApi.Persistence.Util;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Microsoft.AspNetCore.Components.Route("api/horses")]
public class HorseController(IHorseService service, ILogger<HorseController> logger, ITransactionProvider transaction)
    : BaseController
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

    [HttpGet("{id:int}")]
    [ProducesResponseType<DataTransfer.HorseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<DataTransfer.HorseDto>> GetById([FromRoute] int id)
    {
        if (id < 0)
        {
            logger.LogWarning("id: {id} was not valid", id);
        }

        var res = await service.GetHorseByIdAsync(id);

        return res.Match<ActionResult<DataTransfer.HorseDto>>(ok => DataTransfer.HorseDto.FromHorse(ok),
                                                              _ => NotFound());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> CreateHorse([FromBody] DataTransfer.AddHorseRequest request)
    {
        if (!ValidateRequest<DataTransfer.AddHorseRequest.Validator, DataTransfer.AddHorseRequest>(request))
        {
            logger.LogWarning("Data for add horse route was not correct");
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            var result = await service.AddHorse(request.Name,
                                                request.DateOfBirth,
                                                request.Weight,
                                                request.Height,
                                                request.Gender,
                                                request.Address,
                                                request.Breeds,
                                                request.OwnerId);

            return await result.Match<ValueTask<IActionResult>>(async success =>
                                                                {
                                                                    logger
                                                                        .LogInformation("Horse {name} successfully added for Owner {ownerId}",
                                                                         request.Name, request.OwnerId);
                                                                    await transaction.CommitAsync();

                                                                    return Created();
                                                                },
                                                                async invalidData =>
                                                                {
                                                                    logger
                                                                        .LogWarning("CreateHorse failed — Invalid data provided for horse {name}",
                                                                         request.Name);
                                                                    await transaction.RollbackAsync();

                                                                    return BadRequest();
                                                                },
                                                                async notFound =>
                                                                {
                                                                    logger
                                                                        .LogWarning("CreateHorse failed — Owner with ID {ownerId} not found",
                                                                         request.OwnerId);
                                                                    await transaction.RollbackAsync();

                                                                    return NotFound();
                                                                });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error adding Horse {name}", request.Name);

            return Problem();
        }
    }
}
