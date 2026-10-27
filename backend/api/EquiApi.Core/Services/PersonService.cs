using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using OneOf;
using OneOf.Types;

namespace EquiApi.Core.Services;

/// <summary>
///     The values of a new person
/// </summary>
public sealed record NewPersonData(
    string FirstName,
    string LastName,
    decimal Height,
    decimal Weight,
    LocalDate DateOfBirth,
    string? Email,
    string? WebsiteLink,
    string? Description,
    AddressData Address,
    RoleName Role);

/// <summary>
///     The values of the person of a new account. The account has no role until it claims a device.
/// </summary>
public sealed record NewAccountData(
    string FirstName,
    string LastName,
    decimal Height,
    decimal Weight,
    LocalDate DateOfBirth,
    string? Email,
    AddressData Address);

/// <summary>
///     The values of a person that should be changed, null values are left unchanged
/// </summary>
public sealed record PersonChanges(
    string? FirstName = null,
    string? LastName = null,
    decimal? Height = null,
    decimal? Weight = null,
    LocalDate? DateOfBirth = null,
    string? Email = null,
    string? WebsiteLink = null,
    string? Description = null,
    AddressData? Address = null,
    IReadOnlyCollection<RoleName>? Roles = null);

public interface IPersonService
{
    /// <summary>
    ///     Gets a person including their address and roles
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The person or <see cref="NotFound" /> if the person does not exist</returns>
    public ValueTask<OneOf<Success<Person>, NotFound>> GetPersonByIdAsync(int personId);

    /// <summary>
    ///     Gets the person linked to a Keycloak account, including their address and roles
    /// </summary>
    /// <param name="externalId">The id of the user in Keycloak (sub claim of the access token)</param>
    /// <returns>The person or <see cref="NotFound" /> if no person is linked to the account yet</returns>
    public ValueTask<OneOf<Success<Person>, NotFound>> GetPersonByExternalIdAsync(string externalId);

    /// <summary>
    ///     Gets the address of a person
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The address or <see cref="NotFound" /> if the person does not exist</returns>
    public ValueTask<OneOf<Success<Address>, NotFound>> GetPersonAddressAsync(int personId);

    /// <summary>
    ///     Sets the address of a person. An existing address with the same values is reused.
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <param name="address">The values of the new address</param>
    /// <returns>
    ///     The new address of the person, <see cref="NotFound" /> if the person does not exist
    ///     or <see cref="StreetRequiredForSaddler" /> if the person is a saddler and the address has no street
    /// </returns>
    public ValueTask<OneOf<Success<Address>, NotFound, StreetRequiredForSaddler>> UpdatePersonAddressAsync(
        int personId, AddressData address);

    /// <summary>
    ///     Gets the first and last name of a person
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The name or <see cref="NotFound" /> if the person does not exist</returns>
    public ValueTask<OneOf<Success<NameData>, NotFound>> GetNameByIdAsync(int personId);

    /// <summary>
    ///     Gets all saddlers with their address
    /// </summary>
    /// <returns>All saddlers, which may be empty</returns>
    public ValueTask<IReadOnlyCollection<SaddlerData>> GetAllSaddlersAsync();

    /// <summary>
    ///     Gets all saddlers a person marked as favourite
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The favourite saddlers (may be empty) or <see cref="NotFound" /> if the person does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<SaddlerData>>, NotFound>> GetFavouriteSaddlersAsync(
        int personId);

    /// <summary>
    ///     Gets all contacts of a person: the saddlers of an equestrian or the equestrians of a saddler
    /// </summary>
    /// <param name="personId">The id of the equestrian or saddler</param>
    /// <returns>The contacts (may be empty) or <see cref="NotFound" /> if the person does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<ContactData>>, NotFound>> GetContactsAsync(int personId);

    /// <summary>
    ///     Adds a new person with a role. An existing address with the same values is reused.
    /// </summary>
    /// <param name="data">The values of the new person</param>
    /// <returns>The new person or <see cref="EmailAlreadyTaken" /> if another person already uses the email</returns>
    public ValueTask<OneOf<Success<Person>, EmailAlreadyTaken>> AddPersonAsync(NewPersonData data);

    /// <summary>
    ///     Adds the person of a Keycloak account (profile setup after the first login).
    ///     The person gets no role, the role is assigned when claiming a device.
    ///     An existing address with the same values is reused.
    /// </summary>
    /// <param name="externalId">The id of the user in Keycloak (sub claim of the access token)</param>
    /// <param name="data">The values of the new person</param>
    /// <returns>
    ///     The new person, <see cref="AccountAlreadyExists" /> if a person is already linked to the account
    ///     or <see cref="EmailAlreadyTaken" /> if another person already uses the email
    /// </returns>
    public ValueTask<OneOf<Success<Person>, AccountAlreadyExists, EmailAlreadyTaken>> AddAccountAsync(
        string externalId, NewAccountData data);

