using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using Library.Core;
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

public class PersonService(IUnitOfWork uow, IDateTimeProvider dateTimeProvider) : IPersonService
{
    /// <summary>
    /// Retrieves the data of a person formatted for an equestrian user view.
    /// </summary>
    /// <param name="id">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success{EquestrianBasicData}"/> containing the data, 
    /// or <see cref="NotFound"/> if the person does not exist.
    /// </returns>
    public async ValueTask<GetPersonAsEquestrianByIdAsyncResult> GetPersonAsEquestrianByIdAsync(int id)
    {
        var result = await uow.PersonRepository.GetPersonAsEquestrianByIdAsync(id, false);

        return result != null
            ? new Success<EquestrianBasicData>(result)
            : new NotFound();
    }

    /// <summary>
    /// Retrieves the specific address for a person.
    /// </summary>
    /// <param name="id">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success{Address}"/> containing the address, 
    /// or <see cref="NotFound"/> if the person or their address is missing.
    /// </returns>
    public async ValueTask<GetAddressAsyncResult> GetPersonAddressAsync(int id)
    {
        var result = await uow.PersonRepository.GetPersonAddressAsync(id, false);

        return result != null
            ? new Success<Address>(result)
            : new NotFound();
    }

    public async ValueTask<GetPersonAsSaddlerByIdAsyncResult> GetPersonAsSaddlerByIdAsync(
        int saddlerId, int equestrianId)
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

    /// <summary>
    /// Retrieves the First and Last name for a person.
    /// </summary>
    /// <param name="id">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success{NameData}"/> if found,
    /// or <see cref="NotFound"/> if the ID is unknown.
    /// </returns>
    public async ValueTask<GetNameByIdAsyncResult> GetNameByIdAsync(int id)
    {
        var result = await uow.PersonRepository.GetNameByIdAsync(id, false);

        return result != null
            ? new Success<NameData>(result)
            : new NotFound();
    }

    /// <summary>
    /// Retrieves the list of persons marked as favorites by the specified user.
    /// </summary>
    /// <param name="id">The id of the perosn</param>
    /// <returns>
    /// A collection of <see cref="Person"/> entities,
    /// <see cref="None"/> if the collection is empty, 
    /// or <see cref="NotFound"/> if the user does not exist.
    /// </returns>
    public async ValueTask<GetFavouritesOrContactsAsyncResult> GetFavouritesAsync(int id)
    {
        var personExists = await uow.PersonRepository.PersonExists(id, false);
        if (!personExists)
        {
            return new NotFound();
        }

        var result = await uow.PersonRepository.GetFavouritesAsync(id, false);

        return result.Count > 0 ? new Success<IReadOnlyCollection<Person>>(result) : new None();
    }

    /// <summary>
    /// Retrieves the list of contacts associated with the specified user.
    /// </summary>
    /// <param name="id">The id of the person</param>
    /// <returns>
    /// A collection of <see cref="Person"/> entities,
    /// <see cref="None"/> if the collection is empty, 
    /// or <see cref="NotFound"/> if the user does not exist.
    /// </returns>
    public async ValueTask<GetFavouritesOrContactsAsyncResult> GetContactsAsync(int id)
    {
        var personExists = await uow.PersonRepository.PersonExists(id, false);
        if (!personExists)
        {
            return new NotFound();
        }

        var result = await uow.PersonRepository.GetContactsAsync(id, false);

        return result.Count > 0 ? new Success<IReadOnlyCollection<Person>>(result) : new None();
    }

    /// <summary>
    /// Retrieves all horses owned by the person.
    /// </summary>
    /// <param name="id">The id of the person</param>
    /// <returns>
    /// A collection of <see cref="Horse"/> entities,
    /// <see cref="None"/> if the collection is empty,
    /// or <see cref="NotFound"/> if the person does not exist.
    /// </returns>
    public async ValueTask<GetOwnedHorsesAsyncResult> GetOwnedHorsesAsync(int id)
    {
        var personExists = await uow.PersonRepository.PersonExists(id, false);
        if (!personExists)
        {
            return new NotFound();
        }

        var result = await uow.PersonRepository.GetOwnedHorsesAsync(id, false);

        return result.Count > 0 ? new Success<IReadOnlyCollection<Horse>>(result) : new None();
    }

