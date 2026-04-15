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

[Route("api/persons")]
public sealed class PersonController(
    ITransactionProvider transaction,
    IPersonService personService,
    ILogger<PersonController> logger) : BaseController
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<DataTransfer.EquestrianBasicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.EquestrianBasicDto>> GetEquestrianById([FromRoute] int id)
    {
        // check, ob die id überhaupt sinn macht (muss positiv sein)
        if (id <= 0)
        {
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        // liefert entweder success oder notfound
        OneOf<Success<EquestrianBasicData>, NotFound> result = await personService.GetPersonAsEquestrianByIdAsync(id);

        // switchen beim logging für die verschiedenen cases
        result.Switch(success => { logger.LogInformation("Successfully got Equestrian"); },
                      notFound => { logger.LogError("Equestrian was not found"); });

        // benutzen dtos für einheitlichkeit wenn 200 Ok, wenn NotFound 404 nicht
        return result.Match<ActionResult<DataTransfer.EquestrianBasicDto>>(success =>
                                                                               Ok(DataTransfer.EquestrianBasicDto
                                                                                   .FromEquestrianBasicData(success
                                                                                       .Value, id)),
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
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        OneOf<Success<NameData>, NotFound> result = await personService.GetNameByIdAsync(id);

        result.Switch(success => { logger.LogInformation("Successfully got Person"); },
                      notFound => { logger.LogError("Person was not found"); });

        return result.Match<ActionResult<DataTransfer.NameDataDto>>(success =>
                                                                        Ok(DataTransfer.NameDataDto
                                                                               .FromData(success.Value)),
                                                                    notFound => NotFound());
    }

    [HttpGet("{id:int}/address")]
    [ProducesResponseType<DataTransfer.AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.AddressDto>> GetAddress([FromRoute] int id)
    {
        if (id <= 0)
        {
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        OneOf<Success<Address>, NotFound> result = await personService.GetPersonAddressAsync(id);

        result.Switch(success => { logger.LogInformation("Successfully got Address"); },
                      notFound => { logger.LogError("Address was not found"); });

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
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        // hier liefern wir entweder eine liste, none oder notfound
        OneOf<Success<IReadOnlyCollection<Person>>, None, NotFound> result = await personService.GetContactsAsync(id);

        result.Switch(success => { logger.LogInformation("Successfully got list of Contacts"); },
                      none => { logger.LogInformation("List of Contacts was found empty"); },
                      notFound => { logger.LogError("Person was not found"); });

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
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        OneOf<Success<IReadOnlyCollection<Person>>, None, NotFound> result = await personService.GetFavouritesAsync(id);

        result.Switch(success => { logger.LogInformation("Successfully got list of Favourites"); },
                      none => { logger.LogInformation("List of Favourites was found empty"); },
                      notFound => { logger.LogError("Person was not found"); });

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
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        OneOf<Success<IReadOnlyCollection<Horse>>, None, NotFound> result = await personService.GetOwnedHorsesAsync(id);

        result.Switch(success => { logger.LogInformation("Successfully got list of Horses"); },
                      none => { logger.LogInformation("List of Horses was found empty"); },
                      notFound => { logger.LogError("Person was not found"); });

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
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        OneOf<Success<List<MeasurementDevice>>, None, NotFound> result = await personService.GetAllDevicesAsync(id);

        result.Switch(success => { logger.LogInformation("Successfully got list of Devices"); },
                      none => { logger.LogInformation("List of Devices was found empty"); },
                      notFound => { logger.LogError("Person was not found"); });

        return result.Match<ActionResult<DataTransfer.DeviceListResponse>>(success =>
                                                                               Ok(DataTransfer.DeviceListResponse
                                                                                   .FromDevices(success.Value)),
                                                                           none => Ok(DataTransfer.DeviceListResponse
                                                                               .FromDevices([])),
                                                                           notFound => NotFound());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult> CreatePerson([FromBody] DataTransfer.AddPersonRequest request)
    {
        await transaction.BeginTransactionAsync();
        var addressEntity = new Address
        {
            Street = request.Address.Street,
            HouseNumber = request.Address.HouseNumber,
            CityId = request.Address.CityId
        };

        OneOf<Success<Person>, IBaseService.InvalidData, IBaseService.Conflict> result
            = await personService.AddPersonAsync(request.FirstName, request.LastName, request.Height,
                                                 request.Weight, request.DateOfBirth, request.Email,
                                                 request.WebsiteLink, request.Description, addressEntity,
                                                 request.Role);

        result.Switch(async success =>
        {
            logger.LogInformation("Successfully added person");
            await transaction.CommitAsync();
        }, async invalidData =>
        {
            logger.LogError("data in incorrect format.");
            await transaction.RollbackAsync();
        }, async conflict =>
        {
            logger.LogError("email already exists.");
            await transaction.RollbackAsync();
        });

        return result.Match<ActionResult>(success => Created(),
                                          invalid => BadRequest(),
                                          conflict => Conflict());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult> UpdatePerson([FromRoute] int id,
                                                      [FromBody] DataTransfer.UpdatePersonRequest request)
    {
        await transaction.BeginTransactionAsync();
        if (id != request.Id)
        {
            logger.LogError("id doesnt match request id");

            return BadRequest();
        }

        OneOf<Success<Person>, NotFound, IBaseService.InvalidData, IBaseService.Conflict> result
            = await personService.UpdatePersonAsync(request.Id, request.FirstName, request.LastName, request.Height,
                                                    request.Weight, request.DateOfBirth, request.Email,
                                                    request.WebsiteLink, request.Description, request.Address, request.RoleAssignments);

        result.Switch(async success =>
        {
            logger.LogInformation("Successfully updated person");
            await transaction.CommitAsync();
        }, async notFound =>
        {
            logger.LogError("person not found.");
            await transaction.RollbackAsync();
        }, async invalidData =>
        {
            logger.LogError("data in invalid format.");
            await transaction.RollbackAsync();
        }, async conflict =>
        {
            logger.LogError("email already exists.");
            await transaction.RollbackAsync();
        });

        return result.Match<ActionResult>(success => NoContent(),
                                          notFound => NotFound(),
                                          invalid => BadRequest(),
                                          conflict => Conflict());
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeletePerson([FromRoute] int id)
    {
        await transaction.BeginTransactionAsync();
        if (id <= 0)
        {
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        OneOf<Success, NotFound> result = await personService.DeletePersonAsync(id);

        result.Switch(async success =>
        {
            logger.LogInformation("Successfully deleted person");
            await transaction.CommitAsync();
        }, async notFound =>
        {
            logger.LogError("person not found.");
            await transaction.RollbackAsync();
        });

        return result.Match<IActionResult>(success => NoContent(),
                                           notFound => NotFound());
    }
}
