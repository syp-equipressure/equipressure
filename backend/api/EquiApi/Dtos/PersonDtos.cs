using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Shared;
using FluentValidation;

namespace EquiApi.Dtos;

/// <summary>
///     Basic data of a person, used in lists
/// </summary>
/// <param name="Id">The id of the person</param>
/// <param name="FirstName">The first name of the person</param>
/// <param name="LastName">The last name of the person</param>
/// <param name="Email">The optional email of the person</param>
public sealed record PersonDto(int Id, string FirstName, string LastName, string? Email)
{
    public static PersonDto FromPerson(Person person) =>
        new(person.Id, person.FirstName, person.LastName, person.Email);
}

/// <summary>
///     A list of persons
/// </summary>
/// <param name="Persons">The persons</param>
public sealed record PersonListResponse(IReadOnlyList<PersonDto> Persons)
{
    public static PersonListResponse FromPersons(IEnumerable<Person> persons) =>
        new(persons.Select(PersonDto.FromPerson).ToList());
}

/// <summary>
///     All data of a person
/// </summary>
/// <param name="Id">The id of the person</param>
/// <param name="FirstName">The first name of the person</param>
/// <param name="LastName">The last name of the person</param>
/// <param name="DateOfBirth">The birthdate of the person</param>
/// <param name="Height">The height of the person in cm</param>
/// <param name="Weight">The weight of the person in kg</param>
/// <param name="Email">The optional email of the person</param>
/// <param name="WebsiteLink">The optional website of a saddler</param>
/// <param name="Description">The optional description of a saddler</param>
/// <param name="Address">The address of the person</param>
/// <param name="Roles">The roles of the person</param>
public sealed record PersonDetailDto(
    int Id,
    string FirstName,
    string LastName,
    LocalDate DateOfBirth,
    decimal Height,
    decimal Weight,
    string? Email,
    string? WebsiteLink,
    string? Description,
    AddressDto Address,
    IReadOnlyList<RoleName> Roles,
    Instant CreatedAt)
{
    public static PersonDetailDto FromPerson(Person person) =>
        new(person.Id,
            person.FirstName,
            person.LastName,
            person.DateOfBirth,
            person.Height,
            person.Weight,
            person.Email,
            person.WebsiteLink,
            person.Description,
            AddressDto.FromAddress(person.Address),
            person.Roles.Select(assignment => assignment.Role.Name).ToList(),
            person.CreatedAt);
}


/// <summary>
///     A detail list of persons
/// </summary>
/// <param name="Persons">The persons</param>
public sealed record PersonDetailListResponse(IReadOnlyList<PersonDetailDto> Persons)
{
    public static PersonDetailListResponse FromPersons(IEnumerable<Person> persons) =>
        new(persons.Select(PersonDetailDto.FromPerson).ToList());
}

/// <summary>
///     The name of a person
/// </summary>
/// <param name="FirstName">The first name of the person</param>
/// <param name="LastName">The last name of the person</param>
public sealed record PersonNameDto(string FirstName, string LastName)
{
    public static PersonNameDto FromNameData(NameData name) => new(name.FirstName, name.LastName);
}

/// <summary>
///     A saddler with their address
/// </summary>
/// <param name="Id">The id of the saddler</param>
/// <param name="FirstName">The first name of the saddler</param>
/// <param name="LastName">The last name of the saddler</param>
/// <param name="AddressName">Optional street and house number</param>
/// <param name="CityName">The name of the city</param>
/// <param name="PLZ">The postal code</param>
/// <param name="WebsiteLink">The optional website of the saddler</param>
/// <param name="Description">The optional description of the saddler</param>
public sealed record SaddlerDto(
    int Id,
    string FirstName,
    string LastName,
    string? AddressName,
    string CityName,
    string PLZ,
    string? WebsiteLink,
    string? Description)
{
    public static SaddlerDto FromSaddlerData(SaddlerData saddler) =>
        new(saddler.Id, saddler.FirstName, saddler.LastName, saddler.AddressName, saddler.CityName, saddler.PLZ,
            saddler.WebsiteLink, saddler.Description);
}

/// <summary>
///     A list of saddlers
/// </summary>
/// <param name="Saddlers">The saddlers</param>
public sealed record SaddlerListResponse(IReadOnlyList<SaddlerDto> Saddlers)
{
    public static SaddlerListResponse FromSaddlers(IEnumerable<SaddlerData> saddlers) =>
        new(saddlers.Select(SaddlerDto.FromSaddlerData).ToList());
}

/// <summary>
///     A contact of a person: a saddler for an equestrian, an equestrian for a saddler
/// </summary>
/// <param name="Id">The id of the contact</param>
/// <param name="FirstName">The first name of the contact</param>
/// <param name="LastName">The last name of the contact</param>
/// <param name="AddressName">Optional street and house number</param>
/// <param name="CityName">The name of the city</param>
/// <param name="PLZ">The postal code</param>
/// <param name="WebsiteLink">The optional website, only set for saddlers</param>
/// <param name="Description">The optional description, only set for saddlers</param>
public sealed record ContactDto(
    int Id,
    string FirstName,
    string LastName,
    string? AddressName,
    string CityName,
    string PLZ,
    string? WebsiteLink,
    string? Description)
{
    public static ContactDto FromContactData(ContactData contact) =>
        new(contact.Id, contact.FirstName, contact.LastName, contact.AddressName, contact.CityName, contact.PLZ,
            contact.WebsiteLink, contact.Description);
}

