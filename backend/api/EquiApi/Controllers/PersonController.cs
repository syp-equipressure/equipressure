using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;
using EquiApi.Util;
using FluentValidation;
using Library.Core;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace EquiApi.Controllers;

[Route("api/persons")]
public sealed class PersonController(
    /*ITransactionProvider transaction,*/
    IPersonService personService /*,
    ILogger<PersonController> logger*/) : BaseController
{
    [HttpGet]
    [Route("{id:int}/isSaddler")]
    [ProducesResponseType<SaddlerBasicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<SaddlerBasicDto>> GetSaddlerById([FromRoute] int id)
    {
        var result = await personService.GetPersonAsSaddlerByIdAsync(id);

        return result.Match<ActionResult<SaddlerBasicDto>>(success =>
                                                               Ok(SaddlerBasicDto.FromSaddlerBasicData(success.Value,
                                                                   id)),
                                                           notFound => NotFound());
    }

    [HttpGet]
    [Route("{id:int}/address")]
    [ProducesResponseType<Address>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<Address>> GetAddressOfPersonById([FromRoute] int id)
    {
        
    }

    [HttpGet]
    [Route("{id:int}/isEquestrian")]
    [ProducesResponseType<EquestrianBasicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<EquestrianBasicDto>> GetEquestrianById([FromRoute] int id)
    {
        var result = await personService.GetPersonAsEquestrianByIdAsync(id);

        return
            result.Match<ActionResult<EquestrianBasicDto>>(success =>
                                                               Ok(EquestrianBasicDto
                                                                      .FromEquestrianBasicData(success.Value, id)),
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
            RuleFor(x => x.DateOfBirth)
                .NotNull()
                .LessThan(LocalDate.FromDateTime(DateTime.Today));
            RuleFor(x => x.Email)
                .Matches(@"^[^@]+@[^@]+\.[^@]+$")
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Email must contain '@' and a '.' after it");
            RuleFor(x => x.WebsiteLink).Empty().When(x => x.Role.Name == "Equestrian");
            RuleFor(x => x.Description).Empty().When(x => x.Role.Name == "Equestrian");
        }
    }
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
