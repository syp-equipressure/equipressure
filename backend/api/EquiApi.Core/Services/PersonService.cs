using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using OneOf.Types;

namespace EquiApi.Core.Services;

using GetPersonAsEquestrianByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<EquestrianMinimalData>, OneOf.Types.NotFound>;
using GetAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Address>, OneOf.Types.NotFound>;
using GetPersonAsSaddlerByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<SaddlerMinimalData>, IBaseService.InvalidData, OneOf.Types.NotFound>;
using GetNameByIdAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<NameData>, OneOf.Types.NotFound>;
using GetFavouritesOrContactsAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<List<Person>>, OneOf.Types.NotFound>;
using GetOwnedHorsesAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<List<Horse>>, OneOf.Types.NotFound>;

using GetAllDevicesAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<List<MeasurementDevice>>, OneOf.Types.Error>;
using AddPersonAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<Person>, OneOf.Types.Error, IBaseService.InvalidData>;
using UpdatePersonAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<Person>, OneOf.Types.Error, IBaseService.InvalidData>;
using DeletePersonAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success, OneOf.Types.Error, OneOf.Types.NotFound>;


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
            ? new Success<EquestrianMinimalData>(result) 
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
        var equestrian = GetPersonAsEquestrianByIdAsync(equestrianId);
        if (!equestrian.Result.IsT0)
        {
            return new IBaseService.InvalidData();
        }
        
        var result = await  context.PersonRoleAssignments
                            .Include(pra => pra.Person)
                                .ThenInclude(p => p.Address)
                                .ThenInclude(a => a.City)
                            .Include(pra => pra.Person)
                                .ThenInclude(p => p.Relationships)
                            .Include(pra => pra.Role)
                            .Where(pra => pra.Role.Name.ToLower() == "saddler")
                            .Where(pra => pra.PersonId == saddlerId)
                            .Select(pra => new { pra.Person, 
                                        rel = (pra.Person.Relationships.Where(r => 
                                                                                   (r.Person1Id == saddlerId
                                                                         && r.Person2Id == equestrianId) 
                                    || (r.Person1Id == equestrianId && r.Person2Id == saddlerId)))})
                            .Select(p => new SaddlerMinimalData
                            (
                                p.Person.FirstName,
                                p.Person.LastName,
                                p.Person.Address.Street,
                                p.Person.Address.HouseNumber,
                                p.Person.Address.City.Name,
                                p.Person.Address.City.PLZ,
                                p.Person.WebsiteLink,
                                p.Person.Description,
                                p.rel.Select(r => r.IsFavourite).FirstOrDefault()
                            ))
                            .FirstOrDefaultAsync();
        
        return result != null
            ? new Success<SaddlerMinimalData>(result)
                : new NotFound();
    }

    public async ValueTask<GetNameByIdAsyncResult> GetNameByIdAsync(int id)
    {
        var result = await context.Persons
                            .Where(p => p.Id == id)
                            .Select(p => new NameData(p.FirstName, p.LastName))
                            .FirstOrDefaultAsync();

        return result != null
            ? new Success<NameData>(result)
            : new NotFound();
    }

    public async ValueTask<GetFavouritesOrContactsAsyncResult> GetFavouritesAsync(int id)
    {
        var personExists = await context.Persons.AnyAsync(p => p.Id == id);
        if (!personExists)
        {
            return new NotFound();
        }
        var result = await context.Persons
                            .Include(p => p.Relationships)
                            .Where(p => p.Id == id)
                            .Where(p => p.Relationships.All(r => r.IsFavourite))
                            .ToListAsync();
        return new Success<List<Person>>(result);
    }

    public async ValueTask<GetFavouritesOrContactsAsyncResult> GetContactsAsync(int id)
    {
        var personExists = await context.Persons.AnyAsync(p => p.Id == id);
        if (!personExists)
        {
            return new NotFound();
        }
        var result = await context.Persons
                                  .Include(p => p.Relationships)
                                  .Where(p => p.Id == id)
                                  .Where(p => p.Relationships.All(r => r.IsContact))
                                  .ToListAsync();
        return new Success<List<Person>>(result);
    }

    public async ValueTask<GetOwnedHorsesAsyncResult> GetOwnedHorsesAsync(int id)
    {
        var personExists = await context.Persons.AnyAsync(p => p.Id == id);
        if (!personExists)
        {
            return new NotFound();
        }

        var result = await context.Horses
                                  .Include(h => h.Persons)
                                  .Where(ph => ph.Persons.Any(p => p.PersonId == id && p.IsOwner))
                                  .ToListAsync();
        return new Success<List<Horse>>(result);
    }
}

