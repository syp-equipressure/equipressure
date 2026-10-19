using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface ISaddleRepository
{
    /// <summary>
    /// Checks if the the category exists
    /// </summary>
    /// <param name="saddleCategoryId">id of the category it should check</param>
    /// <returns>true if the category exists, false if not</returns>
    public ValueTask<bool> DoesSaddleCategoryExist(int saddleCategoryId);

    /// <summary>
    /// Gets all saddle categories
    /// </summary>
    /// <returns>All saddle categories</returns>
    public ValueTask<IReadOnlyCollection<SaddleCategory>> GetAllSaddleCategories();

}
public class SaddleRepository(DbSet<SaddleCategory> saddleCategories) : ISaddleRepository
{
    public async ValueTask<bool> DoesSaddleCategoryExist(int saddleCategoryId) =>
        await saddleCategories.AnyAsync(sc => sc.Id == saddleCategoryId);

    public async ValueTask<IReadOnlyCollection<SaddleCategory>> GetAllSaddleCategories() =>
        await saddleCategories.ToListAsync();
}
