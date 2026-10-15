using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Shared;
using FluentValidation;

namespace EquiApi.Dtos;

/// <summary>
///     A horse
/// </summary>
/// <param name="Id">The id of the horse</param>
/// <param name="Name">The name of the horse</param>
/// <param name="DateOfBirth">The birthdate of the horse</param>
/// <param name="Weight">The weight of the horse in kg</param>
/// <param name="Height">The height of the horse in cm</param>
/// <param name="Gender">The gender of the horse</param>
/// <param name="BreedNames">The names of the breeds of the horse</param>
/// <param name="Address">The address where the horse is kept</param>
public sealed record HorseDto(
    int Id,
    string Name,
    LocalDate DateOfBirth,
    decimal Weight,
    decimal Height,
    HorseGender Gender,
    IReadOnlyList<string> BreedNames,
    AddressDto Address)
{
    public static HorseDto FromHorse(Horse horse) =>
        new(horse.Id, horse.Name, horse.DateOfBirth, horse.Weight, horse.Height, horse.Gender,
            horse.HorseBreeds.Select(hb => hb.Breed.Name).ToList(), AddressDto.FromAddress(horse.Address));
}

/// <summary>
///     A list of horses
/// </summary>
/// <param name="Horses">The horses</param>
public sealed record HorseListResponse(IReadOnlyList<HorseDto> Horses)
{
    public static HorseListResponse FromHorses(IEnumerable<Horse> horses) =>
        new(horses.Select(HorseDto.FromHorse).ToList());
}

/// <summary>
///     A saddle
/// </summary>
/// <param name="Id">The id of the saddle</param>
/// <param name="Name">The name of the saddle</param>
/// <param name="HorseId">The id of the horse the saddle belongs to</param>
/// <param name="CategoryId">The id of the saddle category</param>
/// <param name="CategoryName">The name of the saddle category</param>
public sealed record SaddleDto(int Id, string Name, int HorseId, int CategoryId, string CategoryName)
{
    public static SaddleDto FromSaddle(Saddle saddle) =>
        new(saddle.Id, saddle.Name, saddle.HorseId, saddle.CategoryId, saddle.Category.Name);
}

/// <summary>
///     A list of saddles
/// </summary>
/// <param name="Saddles">The saddles</param>
public sealed record SaddleListResponse(IReadOnlyList<SaddleDto> Saddles)
{
    public static SaddleListResponse FromSaddles(IEnumerable<Saddle> saddles) =>
        new(saddles.Select(SaddleDto.FromSaddle).ToList());
}

/// <summary>
///     Request to add a horse
/// </summary>
/// <param name="Name">The name of the horse</param>
/// <param name="DateOfBirth">The birthdate of the horse</param>
/// <param name="Weight">The weight of the horse in kg</param>
/// <param name="Height">The height of the horse in cm</param>
/// <param name="Gender">The gender of the horse</param>
/// <param name="Address">The address where the horse is kept, an existing address with the same values is reused</param>
/// <param name="BreedIds">The ids of the breeds of the horse</param>
/// <param name="OwnerId">The id of the owner</param>
public sealed record AddHorseRequest(
    string Name,
    LocalDate DateOfBirth,
    decimal Weight,
    decimal Height,
    HorseGender Gender,
    AddressRequest Address,
    IReadOnlyList<int> BreedIds,
    int OwnerId)
{
    public NewHorseData ToData() =>
        new(Name, DateOfBirth, Weight, Height, Gender, Address.ToData(), BreedIds, OwnerId);

    public sealed class Validator : AbstractValidator<AddHorseRequest>
    {
        public Validator(IClock clock)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DateOfBirth).LessThan(clock.LocalDate);
            RuleFor(x => x.Weight).GreaterThan(0);
            RuleFor(x => x.Height).GreaterThan(0);
            RuleFor(x => x.Gender).IsInEnum();
            RuleFor(x => x.Address).NotNull().SetValidator(new AddressRequest.Validator());
            RuleFor(x => x.BreedIds).NotEmpty().WithMessage("At least one breed must be selected");
            RuleForEach(x => x.BreedIds).GreaterThan(0);
            RuleFor(x => x.OwnerId).GreaterThan(0);
        }
    }
}