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

/// <summary>
///     Request to add a saddle to a horse
/// </summary>
/// <param name="Name">The name of the saddle</param>
/// <param name="CategoryId">The id of the saddle category</param>
public sealed record AddSaddleRequest(string Name, int CategoryId)
{
    public sealed class Validator : AbstractValidator<AddSaddleRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.CategoryId).GreaterThan(0);
        }
    }
}

/// <summary>
///     A saddle category
/// </summary>
/// <param name="Id">The id of the saddle category</param>
/// <param name="Name">The name of the saddle category</param>
public sealed record SaddleCategoryDto(int Id, string Name)
{
    public static SaddleCategoryDto FromCategory(SaddleCategory category) =>
        new(category.Id, category.Name);
}

/// <summary>
///     A list of saddle categories
/// </summary>
/// <param name="Categories">The saddle categories</param>
public sealed record SaddleCategoryListResponse(IReadOnlyList<SaddleCategoryDto> Categories)
{
    public static SaddleCategoryListResponse FromCategories(IEnumerable<SaddleCategory> categories) =>
        new(categories.Select(SaddleCategoryDto.FromCategory).ToList());
}


/// <summary>
///     Request to change a horse, properties that are null stay unchanged
/// </summary>
/// <param name="Name">The new name</param>
/// <param name="DateOfBirth">The new date of birth</param>
/// <param name="Weight">The new weight</param>
/// <param name="Height">The new height</param>
/// <param name="Gender">The new gender</param>
/// <param name="Address">The new address</param>
/// <param name="BreedIds">The new breeds, replaces all existing breeds if set</param>
public sealed record PatchHorseRequest(
    string? Name,
    LocalDate? DateOfBirth,
    decimal? Weight,
    decimal? Height,
    HorseGender? Gender,
    AddressRequest? Address, // denselben Typ verwenden wie in AddHorseRequest
    List<int>? BreedIds)
{
    /// <summary>
    ///     Converts the request to the data used by the service
    /// </summary>
    public PatchHorseData ToData() => new(
        Name,
        DateOfBirth,
        Weight,
        Height,
        Gender,
        Address?.ToData(),
        BreedIds);

    /// <summary>
    ///     Validator for <see cref="PatchHorseRequest" />
    /// </summary>
    public sealed class Validator : AbstractValidator<PatchHorseRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100)
                .When(x => x.Name is not null);

            RuleFor(x => x.Weight)
                .GreaterThan(0)
                .When(x => x.Weight is not null);

            RuleFor(x => x.Height)
                .GreaterThan(0)
                .When(x => x.Height is not null);

            RuleFor(x => x.Gender)
                .IsInEnum()
                .When(x => x.Gender is not null);

            RuleFor(x => x.BreedIds)
                .NotEmpty()
                .When(x => x.BreedIds is not null);

            RuleForEach(x => x.BreedIds)
                .GreaterThan(0)
                .When(x => x.BreedIds is not null);
        }
    }
}