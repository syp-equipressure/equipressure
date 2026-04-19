using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IPersonRepository
{
    /// <summary>
    /// returns a person with the given id
    /// </summary>
    /// <param name="personId">the id of person we want to get</param>
    /// <returns>a person</returns>
    public ValueTask<Person?> GetPersonById(int personId);

    /// <summary>
    /// searches for an equestrian with the given id
    /// </summary>
    /// <param name="personId">the id of equestrian we want to get</param>
    /// <returns>Minimal Data for an equestrian if existing</returns>
    public ValueTask<EquestrianBasicData?> GetPersonAsEquestrianByIdAsync(int personId);

    /// <summary>
    /// searches for the address of a person with the given id
    /// </summary>
    /// <param name="personId">the id of person we want to get</param>
    /// <returns>the address if it exists or at least the city</returns>
    public ValueTask<Address?> GetPersonAddressAsync(int personId);

    /// <summary>
    /// searches for a saddler with the given id
    /// </summary>
    /// <param name="saddlerId">the id of saddler we want to get</param>
    /// <param name="equestrianId">the id of equestrian we want to check</param>
    /// <returns>minimal data for the saddler if found</returns>
    public ValueTask<SaddlerBasicData?> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId);

    /// <summary>
    /// returns the firstname and lastname of a person with the given id
    /// </summary>
    /// <param name="personId">the id of person we want to get</param>
    /// <returns>firstname and lastname</returns>
    public ValueTask<NameData?> GetNameByIdAsync(int personId);

    /// <summary>
    /// checks if a person with the given id exists
    /// </summary>
    /// <param name="personId">the id of person we want to check</param>
    /// <returns>true if exists false if not</returns>
    public ValueTask<bool> PersonExists(int personId);

    /// <summary>
    /// checks if a person with the given email exists
    /// </summary>
    /// <param name="personEmail">the email of person we want to check</param>
    /// <returns>true if exists false if not</returns>
    public ValueTask<bool> PersonWithEmailExists(string personEmail);

    /// <summary>
    /// checks if a role with the given role exists
    /// </summary>
    /// <param name="role">the role we want to check</param>
    /// <returns>true if exists, false if not</returns>
    public ValueTask<bool> RoleExists(AccountRole role);

    /// <summary>
    /// checks if an email by a certain person is taken by another person
    /// </summary>
    /// <param name="personEmail">email of person </param>
    /// <param name="personId">id of perso</param>
    /// <returns>true if taken, false if not</returns>
    public ValueTask<bool> IsEmailTakenByAnotherUser(string personEmail, int personId);

    /// <summary>
    /// returns all persons that are marked as favourites for the given person
    /// </summary>
    /// <param name="personId">the id of person we want to get</param>
    /// <returns>all favourites of a person</returns>
    public ValueTask<IReadOnlyCollection<Person>> GetFavouritesAsync(int personId);

    /// <summary>
    /// returns all persons that are marked as contacts for the given person
    /// </summary>
    /// <param name="personId">the id of person we want to get</param>
    /// <returns>all contacts of a Person</returns>
    public ValueTask<IReadOnlyCollection<Person>> GetContactsAsync(int personId);

    /// <summary>
    /// returns all horses that are owned by the person with the given id
    /// </summary>
    /// <param name="personId">the id of person we want to get</param>
    /// <returns>a list of the owned horses</returns>
    public ValueTask<IReadOnlyCollection<Horse>> GetOwnedHorsesAsync(int personId);

    /// <summary>
    /// returns all devices that belong to the person with the given id
    /// </summary>
    /// <param name="personId">the id of person we want to get</param>
    /// <returns>list of devices</returns>
    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetAllDevicesAsync(int personId);

    /// <summary>
    /// returns all saddlers with their address
    /// </summary>
    /// <param name="equestrianId">the id of equestriant</param>
    /// <returns>list of saddlers</returns>
    public ValueTask<IReadOnlyCollection<SaddlerBasicData>> GetAllSaddlersByEquestrianIdAsync(int equestrianId);

    /// <summary>
    /// Creates a new person in the system and assigns a role.
    /// </summary>
    /// <param name="person">The person object to add</param>
    public void AddPerson(Person person);

    /// <summary>
    /// Removes a person and their associated role assignments from the database.
    /// </summary>
    /// <param name="person">The person entity to be deleted.</param>
    public void RemovePerson(Person person);
}

