using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface ISaddleRepository
{
    /// <summary>
    /// Gets a saddle category
    /// </summary>
    /// <param name="saddleCategoryId">id of the category</param>
    /// <returns>the tracked category or null if it does not exist</returns>
    public ValueTask<SaddleCategory?> GetSaddleCategoryByIdAsync(int saddleCategoryId);

    /// <summary>
    /// Gets all saddle categories
    /// </summary>
    /// <returns>All saddle categories</returns>
    public ValueTask<IReadOnlyCollection<SaddleCategory>> GetAllSaddleCategories();

}
public class SaddleRepository(DbSet<SaddleCategory> saddleCategories) : ISaddleRepository
{
    public async ValueTask<SaddleCategory?> GetSaddleCategoryByIdAsync(int saddleCategoryId) =>
        await saddleCategories.FirstOrDefaultAsync(sc => sc.Id == saddleCategoryId);

    public async ValueTask<IReadOnlyCollection<SaddleCategory>> GetAllSaddleCategories() =>
        await saddleCategories.ToListAsync();
}