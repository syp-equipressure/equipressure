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
    /// <summary>
    /// Creates a new address or returns an existing one if it already exists.
    /// </summary>
    /// <param name="request">The data transfer object containing the address details.</param>
    /// <returns>The created or existing address as a DTO.</returns>
    [HttpPost]
    [ProducesResponseType<DataTransfer.AddressDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> CreateLocation([FromBody] DataTransfer.AddLocationRequest request)
    {
        if (!ValidateRequest<DataTransfer.AddLocationRequest.Validator, DataTransfer.AddLocationRequest>(request))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success<Address>, IBaseService.Conflict> result
                = await locationService.AddAddressAsync(request.Street, request.HouseNumber, request.CityName,
                                                        request.PLZ);

            return await result.Match<ValueTask<ActionResult>>(async success =>
                                                               {
                                                                   await transaction.CommitAsync();

                                                                   var dto
                                                                       = DataTransfer.AddressDto
                                                                           .FromAddress(success.Value);

                                                                   return CreatedAtAction(nameof(GetCities),
                                                                    new { nameFilter = success.Value.City.Name },
                                                                    dto);
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
            logger.LogError(ex, "Error adding Location");
            return Problem();
        }
    }

    /// <summary>
    /// Retrieves a list of cities, optionally filtered by name or limited by a specific count.
    /// </summary>
    /// <param name="length">The maximum number of cities to return.</param>
    /// <param name="nameFilter">An optional filter for the city name.</param>
    /// <returns>A collection of city DTOs, which may be empty if no matches are found.</returns>
    [HttpGet("cities")]
    [ProducesResponseType<IEnumerable<DataTransfer.CityDto>>(StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> GetCities([FromQuery] int? length, [FromQuery] string? nameFilter)
    {
        OneOf<Success<IReadOnlyCollection<City>>, None> result = await locationService.GetCityAsync(length, nameFilter);

        return result.Match<IActionResult>(success =>
                                           {
                                               IEnumerable<DataTransfer.CityDto> dtos
                                                   = success.Value.Select(DataTransfer.CityDto.FromCity);

                                               return Ok(dtos);
                                           },
                                           none => Ok(Enumerable.Empty<DataTransfer.CityDto>()));
    }
}