    /// <summary>
    ///     Updates the values of a person. An existing address with the same values is reused.
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <param name="changes">The values to change, null values are left unchanged</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if the person does not exist,
    ///     <see cref="EmailAlreadyTaken" /> if another person already uses the new email
    ///     or <see cref="StreetRequiredForSaddler" /> if the person is (or becomes) a saddler without a street
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, EmailAlreadyTaken, StreetRequiredForSaddler>> UpdatePersonAsync(
        int personId, PersonChanges changes);

    /// <summary>
    ///     Removes a person including all their contacts and favourites
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns><see cref="Success" /> or <see cref="NotFound" /> if the person does not exist</returns>
    public ValueTask<OneOf<Success, NotFound>> DeletePersonAsync(int personId);

    /// <summary>
    ///     Marks a saddler as favourite of an equestrian, does nothing if the saddler already is a favourite
    /// </summary>
    /// <param name="equestrianId">The id of the equestrian</param>
    /// <param name="saddlerId">The id of the saddler</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if one of the persons does not exist
    ///     or <see cref="InvalidRelationship" /> if the first person is no equestrian or the second one no saddler
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, InvalidRelationship>> AddFavouriteAsync(int equestrianId,
                                                                                      int saddlerId);

    /// <summary>
    ///     Removes a saddler from the favourites of an equestrian, a contact between them is kept
    /// </summary>
    /// <param name="equestrianId">The id of the equestrian</param>
    /// <param name="saddlerId">The id of the saddler</param>
    /// <returns><see cref="Success" /> or <see cref="NotFound" /> if the saddler is no favourite of the equestrian</returns>
    public ValueTask<OneOf<Success, NotFound>> RemoveFavouriteAsync(int equestrianId, int saddlerId);

    /// <summary>
    ///     Adds a contact between an equestrian and a saddler, works from both sides.
    ///     Does nothing if they already are contacts.
    /// </summary>
    /// <param name="personId">The id of the equestrian or saddler that adds the contact</param>
    /// <param name="contactId">The id of the saddler or equestrian that becomes the contact</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if one of the persons does not exist
    ///     or <see cref="InvalidRelationship" /> if the persons are not an equestrian and a saddler
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, InvalidRelationship>> AddContactAsync(int personId, int contactId);

    /// <summary>
    ///     Removes a contact between an equestrian and a saddler, works from both sides. A favourite is kept.
    /// </summary>
    /// <param name="personId">The id of the equestrian or saddler that removes the contact</param>
    /// <param name="contactId">The id of the contact</param>
    /// <returns><see cref="Success" /> or <see cref="NotFound" /> if the persons are no contacts</returns>
    public ValueTask<OneOf<Success, NotFound>> RemoveContactAsync(int personId, int contactId);

    /// <summary>
    ///     Returned if another person already uses an email
    /// </summary>
    public readonly record struct EmailAlreadyTaken;

    /// <summary>
    ///     Returned if a person is already linked to a Keycloak account
    /// </summary>
    public readonly record struct AccountAlreadyExists;

    /// <summary>
    ///     Returned if a saddler would end up with an address without street
    /// </summary>
    public readonly record struct StreetRequiredForSaddler;

    /// <summary>
    ///     Returned if a contact or favourite is not between an equestrian and a saddler
    /// </summary>
    /// <param name="Reason">Why the relationship is not possible</param>
    public readonly record struct InvalidRelationship(string Reason);
}

internal sealed class PersonService(IUnitOfWork uow, ILogger<PersonService> logger) : IPersonService
{
    public async ValueTask<OneOf<Success<Person>, NotFound>> GetPersonByIdAsync(int personId)
    {
        var person = await uow.PersonRepository.GetPersonByIdAsync(personId);
        if (person is null)
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        return new Success<Person>(person);
    }

    public async ValueTask<OneOf<Success<Person>, NotFound>> GetPersonByExternalIdAsync(string externalId)
    {
        var person = await uow.PersonRepository.GetPersonByExternalIdAsync(externalId);
        if (person is null)
        {
            logger.LogInformation("No person linked to the account yet");

            return new NotFound();
        }

        return new Success<Person>(person);
    }

