using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IHorseRepository
{
    public ValueTask<IReadOnlyCollection<Horse>> GetAllHorsesOfUserAsync(int userId);
    public ValueTask<Horse> GetHorseByIdAsync(int horseId);
}
public class HorseRepository(DbSet<Horse> horses) : IHorseRepository
{
    public async ValueTask<IReadOnlyCollection<Horse>> GetAllHorsesOfUserAsync(int userId) 
        => await horses.Include(h => h.Persons)
                 .Where(h => h.Persons.Any(p => p.IsOwner && p.PersonId == userId))
                 .ToListAsync();

    public ValueTask<Horse> GetHorseByIdAsync(int horseId) => throw new NotImplementedException();
}
