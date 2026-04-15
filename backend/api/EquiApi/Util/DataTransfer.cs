using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using FluentValidation;

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

    public sealed class HorseDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public LocalDate DateOfBirth { get; set; }
        public decimal Weight { get; set; }
        public decimal Height { get; set; }
        public string Gender { get; set; } = null!;
        public string BreedName { get; set; } = null!;
        public int AddressId { get; set; }

        public static HorseDto FromHorse(Horse horse) =>
            new()
            {
                Id = horse.Id,
                Name = horse.Name,
                DateOfBirth = horse.DateOfBirth,
                Weight = horse.Weight,
                Height = horse.Height,
                Gender = horse.Gender.ToString(),
                BreedName = horse.Breed.Name,
                AddressId = horse.AddressId
            };
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

    public sealed class UpdatePersonRequest
    {
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public LocalDate DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? WebsiteLink { get; set; }
        public string? Description { get; set; }

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
}
