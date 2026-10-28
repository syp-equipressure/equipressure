using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

/// <summary>
///     Checks which data a person may access. All checks return null if the requested data does not exist,
///     so that the caller can decide how to handle that (usually the endpoint answers with 404 anyway)
/// </summary>
public interface IAccessRepository
{
    /// <summary>
    ///     Gets the id of the person linked to a Keycloak user
    /// </summary>
    /// <param name="externalId">The id of the user in Keycloak (sub claim)</param>
    /// <returns>The id of the person or null if the user has not set up a profile yet</returns>
    public ValueTask<int?> GetPersonIdByExternalIdAsync(string externalId);

    /// <summary>
    ///     A person may see the profile of themselves, of all saddlers, of their contacts and favourites
    ///     and of the people they share a device with
    /// </summary>
    public ValueTask<bool?> CanViewPersonAsync(int personId, int otherPersonId);

    /// <summary>
    ///     A person may change themselves, a saddler may also change customers without an account of their own
    /// </summary>
    public ValueTask<bool?> CanManagePersonAsync(int personId, int otherPersonId);

    /// <summary>
    ///     A person may see and add their own horses, a saddler also those of their customers
    /// </summary>
    public ValueTask<bool?> CanManageHorsesOfPersonAsync(int personId, int otherPersonId);

    /// <summary>
    ///     A person may see a horse they own or ride, a saddler also the horses of their customers
    /// </summary>
    public ValueTask<bool?> CanViewHorseAsync(int personId, int horseId);

    /// <summary>
    ///     A person may change a horse they own, a saddler also the horses of their customers
    /// </summary>
    public ValueTask<bool?> CanManageHorseAsync(int personId, int horseId);

    /// <summary>
    ///     Only the owner may remove a horse
    /// </summary>
    public ValueTask<bool?> IsHorseOwnerAsync(int personId, int horseId);

    /// <summary>
    ///     The owner and the users of a device may see and use it
    /// </summary>
    public ValueTask<bool?> CanUseDeviceAsync(int personId, string deviceId);

    /// <summary>
    ///     Only the owner may change the users or the owner of a device
    /// </summary>
    public ValueTask<bool?> IsDeviceOwnerAsync(int personId, string deviceId);

    /// <summary>
    ///     A person may see the measurements they took and all measurements of horses they may see
    /// </summary>
    public ValueTask<bool?> CanViewMeasurementGroupAsync(int personId, int groupId);

    /// <summary>
    ///     The person who took the measurements and the owner of the horse may change them
    /// </summary>
    public ValueTask<bool?> CanManageMeasurementGroupAsync(int personId, int groupId);
}

