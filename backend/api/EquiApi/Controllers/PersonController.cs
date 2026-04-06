using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;
using EquiApi.Util;
using FluentValidation;
using Library.Core;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
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
    [ProducesResponseType<EquestrianBasicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<EquestrianBasicDto>> GetEquestrianById([FromRoute] int id)
    {
        if (id <= 0)
        {
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        OneOf<Success<EquestrianBasicData>, NotFound> result = await personService.GetPersonAsEquestrianByIdAsync(id);

        result.Switch(success => { logger.LogInformation("Successfully got Equestrian"); },
                      notFound => { logger.LogError("Equestrian was not found"); });

        return result.Match<ActionResult<EquestrianBasicDto>>(success =>
                                                                  Ok(EquestrianBasicDto
                                                                         .FromEquestrianBasicData(success.Value, id)),
                                                              notFound => NotFound());
    }

    [HttpGet("{id:int}/profile-data")]
    [ProducesResponseType<NameDataDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<NameDataDto>> GetProfileData([FromRoute] int id)
    {
        if (id <= 0)
        {
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        OneOf<Success<NameData>, NotFound> result = await personService.GetNameByIdAsync(id);

        result.Switch(success => { logger.LogInformation("Successfully got Person"); },
                      notFound => { logger.LogError("Person was not found"); });
        
        return result.Match<ActionResult<NameDataDto>>(success => Ok(NameDataDto.FromData(success.Value)),
                                                       notFound => NotFound());
    }

    [HttpGet("{id:int}/address")]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<AddressDto>> GetAddress([FromRoute] int id)
    {
        if (id <= 0)
        {
            logger.LogError("Bad request, Id is invalid");

            return BadRequest();
        }

        OneOf<Success<Address>, NotFound> result = await personService.GetPersonAddressAsync(id);

        result.Switch(success => { logger.LogInformation("Successfully got Address"); },
                      notFound => { logger.LogError("Address was not found"); });
        
        return result.Match<ActionResult<AddressDto>>(success => Ok(AddressDto.FromAddress(success.Value)),
                                                      notFound => NotFound());
    }
}

public sealed class AddPersonRequest()
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public LocalDate DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? WebsiteLink { get; set; }
    public string? Description { get; set; }
    public Address Address { get; set; } = null!;
    public AccountRole Role { get; set; } = null!;

    public sealed class Validator : AbstractValidator<AddPersonRequest>
    {
        public Validator()
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Height).GreaterThan(0);
            RuleFor(x => x.Weight).GreaterThan(0);
            RuleFor(x => x.DateOfBirth).NotNull().LessThan(LocalDate.FromDateTime(DateTime.Today));
            RuleFor(x => x.Email).Matches(@"^[^@]+@[^@]+\.[^@]+$").When(x => !string.IsNullOrEmpty(x.Email))
                                 .WithMessage("Email must contain '@' and a '.' after it");
            RuleFor(x => x.WebsiteLink).Empty().When(x => x.Role.Name == "Equestrian");
            RuleFor(x => x.Description).Empty().When(x => x.Role.Name == "Equestrian");
        }
    }
}

public sealed class NameDataDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public static NameDataDto FromData(NameData data) => new() { FirstName = data.FirstName, LastName = data.LastName };
}

public sealed class AddressDto
{
    public string? Street { get; set; }
    public int? HouseNumber { get; set; }
    public required string CityName { get; set; }
    public required string PLZ { get; set; }

    public sealed class Validator : AbstractValidator<AddressDto>
    {
        public Validator()
        {
            RuleFor(x => x.CityName).NotEmpty();
            RuleFor(x => x.PLZ).NotEmpty();
        }
    }

    public static AddressDto FromAddress(Address address) =>
        new()
        {
            Street = address.Street, HouseNumber = address.HouseNumber, CityName = address.City.Name,
            PLZ = address.City.PLZ
        };
}

public sealed class EquestrianBasicDto
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public string? Email { get; set; }
    public string? Street { get; set; }
    public int? HouseNumber { get; set; }
    public string? City { get; set; }
    public string? PLZ { get; set; }

    public static EquestrianBasicDto FromEquestrianBasicData(EquestrianBasicData data, int id) =>
        new()
        {
            Id = id,
            FirstName = data.FirstName,
            LastName = data.LastName,
            Height = data.Height,
            Weight = data.Weight,
            Email = data.Email,
            Street = data.Street,
            HouseNumber = data.HouseNumber,
            City = data.City,
            PLZ = data.PLZ
        };
}

public sealed class SaddlerBasicDto
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Street { get; set; }
    public int? HouseNumber { get; set; }
    public required string City { get; set; }
    public required string PLZ { get; set; }
    public string? Link { get; set; }
    public string? Description { get; set; }
    public bool IsFavourite { get; set; }

    public static SaddlerBasicDto FromSaddlerBasicData(SaddlerBasicData data, int id) =>
        new()
        {
            Id = id,
            FirstName = data.FirstName,
            LastName = data.LastName,
            Street = data.Street,
            HouseNumber = data.HouseNumber,
            City = data.City,
            PLZ = data.PLZ,
            Link = data.Link,
            Description = data.Description,
            IsFavourite = data.isFavourite
        };
}
