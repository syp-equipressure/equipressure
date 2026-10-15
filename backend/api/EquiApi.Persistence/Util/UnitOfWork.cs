using EquiApi.Persistence.Repositories;

namespace EquiApi.Persistence.Util;

public interface IUnitOfWork
{
    public IPersonRepository PersonRepository { get; }
    public ILocationRepository LocationRepository { get; }
    public IDeviceRepository DeviceRepository { get; }
    public IHorseRepository HorseRepository { get; }

    /// <summary>
    ///     Saves all pending changes in a single database transaction
    /// </summary>
    public Task SaveChangesAsync();
}

internal sealed class UnitOfWork(DatabaseContext context) : IUnitOfWork
{
    public IPersonRepository PersonRepository =>
        field ??= new PersonRepository(context.Persons, context.PersonRelationships, context.AccountRoles);

    public ILocationRepository LocationRepository =>
        field ??= new LocationRepository(context.Addresses);

    public IDeviceRepository DeviceRepository =>
        field ??= new DeviceRepository(context.Devices, context.DeviceCategories);

    public IHorseRepository HorseRepository =>
        field ??= new HorseRepository(context.Horses, context.PersonHorses, context.Breeds);

    public Task SaveChangesAsync() => context.SaveChangesAsync();
}