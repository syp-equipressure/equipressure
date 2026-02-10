using Horse = EquiPressure.Core.Model.Horse;
using Person = EquiPressure.Core.Model.Person;

namespace EquiPressure.Core.Service;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using Model;

using GetPersonAsEquestrianByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<EquestrianMinimalData>, OneOf.Types.NotFound>;
using GetAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Model.Address>, OneOf.Types.NotFound>;
using GetPersonAsSaddlerByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<SaddlerMinimalData>, IBaseService.InvalidData, OneOf.Types.NotFound>;
using GetNameByIdAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<NameData>, OneOf.Types.NotFound>;
using GetFavouritesOrContactsAsyncResult 
    = OneOf.OneOf<OneOf.Types.Success<List<Person>>, OneOf.Types.NotFound>;
using GetOwnedHorsesAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<List<Horse>>, OneOf.Types.NotFound>;


public interface IPersonService
{
    public ValueTask<GetPersonAsEquestrianByIdAsyncResult> GetPersonAsEquestrianByIdAsync(int id);
    public ValueTask<GetAddressAsyncResult> GetPersonAddressAsync(int id);
    public ValueTask<GetPersonAsSaddlerByIdAsyncResult> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId);
    public ValueTask<GetNameByIdAsyncResult> GetNameByIdAsync(int id);
    public ValueTask<GetFavouritesOrContactsAsyncResult> GetFavouritesAsync(int id);
    public ValueTask<GetFavouritesOrContactsAsyncResult> GetContactsAsync(int id);
    public ValueTask<GetOwnedHorsesAsyncResult> GetOwnedHorsesAsync(int id);
}

public class PersonService(EquiContext context) : IPersonService
{
    public async ValueTask<GetPersonAsEquestrianByIdAsyncResult> GetPersonAsEquestrianByIdAsync(int id)
    {
        var result = await context.PersonRoleAssignments
                                  .Include(pra => pra.Person)
                                    .ThenInclude(p => p.Address)
                                    .ThenInclude(a => a.City)
                                  .Include(pra => pra.Role)
                                  .Where(pra => pra.Role.Name.ToLower() == "equestrian")
                                  .Where(pra => pra.PersonId == id)
                                  .Select(pra => new EquestrianMinimalData
                                  (
                                   pra.Person.FirstName,
                                   pra.Person.LastName,
                                   pra.Person.Address.Street,
                                   pra.Person.Address.HouseNumber,
                                   pra.Person.Address.City.Name,
                                   pra.Person.Address.City.PLZ,
                                   pra.Person.Email!,
                                   pra.Person.Height,
                                   pra.Person.Weight
                                      
                                  ))
                                  .FirstOrDefaultAsync();

        return result != null 
            ? new Success<EquestrianMinimalData>(result) 
            : new NotFound();
    }

    public async ValueTask<GetAddressAsyncResult> GetPersonAddressAsync(int id)
    {
        var result = await context.Persons
                                  .Include(p => p.Address)
                                  .ThenInclude(a => a.City)
                                  .Where(p => p.Id == id)
                                  .Select(p => p.Address)
                                  .FirstOrDefaultAsync();
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

public record EquestrianMinimalData(string FirstName, string LastName, string? Street, int? HouseNumber, string City,
                                    string PLZ, string Email, decimal Height, decimal Weight);
                                    
public record SaddlerMinimalData(string FirstName, string LastName, string? Street, int? HouseNumber, string City,
                                 string PLZ, string? Link, string? Description, bool isFavourite);
                                 
public record NameData(string FirstName, string LastName);