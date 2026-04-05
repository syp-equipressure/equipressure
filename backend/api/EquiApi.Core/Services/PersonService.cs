using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using OneOf.Types;

namespace EquiApi.Core.Services;

using GetPersonAsEquestrianByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<EquestrianBasicData>, OneOf.Types.NotFound>;
using GetAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Address>, OneOf.Types.NotFound>;
using GetPersonAsSaddlerByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<SaddlerBasicData>, IBaseService.InvalidData, OneOf.Types.NotFound>;
using GetNameByIdAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<NameData>, OneOf.Types.NotFound>;
using GetFavouritesOrContactsAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<IReadOnlyCollection<Person>>, OneOf.Types.None, OneOf.Types.NotFound>;
using GetOwnedHorsesAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<IReadOnlyCollection<Horse>>, OneOf.Types.None, OneOf.Types.NotFound>;

using GetAllDevicesAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<List<MeasurementDevice>>, OneOf.Types.None, OneOf.Types.NotFound>;
// TODO: why would we get a InvalidData if we only check the data in the controller?
using AddPersonAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<Person>, IBaseService.InvalidData, IBaseService.Conflict>;
using UpdatePersonAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<Person>, OneOf.Types.NotFound, IBaseService.InvalidData, IBaseService.Conflict>;
using DeletePersonAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success, OneOf.Types.NotFound>;


public interface IPersonService
{
    public ValueTask<GetPersonAsEquestrianByIdAsyncResult> GetPersonAsEquestrianByIdAsync(int id);
    public ValueTask<GetAddressAsyncResult> GetPersonAddressAsync(int id);
    public ValueTask<GetPersonAsSaddlerByIdAsyncResult> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId);
    public ValueTask<GetNameByIdAsyncResult> GetNameByIdAsync(int id);
    public ValueTask<GetFavouritesOrContactsAsyncResult> GetFavouritesAsync(int id);
    public ValueTask<GetFavouritesOrContactsAsyncResult> GetContactsAsync(int id);
    public ValueTask<GetOwnedHorsesAsyncResult> GetOwnedHorsesAsync(int id);
    public ValueTask<GetAllDevicesAsyncResult> GetAllDevicesAsync(int personId);
    public ValueTask<AddPersonAsyncResult> AddPersonAsync(string firstName, string lastName, decimal height,
                                                          decimal weight, LocalDate dateOfBirth, string? email, 
                                                          string? websiteLink, string? description, Address address,
                                                          AccountRole role);
    public ValueTask<UpdatePersonAsyncResult> UpdatePersonAsync(Person person);
    public ValueTask<DeletePersonAsyncResult> DeletePersonAsync(int id);
}

public class PersonService(IUnitOfWork uow) : IPersonService
{
    public async ValueTask<GetPersonAsEquestrianByIdAsyncResult> GetPersonAsEquestrianByIdAsync(int id)
    {
        var result = await uow.PersonRepository.GetPersonAsEquestrianByIdAsync(id, false);

        return result != null 
            ? new Success<EquestrianBasicData>(result) 
            : new NotFound();
    }

    public async ValueTask<GetAddressAsyncResult> GetPersonAddressAsync(int id)
    {
        var result = await uow.PersonRepository.GetPersonAddressAsync(id, false);
        return result != null
            ? new Success<Address>(result)
            : new NotFound();
    }

    public async ValueTask<GetPersonAsSaddlerByIdAsyncResult> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId)
    {
        if (!await uow.PersonRepository.PersonExists(equestrianId, false))
        {
            return new IBaseService.InvalidData();
        }

        var result = await uow.PersonRepository.GetPersonAsSaddlerByIdAsync(saddlerId, equestrianId
                                                                            , false);
        
        return result != null
            ? new Success<SaddlerBasicData>(result)
                : new NotFound();
    }

    public async ValueTask<GetNameByIdAsyncResult> GetNameByIdAsync(int id)
    {
        var result = await uow.PersonRepository.GetNameByIdAsync(id, false);

        return result != null
            ? new Success<NameData>(result)
            : new NotFound();
    }

    public async ValueTask<GetFavouritesOrContactsAsyncResult> GetFavouritesAsync(int id)
    {
        var personExists = await uow.PersonRepository.PersonExists(id, false);
        if (!personExists)
        {
            return new NotFound();
        }
        var result = await uow.PersonRepository.GetFavouritesAsync(id, false);
        return new Success<IReadOnlyCollection<Person>>(result);
    }

    public async ValueTask<GetFavouritesOrContactsAsyncResult> GetContactsAsync(int id)
    {
        var personExists = await uow.PersonRepository.PersonExists(id, false);
        if (!personExists)
        {
            return new NotFound();
        }
        var result = await uow.PersonRepository.GetContactsAsync(id, false);
        return new Success<IReadOnlyCollection<Person>>(result);
    }

    public async ValueTask<GetOwnedHorsesAsyncResult> GetOwnedHorsesAsync(int id)
    {
        var personExists = await uow.PersonRepository.PersonExists(id, false);
        if (!personExists)
        {
            return new NotFound();
        }

        var result = await uow.PersonRepository.GetOwnedHorsesAsync(id, false);
        return new Success<IReadOnlyCollection<Horse>>(result);
    }

    public ValueTask<GetAllDevicesAsyncResult> GetAllDevicesAsync(int personId)
    {
        throw new NotImplementedException();
    }

    public ValueTask<AddPersonAsyncResult> AddPersonAsync(string firstName, string lastName, decimal height, decimal weight, LocalDate dateOfBirth,
                                                          string? email, string? websiteLink, string? description,
                                                          Address address, AccountRole role)
    {
        throw new NotImplementedException();
    }

    public ValueTask<UpdatePersonAsyncResult> UpdatePersonAsync(Person person)
    {
        throw new NotImplementedException();
    }

    public ValueTask<DeletePersonAsyncResult> DeletePersonAsync(int id)
    {
        throw new NotImplementedException();
    }
}

