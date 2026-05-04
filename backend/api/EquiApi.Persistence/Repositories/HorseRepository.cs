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
}
public class HorseRepository(DbSet<Horse> horses) : IHorseRepository
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
}
