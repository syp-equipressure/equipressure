using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;
using EquiApi.Util;
using EquiPressure.Core.Service;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace EquiApi.Controllers;

[Route("api/locations")]
public sealed class LocationController(
    ITransactionProvider transaction,
    ILocationService locationService,
    ILogger<LocationController> logger) : BaseController
{
    //TODO: Created at action
    // TODO: xml doc
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> CreateLocation([FromBody] DataTransfer.AddLocationRequest request)
    {
        if (!ValidateRequest<DataTransfer.AddLocationRequest.Validator, DataTransfer.AddLocationRequest>(request))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success<Address>, IBaseService.InvalidData, Error> result
                = await locationService.AddAddressAsync(request.Street, request.HouseNumber, request.CityName,
                                                        request.PLZ);

            return await result.Match<ValueTask<ActionResult>>(async success =>
                                                               {
                                                                   await transaction.CommitAsync();

                                                                   return Created();
                                                               },
                                                               async invalid =>
                                                               {
                                                                   await transaction.RollbackAsync();

                                                                   return BadRequest();
                                                               },
                                                               async error =>
                                                               {
                                                                   await transaction.RollbackAsync();

                                                                   return Conflict();
                                                               });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            logger.LogError("Error adding Location");
            return Problem();
        }
    }
}