internal sealed class PersonRepository(
    DbSet<Person> personSet,
    DbSet<PersonRoleAssignment> personRoleSet,
    DbSet<AccountRole> rolesSet) : IPersonRepository
{
    public async ValueTask<Person?> GetPersonById(int personId)
    {
        return await personSet.Where(p => p.Id == personId).FirstOrDefaultAsync();
    }

    public async ValueTask<EquestrianBasicData?> GetPersonAsEquestrianByIdAsync(int personId)
    {
        return await personRoleSet.Include(pra => pra.Person)
                                  .ThenInclude(p => p.Address)
                                  .ThenInclude(a => a.City)
                                  .Include(pra => pra.Role)
                                  .Where(pra => pra.Role.Name.ToLower() == "equestrian")
                                  .Where(pra => pra.PersonId == personId)
                                  .Select(pra => new EquestrianBasicData(pra.Person.FirstName,
                                                                         pra.Person.LastName,
                                                                         pra.Person.Address.Street,
                                                                         pra.Person.Address.HouseNumber,
                                                                         pra.Person.Address.City.Name,
                                                                         pra.Person.Address.City.PLZ,
                                                                         pra.Person.Email!,
                                                                         pra.Person.Height,
                                                                         pra.Person.Weight))
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync();
    }

    public async ValueTask<Address?> GetPersonAddressAsync(int personId)
    {
        return await personSet.Include(p => p.Address)
                              .ThenInclude(a => a.City)
                              .Where(p => p.Id == personId)
                              .Select(p => p.Address)
                              .AsNoTracking()
                              .FirstOrDefaultAsync();
    }

    public async ValueTask<SaddlerBasicData?> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId)
    {
        return await personRoleSet
                     .Include(pra => pra.Person)
                     .ThenInclude(p => p.Address)
                     .ThenInclude(a => a.City)
                     .Include(pra => pra.Person)
                     .ThenInclude(p => p.Relationships)
                     .Include(pra => pra.Role)
                     .Where(pra => pra.Role.Name.ToLower() == "saddler")
                     .Where(pra => pra.PersonId == saddlerId)
                     .Select(pra => new
                     {
                         pra.Person,
                         rel = (pra.Person.Relationships.Where(r =>
                                                                   (r.EquestrianId == saddlerId
                                                                    && r.SaddlerId == equestrianId)
                                                                   || (r.EquestrianId == equestrianId &&
                                                                       r.SaddlerId == saddlerId)))
                     })
                     .Select(p => new SaddlerBasicData(p.Person.Id,
                                                       p.Person.FirstName,
                                                       p.Person.LastName,
                                                       p.Person.Address.Street,
                                                       p.Person.Address.HouseNumber,
                                                       p.Person.Address.City.Name,
                                                       p.Person.Address.City.PLZ,
                                                       p.Person.WebsiteLink,
                                                       p.Person.Description,
                                                       p.rel.Select(r => r.IsFavourite).FirstOrDefault()))
                     .AsNoTracking()
                     .FirstOrDefaultAsync();
    }

    public async ValueTask<NameData?> GetNameByIdAsync(int personId)
    {
        return await personSet
                     .Where(p => p.Id == personId)
                     .Select(p => new NameData(p.FirstName, p.LastName))
                     .AsNoTracking()
                     .FirstOrDefaultAsync();
    }

    public async ValueTask<bool> PersonExists(int personId)
    {
        return await personSet.AnyAsync(p => p.Id == personId);
    }

    public async ValueTask<bool> PersonWithEmailExists(string personEmail)
    {
        return await personSet.AnyAsync(p => p.Email == personEmail);
    }

    public async ValueTask<bool> IsEmailTakenByAnotherUser(string personEmail, int personId)
    {
        return await personSet.AnyAsync(p => p.Email == personEmail && p.Id != personId);
    }

    public async ValueTask<IReadOnlyCollection<Person>> GetFavouritesAsync(int personId)
    {
        return await personSet
                     .Include(p => p.Relationships)
                     .Where(p => p.Id == personId)
                     .Where(p => p.Relationships.All(r => r.IsFavourite))
                     .AsNoTracking()
                     .ToListAsync();
    }

    public async ValueTask<IReadOnlyCollection<Person>> GetContactsAsync(int personId)
    {
        return await personSet
                     .Include(p => p.Relationships)
                     .Where(p => p.Id == personId)
                     .Where(p => p.Relationships.All(r => r.IsContact))
                     .AsNoTracking()
                     .ToListAsync();
    }

    public async ValueTask<IReadOnlyCollection<Horse>> GetOwnedHorsesAsync(int personId)
    {
        return await personSet.Include(p => p.Horses)
                              .ThenInclude(ph => ph.Horse)
                              .Where(p => p.Horses.Any(p => p.PersonId == personId && p.IsOwner))
                              .SelectMany(p => p.Horses.Select(ph => ph.Horse))
                              .AsNoTracking()
                              .ToListAsync();
    }

    public async ValueTask<bool> RoleExists(AccountRole role)
    {
        return await rolesSet.AnyAsync(r => r.Name.ToLower() == role.Name.ToLower() && r.Id == role.Id);
    }

    public async ValueTask<IReadOnlyCollection<MeasurementDevice>> GetAllDevicesAsync(int personId)
    {
        return await personSet.Include(p => p.UserDevices)
                              .ThenInclude(ud => ud.Device)
                              .Include(p => p.OwnerDevices)
                              .Select(p => new
                              {
                                  uDevice = p.UserDevices.Select(ud => ud.Device),
                                  oDevice = p.OwnerDevices
                              })
                              .SelectMany(p => p.uDevice.Concat(p.oDevice))
                              .AsNoTracking()
                              .ToListAsync();
    }

    public async ValueTask<IReadOnlyCollection<SaddlerBasicData>> GetAllSaddlersByEquestrianIdAsync(int equestrianId)
    {
        return await personRoleSet
                     .Include(pr => pr.Role)
                     .Include(pr => pr.Person)
                     .ThenInclude(p => p.Address)
                     .ThenInclude(a => a.City)
                     .Where(pr => pr.Role.Name == "saddler")
                     .Select(pr => new SaddlerBasicData(pr.PersonId,
                                                        pr.Person.FirstName,
                                                        pr.Person.LastName,
                                                        pr.Person.Address.Street,
                                                        pr.Person.Address.HouseNumber,
                                                        pr.Person.Address.City.Name,
                                                        pr.Person.Address.City.PLZ,
                                                        pr.Person.WebsiteLink,
                                                        pr.Person.Description,
                                                        // wir müssen isFavourite setzen und deshalb machen wir die anfrage pro reiter
                                                        // wir überprüfen ob der sattler ein favorit des reiters ist und setzen den wert true ode rfalse dementsprechend
                                                        pr.Person.Relationships
                                                          .Where(r => r.EquestrianId == equestrianId)
                                                          .Select(r => r.IsFavourite)
                                                          .FirstOrDefault()))
                     .OrderBy(p => p.LastName)
                     .ToListAsync();
        
        //resultat: eine liste von sattlern personalisiert für einen reiter 
    }

    public void AddPerson(Person person)
    {
        personSet.Add(person);
    }

    public void RemovePerson(Person person)
    {
        personSet.Remove(person);
    }
}

public record EquestrianBasicData(
    string FirstName,
    string LastName,
    string? Street,
    int? HouseNumber,
    string City,
    string PLZ,
    string Email,
    decimal Height,
    decimal Weight);

public record SaddlerBasicData(
    int Id,
    string FirstName,
    string LastName,
    string? Street,
    int? HouseNumber,
    string City,
    string PLZ,
    string? Link,
    string? Description,
    bool IsFavourite);

public record NameData(string FirstName, string LastName);
