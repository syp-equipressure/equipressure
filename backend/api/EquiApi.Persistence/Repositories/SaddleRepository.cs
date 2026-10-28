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

    /// <summary>
    ///     Gets a saddle category
    /// </summary>
    /// <param name="saddleCategoryId">The id of the category</param>
    /// <returns>The tracked category or null if it does not exist</returns>
    public ValueTask<SaddleCategory?> GetSaddleCategoryByIdAsync(int saddleCategoryId);

    /// <summary>
    ///     Gets a saddle of a horse including its category
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <param name="saddleId">The id of the saddle</param>
    /// <returns>The tracked saddle or null if the horse has no saddle with this id</returns>
    public ValueTask<Saddle?> GetSaddleOfHorseAsync(int horseId, int saddleId);

    /// <summary>
    ///     Checks if measurement groups were recorded with a saddle
    /// </summary>
    /// <param name="saddleId">The id of the saddle</param>
    /// <returns>True if at least one measurement group uses the saddle, false otherwise</returns>
    public ValueTask<bool> HasMeasurementGroupsAsync(int saddleId);

    /// <summary>
    ///     Removes a saddle
    /// </summary>
    /// <param name="saddle">The saddle to remove</param>
    public void RemoveSaddle(Saddle saddle);
}

public class SaddleRepository(
    DbSet<Saddle> saddles,
    DbSet<SaddleCategory> saddleCategories,
    DbSet<MeasurementGroup> measurementGroups) : ISaddleRepository
{
    public async ValueTask<bool> DoesSaddleCategoryExist(int saddleCategoryId) =>
        await saddleCategories.AnyAsync(sc => sc.Id == saddleCategoryId);

    public async ValueTask<IReadOnlyCollection<SaddleCategory>> GetAllSaddleCategories() =>
        await saddleCategories.ToListAsync();

    public async ValueTask<SaddleCategory?> GetSaddleCategoryByIdAsync(int saddleCategoryId) =>
        await saddleCategories.FirstOrDefaultAsync(sc => sc.Id == saddleCategoryId);

    public async ValueTask<Saddle?> GetSaddleOfHorseAsync(int horseId, int saddleId) =>
        await saddles.Include(s => s.Category)
                     .FirstOrDefaultAsync(s => s.HorseId == horseId && s.Id == saddleId);

    public async ValueTask<bool> HasMeasurementGroupsAsync(int saddleId) =>
        await measurementGroups.AnyAsync(mg => mg.SaddleId == saddleId);

    public void RemoveSaddle(Saddle saddle)
    {
        saddles.Remove(saddle);
    }
}