internal sealed class AccessRepository(
    DbSet<Person> personSet,
    DbSet<PersonRelationship> relationshipSet,
    DbSet<Horse> horseSet,
    DbSet<MeasurementDevice> deviceSet,
    DbSet<MeasurementGroup> groupSet) : IAccessRepository
{
    public async ValueTask<int?> GetPersonIdByExternalIdAsync(string externalId) =>
        await personSet.Where(p => p.ExternalId == externalId)
                       .Select(p => (int?) p.Id)
                       .FirstOrDefaultAsync();

    public async ValueTask<bool?> CanViewPersonAsync(int personId, int otherPersonId) =>
        await personSet.Where(p => p.Id == otherPersonId)
                       .Select(p => (bool?) (p.Id == personId
                                            || p.Roles.Any(r => r.Role.Name == RoleName.Saddler)
                                            || relationshipSet.Any(r =>
                                                (r.SaddlerId == personId && r.EquestrianId == p.Id)
                                                || (r.EquestrianId == personId && r.SaddlerId == p.Id))
                                            || deviceSet.Any(d =>
                                                (d.OwnerId == personId || d.Users.Any(u => u.UserId == personId))
                                                && (d.OwnerId == p.Id || d.Users.Any(u => u.UserId == p.Id)))))
                       .FirstOrDefaultAsync();

    public async ValueTask<bool?> CanManagePersonAsync(int personId, int otherPersonId) =>
        await personSet.Where(p => p.Id == otherPersonId)
                       .Select(p => (bool?) (p.Id == personId
                                            || (p.ExternalId == null
                                                && relationshipSet.Any(r => r.SaddlerId == personId
                                                                            && r.EquestrianId == p.Id
                                                                            && r.IsContact))))
                       .FirstOrDefaultAsync();

    public async ValueTask<bool?> CanManageHorsesOfPersonAsync(int personId, int otherPersonId) =>
        await personSet.Where(p => p.Id == otherPersonId)
                       .Select(p => (bool?) (p.Id == personId
                                            || relationshipSet.Any(r => r.SaddlerId == personId
                                                                        && r.EquestrianId == p.Id
                                                                        && r.IsContact)))
                       .FirstOrDefaultAsync();

    public async ValueTask<bool?> CanViewHorseAsync(int personId, int horseId) =>
        await horseSet.Where(h => h.Id == horseId)
                      .Select(h => (bool?) (h.Persons.Any(ph => ph.PersonId == personId)
                                           || h.Persons.Any(ph => ph.IsOwner
                                                                  && relationshipSet.Any(r =>
                                                                      r.SaddlerId == personId
                                                                      && r.EquestrianId == ph.PersonId
                                                                      && r.IsContact))))
                      .FirstOrDefaultAsync();

    public async ValueTask<bool?> CanManageHorseAsync(int personId, int horseId) =>
        await horseSet.Where(h => h.Id == horseId)
                      .Select(h => (bool?) h.Persons.Any(ph => ph.IsOwner
                                                              && (ph.PersonId == personId
                                                                  || relationshipSet.Any(r =>
                                                                      r.SaddlerId == personId
                                                                      && r.EquestrianId == ph.PersonId
                                                                      && r.IsContact))))
                      .FirstOrDefaultAsync();

    public async ValueTask<bool?> IsHorseOwnerAsync(int personId, int horseId) =>
        await horseSet.Where(h => h.Id == horseId)
                      .Select(h => (bool?) h.Persons.Any(ph => ph.IsOwner && ph.PersonId == personId))
                      .FirstOrDefaultAsync();

    public async ValueTask<bool?> CanUseDeviceAsync(int personId, string deviceId) =>
        await deviceSet.Where(d => d.Id == deviceId)
                       .Select(d => (bool?) (d.OwnerId == personId || d.Users.Any(u => u.UserId == personId)))
                       .FirstOrDefaultAsync();

    public async ValueTask<bool?> IsDeviceOwnerAsync(int personId, string deviceId) =>
        await deviceSet.Where(d => d.Id == deviceId)
                       .Select(d => (bool?) (d.OwnerId == personId))
                       .FirstOrDefaultAsync();

    public async ValueTask<bool?> CanViewMeasurementGroupAsync(int personId, int groupId) =>
        await groupSet.Where(g => g.Id == groupId)
                      .Select(g => (bool?) (g.PersonId == personId
                                           || g.Horse.Persons.Any(ph => ph.PersonId == personId)
                                           || g.Horse.Persons.Any(ph => ph.IsOwner
                                                                        && relationshipSet.Any(r =>
                                                                            r.SaddlerId == personId
                                                                            && r.EquestrianId == ph.PersonId
                                                                            && r.IsContact))))
                      .FirstOrDefaultAsync();

    public async ValueTask<bool?> CanManageMeasurementGroupAsync(int personId, int groupId) =>
        await groupSet.Where(g => g.Id == groupId)
                      .Select(g => (bool?) (g.PersonId == personId
                                           || g.Horse.Persons.Any(ph => ph.IsOwner && ph.PersonId == personId)))
                      .FirstOrDefaultAsync();
}