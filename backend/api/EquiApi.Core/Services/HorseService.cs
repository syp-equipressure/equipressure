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

/// <summary>
///     The values of a horse that should be changed, properties that are null stay unchanged
/// </summary>
public sealed record PatchHorseData(
    string? Name,
    LocalDate? DateOfBirth,
    decimal? Weight,
    decimal? Height,
    HorseGender? Gender,
    AddressData? Address,
    IReadOnlyCollection<int>? BreedIds);

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
    ///     Deletes a horse
    /// </summary>
    /// <param name="horseId">the id of the horse that should be deleted</param>
    /// <returns>success if it works, not found if it does not exist</returns>
    public ValueTask<OneOf<Success, NotFound>> RemoveHorseAsync(int horseId);

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
    ///     Adds a person as rider (Reitbeteiligung) of a horse. A rider measures with the device of the owner and does
    ///     not need a device of their own.
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <param name="personId">The id of the new rider</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if the horse or the person does not exist
    ///     or <see cref="RiderAlreadyAssigned" /> if the person already is the owner or a (hidden) rider of the horse
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, RiderAlreadyAssigned>> AddRiderToHorseAsync(int horseId, int personId);

    /// <summary>
    ///     Hides a rider of a horse (e.g. a former rider) or shows them again. Hidden riders cannot be selected for new
    ///     measurements, their previous measurements are kept.
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <param name="personId">The id of the rider</param>
    /// <param name="isHidden">True to hide the rider, false to show them again</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if the person is no rider of the horse
    ///     or <see cref="OwnerCannotBeHidden" /> if the person is the owner of the horse
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, OwnerCannotBeHidden>> SetRiderHiddenAsync(int horseId, int personId,
                                                                                        bool isHidden);

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

    /// <summary>
    ///     Changes the given values of a horse, values that are null stay unchanged
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <param name="data">The values to change</param>
    /// <returns>The updated horse or <see cref="NotFound" /> if the horse or one of the breeds does not exist</returns>
    public ValueTask<OneOf<Success<Horse>, NotFound>> PatchHorseAsync(int horseId, PatchHorseData data);

    /// <summary>
    ///     Returned if a person already is the owner or a (hidden) rider of a horse
    /// </summary>
    public readonly record struct RiderAlreadyAssigned;

    /// <summary>
    ///     Returned if the owner of a horse should be hidden
    /// </summary>
    public readonly record struct OwnerCannotBeHidden;

    /// <summary>
    ///     Gets all breeds
    /// </summary>
    /// <returns>All breeds ordered by name, which may be empty</returns>
    public ValueTask<IReadOnlyCollection<Breed>> GetAllBreedsAsync();

    /// <summary>
    ///     Changes the name and/or the category of a saddle, values that are null stay unchanged
    /// </summary>
    /// <param name="horseId">The id of the horse the saddle belongs to</param>
    /// <param name="saddleId">The id of the saddle</param>
    /// <param name="name">The new name of the saddle</param>
    /// <param name="categoryId">The id of the new saddle category</param>
    /// <returns>
    ///     The updated saddle including its category or <see cref="NotFound" /> if the horse has no saddle with this
    ///     id or the category does not exist
    /// </returns>
    public ValueTask<OneOf<Success<Saddle>, NotFound>> UpdateSaddleAsync(int horseId, int saddleId, string? name,
                                                                         int? categoryId);

    /// <summary>
    ///     Removes a saddle of a horse. A saddle that was used for measurements cannot be removed, because its
    ///     measurement groups would be deleted as well.
    /// </summary>
    /// <param name="horseId">The id of the horse the saddle belongs to</param>
    /// <param name="saddleId">The id of the saddle</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if the horse has no saddle with this id
    ///     or <see cref="SaddleInUse" /> if measurement groups were recorded with the saddle
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, SaddleInUse>> RemoveSaddleAsync(int horseId, int saddleId);

    /// <summary>
    ///     Returned if a saddle cannot be removed because measurement groups were recorded with it
    /// </summary>
    public readonly record struct SaddleInUse;
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

    public async ValueTask<OneOf<Success, NotFound>> RemoveHorseAsync(int horseId)
    {
        var horse = await uow.HorseRepository.GetHorseByIdAsync(horseId);
        if (horse == null)
        {
            logger.LogWarning("Horse with id {horseId} was not found", horseId);
            return new NotFound();
        }

        uow.HorseRepository.DeleteHorse(horse);
        await uow.SaveChangesAsync();
        logger.LogInformation("Horse with id {id} was deleted successfully", horseId);

        return new Success();
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

    public async ValueTask<OneOf<Success, NotFound, IHorseService.RiderAlreadyAssigned>> AddRiderToHorseAsync(
        int horseId, int personId)
    {
        if (!await uow.HorseRepository.HorseExistsAsync(horseId))
        {
            LogHorseNotFound(horseId);

            return new NotFound();
        }

        if (!await uow.PersonRepository.PersonExistsAsync(personId))
        {
            logger.LogInformation("Person {PersonId} not found", personId);

            return new NotFound();
        }

        if (await uow.HorseRepository.GetPersonOfHorseAsync(horseId, personId) is not null)
        {
            logger.LogWarning("Person {PersonId} already is the owner or a rider of horse {HorseId}", personId,
                              horseId);

            return new IHorseService.RiderAlreadyAssigned();
        }

        uow.HorseRepository.AddPersonToHorse(new PersonHorse
        {
            HorseId = horseId,
            PersonId = personId,
            IsOwner = false,
            IsHidden = false
        });

        await uow.SaveChangesAsync();
        logger.LogInformation("Person {PersonId} added as rider of horse {HorseId}", personId, horseId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound, IHorseService.OwnerCannotBeHidden>> SetRiderHiddenAsync(
        int horseId, int personId, bool isHidden)
    {
        var rider = await uow.HorseRepository.GetPersonOfHorseAsync(horseId, personId);
        if (rider is null)
        {
            logger.LogInformation("Person {PersonId} is no rider of horse {HorseId}", personId, horseId);

            return new NotFound();
        }

        if (rider.IsOwner)
        {
            logger.LogWarning("Person {PersonId} is the owner of horse {HorseId} and cannot be hidden", personId,
                              horseId);

            return new IHorseService.OwnerCannotBeHidden();
        }

        rider.IsHidden = isHidden;
        await uow.SaveChangesAsync();
        logger.LogInformation("Rider {PersonId} of horse {HorseId} is hidden: {IsHidden}", personId, horseId,
                              isHidden);

        return new Success();
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

    public async ValueTask<IReadOnlyCollection<Breed>> GetAllBreedsAsync() =>
        await uow.HorseRepository.GetAllBreedsAsync();

    public async ValueTask<OneOf<Success<Saddle>, NotFound>> UpdateSaddleAsync(int horseId, int saddleId,
                                                                               string? name, int? categoryId)
    {
        var saddle = await uow.SaddleRepository.GetSaddleOfHorseAsync(horseId, saddleId);
        if (saddle is null)
        {
            LogSaddleNotFound(horseId, saddleId);

            return new NotFound();
        }

        if (categoryId is not null)
        {
            var category = await uow.SaddleRepository.GetSaddleCategoryByIdAsync(categoryId.Value);
            if (category is null)
            {
                logger.LogInformation("Cannot update saddle {SaddleId}, saddle category {CategoryId} not found",
                                      saddleId, categoryId);

                return new NotFound();
            }

            saddle.Category = category;
        }

        saddle.Name = name ?? saddle.Name;

        await uow.SaveChangesAsync();
        logger.LogInformation("Saddle {SaddleId} of horse {HorseId} updated", saddleId, horseId);

        return new Success<Saddle>(saddle);
    }

    public async ValueTask<OneOf<Success, NotFound, IHorseService.SaddleInUse>> RemoveSaddleAsync(int horseId,
                                                                                                int saddleId)
    {
        var saddle = await uow.SaddleRepository.GetSaddleOfHorseAsync(horseId, saddleId);
        if (saddle is null)
        {
            LogSaddleNotFound(horseId, saddleId);

            return new NotFound();
        }

        if (await uow.SaddleRepository.HasMeasurementGroupsAsync(saddleId))
        {
            logger.LogWarning("Cannot remove saddle {SaddleId}, measurement groups were recorded with it", saddleId);

            return new IHorseService.SaddleInUse();
        }

        uow.SaddleRepository.RemoveSaddle(saddle);
        await uow.SaveChangesAsync();
        logger.LogInformation("Saddle {SaddleId} of horse {HorseId} removed", saddleId, horseId);

        return new Success();
    }

    private void LogSaddleNotFound(int horseId, int saddleId)
    {
        logger.LogInformation("Saddle {SaddleId} of horse {HorseId} not found", saddleId, horseId);
    }

    public async ValueTask<OneOf<Success<Horse>, NotFound>> PatchHorseAsync(int horseId, PatchHorseData data)
    {
        var horse = await uow.HorseRepository.GetHorseByIdAsync(horseId);
        if (horse is null)
        {
            LogHorseNotFound(horseId);

            return new NotFound();
        }

        IReadOnlyCollection<Breed>? breeds = null;
        int[]? breedIds = null;
        if (data.BreedIds is not null)
        {
            breedIds = data.BreedIds.Distinct().ToArray();
            breeds = await uow.HorseRepository.GetBreedsByIdAsync(breedIds);
            if (breeds.Count != breedIds.Length)
            {
                logger.LogInformation("Cannot patch horse {HorseId}, at least one of the breeds {BreedIds} not found",
                                      horseId, breedIds);

                return new NotFound();
            }
        }

        if (data.Name is not null)
        {
            horse.Name = data.Name;
        }

        if (data.DateOfBirth is not null)
        {
            horse.DateOfBirth = data.DateOfBirth.Value;
        }

        if (data.Weight is not null)
        {
            horse.Weight = data.Weight.Value;
        }

        if (data.Height is not null)
        {
            horse.Height = data.Height.Value;
        }

        if (data.Gender is not null)
        {
            horse.Gender = data.Gender.Value;
        }

        if (data.Address is not null)
        {
            horse.Address = await uow.LocationRepository.FindOrAddAddressAsync(data.Address);
        }

        if (breeds is not null && breedIds is not null)
        {
            // only remove / add the difference instead of replacing the whole list
            horse.HorseBreeds.RemoveAll(hb => !breedIds.Contains(hb.BreedId));

            var existingBreedIds = horse.HorseBreeds.Select(hb => hb.BreedId).ToHashSet();
            foreach (var breed in breeds.Where(b => !existingBreedIds.Contains(b.Id)))
            {
                horse.HorseBreeds.Add(new HorseBreed { Breed = breed });
            }
        }

        await uow.SaveChangesAsync();
        logger.LogInformation("Horse {HorseId} was patched", horseId);

        return new Success<Horse>(horse);
    }
}