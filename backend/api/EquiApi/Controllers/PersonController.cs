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

// TODO: xml documentation
// TODO: CreatedAtAction verwenden!
// TODO: dto stimmen ned mit http requests, post!! put
[Route("api/persons")]
public sealed class PersonController(
    ITransactionProvider transaction,
    IPersonService personService,
    ILogger<PersonController> logger) : BaseController
{
    [HttpGet("equestrians/{id:int}")]
    [ProducesResponseType<DataTransfer.EquestrianBasicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.EquestrianBasicDto>> GetEquestrianById([FromRoute] int id)
    {
        // check, ob die id überhaupt sinn macht (muss positiv sein)
        if (id <= 0)
        {
            return BadRequest();
        }

        // liefert entweder success oder notfound
        OneOf<Success<EquestrianBasicData>, NotFound> result = await personService.GetPersonAsEquestrianByIdAsync(id);

        // benutzen dtos für einheitlichkeit wenn 200 Ok, wenn NotFound 404 nicht
        return result.Match<ActionResult<DataTransfer.EquestrianBasicDto>>(success =>
                                                                               Ok(DataTransfer.EquestrianBasicDto
                                                                                        .FromEquestrianBasicData(success
                                                                                                 .Value, id)),
                                                                           notFound => NotFound());
    }

    [HttpGet("equestrians/{equestrianId:int}/saddlers/{saddlerId:int}")]
    [ProducesResponseType<DataTransfer.SaddlerBasicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.SaddlerBasicDto>> GetSaddlerById(
        [FromRoute] int equestrianId, [FromRoute] int saddlerId)
    {
        if (equestrianId <= 0 || saddlerId <= 0)
        {
            return BadRequest();
        }

        OneOf<Success<SaddlerBasicData>, IBaseService.InvalidData, NotFound> result
            = await personService.GetPersonAsSaddlerByIdAsync(saddlerId, equestrianId);

        return result.Match<ActionResult<DataTransfer.SaddlerBasicDto>>(success =>
                                                                            Ok(DataTransfer.SaddlerBasicDto
                                                                                   .FromSaddlerBasicData(success.Value)),
                                                                        invalidData => BadRequest(),
                                                                        notFound => NotFound());
    }

    [HttpGet("{id:int}/profile-data")]
    [ProducesResponseType<DataTransfer.NameDataDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.NameDataDto>> GetProfileData([FromRoute] int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        OneOf<Success<NameData>, NotFound> result = await personService.GetNameByIdAsync(id);

        return result.Match<ActionResult<DataTransfer.NameDataDto>>(success =>
                                                                        Ok(DataTransfer.NameDataDto
                                                                               .FromData(success.Value)),
                                                                    notFound => NotFound());
    }

    [HttpGet("{id:int}/location")]
    [ProducesResponseType<DataTransfer.AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.AddressDto>> GetAddress([FromRoute] int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        OneOf<Success<Address>, NotFound> result = await personService.GetPersonAddressAsync(id);

        return result.Match<ActionResult<DataTransfer.AddressDto>>(success =>
                                                                       Ok(DataTransfer.AddressDto
                                                                              .FromAddress(success.Value)),
                                                                   notFound => NotFound());
    }

    [HttpGet("{id:int}/contacts")]
    [ProducesResponseType<DataTransfer.PersonListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.PersonListResponse>> GetContacts([FromRoute] int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        // hier liefern wir entweder eine liste, none oder notfound
        OneOf<Success<IReadOnlyCollection<Person>>, None, NotFound> result = await personService.GetContactsAsync(id);

        // bei none einfach eine leere liste zurückgeben
        return result.Match<ActionResult<DataTransfer.PersonListResponse>>(success =>
                                                                               Ok(DataTransfer.PersonListResponse
                                                                                        .FromPersons(success.Value)),
                                                                           none => Ok(DataTransfer.PersonListResponse
                                                                                    .FromPersons([])),
                                                                           notFound => NotFound());
    }

    [HttpGet("{id:int}/favourites")]
    [ProducesResponseType<DataTransfer.PersonListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.PersonListResponse>> GetFavourites([FromRoute] int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        OneOf<Success<IReadOnlyCollection<Person>>, None, NotFound> result = await personService.GetFavouritesAsync(id);

        return result.Match<ActionResult<DataTransfer.PersonListResponse>>(success =>
                                                                               Ok(DataTransfer.PersonListResponse
                                                                                        .FromPersons(success.Value)),
                                                                           none => Ok(DataTransfer.PersonListResponse
                                                                                    .FromPersons([])),
                                                                           notFound => NotFound());
    }

    [HttpGet("{id:int}/horses")]
    [ProducesResponseType<DataTransfer.HorseListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.HorseListResponse>> GetHorses([FromRoute] int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        OneOf<Success<IReadOnlyCollection<Horse>>, None, NotFound> result = await personService.GetOwnedHorsesAsync(id);

        return result.Match<ActionResult<DataTransfer.HorseListResponse>>(success =>
                                                                              Ok(DataTransfer.HorseListResponse
                                                                                       .FromHorses(success.Value)),
                                                                          none => Ok(DataTransfer.PersonListResponse
                                                                                   .FromPersons([])),
                                                                          notFound => NotFound());
    }

    [HttpGet("{id:int}/devices")]
    [ProducesResponseType<DataTransfer.DeviceListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.DeviceListResponse>> GetDevices([FromRoute] int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        OneOf<Success<List<MeasurementDevice>>, None, NotFound> result = await personService.GetAllDevicesByPersonAsync(id);

        return result.Match<ActionResult<DataTransfer.DeviceListResponse>>(success =>
                                                                               Ok(DataTransfer.DeviceListResponse
                                                                                        .FromDevices(success.Value)),
                                                                           none => Ok(DataTransfer.DeviceListResponse
                                                                                    .FromDevices([])),
                                                                           notFound => NotFound());
    }

    [HttpGet("equestrians/{equestrianId:int}/saddlers/locations")]
    [ProducesResponseType<DataTransfer.SaddlersListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.SaddlersListResponse>> GetSaddlersWithAddress(
        [FromRoute] int equestrianId)
    {
        if (equestrianId <= 0)
        {
            return BadRequest();
        }

        OneOf<Success<List<SaddlerBasicData>>, None, NotFound> result
            = await personService.GetAllSaddlersAsync(equestrianId);

        return result.Match<ActionResult<DataTransfer.SaddlersListResponse>>(success =>
                                                                           {
                                                                               var dtos = success.Value
                                                                                   .Select(DataTransfer.SaddlerBasicDto
                                                                                            .FromSaddlerBasicData)
                                                                                   .ToList();

                                                                               return Ok(new DataTransfer.
                                                                                        SaddlersListResponse(dtos));
                                                                           },
                                                                           none =>
                                                                               Ok(new DataTransfer.
                                                                                        SaddlersListResponse([])),
                                                                           notFound => NotFound());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> CreatePerson([FromBody] DataTransfer.AddPersonRequest request)
    {
        if (!ValidateRequest<DataTransfer.AddPersonRequest.Validator, DataTransfer.AddPersonRequest>(request))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success<Person>, IBaseService.InvalidData, IBaseService.Conflict> result
                = await personService.AddPersonAsync(request.FirstName, request.LastName, request.Height,
                                                     request.Weight, request.DateOfBirth, request.Email,
                                                     request.WebsiteLink, request.Description, request.Address,
                                                     request.Role);

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
                                                               async conflict =>
                                                               {
                                                                   await transaction.RollbackAsync();

                                                                   return Conflict();
                                                               });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            logger.LogError("Error adding Person");

            return Problem();
        }
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> UpdatePerson([FromRoute] int id,
                                                       [FromBody] DataTransfer.UpdatePersonRequest request)
    {
        if (id <= 0 || id != request.Id ||
            !ValidateRequest<DataTransfer.UpdatePersonRequest.Validator, DataTransfer.UpdatePersonRequest>(request))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, IBaseService.InvalidData, IBaseService.Conflict> result
                = await personService.UpdatePersonAsync(request.Id, request.FirstName, request.LastName, request.Height,
                                                        request.Weight, request.DateOfBirth, request.Email,
                                                        request.WebsiteLink, request.Description, request.Address,
                                                        request.RoleAssignments);

            return await result.Match<ValueTask<ActionResult>>(async success =>
                                                               {
                                                                   await transaction.CommitAsync();

                                                                   return NoContent();
                                                               },
                                                               async notFound =>
                                                               {
                                                                   await transaction.RollbackAsync();

                                                                   return NotFound();
                                                               },
                                                               async invalid =>
                                                               {
                                                                   await transaction.RollbackAsync();

                                                                   return BadRequest();
                                                               },
                                                               async conflict =>
                                                               {
                                                                   await transaction.RollbackAsync();

                                                                   return Conflict();
                                                               });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            logger.LogError("Error updating person");

            return Problem();
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeletePerson([FromRoute] int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> result = await personService.DeletePersonAsync(id);

            return await result.Match<ValueTask<IActionResult>>(async success =>
                                                                {
                                                                    await transaction.CommitAsync();

                                                                    return NoContent();
                                                                },
                                                                async notFound =>
                                                                {
                                                                    await transaction.RollbackAsync();

                                                                    return NotFound();
                                                                });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            logger.LogError("Error removing Person");

            return Problem();
        }
    }
}
