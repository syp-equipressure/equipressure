using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/persons")]
public sealed class PersonController(
    IPersonService personService,
    IHorseService horseService,
    IDeviceService deviceService) : BaseController
{
    /// <summary>
    ///     Gets all data of a person
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PersonDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PersonDetailDto>> GetPersonById([FromRoute][ValidId] int id)
    {
        var result = await personService.GetPersonByIdAsync(id);

        return result.Match<ActionResult<PersonDetailDto>>(
            success => Ok(PersonDetailDto.FromPerson(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the name of a person
    /// </summary>
    [HttpGet("{id:int}/profile-data")]
    [ProducesResponseType<PersonNameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PersonNameDto>> GetProfileData([FromRoute][ValidId] int id)
    {
        var result = await personService.GetNameByIdAsync(id);

        return result.Match<ActionResult<PersonNameDto>>(
            success => Ok(PersonNameDto.FromNameData(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the address of a person
    /// </summary>
    [HttpGet("{id:int}/location")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AddressDto>> GetAddress([FromRoute][ValidId] int id)
    {
        var result = await personService.GetPersonAddressAsync(id);

        return result.Match<ActionResult<AddressDto>>(
            success => Ok(AddressDto.FromAddress(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Sets the address of a person, an existing address with the same values is reused
    /// </summary>
    [HttpPut("{id:int}/location")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AddressDto>> UpdateAddress([FromRoute][ValidId] int id,
                                                                   [FromBody] AddressRequest request)
    {
        if (!IsValid<AddressRequest.Validator, AddressRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await personService.UpdatePersonAddressAsync(id, request.ToData());

        return result.Match<ActionResult<AddressDto>>(
            success => Ok(AddressDto.FromAddress(success.Value)),
            _ => NotFound(),
            _ => StreetRequiredForSaddler(nameof(AddressRequest.AddressName)));
    }

    /// <summary>
    ///     Gets the contacts of a person: the saddlers of an equestrian or the equestrians of a saddler
    /// </summary>
    [HttpGet("{id:int}/contacts")]
    [ProducesResponseType<ContactListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<ContactListResponse>> GetContacts([FromRoute][ValidId] int id)
    {
        var result = await personService.GetContactsAsync(id);

        return result.Match<ActionResult<ContactListResponse>>(
            success => Ok(ContactListResponse.FromContacts(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the saddlers a person marked as favourite
    /// </summary>
    [HttpGet("{id:int}/favourites")]
    [ProducesResponseType<SaddlerListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<SaddlerListResponse>> GetFavourites([FromRoute][ValidId] int id)
    {
        var result = await personService.GetFavouriteSaddlersAsync(id);

        return result.Match<ActionResult<SaddlerListResponse>>(
            success => Ok(SaddlerListResponse.FromSaddlers(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the horses a person owns
    /// </summary>
    [HttpGet("{id:int}/horses")]
    [ProducesResponseType<HorseListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<HorseListResponse>> GetHorses([FromRoute][ValidId] int id)
    {
        var result = await horseService.GetOwnedHorsesOfPersonAsync(id);

        return result.Match<ActionResult<HorseListResponse>>(
            success => Ok(HorseListResponse.FromHorses(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the devices a person owns or uses
    /// </summary>
    [HttpGet("{id:int}/devices")]
    [ProducesResponseType<DeviceListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<DeviceListResponse>> GetDevices([FromRoute][ValidId] int id)
    {
        var result = await deviceService.GetDevicesOfPersonAsync(id);

        return result.Match<ActionResult<DeviceListResponse>>(
            success => Ok(DeviceListResponse.FromDevices(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets all saddlers with their address
    /// </summary>
    [HttpGet("saddlers")]
    [ProducesResponseType<SaddlerListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<SaddlerListResponse>> GetSaddlers()
    {
        var saddlers = await personService.GetAllSaddlersAsync();

        return Ok(SaddlerListResponse.FromSaddlers(saddlers));
    }

    /// <summary>
    ///     Adds a person
    /// </summary>
    [HttpPost]
    [ProducesResponseType<PersonDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<PersonDetailDto>> CreatePerson([FromBody] AddPersonRequest request)
    {
        if (!IsValid<AddPersonRequest.Validator, AddPersonRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await personService.AddPersonAsync(request.ToData());

        return result.Match<ActionResult<PersonDetailDto>>(
            success => CreatedAtAction(nameof(GetPersonById), new { id = success.Value.Id },
                                       PersonDetailDto.FromPerson(success.Value)),
            _ => Conflict());
    }

    /// <summary>
    ///     Updates a person, values that are not set are left unchanged
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> UpdatePerson([FromRoute][ValidId] int id,
                                                       [FromBody] UpdatePersonRequest request)
    {
        if (!IsValid<UpdatePersonRequest.Validator, UpdatePersonRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await personService.UpdatePersonAsync(id, request.ToChanges());

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            _ => Conflict(),
            _ => StreetRequiredForSaddler($"{nameof(UpdatePersonRequest.Address)}.{nameof(AddressRequest.AddressName)}"));
    }

    /// <summary>
    ///     Removes a person
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeletePerson([FromRoute][ValidId] int id)
    {
        var result = await personService.DeletePersonAsync(id);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound());
    }

    /// <summary>
    ///     Marks a saddler as favourite of an equestrian
    /// </summary>
    [HttpPut("{id:int}/favourites/{saddlerId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> AddFavourite([FromRoute][ValidId] int id,
                                                       [FromRoute][ValidId] int saddlerId)
    {
        var result = await personService.AddFavouriteAsync(id, saddlerId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            invalid => Problem(statusCode: StatusCodes.Status409Conflict, detail: invalid.Reason));
    }

    /// <summary>
    ///     Removes a saddler from the favourites of an equestrian
    /// </summary>
    [HttpDelete("{id:int}/favourites/{saddlerId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> RemoveFavourite([FromRoute][ValidId] int id,
                                                          [FromRoute][ValidId] int saddlerId)
    {
        var result = await personService.RemoveFavouriteAsync(id, saddlerId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound());
    }

    /// <summary>
    ///     Adds a contact between an equestrian and a saddler, can be called by both of them
    /// </summary>
    [HttpPut("{id:int}/contacts/{contactId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> AddContact([FromRoute][ValidId] int id,
                                                     [FromRoute][ValidId] int contactId)
    {
        var result = await personService.AddContactAsync(id, contactId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            invalid => Problem(statusCode: StatusCodes.Status409Conflict, detail: invalid.Reason));
    }

    /// <summary>
    ///     Removes a contact between an equestrian and a saddler, can be called by both of them
    /// </summary>
    [HttpDelete("{id:int}/contacts/{contactId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> RemoveContact([FromRoute][ValidId] int id,
                                                        [FromRoute][ValidId] int contactId)
    {
        var result = await personService.RemoveContactAsync(id, contactId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound());
    }

    private ActionResult StreetRequiredForSaddler(string propertyName)
    {
        ModelState.AddModelError(propertyName, ValidationPatterns.StreetRequiredForSaddlerMessage);

        return ValidationProblem();
    }
}