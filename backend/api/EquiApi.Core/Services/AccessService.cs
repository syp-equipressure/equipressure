using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;

namespace EquiApi.Core.Services;

/// <summary>
///     Decides which data the logged-in person may see and change, see <see cref="IAccessRepository" /> for the rules.
///     Data that does not exist is always accessible, so that the endpoints can answer with 404 as before
/// </summary>
public interface IAccessService
{
    /// <summary>
    ///     Gets the id of the person linked to a Keycloak user
    /// </summary>
    /// <param name="externalId">The id of the user in Keycloak (sub claim)</param>
    /// <returns>The id of the person or null if the user has not set up a profile yet</returns>
    public ValueTask<int?> GetPersonIdAsync(string externalId);

    public ValueTask<bool> CanViewPersonAsync(int personId, int otherPersonId);
    public ValueTask<bool> CanManagePersonAsync(int personId, int otherPersonId);

    /// <summary>
    ///     Persons without an account (customers of a saddler, riders of a measurement) may only be equestrians,
    ///     the saddler role comes with a claimed device
    /// </summary>
    public bool CanAddPerson(RoleName role);

    public ValueTask<bool> CanManageHorsesOfPersonAsync(int personId, int otherPersonId);
    public ValueTask<bool> CanViewHorseAsync(int personId, int horseId);
    public ValueTask<bool> CanManageHorseAsync(int personId, int horseId);
    public ValueTask<bool> CanRemoveHorseAsync(int personId, int horseId);
    public ValueTask<bool> CanUseDeviceAsync(int personId, string deviceId);
    public ValueTask<bool> CanManageDeviceAsync(int personId, string deviceId);

    /// <summary>
    ///     The owner may remove every user, a user may also leave the device on their own
    /// </summary>
    public ValueTask<bool> CanRemoveDeviceUserAsync(int personId, string deviceId, int userId);

    public ValueTask<bool> CanViewMeasurementGroupAsync(int personId, int groupId);
    public ValueTask<bool> CanManageMeasurementGroupAsync(int personId, int groupId);

    /// <summary>
    ///     A person may add measurements of a rider and a horse they may see, with a device they use
    /// </summary>
    public ValueTask<bool> CanAddMeasurementGroupAsync(int personId, NewMeasurementGroupData data);
}

internal sealed class AccessService(IUnitOfWork uow) : IAccessService
{
    private IAccessRepository Repository => uow.AccessRepository;

    public ValueTask<int?> GetPersonIdAsync(string externalId) =>
        Repository.GetPersonIdByExternalIdAsync(externalId);

    public async ValueTask<bool> CanViewPersonAsync(int personId, int otherPersonId) =>
        await Repository.CanViewPersonAsync(personId, otherPersonId) ?? true;

    public async ValueTask<bool> CanManagePersonAsync(int personId, int otherPersonId) =>
        await Repository.CanManagePersonAsync(personId, otherPersonId) ?? true;

    public bool CanAddPerson(RoleName role) => role == RoleName.Equestrian;

    public async ValueTask<bool> CanManageHorsesOfPersonAsync(int personId, int otherPersonId) =>
        await Repository.CanManageHorsesOfPersonAsync(personId, otherPersonId) ?? true;

    public async ValueTask<bool> CanViewHorseAsync(int personId, int horseId) =>
        await Repository.CanViewHorseAsync(personId, horseId) ?? true;

    public async ValueTask<bool> CanManageHorseAsync(int personId, int horseId) =>
        await Repository.CanManageHorseAsync(personId, horseId) ?? true;

    public async ValueTask<bool> CanRemoveHorseAsync(int personId, int horseId) =>
        await Repository.IsHorseOwnerAsync(personId, horseId) ?? true;

    public async ValueTask<bool> CanUseDeviceAsync(int personId, string deviceId) =>
        await Repository.CanUseDeviceAsync(personId, deviceId) ?? true;

    public async ValueTask<bool> CanManageDeviceAsync(int personId, string deviceId) =>
        await Repository.IsDeviceOwnerAsync(personId, deviceId) ?? true;

    public async ValueTask<bool> CanRemoveDeviceUserAsync(int personId, string deviceId, int userId) =>
        userId == personId || await CanManageDeviceAsync(personId, deviceId);

    public async ValueTask<bool> CanViewMeasurementGroupAsync(int personId, int groupId) =>
        await Repository.CanViewMeasurementGroupAsync(personId, groupId) ?? true;

    public async ValueTask<bool> CanManageMeasurementGroupAsync(int personId, int groupId) =>
        await Repository.CanManageMeasurementGroupAsync(personId, groupId) ?? true;

    public async ValueTask<bool> CanAddMeasurementGroupAsync(int personId, NewMeasurementGroupData data) =>
        await CanViewPersonAsync(personId, data.PersonId)
        && await CanViewHorseAsync(personId, data.HorseId)
        && await CanUseDeviceAsync(personId, data.DeviceId);
}
