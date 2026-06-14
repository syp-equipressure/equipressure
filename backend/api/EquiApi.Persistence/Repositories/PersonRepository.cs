using EquiApi.Core.Util;
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
    public ValueTask<Person?> GetPersonByIdAsync(int personId);

    /// <summary>
    /// searches for an equestrian with the given id
    /// </summary>
    /// <param name="personId">the id of equestrian we want to get</param>
    /// <returns>Minimal Data for an equestrian if existing</returns>
    public ValueTask<Helper.EquestrianBasicData?> GetPersonAsEquestrianByIdAsync(int personId);

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
    /// <returns>minimal data for the saddler if found</returns>
    public ValueTask<Helper.SaddlerBasicData?> GetPersonAsSaddlerByIdAsync(int saddlerId);

    /// <summary>
    /// returns the firstname and lastname of a person with the given id
    /// </summary>
    /// <param name="personId">the id of person we want to get</param>
    /// <returns>firstname and lastname</returns>
    public ValueTask<Helper.NameData?> GetNameByIdAsync(int personId);

    /// <summary>
    /// checks if a person with the given id exists
    /// </summary>
    /// <param name="personId">the id of person we want to check</param>
    /// <returns>true if exists false if not</returns>
    public ValueTask<bool> PersonExistsAsync(int personId);

    /// <summary>
    /// checks if a person with the given email exists
    /// </summary>
    /// <param name="personEmail">the email of person we want to check</param>
    /// <returns>true if exists false if not</returns>
    public ValueTask<bool> PersonWithEmailExistsAsync(string personEmail);

    /// <summary>
    /// checks if a role with the given role exists
    /// </summary>
    /// <param name="role">the role we want to check</param>
    /// <returns>true if exists, false if not</returns>
    public ValueTask<bool> RoleExistsAsync(AccountRole role);

    /// <summary>
    /// checks if an email by a certain person is taken by another person
    /// </summary>
    /// <param name="personEmail">email of person </param>
    /// <param name="personId">id of perso</param>
    /// <returns>true if taken, false if not</returns>
    public ValueTask<bool> IsEmailTakenByAnotherUserAsync(string personEmail, int personId);

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
    /// <returns>list of saddlers</returns>
    public ValueTask<IReadOnlyCollection<Helper.SaddlerBasicData>> GetAllSaddlersAsync();

    /// <summary>
    /// returns all saddlers that is a favourite of an equestrian + their address
    /// </summary>
    /// <param name="equestrianId">the id of equestrian</param>
    /// <returns>list of saddlers</returns>
    public ValueTask<IReadOnlyCollection<Helper.SaddlerBasicData>> GetAllSaddlerFavouritesOfEquestrianAsync(int equestrianId);

    /// <summary>
    /// returns all saddlers that is a contact of an equestrian + their address
    /// </summary>
    /// <param name="equestrianId">the id of equestrian</param>
    /// <returns>list of saddlers</returns>
    public ValueTask<IReadOnlyCollection<Helper.SaddlerBasicData>> GetAllSaddlerContactsOfEquestrianAsync(int equestrianId);

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
    public async ValueTask<Person?> GetPersonByIdAsync(int personId)
    {
        return await personSet.Where(p => p.Id == personId).FirstOrDefaultAsync();
    }

    public async ValueTask<Helper.EquestrianBasicData?> GetPersonAsEquestrianByIdAsync(int personId)
    {
        return await personRoleSet.Include(pra => pra.Person)
                                  .ThenInclude(p => p.Address)
                                  .Include(pra => pra.Role)
                                  .Where(pra => pra.Role.Name == RoleName.Equestrian)
                                  .Where(pra => pra.PersonId == personId)
                                  .Select(pra => new Helper.EquestrianBasicData(pra.Person.FirstName,
                                                                         pra.Person.LastName,
                                                                         pra.Person.Address.AddressName,
                                                                         pra.Person.Address.CityName,
                                                                         pra.Person.Address.PLZ,
                                                                         pra.Person.Email!,
                                                                         pra.Person.Height,
                                                                         pra.Person.Weight))
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync();
    }

    public async ValueTask<Address?> GetPersonAddressAsync(int personId)
    {
        return await personSet.Include(p => p.Address)
                              .Where(p => p.Id == personId)
                              .Select(p => p.Address)
                              .AsNoTracking()
                              .FirstOrDefaultAsync();
    }

    public async ValueTask<Helper.SaddlerBasicData?> GetPersonAsSaddlerByIdAsync(int saddlerId)
    {
        return await personRoleSet
                     .Include(pra => pra.Person)
                     .ThenInclude(p => p.Address)
                     .Include(pra => pra.Person)
                     .ThenInclude(p => p.Relationships)
                     .Include(pra => pra.Role)
                     .Where(pra => pra.Role.Name == RoleName.Saddler)
                     .Where(pra => pra.PersonId == saddlerId)
                     .Select(pra => new
                     {
                         pra.Person,
                         rel = (pra.Person.Relationships.Where(r =>
                                                                   
                                                                       r.SaddlerId == saddlerId))
                     })
                     .Select(p => new Helper.SaddlerBasicData(p.Person.Id,
                                                                    p.Person.FirstName,
                                                                    p.Person.LastName,
                                                                    p.Person.Address.AddressName,
                                                                    p.Person.Address.CityName,
                                                                    p.Person.Address.PLZ,
                                                                    p.Person.WebsiteLink,
                                                                    p.Person.Description))
                     .AsNoTracking()
                     .FirstOrDefaultAsync();
    }

    public async ValueTask<Helper.NameData?> GetNameByIdAsync(int personId)
    {
        return await personSet
                     .Where(p => p.Id == personId)
                     .Select(p => new Helper.NameData(p.FirstName, p.LastName))
                     .AsNoTracking()
                     .FirstOrDefaultAsync();
    }

    public async ValueTask<bool> PersonExistsAsync(int personId)
    {
        return await personSet.AnyAsync(p => p.Id == personId);
    }

    public async ValueTask<bool> PersonWithEmailExistsAsync(string personEmail)
    {
        return await personSet.AnyAsync(p => p.Email == personEmail);
    }

    public async ValueTask<bool> IsEmailTakenByAnotherUserAsync(string personEmail, int personId)
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

    public async ValueTask<bool> RoleExistsAsync(AccountRole role)
    {
        return await rolesSet.AnyAsync(r => r.Name == role.Name && r.Id == role.Id);
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

    public async ValueTask<IReadOnlyCollection<Helper.SaddlerBasicData>> GetAllSaddlersAsync()
    {
        return await personRoleSet
                     .Include(pr => pr.Role)
                     .Include(pr => pr.Person)
                     .ThenInclude(p => p.Address)
                     .Where(pr => pr.Role.Name == RoleName.Saddler)
                     .Select(pr => new Helper.SaddlerBasicData(pr.PersonId,
                                                                     pr.Person.FirstName,
                                                                     pr.Person.LastName,
                                                                     pr.Person.Address.AddressName,
                                                                     pr.Person.Address.CityName,
                                                                     pr.Person.Address.PLZ,
                                                                     pr.Person.WebsiteLink,
                                                                     pr.Person.Description))
                     .OrderBy(p => p.LastName)
                     .ToListAsync();
    }

    public async ValueTask<IReadOnlyCollection<Helper.SaddlerBasicData>> GetAllSaddlerFavouritesOfEquestrianAsync(int equestrianId)
    {
        return await personRoleSet
                     .Include(pr => pr.Role)
                     .Include(pr => pr.Person)
                     .ThenInclude(p => p.Address)
                     .Where(pr => pr.Role.Name == RoleName.Saddler &&
                                  pr.Person.Relationships.Any(r => r.IsFavourite && r.EquestrianId == equestrianId))
                     .Select(pr => new Helper.SaddlerBasicData(pr.PersonId,
                                                                     pr.Person.FirstName,
                                                                     pr.Person.LastName,
                                                                     pr.Person.Address.AddressName,
                                                                     pr.Person.Address.CityName,
                                                                     pr.Person.Address.PLZ,
                                                                     pr.Person.WebsiteLink,
                                                                     pr.Person.Description))
                     .OrderBy(p => p.LastName)
                     .ToListAsync();
    }

    public async ValueTask<IReadOnlyCollection<Helper.SaddlerBasicData>> GetAllSaddlerContactsOfEquestrianAsync(int equestrianId)
    {
        return await personRoleSet
                     .Include(pr => pr.Role)
                     .Include(pr => pr.Person)
                     .ThenInclude(p => p.Address)
                     .Where(pr => pr.Role.Name == RoleName.Saddler &&
                                  pr.Person.Relationships.Any(r => r.IsContact && r.EquestrianId == equestrianId))
                     .Select(pr => new Helper.SaddlerBasicData(pr.PersonId,
                                                                     pr.Person.FirstName,
                                                                     pr.Person.LastName,
                                                                     pr.Person.Address.AddressName,
                                                                     pr.Person.Address.CityName,
                                                                     pr.Person.Address.PLZ,
                                                                     pr.Person.WebsiteLink,
                                                                     pr.Person.Description))
                     .OrderBy(p => p.LastName)
                     .ToListAsync();
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

