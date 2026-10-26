using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using OneOf;
using OneOf.Types;

namespace EquiApi.Core.Services;

/// <summary>
///     The values of a new horse
/// </summary>
public sealed record NewHorseData(
    string Name,
    LocalDate DateOfBirth,
    decimal Weight,
    decimal Height,
    HorseGender Gender,
    AddressData Address,
    IReadOnlyCollection<int> BreedIds,
    int OwnerId);

public interface IHorseService
{
    /// <summary>
    ///     Gets a horse including its breeds
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The horse or <see cref="NotFound" /> if it does not exist</returns>
    public ValueTask<OneOf<Success<Horse>, NotFound>> GetHorseByIdAsync(int horseId);

    /// <summary>
    ///     Gets all horses a person owns
    /// </summary>
    /// <param name="personId">The id of the owner</param>
    /// <returns>The horses (may be empty) or <see cref="NotFound" /> if the person does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<Horse>>, NotFound>> GetOwnedHorsesOfPersonAsync(int personId);

    /// <summary>
    ///     Adds a new horse and makes the given person its owner. An existing address with the same values is reused.
    /// </summary>
    /// <param name="data">The values of the new horse</param>
    /// <returns>The new horse or <see cref="NotFound" /> if the owner or one of the breeds does not exist</returns>
    public ValueTask<OneOf<Success<Horse>, NotFound>> AddHorseAsync(NewHorseData data);

    /// <summary>
    ///     Gets all saddles of a horse
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The saddles (may be empty) or <see cref="NotFound" /> if the horse does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<Saddle>>, NotFound>> GetSaddlesOfHorseAsync(int horseId);

    /// <summary>
    ///     Gets the owner and all visible riders of a horse, the owner is always the first entry
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The riders (may be empty) or <see cref="NotFound" /> if the horse does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound>> GetRidersOfHorseAsync(int horseId);

    /// <summary>
    ///     Gets all hidden riders of a horse
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The hidden riders (may be empty) or <see cref="NotFound" /> if the horse does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound>> GetHiddenRidersOfHorseAsync(int horseId);

    /// <summary>
    /// Adds a new saddle to an existing horse
    /// </summary>
    /// <param name="name">name of the saddle</param>
    /// <param name="horseId">id of the horse</param>
    /// <param name="categoryId">id of the saddle category</param>
    /// <returns>the saddle if it worked, not found if the horse or the category does not exist</returns>
    public ValueTask<OneOf<Success<Saddle>, NotFound>> AddSaddleToHorse(string name, int horseId, int categoryId);

    /// <summary>
    /// Gets all saddle categories
    /// </summary>
    /// <returns>a list of the saddle categories</returns>
    public ValueTask<IReadOnlyCollection<SaddleCategory>> GetAllSaddleCategories();
}

internal sealed class HorseService(IUnitOfWork uow, ILogger<HorseService> logger) : IHorseService
{
    public async ValueTask<OneOf<Success<Horse>, NotFound>> GetHorseByIdAsync(int horseId)
    {
        var horse = await uow.HorseRepository.GetHorseByIdAsync(horseId);
        if (horse is null)
        {
            LogHorseNotFound(horseId);

            return new NotFound();
        }

        return new Success<Horse>(horse);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<Horse>>, NotFound>> GetOwnedHorsesOfPersonAsync(
        int personId)
    {
        if (!await uow.PersonRepository.PersonExistsAsync(personId))
        {
            logger.LogInformation("Person {PersonId} not found", personId);

            return new NotFound();
        }

        var horses = await uow.HorseRepository.GetOwnedHorsesOfPersonAsync(personId);

        return new Success<IReadOnlyCollection<Horse>>(horses);
    }

    public async ValueTask<OneOf<Success<Horse>, NotFound>> AddHorseAsync(NewHorseData data)
    {
        var owner = await uow.PersonRepository.GetPersonByIdAsync(data.OwnerId);
        if (owner is null)
        {
            logger.LogInformation("Cannot add horse, owner {PersonId} not found", data.OwnerId);

            return new NotFound();
        }

        int[] breedIds = data.BreedIds.Distinct().ToArray();
        var breeds = await uow.HorseRepository.GetBreedsByIdAsync(breedIds);
        if (breeds.Count != breedIds.Length)
        {
            logger.LogInformation("Cannot add horse, at least one of the breeds {BreedIds} not found", breedIds);

            return new NotFound();
        }

        var address = await uow.LocationRepository.FindOrAddAddressAsync(data.Address);

        var horse = new Horse
        {
            Name = data.Name,
            DateOfBirth = data.DateOfBirth,
            Weight = data.Weight,
            Height = data.Height,
            Gender = data.Gender,
            Address = address,
            HorseBreeds = breeds.Select(breed => new HorseBreed { Breed = breed }).ToList(),
            Persons =
            [
                new PersonHorse
                {
                    Person = owner,
                    IsOwner = true,
                    IsHidden = false
                }
            ]
        };

        uow.HorseRepository.AddHorse(horse);
        await uow.SaveChangesAsync();
        logger.LogInformation("Horse {HorseId} added for owner {PersonId}", horse.Id, owner.Id);

        return new Success<Horse>(horse);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<Saddle>>, NotFound>> GetSaddlesOfHorseAsync(int horseId)
    {
        if (!await uow.HorseRepository.HorseExistsAsync(horseId))
        {
            LogHorseNotFound(horseId);

            return new NotFound();
        }

        var saddles = await uow.HorseRepository.GetSaddlesOfHorseAsync(horseId);

        return new Success<IReadOnlyCollection<Saddle>>(saddles);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound>> GetRidersOfHorseAsync(int horseId)
    {
        if (!await uow.HorseRepository.HorseExistsAsync(horseId))
        {
            LogHorseNotFound(horseId);

            return new NotFound();
        }

        var riders = await uow.HorseRepository.GetRidersOfHorseAsync(horseId);

        return new Success<IReadOnlyCollection<Person>>(riders);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound>> GetHiddenRidersOfHorseAsync(
        int horseId)
    {
        if (!await uow.HorseRepository.HorseExistsAsync(horseId))
        {
            LogHorseNotFound(horseId);

            return new NotFound();
        }

        var riders = await uow.HorseRepository.GetHiddenRidersOfHorseAsync(horseId);

        return new Success<IReadOnlyCollection<Person>>(riders);
    }

    private void LogHorseNotFound(int horseId)
    {
        logger.LogInformation("Horse {HorseId} not found", horseId);
    }

    public async ValueTask<OneOf<Success<Saddle>, NotFound>> AddSaddleToHorse(string name, int horseId, int categoryId)
    {
        var horse = await uow.HorseRepository.GetHorseByIdAsync(horseId);

        if (horse == null)
        {
            logger.LogWarning("Horse with id {id} could not be found", horseId);
            return new NotFound();
        }

        var category = await uow.SaddleRepository.GetSaddleCategoryByIdAsync(categoryId);
        if (category is null)
        {
            logger.LogWarning("Saddle category with id {id} could not be found", categoryId);
            return new NotFound();
        }

        // the category is set as well, because the response contains its name
        var saddle = new Saddle
        {
            CategoryId = categoryId,
            Category = category,
            HorseId = horseId,
            Name = name
        };
        horse.Saddles.Add(saddle);
        logger.LogInformation("Saddle was added to horse {id}", horseId);
        await uow.SaveChangesAsync();

        return new Success<Saddle>(saddle);
    }

    public async ValueTask<IReadOnlyCollection<SaddleCategory>> GetAllSaddleCategories()
        => await uow.SaddleRepository.GetAllSaddleCategories();
}