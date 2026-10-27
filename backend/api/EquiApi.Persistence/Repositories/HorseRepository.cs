using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IHorseRepository
{
    /// <summary>
    ///     Gets a horse including its breeds
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The horse or null if it does not exist</returns>
    public ValueTask<Horse?> GetHorseByIdAsync(int horseId);

    /// <summary>
    ///     Checks if a horse exists
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>True if the horse exists, false otherwise</returns>
    public ValueTask<bool> HorseExistsAsync(int horseId);

    /// <summary>
    ///     Gets all horses a person owns, including their breeds
    /// </summary>
    /// <param name="personId">The id of the owner</param>
    /// <returns>The horses owned by the person</returns>
    public ValueTask<IReadOnlyCollection<Horse>> GetOwnedHorsesOfPersonAsync(int personId);

    /// <summary>
    ///     Gets all saddles of a horse including their category
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The saddles of the horse</returns>
    public ValueTask<IReadOnlyCollection<Saddle>> GetSaddlesOfHorseAsync(int horseId);

    /// <summary>
    ///     Checks if a saddle belongs to a horse
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <param name="saddleId">The id of the saddle</param>
    /// <returns>True if the saddle exists and belongs to the horse, false otherwise</returns>
    public ValueTask<bool> SaddleOfHorseExistsAsync(int horseId, int saddleId);

    /// <summary>
    ///     Gets the owner and all riders of a horse that are not hidden, the owner is always the first entry
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The owner and the visible riders</returns>
    public ValueTask<IReadOnlyCollection<Person>> GetRidersOfHorseAsync(int horseId);

    /// <summary>
    ///     Gets all hidden riders of a horse
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The hidden riders</returns>
    public ValueTask<IReadOnlyCollection<Person>> GetHiddenRidersOfHorseAsync(int horseId);

    /// <summary>
    ///     Gets the assignment of a person to a horse, no matter if the person is the owner, a rider or a hidden rider
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <param name="personId">The id of the person</param>
    /// <returns>The tracked assignment or null if the person does not belong to the horse</returns>
    public ValueTask<PersonHorse?> GetPersonOfHorseAsync(int horseId, int personId);

    /// <summary>
    ///     Checks if a person is the owner or a rider of a horse that is not hidden, i.e. if they can be selected as
    ///     rider of a measurement
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <param name="personId">The id of the person</param>
    /// <returns>True if the person is the owner or a visible rider of the horse, false otherwise</returns>
    public ValueTask<bool> IsVisibleRiderOfHorseAsync(int horseId, int personId);

    /// <summary>
    ///     Gets the (tracked) breeds with the given ids
    /// </summary>
    /// <param name="breedIds">The ids of the breeds</param>
    /// <returns>The breeds that exist for the given ids</returns>
    public ValueTask<IReadOnlyCollection<Breed>> GetBreedsByIdAsync(IReadOnlyCollection<int> breedIds);

    /// <summary>
    ///     Adds a new horse
    /// </summary>
    /// <param name="horse">The horse to add</param>
    public void AddHorse(Horse horse);

    /// <summary>
    ///     Adds a person to a horse, e.g. as a new rider
    /// </summary>
    /// <param name="personHorse">The assignment to add</param>
    public void AddPersonToHorse(PersonHorse personHorse);
}

internal sealed class HorseRepository(DbSet<Horse> horseSet, DbSet<PersonHorse> personHorseSet, DbSet<Breed> breedSet)
    : IHorseRepository
{
    public async ValueTask<Horse?> GetHorseByIdAsync(int horseId) =>
        await horseSet.Include(h => h.Address)
                      .Include(h => h.HorseBreeds)
                      .ThenInclude(hb => hb.Breed)
                      .FirstOrDefaultAsync(h => h.Id == horseId);

    public async ValueTask<bool> HorseExistsAsync(int horseId) =>
        await horseSet.AnyAsync(h => h.Id == horseId);

    public async ValueTask<IReadOnlyCollection<Horse>> GetOwnedHorsesOfPersonAsync(int personId) =>
        await horseSet.AsNoTracking()
                      .Include(h => h.Address)
                      .Include(h => h.HorseBreeds)
                      .ThenInclude(hb => hb.Breed)
                      .Where(h => h.Persons.Any(ph => ph.PersonId == personId && ph.IsOwner))
                      .OrderBy(h => h.Name)
                      .ToListAsync();

    public async ValueTask<IReadOnlyCollection<Saddle>> GetSaddlesOfHorseAsync(int horseId) =>
        await horseSet.AsNoTracking()
                      .Where(h => h.Id == horseId)
                      .SelectMany(h => h.Saddles)
                      .Include(s => s.Category)
                      .OrderBy(s => s.Name)
                      .ToListAsync();

    public async ValueTask<bool> SaddleOfHorseExistsAsync(int horseId, int saddleId) =>
        await horseSet.AnyAsync(h => h.Id == horseId && h.Saddles.Any(s => s.Id == saddleId));

    public async ValueTask<IReadOnlyCollection<Person>> GetRidersOfHorseAsync(int horseId) =>
        await personHorseSet.AsNoTracking()
                            .Where(ph => ph.HorseId == horseId && (ph.IsOwner || !ph.IsHidden))
                            .OrderByDescending(ph => ph.IsOwner)
                            .ThenBy(ph => ph.Person.LastName)
                            .Select(ph => ph.Person)
                            .ToListAsync();

    public async ValueTask<IReadOnlyCollection<Person>> GetHiddenRidersOfHorseAsync(int horseId) =>
        await personHorseSet.AsNoTracking()
                            .Where(ph => ph.HorseId == horseId && ph.IsHidden)
                            .OrderBy(ph => ph.Person.LastName)
                            .Select(ph => ph.Person)
                            .ToListAsync();

    public async ValueTask<PersonHorse?> GetPersonOfHorseAsync(int horseId, int personId) =>
        await personHorseSet.FirstOrDefaultAsync(ph => ph.HorseId == horseId && ph.PersonId == personId);

    public async ValueTask<bool> IsVisibleRiderOfHorseAsync(int horseId, int personId) =>
        await personHorseSet.AnyAsync(ph => ph.HorseId == horseId && ph.PersonId == personId
                                            && (ph.IsOwner || !ph.IsHidden));

    public async ValueTask<IReadOnlyCollection<Breed>> GetBreedsByIdAsync(IReadOnlyCollection<int> breedIds) =>
        await breedSet.Where(b => breedIds.Contains(b.Id))
                      .ToListAsync();

    public void AddHorse(Horse horse)
    {
        horseSet.Add(horse);
    }

    public void AddPersonToHorse(PersonHorse personHorse)
    {
        personHorseSet.Add(personHorse);
    }
}