/// <summary>
///     A list of contacts
/// </summary>
/// <param name="Contacts">The contacts</param>
public sealed record ContactListResponse(IReadOnlyList<ContactDto> Contacts)
{
    public static ContactListResponse FromContacts(IEnumerable<ContactData> contacts) =>
        new(contacts.Select(ContactDto.FromContactData).ToList());
}

/// <summary>
///     Request to add a person
/// </summary>
/// <param name="FirstName">The first name of the person</param>
/// <param name="LastName">The last name of the person</param>
/// <param name="Height">The height of the person in cm</param>
/// <param name="Weight">The weight of the person in kg</param>
/// <param name="DateOfBirth">The birthdate of the person</param>
/// <param name="Email">The optional email of the person</param>
/// <param name="WebsiteLink">The optional website, only allowed for saddlers</param>
/// <param name="Description">The optional description, only allowed for saddlers</param>
/// <param name="Address">The address of the person, an existing address with the same values is reused</param>
/// <param name="Role">The role of the person</param>
public sealed record AddPersonRequest(
    string FirstName,
    string LastName,
    decimal Height,
    decimal Weight,
    LocalDate DateOfBirth,
    string? Email,
    string? WebsiteLink,
    string? Description,
    AddressRequest Address,
    RoleName Role)
{
    public NewPersonData ToData() =>
        new(FirstName, LastName, Height, Weight, DateOfBirth, Email, WebsiteLink, Description, Address.ToData(), Role);

    public sealed class Validator : AbstractValidator<AddPersonRequest>
    {
        public Validator(IClock clock)
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Height).GreaterThan(0);
            RuleFor(x => x.Weight).GreaterThan(0);
            RuleFor(x => x.DateOfBirth).LessThan(clock.LocalDate);
            RuleFor(x => x.Email).Matches(ValidationPatterns.Email)
                                 .When(x => x.Email is not null)
                                 .WithMessage(ValidationPatterns.EmailMessage);
            RuleFor(x => x.Address).NotNull().SetValidator(new AddressRequest.Validator());
            RuleFor(x => x.Address.AddressName).NotEmpty()
                                               .When(x => x.Role == RoleName.Saddler && x.Address is not null)
                                               .WithMessage(ValidationPatterns.StreetRequiredForSaddlerMessage);
            RuleFor(x => x.Role).IsInEnum();
            RuleFor(x => x.WebsiteLink).Empty().When(x => x.Role == RoleName.Equestrian)
                                       .WithMessage("Only saddlers can have a website link");
            RuleFor(x => x.Description).Empty().When(x => x.Role == RoleName.Equestrian)
                                       .WithMessage("Only saddlers can have a description");
        }
    }
}

/// <summary>
///     Request to update a person, all values are optional and null values are left unchanged
/// </summary>
/// <param name="FirstName">The new first name</param>
/// <param name="LastName">The new last name</param>
/// <param name="Height">The new height in cm</param>
/// <param name="Weight">The new weight in kg</param>
/// <param name="DateOfBirth">The new birthdate</param>
/// <param name="Email">The new email</param>
/// <param name="WebsiteLink">The new website link</param>
/// <param name="Description">The new description</param>
/// <param name="Address">The new address, an existing address with the same values is reused</param>
/// <param name="Roles">The new roles, replace all existing roles</param>
public sealed record UpdatePersonRequest(
    string? FirstName,
    string? LastName,
    decimal? Height,
    decimal? Weight,
    LocalDate? DateOfBirth,
    string? Email,
    string? WebsiteLink,
    string? Description,
    AddressRequest? Address,
    IReadOnlyList<RoleName>? Roles)
{
    public PersonChanges ToChanges() =>
        new(FirstName, LastName, Height, Weight, DateOfBirth, Email, WebsiteLink, Description, Address?.ToData(),
            Roles);

    public sealed class Validator : AbstractValidator<UpdatePersonRequest>
    {
        public Validator(IClock clock)
        {
            RuleFor(x => x.FirstName).NotEmpty().When(x => x.FirstName is not null);
            RuleFor(x => x.LastName).NotEmpty().When(x => x.LastName is not null);
            RuleFor(x => x.Height).GreaterThan(0).When(x => x.Height is not null);
            RuleFor(x => x.Weight).GreaterThan(0).When(x => x.Weight is not null);
            RuleFor(x => x.DateOfBirth).LessThan(clock.LocalDate).When(x => x.DateOfBirth is not null);
            RuleFor(x => x.Email).Matches(ValidationPatterns.Email)
                                 .When(x => x.Email is not null)
                                 .WithMessage(ValidationPatterns.EmailMessage);
            RuleFor(x => x.Address!).SetValidator(new AddressRequest.Validator()).When(x => x.Address is not null);
            RuleForEach(x => x.Roles).IsInEnum();
        }
    }
}

internal static class ValidationPatterns
{
    public const string Email = @"^[^@]+@[^@]+\.[^@]+$";
    public const string EmailMessage = "Email must contain '@' and a '.' after it";

    public const string StreetRequiredForSaddlerMessage =
        "Saddlers have to provide their street, so that customers can find their workshop";
}