    public async ValueTask<OneOf<Success<Address>, NotFound>> GetPersonAddressAsync(int personId)
    {
        var address = await uow.PersonRepository.GetPersonAddressAsync(personId);
        if (address is null)
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        return new Success<Address>(address);
    }

    public async ValueTask<OneOf<Success<Address>, NotFound, IPersonService.StreetRequiredForSaddler>>
        UpdatePersonAddressAsync(int personId, AddressData address)
    {
        var person = await uow.PersonRepository.GetPersonByIdAsync(personId);
        if (person is null)
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        if (HasRole(person, RoleName.Saddler) && string.IsNullOrWhiteSpace(address.AddressName))
        {
            logger.LogWarning("Cannot change address of saddler {PersonId} to an address without street", personId);

            return new IPersonService.StreetRequiredForSaddler();
        }

        person.Address = await uow.LocationRepository.FindOrAddAddressAsync(address);
        await uow.SaveChangesAsync();
        logger.LogInformation("Address of person {PersonId} changed to address {AddressId}", personId,
                              person.Address.Id);

        return new Success<Address>(person.Address);
    }

    public async ValueTask<OneOf<Success<NameData>, NotFound>> GetNameByIdAsync(int personId)
    {
        var name = await uow.PersonRepository.GetNameByIdAsync(personId);
        if (name is null)
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        return new Success<NameData>(name);
    }

    public async ValueTask<IReadOnlyCollection<SaddlerData>> GetAllSaddlersAsync() =>
        await uow.PersonRepository.GetAllSaddlersAsync();