    /// <summary>
    /// Retrieves a list of all devices registered to a specific person.
    /// </summary>
    /// <param name="personId">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success{List}"/> of devices, 
    /// <see cref="None"/> if no devices are registered, 
    /// or <see cref="NotFound"/> if the person does not exist.
    /// </returns>
    public async ValueTask<GetAllDevicesAsyncResult> GetAllDevicesAsync(int personId)
    {
        if (!await uow.PersonRepository.PersonExists(personId, false))
        {
            return new NotFound();
        }

        var result = await uow.PersonRepository.GetAllDevicesAsync(personId, false);

        return result.Count > 0 ? new Success<List<MeasurementDevice>>(result.ToList()) : new None();
    }

    /// <summary>
    /// Validates and registers a new person in the system with a specific role.
    /// </summary>
    /// <param name="firstName">The first name of the person.</param>
    /// <param name="lastName">The last name of the person.</param>
    /// <param name="height">The persons height.</param>
    /// <param name="weight">The persons weight.</param>
    /// <param name="dateOfBirth">The persons birth date</param>
    /// <param name="email">The unique email address for the account (optional).</param>
    /// <param name="websiteLink">A website link (optional).</param>
    /// <param name="description">A profile description (optional).</param>
    /// <param name="address">The address entity to be associated with the person.</param>
    /// <param name="role">The role assigned to the new person.</param>
    /// <returns>
    /// A <see cref="Success{Person}"/> containing the created entity, 
    /// <see cref="IBaseService.InvalidData"/> if attributes are logically incorrect, 
    /// or <see cref="IBaseService.Conflict"/> if the provided email is already claimed by another user.
    /// </returns>
    public async ValueTask<AddPersonAsyncResult> AddPersonAsync(string firstName, string lastName, decimal height,
                                                                decimal weight, LocalDate dateOfBirth,
                                                                string? email, string? websiteLink, string? description,
                                                                Address address, AccountRole role)
    {
        if (height <= 0 || weight <= 0 || dateOfBirth >= dateTimeProvider.GetCurrentDate())
        {
            return new IBaseService.InvalidData();
        }

        if (email != null && await uow.PersonRepository.PersonWithEmailExists(email, true))
        {
            return new IBaseService.Conflict();
        }

        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Height = height,
            Weight = weight,
            DateOfBirth = dateOfBirth,
            Email = email,
            WebsiteLink = websiteLink,
            Description = description,
            Address = address,
            Roles = new List<PersonRoleAssignment>()
        };

        person.Roles.Add(new PersonRoleAssignment
        {
            PersonId = person.Id,
            RoleId = role.Id,
            Person = person,
            Role = role
        });

        uow.PersonRepository.AddPerson(person);
        await uow.SaveChangesAsync();
        return new Success<Person>(person);
    }

    /// <summary>
    /// Updates the information for an existing person.
    /// </summary>
    /// <param name="person">The person entity containing updated values.</param>
    /// <returns>
    /// A <see cref="Success{Person}"/> if updated, 
    /// <see cref="NotFound"/> if the person does not exist, 
    /// <see cref="IBaseService.InvalidData"/> for invalid field values, 
    /// or <see cref="IBaseService.Conflict"/> if the new email is claimed by another user.
    /// </returns>
    public async ValueTask<UpdatePersonAsyncResult> UpdatePersonAsync(Person person)
    {
        if (!await uow.PersonRepository.PersonExists(person.Id, true))
        {
            return new NotFound();
        }

        if (person.Height <= 0 || person.Weight <= 0 || person.DateOfBirth >= dateTimeProvider.GetCurrentDate())
        {
            return new IBaseService.InvalidData();
        }

        if (person.Email != null && await uow.PersonRepository.IsEmailTakenByAnotherUser(person.Email, person.Id, true))
        {
            return new IBaseService.Conflict();
        }
        
        

        await uow.SaveChangesAsync();

        return new Success<Person>(person);
    }

    /// <summary>
    /// Removes a person from the system.
    /// </summary>
    /// <param name="id">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success"/> result if the deletion was successful; 
    /// otherwise, a <see cref="NotFound"/> result.
    /// </returns>
    public async ValueTask<DeletePersonAsyncResult> DeletePersonAsync(int id)
    {
        var person = await uow.PersonRepository.GetPersonById(id, true);

        if (person == null)
        {
            return new NotFound();
        }

        uow.PersonRepository.RemovePerson(person);
        await uow.SaveChangesAsync();

        return new Success();
    }
}
