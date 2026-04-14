using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IPersonRepository
{
    public ValueTask<Person?> GetPersonById(int id, bool tracking);
    public ValueTask<EquestrianBasicData?> GetPersonAsEquestrianByIdAsync(int id, bool tracking);
    public ValueTask<Address?> GetPersonAddressAsync(int id, bool tracking);
    public ValueTask<SaddlerBasicData?> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId, bool tracking);
    public ValueTask<NameData?> GetNameByIdAsync(int id, bool tracking);
    public ValueTask<bool> PersonExists(int id, bool tracking);
    public ValueTask<bool> PersonWithEmailExists(string email, bool tracking);
    public ValueTask<bool> RoleExists(AccountRole role);

    public ValueTask<bool> IsEmailTakenByAnotherUser(string personEmail, int personId, bool tracking);
    
    public ValueTask<IReadOnlyCollection<Person>> GetFavouritesAsync(int id, bool tracking);
    public ValueTask<IReadOnlyCollection<Person>> GetContactsAsync(int id, bool tracking);
    public ValueTask<IReadOnlyCollection<Horse>> GetOwnedHorsesAsync(int id, bool tracking);
    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetAllDevicesAsync(int personId, bool tracking);
    public void AddPerson(Person person);
    public void RemovePerson(Person person); 
}

internal sealed class PersonRepository(DbSet<Person> personSet, DbSet<PersonRoleAssignment> personRoleSet,
                                     DbSet<AccountRole> rolesSet) : IPersonRepository
{
    private IQueryable<Person> Persons => personSet;
    
    private IQueryable<Person> PersonsNoTracking => Persons.AsNoTracking();
    private IQueryable<PersonRoleAssignment> PersonRoleAssignments => personRoleSet;
    private IQueryable<PersonRoleAssignment> PersonRoleAssignmentsNoTracking => PersonRoleAssignments.AsNoTracking();
    private IQueryable<AccountRole> Roles => rolesSet;
    private IQueryable<AccountRole> RolesNoTracking => Roles.AsNoTracking();


    /// <summary>
    /// returns a person with the given id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tracking"></param>
    /// <returns>a person</returns>
    public async ValueTask<Person?> GetPersonById(int id, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        return await source.Where(p => p.Id == id).FirstOrDefaultAsync();
    }
    
    /// <summary>
    /// searches for an equestrian with the given id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="tracking"></param>
    /// <returns>Minimal Data for an equestrian if existing</returns>
    public async ValueTask<EquestrianBasicData?> GetPersonAsEquestrianByIdAsync(int id, bool tracking)
    {
        IQueryable<PersonRoleAssignment> source = tracking ? PersonRoleAssignments : PersonRoleAssignmentsNoTracking;
        return await source.Include(pra => pra.Person)
                                 .ThenInclude(p => p.Address)
                                 .ThenInclude(a => a.City)
                                 .Include(pra => pra.Role)
                                 .Where(pra => pra.Role.Name.ToLower() == "equestrian")
                                 .Where(pra => pra.PersonId == id)
                                 .Select(pra => new EquestrianBasicData
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
    public async ValueTask<SaddlerBasicData?> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId, bool tracking)
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
                                                   (r.EquestrianId == saddlerId
                                                    && r.SaddlerId == equestrianId) 
                                                   || (r.EquestrianId == equestrianId && r.SaddlerId == saddlerId)))})
                                   .Select(p => new SaddlerBasicData
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
    /// checks if a person with the given email exists
    /// </summary>
    /// <param name="email"></param>
    /// <param name="tracking"></param>
    /// <returns>true if exists false if not</returns>
    public async ValueTask<bool> PersonWithEmailExists(string email, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        return await source.AnyAsync(p => p.Email == email);
    }

    public async ValueTask<bool> IsEmailTakenByAnotherUser(string personEmail, int personId, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        return await source.AnyAsync(p => p.Email == personEmail && p.Id != personId);
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

    /// <summary>
    /// checks if a role with the given role exists
    /// </summary>
    /// <param name="role"></param>
    /// <returns>true if exists, false if not</returns>
    public async ValueTask<bool> RoleExists(AccountRole role)
    {
        var source = RolesNoTracking;
        return await source.AnyAsync(r => r.Name.ToLower() == role.Name.ToLower() && r.Id == role.Id);
    }
    
    /// <summary>
    /// returns all devices that belong to the person with the given id
    /// </summary>
    /// <param name="personId"></param>
    /// <param name="tracking"></param>
    /// <returns>list of devices</returns>
    public async ValueTask<IReadOnlyCollection<MeasurementDevice>> GetAllDevicesAsync(int personId, bool tracking)
    {
        var source = tracking ? Persons : PersonsNoTracking;
        

        return await source.Include(p => p.UserDevices)
                           .ThenInclude(ud => ud.Device)
                           .Include(p => p.OwnerDevices)
                           .Select(p => new
                           {
                               uDevice = p.UserDevices.Select(ud => ud.Device),
                               oDevice = p.OwnerDevices
                           })
                           .SelectMany(p => p.uDevice.Concat(p.oDevice))
                           .ToListAsync();
    }
    
    /// <summary>
    /// Creates a new person in the system and assigns a role.
    /// </summary>
    /// <param name="person">The person object to add</param>
    public void AddPerson(Person person)
    {
        personSet.Add(person);
    }
    
    /// <summary>
    /// Removes a person and their associated role assignments from the database.
    /// </summary>
    /// <param name="person">The person entity to be deleted.</param>
    public void RemovePerson(Person person)
    {
        personSet.Remove(person);
    }
}

public record EquestrianBasicData(string FirstName, string LastName, string? Street, int? HouseNumber, string City,
                                    string PLZ, string Email, decimal Height, decimal Weight);
                                    
public record SaddlerBasicData(string FirstName, string LastName, string? Street, int? HouseNumber, string City,
                                 string PLZ, string? Link, string? Description, bool isFavourite);
                                 
public record NameData(string FirstName, string LastName);