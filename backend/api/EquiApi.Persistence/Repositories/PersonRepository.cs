using System.Linq.Expressions;
using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IPersonRepository
{
    /// <summary>
    ///     Gets a person including their address and roles
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The tracked person or null if it does not exist</returns>
    public ValueTask<Person?> GetPersonByIdAsync(int personId);

    /// <summary>
    ///     Gets the address of a person
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The address or null if the person does not exist</returns>
    public ValueTask<Address?> GetPersonAddressAsync(int personId);

    /// <summary>
    ///     Gets the first and last name of a person
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The name or null if the person does not exist</returns>
    public ValueTask<NameData?> GetNameByIdAsync(int personId);

    /// <summary>
    ///     Checks if a person exists
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>True if the person exists, false otherwise</returns>
    public ValueTask<bool> PersonExistsAsync(int personId);

    /// <summary>
    ///     Checks if an email is already used by a person
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <param name="excludedPersonId">Optional id of a person whose email is ignored (e.g. the person being updated)</param>
    /// <returns>True if another person already uses the email, false otherwise</returns>
    public ValueTask<bool> IsEmailTakenAsync(string email, int? excludedPersonId = null);

    /// <summary>
    ///     Gets the (tracked) account roles with the given names
    /// </summary>
    /// <param name="roleNames">The names of the roles</param>
    /// <returns>The roles that exist for the given names</returns>
    public ValueTask<IReadOnlyCollection<AccountRole>> GetRolesByNameAsync(IReadOnlyCollection<RoleName> roleNames);

    /// <summary>
    ///     Gets all saddlers with their address, ordered by last name
    /// </summary>
    /// <returns>All saddlers</returns>
    public ValueTask<IReadOnlyCollection<SaddlerData>> GetAllSaddlersAsync();

    /// <summary>
    ///     Gets all saddlers an equestrian marked as favourite, ordered by last name
    /// </summary>
    /// <param name="equestrianId">The id of the equestrian</param>
    /// <returns>The favourite saddlers of the equestrian</returns>
    public ValueTask<IReadOnlyCollection<SaddlerData>> GetFavouriteSaddlersAsync(int equestrianId);

    /// <summary>
    ///     Gets all contacts of a person, ordered by last name. Contacts work in both directions:
    ///     the contacts of an equestrian are saddlers and the contacts of a saddler are equestrians.
    /// </summary>
    /// <param name="personId">The id of the equestrian or saddler</param>
    /// <returns>The contacts of the person</returns>
    public ValueTask<IReadOnlyCollection<ContactData>> GetContactsAsync(int personId);

    /// <summary>
    ///     Gets the relationship (contact and/or favourite) between an equestrian and a saddler
    /// </summary>
    /// <param name="equestrianId">The id of the equestrian</param>
    /// <param name="saddlerId">The id of the saddler</param>
    /// <returns>The tracked relationship or null if there is none</returns>
    public ValueTask<PersonRelationship?> GetRelationshipAsync(int equestrianId, int saddlerId);

    /// <summary>
    ///     Gets all relationships of a person, no matter if the person is the equestrian or the saddler
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The tracked relationships</returns>
    public ValueTask<IReadOnlyCollection<PersonRelationship>> GetRelationshipsOfPersonAsync(int personId);

    /// <summary>
    ///     Adds a new relationship between an equestrian and a saddler
    /// </summary>
    /// <param name="relationship">The relationship to add</param>
    public void AddRelationship(PersonRelationship relationship);

    /// <summary>
    ///     Removes a relationship between an equestrian and a saddler
    /// </summary>
    /// <param name="relationship">The relationship to remove</param>
    public void RemoveRelationship(PersonRelationship relationship);

    /// <summary>
    ///     Adds a new person
    /// </summary>
    /// <param name="person">The person to add</param>
    public void AddPerson(Person person);

    /// <summary>
    ///     Removes a person, their role assignments are removed as well
    /// </summary>
    /// <param name="person">The person to remove</param>
    public void RemovePerson(Person person);
}

