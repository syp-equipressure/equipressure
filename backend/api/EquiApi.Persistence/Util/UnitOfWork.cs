using EquiApi.Persistence.Repositories;

namespace EquiApi.Persistence.Util;

public interface IUnitOfWork
{
    public IPersonRepository PersonRepository { get; }
    public ILocationRepository LocationRepository { get; }
    public IDeviceRepository DeviceRepository { get; }
    public IHorseRepository HorseRepository { get; }
    public IMeasurementRepository MeasurementRepository { get; }
    public ISaddleRepository SaddleRepository { get; }
    public IAccessRepository AccessRepository { get; }
    public IChatRepository ChatRepository { get; }

    /// <summary>
    ///     Saves all pending changes in a single database transaction
    /// </summary>
    public Task SaveChangesAsync();
}

internal sealed class UnitOfWork(DatabaseContext context) : IUnitOfWork
{
    public IPersonRepository PersonRepository =>
        field ??= new PersonRepository(context.Persons, context.PersonRelationships, context.AccountRoles,
                                     context.PersonImages);

    public ILocationRepository LocationRepository =>
        field ??= new LocationRepository(context.Addresses);

    public IDeviceRepository DeviceRepository =>
        field ??= new DeviceRepository(context.Devices, context.DeviceCategories, context.MeasurementGroups);

    public IHorseRepository HorseRepository =>
        field ??= new HorseRepository(context.Horses, context.PersonHorses, context.Breeds, context.HorseImages);

    public ISaddleRepository SaddleRepository =>
        field ??= new SaddleRepository(context.Saddles, context.SaddleCategories, context.MeasurementGroups);

    public IMeasurementRepository MeasurementRepository =>
        field ??= new MeasurementRepository(context.MeasurementGroups, context.Measurements, context.MeasurementDates,
                                            context.Releases);

    public IChatRepository ChatRepository =>
        field ??= new ChatRepository(context.ChatMessages, context.ChatFiles);

    public IAccessRepository AccessRepository =>
        field ??= new AccessRepository(context.Persons, context.PersonRelationships, context.Horses, context.Devices,
                                       context.MeasurementGroups);

    public Task SaveChangesAsync() => context.SaveChangesAsync();
}