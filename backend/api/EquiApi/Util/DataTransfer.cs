using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using FluentValidation;
using NodaTime;

namespace EquiApi.Util;

public class DataTransfer
{
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
                RuleFor(x => x.WebsiteLink).Empty().When(x => x.Role.Name == "Equestrian");
                RuleFor(x => x.Description).Empty().When(x => x.Role.Name == "Equestrian");
            }
        }
    }

    public sealed record NameDataDto(string FirstName, string LastName)
    {
        public static NameDataDto FromData(NameData data) => new(data.FirstName, data.LastName);
    }

    public sealed record AddressDto(string? Street, int? HouseNumber, string CityName, string PLZ)
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
            new(address.Street, address.HouseNumber, address.City.Name, address.City.PLZ);
    }

    public sealed record PersonDto(int Id, string FirstName, string LastName, string? Email)
    {
        public static PersonDto FromPerson(Person person) =>
            new(person.Id, person.FirstName, person.LastName, person.Email);
    }

    public sealed record PersonListResponse(IEnumerable<PersonDto> Persons)
    {
        public static PersonListResponse FromPersons(IEnumerable<Person> persons) =>
            new(persons.Select(PersonDto.FromPerson));
    }

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

    public sealed record HorseListResponse(IEnumerable<HorseDto> Horses)
    {
        public static HorseListResponse FromHorses(IEnumerable<Horse> horses) => new(horses.Select(HorseDto.FromHorse));
    }

    public sealed record MeasurementDeviceDto(int Id, int CategoryId, Person Owner, List<DeviceUser> DeviceUser)
    {
        public static MeasurementDeviceDto FromDevice(MeasurementDevice device) =>
            new(device.Id, device.CategoryId, device.Owner, device.Users);
    }

    public sealed record DeviceListResponse(IEnumerable<MeasurementDeviceDto> Devices)
    {
        public static DeviceListResponse FromDevices(IEnumerable<MeasurementDevice> devices) =>
            new(devices.Select(MeasurementDeviceDto.FromDevice));
    }

    public sealed record UpdatePersonRequest(
        int Id,
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
                RuleFor(x => x.Id).NotEmpty();
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

    public sealed record EquestrianBasicDto(
        int Id,
        string FirstName,
        string LastName,
        decimal Height,
        decimal Weight,
        string? Email,
        string? Street,
        int? HouseNumber,
        string? City,
        string? PLZ)
    {
        public static EquestrianBasicDto FromEquestrianBasicData(EquestrianBasicData data, int id) =>
            new(id, data.FirstName, data.LastName, data.Height, data.Weight, data.Email, data.Street, data.HouseNumber,
                data.City, data.PLZ);
    }

    public sealed record SaddlerBasicDto(
        int Id,
        string FirstName,
        string LastName,
        string? Street,
        int? HouseNumber,
        string City,
        string PLZ,
        string? Link,
        string? Description,
        bool IsFavourite)
    {
        public static SaddlerBasicDto FromSaddlerBasicData(SaddlerBasicData data) =>
            new(data.Id, data.FirstName, data.LastName, data.Street, data.HouseNumber, data.City, data.PLZ, data.Link,
                data.Description, data.IsFavourite);
    }

    public sealed record SaddlersListResponse(IEnumerable<SaddlerBasicDto> Saddlers)
    {
        public static SaddlersListResponse FromSaddlers(IEnumerable<SaddlerBasicDto> saddlers) => new(saddlers);
    }
}
