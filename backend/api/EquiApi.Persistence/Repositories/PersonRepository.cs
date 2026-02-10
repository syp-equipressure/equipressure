using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IPersonRepository
{
    public ValueTask<EquestrianMinimalData?> GetPersonAsEquestrianByIdAsync(int id, bool tracking);
    public ValueTask<Address?> GetPersonAddressAsync(int id, bool tracking);
    public ValueTask<SaddlerMinimalData?> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId, bool tracking);
    public ValueTask<NameData?> GetNameByIdAsync(int id, bool tracking);
    public ValueTask<bool> PersonExists(int id, bool tracking);
    
    public ValueTask<IReadOnlyCollection<Person>> GetFavouritesAsync(int id, bool tracking);
    public ValueTask<IReadOnlyCollection<Person>> GetContactsAsync(int id, bool tracking);
    public ValueTask<IReadOnlyCollection<Horse>> GetOwnedHorsesAsync(int id, bool tracking);
    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetAllDevicesAsync(int personId);
    public Person AddPerson(string firstName, string lastName, decimal height,
                                                          decimal weight, LocalDate dateOfBirth, string? email, 
                                                          string? websiteLink, string? description, Address address,
                                                          AccountRole role);
    public void UpdatePerson(Person person);
    public void RemovePerson(int id);
}

public class PersonRepository(DbSet<Person> personSet, DbSet<PersonRoleAssignment> personRoleSet) : IPersonRepository
{
    private IQueryable<Person> Persons => personSet;
    
    private IQueryable<Person> PersonsNoTracking => Persons.AsNoTracking();
    private IQueryable<PersonRoleAssignment> PersonRoleAssignments => personRoleSet;
    private IQueryable<PersonRoleAssignment> PersonRoleAssignmentsNoTracking => PersonRoleAssignments.AsNoTracking();
    
    /// <summary>
    /// searches for an equestrian with the given id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tracking"></param>
    /// <returns>Minimal Data for an equestrian if existing</returns>
    public async ValueTask<EquestrianMinimalData?> GetPersonAsEquestrianByIdAsync(int id, bool tracking)
    {
        IQueryable<PersonRoleAssignment> source = tracking ? PersonRoleAssignments : PersonRoleAssignmentsNoTracking;
        return await source.Include(pra => pra.Person)
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
    }

    /// <summary>
    /// searches for the address of a person with the given id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tracking"></param>
    /// <returns>the address if it exists or at least the city</returns>
    public async ValueTask<Address?> GetPersonAddressAsync(int id, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        return await source.Include(p => p.Address)
                                 .ThenInclude(a => a.City)
                                 .Where(p => p.Id == id)
                                 .Select(p => p.Address)
                                 .FirstOrDefaultAsync();
        
    }

    /// <summary>
    /// searches for a saddler with the given id
    /// </summary>
    /// <param name="saddlerId"></param>
    /// <param name="equestrianId"></param>
    /// <param name="tracking"></param>
    /// <returns>minimal data for the saddler if found</returns>
    public async ValueTask<SaddlerMinimalData?> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId, bool tracking)
    {
        var source = tracking ? PersonRoleAssignments : PersonRoleAssignmentsNoTracking;
        return await  source
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
    }

    /// <summary>
    /// returns the firstname and lastname of a person with the given id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tracking"></param>
    /// <returns>firstname and lastname</returns>
    public async ValueTask<NameData?> GetNameByIdAsync(int id, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        return await source
                                  .Where(p => p.Id == id)
                                  .Select(p => new NameData(p.FirstName, p.LastName))
                                  .FirstOrDefaultAsync();
    }

    /// <summary>
    /// checks if a person with the given id exists
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tracking"></param>
    /// <returns>true if exists false if not</returns>
    public async ValueTask<bool> PersonExists(int id, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        return await source.AnyAsync(p => p.Id == id);
    }

    /// <summary>
    /// returns all persons that are marked as favourites for the given person
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tracking"></param>
    /// <returns>all favourites of a person</returns>
    public async ValueTask<IReadOnlyCollection<Person>> GetFavouritesAsync(int id, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        return await source
                                  .Include(p => p.Relationships)
                                  .Where(p => p.Id == id)
                                  .Where(p => p.Relationships.All(r => r.IsFavourite))
                                  .ToListAsync();
    }

    /// <summary>
    /// returns all persons that are marked as contacts for the given person
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tracking"></param>
    /// <returns>all contacts of a Person</returns>
    public async ValueTask<IReadOnlyCollection<Person>> GetContactsAsync(int id, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        return await source
                                  .Include(p => p.Relationships)
                                  .Where(p => p.Id == id)
                                  .Where(p => p.Relationships.All(r => r.IsContact))
                                  .ToListAsync();
    }

    /// <summary>
    /// returns all horses that are owned by the person with the given id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tracking"></param>
    /// <returns>a list of the owned horses</returns>
    public async ValueTask<IReadOnlyCollection<Horse>> GetOwnedHorsesAsync(int id, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        return await source.Include(p => p.Horses)
                           .ThenInclude(ph => ph.Horse)
                                  .Where(p => p.Horses.Any(p => p.PersonId == id && p.IsOwner))
                                  .SelectMany(p => p.Horses.Select(ph => ph.Horse))
                                  .ToListAsync();
    }

    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetAllDevicesAsync(int personId)
    {
        throw new NotImplementedException();
    }

    public Person AddPerson(string firstName, string lastName, decimal height, decimal weight, LocalDate dateOfBirth,
                            string? email, string? websiteLink, string? description,
                            Address address, AccountRole role) =>
        throw new NotImplementedException();

    public void UpdatePerson(Person person)
    {
        throw new NotImplementedException();
    }

    public void RemovePerson(int id)
    {
        throw new NotImplementedException();
    }
}

public record EquestrianMinimalData(string FirstName, string LastName, string? Street, int? HouseNumber, string City,
                                    string PLZ, string Email, decimal Height, decimal Weight);
                                    
public record SaddlerMinimalData(string FirstName, string LastName, string? Street, int? HouseNumber, string City,
                                 string PLZ, string? Link, string? Description, bool isFavourite);
                                 
public record NameData(string FirstName, string LastName);