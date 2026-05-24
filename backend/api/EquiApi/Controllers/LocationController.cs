using EquiApi.Core.Services;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/locations")]
public sealed class LocationController(
    ILocationService locationService,
    ILogger<LocationController> logger) : BaseController
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<DataTransfer.AddressDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<IReadOnlyCollection<DataTransfer.AddressDto>>> GetCities(
        [FromQuery] int? length,
        [FromQuery] string? nameFilter)
    {
        var result = await locationService.GetCitiesAsync(length, nameFilter);

        return result.Match<ActionResult<IReadOnlyCollection<DataTransfer.AddressDto>>>(
            success => Ok(success.Value.Select(DataTransfer.AddressDto.FromAddress).ToList()),
            error =>
            {
                logger.LogWarning("No cities found (nameFilter: {nameFilter}, length: {length})", nameFilter, length);
                return NotFound();
            });
    }

    [HttpPost]
    [ProducesResponseType<DataTransfer.AddressDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> AddAddress([FromBody] DataTransfer.AddressDto request)
    {
        if (!ValidateRequest<DataTransfer.AddressDto.Validator, DataTransfer.AddressDto>(request))
        {
            return BadRequest();
        }

        var result = await locationService.AddAddressAsync(request.Address, request.PLZ, request.CityName);

        return result.Match<IActionResult>(
            success =>
            {
                logger.LogInformation("Address successfully added: {cityName}, {plz}", request.CityName, request.PLZ);
                return Created();
            },
            invalidData =>
            {
                logger.LogWarning("AddAddress failed — invalid data for {cityName}, {plz}", request.CityName, request.PLZ);
                return BadRequest();
            },
            error =>
            {
                logger.LogError("AddAddress failed unexpectedly for {cityName}, {plz}", request.CityName, request.PLZ);
                return Problem();
            });
    }
}