    public async ValueTask<OneOf<Success<IReadOnlyCollection<SaddlerData>>, NotFound>> GetFavouriteSaddlersAsync(
        int personId)
    {
        if (!await uow.PersonRepository.PersonExistsAsync(personId))
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        var saddlers = await uow.PersonRepository.GetFavouriteSaddlersAsync(personId);

        return new Success<IReadOnlyCollection<SaddlerData>>(saddlers);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<ContactData>>, NotFound>> GetContactsAsync(int personId)
    {
        if (!await uow.PersonRepository.PersonExistsAsync(personId))
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        var contacts = await uow.PersonRepository.GetContactsAsync(personId);

        return new Success<IReadOnlyCollection<ContactData>>(contacts);
    }

    public async ValueTask<OneOf<Success<Person>, IPersonService.EmailAlreadyTaken>> AddPersonAsync(
        NewPersonData data)
    {
        var person = new Person
        {
            FirstName = data.FirstName,
            LastName = data.LastName,
            Height = data.Height,
            Weight = data.Weight,
            DateOfBirth = data.DateOfBirth,
            Email = data.Email,
            WebsiteLink = data.WebsiteLink,
            Description = data.Description
        };

        return await AddPersonAsync(person, data.Address, [data.Role]);
    }

    public async ValueTask<OneOf<Success<Person>, IPersonService.AccountAlreadyExists,
        IPersonService.EmailAlreadyTaken>> AddAccountAsync(string externalId, NewAccountData data)
    {
        if (await uow.PersonRepository.GetPersonByExternalIdAsync(externalId) is not null)
        {
            logger.LogWarning("Cannot add account, a person is already linked to it");

            return new IPersonService.AccountAlreadyExists();
        }

        var person = new Person
        {
            ExternalId = externalId,
            FirstName = data.FirstName,
            LastName = data.LastName,
            Height = data.Height,
            Weight = data.Weight,
            DateOfBirth = data.DateOfBirth,
            Email = data.Email
        };
        var result = await AddPersonAsync(person, data.Address, []);

        return result.Match<OneOf<Success<Person>, IPersonService.AccountAlreadyExists,
            IPersonService.EmailAlreadyTaken>>(success => success, emailTaken => emailTaken);
    }

    /// <summary>
    ///     Adds a new person with the given address and roles, if the email of the person is not taken yet
    /// </summary>
    private async ValueTask<OneOf<Success<Person>, IPersonService.EmailAlreadyTaken>> AddPersonAsync(
        Person person, AddressData address, IReadOnlyCollection<RoleName> roleNames)
    {
        if (person.Email is not null && await uow.PersonRepository.IsEmailTakenAsync(person.Email))
        {
            logger.LogWarning("Cannot add person, the email is already taken");

            return new IPersonService.EmailAlreadyTaken();
        }

        var roles = roleNames.Count > 0 ? await GetRolesAsync(roleNames) : [];
        person.Address = await uow.LocationRepository.FindOrAddAddressAsync(address);
        person.Roles = roles.Select(role => new PersonRoleAssignment { Role = role }).ToList();

        uow.PersonRepository.AddPerson(person);
        await uow.SaveChangesAsync();
        logger.LogInformation("Person {PersonId} added with roles {Roles}", person.Id, roleNames);

        return new Success<Person>(person);
    }

    public async ValueTask<OneOf<Success, NotFound, IPersonService.EmailAlreadyTaken,
        IPersonService.StreetRequiredForSaddler>> UpdatePersonAsync(int personId, PersonChanges changes)
    {
        var person = await uow.PersonRepository.GetPersonByIdAsync(personId);
        if (person is null)
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        if (changes.Email is not null && await uow.PersonRepository.IsEmailTakenAsync(changes.Email, personId))
        {
            logger.LogWarning("Cannot update person {PersonId}, the email is already taken", personId);

            return new IPersonService.EmailAlreadyTaken();
        }

        // check the state after the update: the person may become a saddler or get a new address
        bool isSaddlerAfterUpdate = changes.Roles is { Count: > 0 }
            ? changes.Roles.Contains(RoleName.Saddler)
            : HasRole(person, RoleName.Saddler);
        string? streetAfterUpdate = changes.Address is not null
            ? changes.Address.AddressName
            : person.Address.AddressName;
        if (isSaddlerAfterUpdate && string.IsNullOrWhiteSpace(streetAfterUpdate))
        {
            logger.LogWarning("Cannot update person {PersonId}, a saddler needs an address with street", personId);

            return new IPersonService.StreetRequiredForSaddler();
        }

        person.FirstName = changes.FirstName ?? person.FirstName;
        person.LastName = changes.LastName ?? person.LastName;
        person.Height = changes.Height ?? person.Height;
        person.Weight = changes.Weight ?? person.Weight;
        person.DateOfBirth = changes.DateOfBirth ?? person.DateOfBirth;
        person.Email = changes.Email ?? person.Email;
        person.WebsiteLink = changes.WebsiteLink ?? person.WebsiteLink;
        person.Description = changes.Description ?? person.Description;

        if (changes.Address is not null)
        {
            person.Address = await uow.LocationRepository.FindOrAddAddressAsync(changes.Address);
        }

        if (changes.Roles is { Count: > 0 })
        {
            var roles = await GetRolesAsync(changes.Roles);

            // only touch the assignments that actually change, re-adding an existing assignment would cause a key conflict
            person.Roles.RemoveAll(assignment => roles.All(role => role.Id != assignment.RoleId));
            foreach (var role in roles.Where(role => person.Roles.All(assignment => assignment.RoleId != role.Id)))
            {
                person.Roles.Add(new PersonRoleAssignment { Role = role });
            }
        }

        await uow.SaveChangesAsync();
        logger.LogInformation("Person {PersonId} updated", personId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeletePersonAsync(int personId)
    {
        var person = await uow.PersonRepository.GetPersonByIdAsync(personId);
        if (person is null)
        {
            LogPersonNotFound(personId);

            return new NotFound();
        }

        // the saddler side of a relationship is restricted in the database (no cascade), so remove them explicitly
        foreach (var relationship in await uow.PersonRepository.GetRelationshipsOfPersonAsync(personId))
        {
            uow.PersonRepository.RemoveRelationship(relationship);
        }

        uow.PersonRepository.RemovePerson(person);
        await uow.SaveChangesAsync();
        logger.LogInformation("Person {PersonId} removed", personId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound, IPersonService.InvalidRelationship>> AddFavouriteAsync(
        int equestrianId, int saddlerId)
    {
        var equestrian = await uow.PersonRepository.GetPersonByIdAsync(equestrianId);
        var saddler = await uow.PersonRepository.GetPersonByIdAsync(saddlerId);
        if (equestrian is null || saddler is null)
        {
            LogPersonNotFound(equestrian is null ? equestrianId : saddlerId);

            return new NotFound();
        }

        if (equestrianId == saddlerId || !HasRole(equestrian, RoleName.Equestrian)
                                      || !HasRole(saddler, RoleName.Saddler))
        {
            logger.LogWarning("Person {EquestrianId} cannot mark person {SaddlerId} as favourite", equestrianId,
                              saddlerId);

            return new IPersonService.InvalidRelationship("Only equestrians can mark saddlers as favourite");
        }

        var relationship = await uow.PersonRepository.GetRelationshipAsync(equestrianId, saddlerId);
        if (relationship is null)
        {
            uow.PersonRepository.AddRelationship(new PersonRelationship
            {
                Equestrian = equestrian,
                Saddler = saddler,
                IsFavourite = true
            });
        }
        else
        {
            relationship.IsFavourite = true;
        }

        await uow.SaveChangesAsync();
        logger.LogInformation("Saddler {SaddlerId} is a favourite of equestrian {EquestrianId}", saddlerId,
                              equestrianId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> RemoveFavouriteAsync(int equestrianId, int saddlerId)
    {
        var relationship = await uow.PersonRepository.GetRelationshipAsync(equestrianId, saddlerId);
        if (relationship is not { IsFavourite: true })
        {
            logger.LogInformation("Saddler {SaddlerId} is no favourite of equestrian {EquestrianId}", saddlerId,
                                  equestrianId);

            return new NotFound();
        }

        relationship.IsFavourite = false;
        RemoveRelationshipIfUnused(relationship);
        await uow.SaveChangesAsync();
        logger.LogInformation("Saddler {SaddlerId} removed from the favourites of equestrian {EquestrianId}",
                              saddlerId, equestrianId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound, IPersonService.InvalidRelationship>> AddContactAsync(
        int personId, int contactId)
    {
        var person = await uow.PersonRepository.GetPersonByIdAsync(personId);
        var contact = await uow.PersonRepository.GetPersonByIdAsync(contactId);
        if (person is null || contact is null)
        {
            LogPersonNotFound(person is null ? personId : contactId);

            return new NotFound();
        }

        var relationship = await GetRelationshipInAnyDirectionAsync(personId, contactId);
        if (relationship is null)
        {
            // the person adding the contact can be the equestrian as well as the saddler
            (Person Equestrian, Person Saddler)? sides = personId == contactId ? null
                : HasRole(person, RoleName.Equestrian) && HasRole(contact, RoleName.Saddler) ? (person, contact)
                : HasRole(person, RoleName.Saddler) && HasRole(contact, RoleName.Equestrian) ? (contact, person)
                : null;
            if (sides is null)
            {
                logger.LogWarning("Persons {PersonId} and {ContactId} cannot be contacts", personId, contactId);

                return new IPersonService.InvalidRelationship("A contact has to be between an equestrian and a saddler");
            }

            uow.PersonRepository.AddRelationship(new PersonRelationship
            {
                Equestrian = sides.Value.Equestrian,
                Saddler = sides.Value.Saddler,
                IsContact = true
            });
        }
        else
        {
            relationship.IsContact = true;
        }

        await uow.SaveChangesAsync();
        logger.LogInformation("Persons {PersonId} and {ContactId} are contacts", personId, contactId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> RemoveContactAsync(int personId, int contactId)
    {
        var relationship = await GetRelationshipInAnyDirectionAsync(personId, contactId);
        if (relationship is not { IsContact: true })
        {
            logger.LogInformation("Persons {PersonId} and {ContactId} are no contacts", personId, contactId);

            return new NotFound();
        }

        relationship.IsContact = false;
        RemoveRelationshipIfUnused(relationship);
        await uow.SaveChangesAsync();
        logger.LogInformation("Contact between persons {PersonId} and {ContactId} removed", personId, contactId);

        return new Success();
    }

    private async ValueTask<PersonRelationship?> GetRelationshipInAnyDirectionAsync(int personId, int otherPersonId) =>
        await uow.PersonRepository.GetRelationshipAsync(personId, otherPersonId)
        ?? await uow.PersonRepository.GetRelationshipAsync(otherPersonId, personId);

    // a relationship that is neither contact nor favourite has no meaning anymore
    private void RemoveRelationshipIfUnused(PersonRelationship relationship)
    {
        if (!relationship.IsContact && !relationship.IsFavourite)
        {
            uow.PersonRepository.RemoveRelationship(relationship);
        }
    }

    private static bool HasRole(Person person, RoleName role) =>
        person.Roles.Any(assignment => assignment.Role.Name == role);

    private async ValueTask<IReadOnlyCollection<AccountRole>> GetRolesAsync(IReadOnlyCollection<RoleName> roleNames)
    {
        var roles = await uow.PersonRepository.GetRolesByNameAsync(roleNames);
        if (roles.Count != roleNames.Distinct().Count())
        {
            // roles are seeded by a migration, so this is a configuration error and not a client error
            throw new InvalidOperationException($"Not all account roles exist: {string.Join(", ", roleNames)}");
        }

        return roles;
    }

    private void LogPersonNotFound(int personId)
    {
        logger.LogInformation("Person {PersonId} not found", personId);
    }
}