internal sealed class PersonRepository(
    DbSet<Person> personSet,
    DbSet<PersonRelationship> relationshipSet,
    DbSet<AccountRole> roleSet) : IPersonRepository
{
    public async ValueTask<Person?> GetPersonByIdAsync(int personId) =>
        await personSet.Include(p => p.Address)
                       .Include(p => p.Roles)
                       .ThenInclude(pra => pra.Role)
                       .FirstOrDefaultAsync(p => p.Id == personId);

    public async ValueTask<Address?> GetPersonAddressAsync(int personId) =>
        await personSet.AsNoTracking()
                       .Where(p => p.Id == personId)
                       .Select(p => p.Address)
                       .FirstOrDefaultAsync();

    public async ValueTask<NameData?> GetNameByIdAsync(int personId) =>
        await personSet.Where(p => p.Id == personId)
                       .Select(p => new NameData(p.FirstName, p.LastName))
                       .FirstOrDefaultAsync();

    public async ValueTask<bool> PersonExistsAsync(int personId) =>
        await personSet.AnyAsync(p => p.Id == personId);

    public async ValueTask<bool> IsEmailTakenAsync(string email, int? excludedPersonId = null) =>
        await personSet.AnyAsync(p => p.Email == email && p.Id != excludedPersonId);

    public async ValueTask<IReadOnlyCollection<AccountRole>> GetRolesByNameAsync(
        IReadOnlyCollection<RoleName> roleNames) =>
        await roleSet.Where(r => roleNames.Contains(r.Name))
                     .ToListAsync();

    public async ValueTask<IReadOnlyCollection<SaddlerData>> GetAllSaddlersAsync() =>
        await QuerySaddlers(_ => true).ToListAsync();

    public async ValueTask<IReadOnlyCollection<SaddlerData>> GetFavouriteSaddlersAsync(int equestrianId) =>
        await QuerySaddlers(p => p.Relationships.Any(r => r.EquestrianId == equestrianId && r.IsFavourite))
            .ToListAsync();

    public async ValueTask<IReadOnlyCollection<ContactData>> GetContactsAsync(int personId) =>
        await personSet.Where(p => relationshipSet.Any(r => r.IsContact
                                                            && ((r.EquestrianId == personId && r.SaddlerId == p.Id)
                                                                || (r.SaddlerId == personId
                                                                    && r.EquestrianId == p.Id))))
                       .OrderBy(p => p.LastName)
                       .ThenBy(p => p.FirstName)
                       .Select(p => new ContactData(p.Id,
                                                    p.FirstName,
                                                    p.LastName,
                                                    p.Address.AddressName,
                                                    p.Address.CityName,
                                                    p.Address.PLZ,
                                                    p.WebsiteLink,
                                                    p.Description))
                       .ToListAsync();

    public async ValueTask<PersonRelationship?> GetRelationshipAsync(int equestrianId, int saddlerId) =>
        await relationshipSet.FirstOrDefaultAsync(r => r.EquestrianId == equestrianId && r.SaddlerId == saddlerId);

    public async ValueTask<IReadOnlyCollection<PersonRelationship>> GetRelationshipsOfPersonAsync(int personId) =>
        await relationshipSet.Where(r => r.EquestrianId == personId || r.SaddlerId == personId)
                             .ToListAsync();

    public void AddRelationship(PersonRelationship relationship)
    {
        relationshipSet.Add(relationship);
    }

    public void RemoveRelationship(PersonRelationship relationship)
    {
        relationshipSet.Remove(relationship);
    }

    public void AddPerson(Person person)
    {
        personSet.Add(person);
    }

    public void RemovePerson(Person person)
    {
        personSet.Remove(person);
    }

    // Person.Relationships contains the relationships in which the person is the saddler
    private IQueryable<SaddlerData> QuerySaddlers(Expression<Func<Person, bool>> filter) =>
        personSet.Where(p => p.Roles.Any(pra => pra.Role.Name == RoleName.Saddler))
                 .Where(filter)
                 .OrderBy(p => p.LastName)
                 .ThenBy(p => p.FirstName)
                 .Select(p => new SaddlerData(p.Id,
                                              p.FirstName,
                                              p.LastName,
                                              p.Address.AddressName,
                                              p.Address.CityName,
                                              p.Address.PLZ,
                                              p.WebsiteLink,
                                              p.Description));
}