using EquiApi.Core.Util;
using EquiApi.Persistence.Model;
using EquiApi.Shared;
using FluentValidation;
using NodaTime;


namespace EquiApi.Util;

public class DataTransfer
{
    /// <summary>
    /// DTO for adding a person
    /// </summary>
    /// <param name="FirstName">First name of the person.</param>
    /// <param name="LastName">Last name of the person</param>
    /// <param name="Height">Height of the person</param>
    /// <param name="Weight">Weight of the person</param>
    /// <param name="DateOfBirth">Birthdate of the person</param>
    /// <param name="Email">optional email of the person</param>
    /// <param name="WebsiteLink">optional websitelink but only allowed for saddlers</param>
    /// <param name="Description">optional description but only for saddlers.</param>
    /// <param name="Address">address entity of the person</param>
    /// <param name="Role">role of the person</param>
    public sealed record AddPersonRequest(
        string FirstName,
        string LastName,
        decimal Height,
        decimal Weight,
        LocalDate DateOfBirth,
        string? Email,
        string? WebsiteLink,
        string? Description,
        Address Address,
        AccountRole Role)
    {
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
                RuleFor(x => x.WebsiteLink).Empty().When(x => x.Role.Name == RoleName.Equestrian);
                RuleFor(x => x.Description).Empty().When(x => x.Role.Name == RoleName.Equestrian);
            }
        }
    }

    /// <summary>
    /// DTO that returns minimal name data
    /// </summary>
    /// <param name="FirstName">First name of the person.</param>
    /// <param name="LastName">Last name of the person</param>
    public sealed record NameDataDto(string FirstName, string LastName)
    {
        public static NameDataDto FromData(Helper.NameData data) => new(data.FirstName, data.LastName);
    }
    
    /// <summary>
    /// DTO that returns address of person
    /// </summary>
    /// <param name="Street">optional Street of the person.</param>
    /// <param name="HouseNumber">optional Housenumber of the person</param>
    /// <param name="CityName">CityName of the person</param>
    /// <param name="PLZ">PLZ of the person</param>
    public sealed record AddressDto(string? Street, string CityName, string PLZ)
    {
        public sealed class Validator : AbstractValidator<AddressDto>
        {
            public Validator()
            {
                RuleFor(x => x.CityName).NotEmpty();
                RuleFor(x => x.PLZ).NotEmpty();
            }
        }

        public static AddressDto FromAddress(Address address) =>
            new(address.AddressName, address.CityName, address.PLZ);
    }

    /// <summary>
    /// DTO that returns person
    /// </summary>
    /// <param name="Id">Id of the person.</param>
    /// <param name="FirstName">FirstName of the person</param>
    /// <param name="LastName">Lastname of the person</param>
    /// <param name="Email">optional Email of the person</param>
    public sealed record PersonDto(int Id, string FirstName, string LastName, string? Email)
    {
        public static PersonDto FromPerson(Person person) =>
            new(person.Id, person.FirstName, person.LastName, person.Email);
    }

    /// <summary>
    /// DTO that returns list of personDtos
    /// </summary>
    /// <param name="Persons">List of persons</param>
    public sealed record PersonListResponse(IEnumerable<PersonDto> Persons)
    {
        public static PersonListResponse FromPersons(IEnumerable<Person> persons) =>
            new(persons.Select(PersonDto.FromPerson));
    }
    
    public sealed record AddDeviceRequest(string DeviceId, int OwnerId, int CategoryId)
    {
        public class Validator : AbstractValidator<AddDeviceRequest>
        {
            public Validator()
            {
                RuleFor(x => x.DeviceId).NotEmpty();
                RuleFor(x => x.OwnerId).GreaterThan(0);
                RuleFor(x => x.CategoryId).GreaterThan(0);
            }
        }
    }
    public sealed record AddUserToDeviceRequest(int UserId, string DeviceId)
    {
        public class Validator : AbstractValidator<AddUserToDeviceRequest>
        {
            public Validator()
            {
                RuleFor(x => x.UserId)
                    .GreaterThan(0);
                RuleFor(x => x.DeviceId)
                    .NotEmpty();
            }
        }
    }

    
    /// <summary>
    /// DTO that returns horse
    /// </summary>
    /// <param name="Id">Id of the horse.</param>
    /// <param name="Name">Name of the horse</param>
    /// <param name="DateOfBirth">Birthdate of the horse</param>
    /// <param name="Height">Height of the horse</param>
    /// <param name="Weight">Weight of the horse</param>
    /// <param name="Gender">Gender of the horse.</param>
    /// <param name="BreedNames">List of Breed Names for the horse</param>
    /// <param name="AddressId">Id of the address</param>
    public sealed record HorseDto(
        int Id,
        string Name,
        LocalDate DateOfBirth,
        decimal Weight,
        decimal Height,
        string Gender,
        List<string> BreedNames,
        int AddressId)
    {
        public static HorseDto FromHorse(Horse horse) =>
            new(horse.Id, horse.Name, horse.DateOfBirth, horse.Weight, horse.Height, horse.Gender.ToString(),
                horse.HorseBreeds.Select(hb => hb.Breed.Name).ToList(), horse.AddressId);
    }

    /// <summary>
    /// DTO that returns list of horse dtos
    /// </summary>
    /// <param name="Horses">List of horses</param>
    public sealed record HorseListResponse(IEnumerable<HorseDto> Horses)
    {
        public static HorseListResponse FromHorses(IEnumerable<Horse> horses) => new(horses.Select(HorseDto.FromHorse));
    }

    /// <summary>
    /// DTO that returns device
    /// </summary>
    /// <param name="Id">Id of the device.</param>
    /// <param name="categoryId">categoryId of the device</param>
    /// <param name="owner">owner of the device</param>
    /// <param name="DeviceUser">List of users of the device</param>
    public sealed record MeasurementDeviceDto(string Id, int CategoryId, Person Owner, List<DeviceUser> DeviceUser)
    {
        public static MeasurementDeviceDto FromDevice(MeasurementDevice device) =>
            new(device.Id, device.CategoryId, device.Owner, device.Users);
    }
    
    /// <summary>
    /// DTO that returns list of device dtos
    /// </summary>
    /// <param name="Devices">List of devices</param>
    public sealed record DeviceListResponse(IEnumerable<MeasurementDeviceDto> Devices)
    {
        public static DeviceListResponse FromDevices(IEnumerable<MeasurementDevice> devices) =>
            new(devices.Select(MeasurementDeviceDto.FromDevice));
    }

    
    /// <summary>
    /// DTO for updating a person
    /// </summary>
    /// <param name="FirstName">First name of the person.</param>
    /// <param name="LastName">Last name of the person</param>
    /// <param name="Height">Height of the person</param>
    /// <param name="Weight">Weight of the person</param>
    /// <param name="DateOfBirth">Birthdate of the person</param>
    /// <param name="Email">optional email of the person</param>
    /// <param name="WebsiteLink">optional websitelink but only allowed for saddlers</param>
    /// <param name="Description">optional description but only for saddlers.</param>
    /// <param name="Address">address entity of the person</param>
    /// <param name="RoleAssignments">role assignments of the person</param>
    public sealed record UpdatePersonRequest(
        string? FirstName,
        string? LastName,
        decimal? Height,
        decimal? Weight,
        LocalDate? DateOfBirth,
        string? Email,
        string? WebsiteLink,
        string? Description,
        Address? Address,
        List<PersonRoleAssignment>? RoleAssignments)
    {
        public sealed class Validator : AbstractValidator<UpdatePersonRequest>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName).NotEmpty();
                RuleFor(x => x.LastName).NotEmpty();
                RuleFor(x => x.Height).GreaterThan(0);
                RuleFor(x => x.Weight).GreaterThan(0);
                RuleFor(x => x.DateOfBirth).LessThan(LocalDate.FromDateTime(DateTime.Today));
                RuleFor(x => x.Email).Matches(@"^[^@]+@[^@]+\.[^@]+$").When(x => !string.IsNullOrEmpty(x.Email))
                                     .WithMessage("Email must contain '@' and a '.' after it");
            }
        }
    }

    public sealed record AddLocationRequest(
        string? Street,
        int? HouseNumber,
        string CityName,
        string PLZ)
    {
        public sealed class Validator : AbstractValidator<AddLocationRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Street).NotEmpty();
                RuleFor(x => x.HouseNumber).GreaterThan(0);
                RuleFor(x => x.CityName).NotEmpty();
                RuleFor(x => x.PLZ).NotEmpty();
            }
        }
    }

    public sealed record EquestrianBasicDto(
        int Id,
        string FirstName,
        string LastName,
        decimal Height,
        decimal Weight,
        string? Email,
        string? AddressName,
        string? CityName,
        string? PLZ)
    {
        public static EquestrianBasicDto FromEquestrianBasicData(Helper.EquestrianBasicData data, int id) =>
            new(id, data.FirstName, data.LastName, data.Height, data.Weight, data.Email, data.Street,
                data.City, data.PLZ);
    }

    public sealed record SaddlerBasicDto(
        int Id,
        string FirstName,
        string LastName,
        string? AddressName,
        string CityName,
        string PLZ,
        string? Link,
        string? Description)
    {
        public static SaddlerBasicDto FromSaddlerBasicData(Helper.SaddlerBasicData data) =>
            new(data.Id, data.FirstName, data.LastName, data.Street, data.City, data.PLZ, data.Link, data.Description);
    }

    public sealed record SaddlersListResponse(IEnumerable<SaddlerBasicDto> Saddlers)
    {
        public static SaddlersListResponse FromSaddlers(IEnumerable<SaddlerBasicDto> saddlers) => new(saddlers);
    }
    
    public record NameData(string FirstName, string LastName);
}
