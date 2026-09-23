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
    /// <param name="horseId">The id of a horse</param>
    /// <returns>Saddle entities of a horse</returns>
    public ValueTask<IReadOnlyCollection<Saddle>> GetSaddlesOfHorse(int horseId);

    /// <summary>
    /// Gets all Riders and the owner of a horse 
    /// </summary>
    /// <param name="horseId"></param>
    /// <returns></returns>
    public ValueTask<IReadOnlyCollection<Person>> GetAllRidersOfHorse(int horseId);
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
        => await personHorses
              .Where(ph => ph.HorseId == horseId && (ph.IsOwner || !ph.IsHidden))
              .OrderByDescending(ph => ph.IsOwner)
              .Select(ph => ph.Person)
              .ToListAsync();
    
                 
}
