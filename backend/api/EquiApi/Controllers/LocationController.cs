using System.ComponentModel.DataAnnotations;
using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/locations")]
public sealed class LocationController(ILocationService locationService) : BaseController
{
    /// <summary>
    ///     Gets the cities that are used by addresses, most used cities first
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<IReadOnlyList<CityDto>>> GetCities(
        [FromQuery][Range(1, int.MaxValue)] int? length,
        [FromQuery] string? nameFilter)
    {
        var cities = await locationService.GetCitiesAsync(length, nameFilter);

        return Ok(cities.Select(CityDto.FromCity).ToList());
    }

    /// <summary>
    ///     Gets an address
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AddressDto>> GetAddressById([FromRoute][ValidId] int id)
    {
        var result = await locationService.GetAddressByIdAsync(id);

        return result.Match<ActionResult<AddressDto>>(
            success => Ok(AddressDto.FromAddress(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Adds an address
    /// </summary>
    [HttpPost]
    [ProducesResponseType<AddressDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<AddressDto>> AddAddress([FromBody] AddressRequest request)
    {
        if (!IsValid<AddressRequest.Validator, AddressRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await locationService.AddAddressAsync(request.ToData());

        return result.Match<ActionResult<AddressDto>>(
            success => CreatedAtAction(nameof(GetAddressById), new { id = success.Value.Id },
                                       AddressDto.FromAddress(success.Value)),
            conflict => Problem(statusCode: StatusCodes.Status409Conflict,
                                detail: $"The address already exists with id {conflict.ExistingAddressId}"));
    }
}