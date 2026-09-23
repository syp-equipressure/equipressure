using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IHorseRepository
{
    /// <summary>
    /// Gets all horses who have an owner with the id
    /// </summary>
    /// <param name="userId">the id the owner should have</param>
    /// <returns>a list of horses who have the given ownerId</returns>
    public ValueTask<IReadOnlyCollection<Horse>> GetAllHorsesOfUserAsync(int userId);
    
    /// <summary>
    /// Returns a horse by Id
    /// </summary>
    /// <param name="horseId">the id of the horse</param>
    /// <returns>
    /// a horse with the id
    /// </returns>
    public ValueTask<Horse?> GetHorseByIdAsync(int horseId);

    /// <summary>
    /// Adds a new horse
    /// </summary>
    /// <param name="horse">the new horse</param>
    public void AddHorse(Horse horse);

    /// <summary>
    /// Get all saddles of a horse
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>Saddle entities of a horse</returns>
    public ValueTask<IReadOnlyCollection<Saddle>> GetSaddlesOfHorse(int horseId);

    /// <summary>
    /// Gets all riders and the owner of a horse
    /// owner is at the first place
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>A List of the owner and all non hidden users and non owner</returns>
    public ValueTask<IReadOnlyCollection<Person>> GetAllRidersOfHorse(int horseId);

    /// <summary>
    /// Gets all the users of a horse which are hidden
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>A Read only Collection of hidden users</returns>
    public ValueTask<IReadOnlyCollection<Person>> GetAllHiddenUsersOfHorse(int horseId);

}
public class HorseRepository(DbSet<Horse> horses, DbSet<PersonHorse> personHorses) : IHorseRepository
{
    public async ValueTask<IReadOnlyCollection<Horse>> GetAllHorsesOfUserAsync(int userId) 
        => await horses.Include(h => h.Persons)
                 .Where(h => h.Persons.Any(p => p.IsOwner && p.PersonId == userId))
                 .ToListAsync();

    public async ValueTask<Horse?> GetHorseByIdAsync(int horseId) 
        => await horses.FirstOrDefaultAsync(h => h.Id == horseId);

    public void AddHorse(Horse horse)
    {
        horses.Add(horse);
    }

    public async ValueTask<IReadOnlyCollection<Saddle>> GetSaddlesOfHorse(int horseId) 
        => await horses.Include(h => h.Saddles)
                 .Where(h => h.Id == horseId)
                 .SelectMany(h => h.Saddles)
                 .ToListAsync();

    public async ValueTask<IReadOnlyCollection<Person>> GetAllRidersOfHorse(int horseId)
        => await personHorses.Include(ph => ph.Person)
                             .Where(ph => ph.HorseId == horseId && (ph.IsOwner || !ph.IsHidden))
                             .OrderByDescending(ph => ph.IsOwner)
                             .Select(ph => ph.Person)
                             .ToListAsync();

    public async ValueTask<IReadOnlyCollection<Person>> GetAllHiddenUsersOfHorse(int horseId) 
        => await personHorses.Include(ph => ph.Person)
                             .Where(ph => ph.IsHidden && ph.HorseId == horseId)
                             .Select(ph => ph.Person)
                             .ToListAsync();

}
