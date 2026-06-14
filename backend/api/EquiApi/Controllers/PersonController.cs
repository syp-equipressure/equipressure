using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.Shared;
using EquiApi.Util;
using EquiPressure.Core.Service;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace EquiApi.Controllers;

// TODO: xml documentation
[Route("api/persons")]
public sealed class PersonController(
    ITransactionProvider transaction,
    IPersonService personService,
    ILogger<PersonController> logger) : BaseController
{
    [HttpGet("equestrians/{id:int}")]
    [ProducesResponseType<Helper.EquestrianBasicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<Helper.EquestrianBasicDto>> GetEquestrianById([FromRoute] int id)
    {
        // check, ob die id überhaupt sinn macht (muss positiv sein)
        if (id <= 0)
        {
            return BadRequest();
        }

        // liefert entweder success oder notfound
        var result = await personService.GetPersonAsEquestrianByIdAsync(id);
        
        // benutzen dtos für einheitlichkeit wenn 200 Ok, wenn NotFound 404 nicht
        return result.Match<ActionResult<Helper.EquestrianBasicDto>>(success =>
                                                                               Ok(Helper.EquestrianBasicDto
                                                                                   .FromEquestrianBasicData(success
                                                                                       .Value, id)),
                                                                           notFound => NotFound());
    }

    [HttpGet("saddlers/{saddlerId:int}")]
    [ProducesResponseType<Helper.SaddlerBasicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<Helper.SaddlerBasicDto>> GetSaddlerById(
        [FromRoute] int equestrianId, [FromRoute] int saddlerId)
    {
        if (equestrianId <= 0 || saddlerId <= 0)
        {
            return BadRequest();
        }

        var result = await personService.GetPersonAsSaddlerByIdAsync(saddlerId);

        return result.Match<ActionResult<Helper.SaddlerBasicDto>>(success =>
                                                                            Ok(Helper.SaddlerBasicDto
                                                                                   .FromSaddlerBasicData(success
                                                                                       .Value)),
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

        var result = await personService.GetNameByIdAsync(id);

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
                                                                          none => Ok(DataTransfer.HorseListResponse
                                                                              .FromHorses([])),
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

        OneOf<Success<List<MeasurementDevice>>, None, NotFound> result
            = await personService.GetAllDevicesByPersonAsync(id);

        return result.Match<ActionResult<DataTransfer.DeviceListResponse>>(success =>
                                                                               Ok(DataTransfer.DeviceListResponse
                                                                                   .FromDevices(success.Value)),
                                                                           none => Ok(DataTransfer.DeviceListResponse
                                                                               .FromDevices([])),
                                                                           notFound => NotFound());
    }

    [HttpGet("persons/saddlers")]
    [ProducesResponseType<Helper.SaddlersListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<Helper.SaddlersListResponse>> GetSaddlersWithAddress()
    {
        var result = await personService.GetAllSaddlersAsync();

        return result.Match<ActionResult<Helper.SaddlersListResponse>>(success =>
                                                                             {
                                                                                 var dtos = success.Value
                                                                                     .Select(Helper
                                                                                         .SaddlerBasicDto
                                                                                         .FromSaddlerBasicData)
                                                                                     .ToList();

                                                                                 return Ok(new Helper.
                                                                                     SaddlersListResponse(dtos));
                                                                             },
                                                                             none =>
                                                                                 Ok(new Helper.
                                                                                     SaddlersListResponse([])));
    }

    [HttpGet("persons/equestrians/{equestrianId:int}/favourites")]
    [ProducesResponseType<Helper.SaddlersListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<Helper.SaddlersListResponse>> GetSaddlerFavouritesWithAddress(
        [FromRoute] int equestrianId)
    {
        if (equestrianId <= 0)
        {
            return BadRequest();
        }

        OneOf<Success<List<Helper.SaddlerBasicData>>, None, NotFound> result
            = await personService.GetAllSaddlerFavouritesAsync(equestrianId);

        return result.Match<ActionResult<Helper.SaddlersListResponse>>(success =>
                                                                             {
                                                                                 var dtos = success.Value
                                                                                     .Select(Helper
                                                                                         .SaddlerBasicDto
                                                                                         .FromSaddlerBasicData)
                                                                                     .ToList();

                                                                                 return Ok(new Helper.
                                                                                     SaddlersListResponse(dtos));
                                                                             },
                                                                             none =>
                                                                                 Ok(new Helper.
                                                                                     SaddlersListResponse([])),
                                                                             notFound => NotFound());
    }

    [HttpGet("persons/equestrians/{equestrianId:int}/contacts")]
    [ProducesResponseType<Helper.SaddlersListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<Helper.SaddlersListResponse>> GetSaddlerContactsWithAddress(
        [FromRoute] int equestrianId)
    {
        if (equestrianId <= 0)
        {
            return BadRequest();
        }

        OneOf<Success<List<Helper.SaddlerBasicData>>, None, NotFound> result
            = await personService.GetAllSaddlerContactsAsync(equestrianId);

        return result.Match<ActionResult<Helper.SaddlersListResponse>>(success =>
                                                                             {
                                                                                 var dtos = success.Value
                                                                                     .Select(Helper
                                                                                         .SaddlerBasicDto
                                                                                         .FromSaddlerBasicData)
                                                                                     .ToList();

                                                                                 return Ok(new Helper.
                                                                                     SaddlersListResponse(dtos));
                                                                             },
                                                                             none =>
                                                                                 Ok(new Helper.
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

                                                                   return CreatedAtAction(nameof(GetProfileData),
                                                                    new { id = success.Value.Id }, success.Value);
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
        if (id <= 0 ||
            !ValidateRequest<DataTransfer.UpdatePersonRequest.Validator, DataTransfer.UpdatePersonRequest>(request))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, IBaseService.InvalidData, IBaseService.Conflict> result
                = await personService.UpdatePersonAsync(id, request.FirstName, request.LastName, request.Height,
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
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error updating person");

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
