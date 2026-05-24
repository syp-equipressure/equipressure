using EquiApi.Core.Util;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;
using EquiApi.Util;
using EquiPressure.Core.Service;
using Library.Core;
using OneOf.Types;

namespace EquiApi.Core.Services;

using GetPersonAsEquestrianByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<DataTransfer.EquestrianBasicData>, OneOf.Types.NotFound>;
using GetAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Address>, OneOf.Types.NotFound>;
using GetPersonAsSaddlerByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<DataTransfer.SaddlerBasicData>, IBaseService.InvalidData, OneOf.Types.NotFound>;
using GetNameByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<DataTransfer.NameData>, OneOf.Types.NotFound>;
using GetFavouritesOrContactsAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<IReadOnlyCollection<Person>>, OneOf.Types.None, OneOf.Types.NotFound>;
using GetOwnedHorsesAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<IReadOnlyCollection<Horse>>, OneOf.Types.None, OneOf.Types.NotFound>;
using GetAllDevicesAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<List<MeasurementDevice>>, OneOf.Types.None, OneOf.Types.NotFound>;
using GetAllSaddlersAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<List<DataTransfer.SaddlerBasicData>>, OneOf.Types.None>;
using GetAllSaddlersOfEquestrianAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<List<DataTransfer.SaddlerBasicData>>, OneOf.Types.None, OneOf.Types.NotFound>;
// TODO: why would we get a InvalidData if we only check the data in the controller?
using AddPersonAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Person>, IBaseService.InvalidData, IBaseService.Conflict>;
using UpdatePersonAsyncResult
    = OneOf.OneOf<OneOf.Types.Success, OneOf.Types.NotFound, IBaseService.InvalidData, IBaseService.Conflict>;
using DeletePersonAsyncResult
    = OneOf.OneOf<OneOf.Types.Success, OneOf.Types.NotFound>;

public interface IPersonService
{
    /// <summary>
    /// Retrieves the data of a person formatted for an equestrian user view.
    /// </summary>
    /// <param name="personId">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success{EquestrianBasicData}"/> containing the data, 
    /// or <see cref="NotFound"/> if the person does not exist.
    /// </returns>
    public ValueTask<GetPersonAsEquestrianByIdAsyncResult> GetPersonAsEquestrianByIdAsync(int personId);

    /// <summary>
    /// Retrieves the specific address for a person.
    /// </summary>
    /// <param name="personId">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success{Address}"/> containing the address, 
    /// or <see cref="NotFound"/> if the person or their address is missing.
    /// </returns>
    public ValueTask<GetAddressAsyncResult> GetPersonAddressAsync(int personId);

    /// <summary>
    /// Retrieves the specific Saddler
    /// </summary>
    /// <param name="saddlerId">The id of the saddler.</param>
    /// <param name="equestrianId">The id of the equestrian.</param>
    /// <returns>
    /// A <see cref="Success{Saddler}"/> containing the Saddler Basic Data,
    /// <see cref="IBaseService.InvalidData"/> Invalid Ids
    /// or <see cref="NotFound"/> if the person(s) are not found.
    /// </returns>
    public ValueTask<GetPersonAsSaddlerByIdAsyncResult> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId);

    /// <summary>
    /// Retrieves the First and Last name for a person.
    /// </summary>
    /// <param name="personId">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success{NameData}"/> if found,
    /// or <see cref="NotFound"/> if the ID is unknown.
    /// </returns>
    public ValueTask<GetNameByIdAsyncResult> GetNameByIdAsync(int personId);

    /// <summary>
    /// Retrieves the list of persons marked as favorites by the specified user.
    /// </summary>
    /// <param name="personId">The id of the perosn</param>
    /// <returns>
    /// A collection of <see cref="Person"/> entities,
    /// <see cref="None"/> if the collection is empty, 
    /// or <see cref="NotFound"/> if the user does not exist.
    /// </returns>
    public ValueTask<GetFavouritesOrContactsAsyncResult> GetFavouritesAsync(int personId);

    /// <summary>
    /// Retrieves the list of contacts associated with the specified user.
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>
    /// A collection of <see cref="Person"/> entities,
    /// <see cref="None"/> if the collection is empty, 
    /// or <see cref="NotFound"/> if the user does not exist.
    /// </returns>
    public ValueTask<GetFavouritesOrContactsAsyncResult> GetContactsAsync(int personId);

    /// <summary>
    /// Retrieves all horses owned by the person.
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>
    /// A collection of <see cref="Horse"/> entities,
    /// <see cref="None"/> if the collection is empty,
    /// or <see cref="NotFound"/> if the person does not exist.
    /// </returns>
    public ValueTask<GetOwnedHorsesAsyncResult> GetOwnedHorsesAsync(int personId);

    /// <summary>
    /// Retrieves a list of all devices registered to a specific person.
    /// </summary>
    /// <param name="personId">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success{List}"/> of devices, 
    /// <see cref="None"/> if no devices are registered, 
    /// or <see cref="NotFound"/> if the person does not exist.
    /// </returns>
    public ValueTask<GetAllDevicesAsyncResult> GetAllDevicesByPersonAsync(int personId);

    /// <summary>
    /// Retrieves a list of all saddlers with their address
    /// </summary>
    /// <returns>
    /// A <see cref="Success{List}"/> of saddlers, 
    /// <see cref="None"/> if no saddlers exist, 
    /// or <see cref="NotFound"/> if the person does not exist.
    /// </returns>
    public ValueTask<GetAllSaddlersAsyncResult> GetAllSaddlersAsync();
    
    /// <summary>
    /// Retrieves a list of all favourites of an equestrian that are saddlers + their address
    /// </summary>
    /// <param name="equestrianId">The id of the equestrian.</param>
    /// <returns>
    /// A <see cref="Success{List}"/> of saddlers, 
    /// <see cref="None"/> if no saddlers exist, 
    /// or <see cref="NotFound"/> if the person does not exist.
    /// </returns>
    public ValueTask<GetAllSaddlersOfEquestrianAsyncResult> GetAllSaddlerFavouritesAsync(int equestrianId);
    
    /// <summary>
    /// Retrieves a list of all contacts of an equestrian that are saddlers + their address
    /// </summary>
    /// <param name="equestrianId">The id of the equestrian.</param>
    /// <returns>
    /// A <see cref="Success{List}"/> of saddlers, 
    /// <see cref="None"/> if no saddlers exist, 
    /// or <see cref="NotFound"/> if the person does not exist.
    /// </returns>
    public ValueTask<GetAllSaddlersOfEquestrianAsyncResult> GetAllSaddlerContactsAsync(int equestrianId);

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
    public ValueTask<AddPersonAsyncResult> AddPersonAsync(string firstName, string lastName, decimal height,
                                                          decimal weight, LocalDate dateOfBirth, string? email,
                                                          string? websiteLink, string? description, Address address,
                                                          AccountRole role);

    /// <summary>
    /// Updates the information for an existing person.
    /// </summary>
    /// <param name="personId">The person id to be updated</param>
    /// <param name="firstName">The new person firstname that we maybe update</param>
    /// <param name="lastName">The new person lastname that we maybe update</param>
    /// <param name="height">The new person height that we maybe update</param>
    /// <param name="weight">The new person weight that we maybe update</param>
    /// <param name="dateOfBirth">The new person dob that we maybe update</param>
    /// <param name="email">The new person email that we maybe update</param>
    /// <param name="websiteLink">The new person websiteLink that we maybe update</param>
    /// <param name="description">The new person description that we maybe update</param>
    /// <param name="address">The new person address object that we maybe update</param>
    /// <param name="roles">The new person roles that we maybe update</param>
    /// <returns>
    /// A <see cref="Success{Person}"/> if updated, 
    /// <see cref="NotFound"/> if the person does not exist, 
    /// <see cref="IBaseService.InvalidData"/> for invalid field values, 
    /// or <see cref="IBaseService.Conflict"/> if the new email is claimed by another user.
    /// </returns>
    public ValueTask<UpdatePersonAsyncResult> UpdatePersonAsync(int personId, string? firstName, string? lastName,
                                                                decimal? height, decimal? weight,
                                                                LocalDate? dateOfBirth,
                                                                string? email, string? websiteLink,
                                                                string? description, Address? address,
                                                                List<PersonRoleAssignment>? roles);

    /// <summary>
    /// Removes a person from the system.
    /// </summary>
    /// <param name="personId">The id of the person.</param>
    /// <returns>
    /// A <see cref="Success"/> result if the deletion was successful; 
    /// otherwise, a <see cref="NotFound"/> result.
    /// </returns>
    public ValueTask<DeletePersonAsyncResult> DeletePersonAsync(int personId);
}

public class PersonService(IUnitOfWork uow, IDateTimeProvider dateTimeProvider, ILogger<PersonService> logger)
    : IPersonService
{
    public async ValueTask<GetPersonAsEquestrianByIdAsyncResult> GetPersonAsEquestrianByIdAsync(int personId)
    {
        var result = await uow.PersonRepository.GetPersonAsEquestrianByIdAsync(personId);

        if (result is null)
        {
            logger.LogWarning("Equestrian could not be found");

            return new NotFound();
        }

        logger.LogInformation("Equestrian successfully got");

        return new Success<DataTransfer.EquestrianBasicData>(result);
    }

    public async ValueTask<GetAddressAsyncResult> GetPersonAddressAsync(int personId)
    {
        var result = await uow.PersonRepository.GetPersonAddressAsync(personId);

        if (result is null)
        {
            logger.LogWarning("Address could not be found");

            return new NotFound();
        }

        logger.LogInformation("Address successfully got");

        return new Success<Address>(result);
    }

    public async ValueTask<GetPersonAsSaddlerByIdAsyncResult> GetPersonAsSaddlerByIdAsync(
        int saddlerId, int equestrianId)
    {
        // TODO: Fix return type, seperate notFounds for each person
        if (!await uow.PersonRepository.PersonExistsAsync(equestrianId))
        {
            logger.LogWarning("Data is invalid");

            return new IBaseService.InvalidData();
        }

        var result = await uow.PersonRepository.GetPersonAsSaddlerByIdAsync(saddlerId, equestrianId);

        if (result is null)
        {
            logger.LogWarning("Saddler could not be found");

            return new NotFound();
        }

        logger.LogInformation("Saddler successfully got");

        return new Success<DataTransfer.SaddlerBasicData>(result);
    }

    public async ValueTask<GetNameByIdAsyncResult> GetNameByIdAsync(int personId)
    {
        var result = await uow.PersonRepository.GetNameByIdAsync(personId);

        if (result is null)
        {
            logger.LogWarning("Person could not be found");

            return new NotFound();
        }

        logger.LogInformation("Name Data successfully got");

        return new Success<DataTransfer.NameData>(result);
    }

    public async ValueTask<GetFavouritesOrContactsAsyncResult> GetFavouritesAsync(int personId)
    {
        bool personExists = await uow.PersonRepository.PersonExistsAsync(personId);
        if (!personExists)
        {
            logger.LogWarning("Person could not be found");

            return new NotFound();
        }

        IReadOnlyCollection<Person> result = await uow.PersonRepository.GetFavouritesAsync(personId);

        if (result.Count <= 0)
        {
            logger.LogWarning("List of Favourites is empty");

            return new None();
        }

        logger.LogInformation("List of Favourites successfully got");

        return new Success<IReadOnlyCollection<Person>>(result);
    }

    public async ValueTask<GetFavouritesOrContactsAsyncResult> GetContactsAsync(int personId)
    {
        bool personExists = await uow.PersonRepository.PersonExistsAsync(personId);
        if (!personExists)
        {
            logger.LogWarning("Person could not be found");

            return new NotFound();
        }

        IReadOnlyCollection<Person> result = await uow.PersonRepository.GetContactsAsync(personId);

        if (result.Count <= 0)
        {
            logger.LogWarning("List of Contacts is empty");

            return new None();
        }

        logger.LogInformation("List of Contacts successfully got");

        return new Success<IReadOnlyCollection<Person>>(result);
    }

    public async ValueTask<GetOwnedHorsesAsyncResult> GetOwnedHorsesAsync(int personId)
    {
        bool personExists = await uow.PersonRepository.PersonExistsAsync(personId);
        if (!personExists)
        {
            logger.LogWarning("Person could not be found");

            return new NotFound();
        }

        IReadOnlyCollection<Horse> result = await uow.PersonRepository.GetOwnedHorsesAsync(personId);

        if (result.Count <= 0)
        {
            logger.LogWarning("List of owned Horses is empty");

            return new None();
        }

        logger.LogInformation("List of owned Horses successfully got");

        return new Success<IReadOnlyCollection<Horse>>(result);
    }

    public async ValueTask<GetAllDevicesAsyncResult> GetAllDevicesByPersonAsync(int personId)
    {
        if (!await uow.PersonRepository.PersonExistsAsync(personId))
        {
            logger.LogWarning("Person could not be found");

            return new NotFound();
        }

        IReadOnlyCollection<MeasurementDevice> result = await uow.PersonRepository.GetAllDevicesAsync(personId);

        if (result.Count <= 0)
        {
            logger.LogWarning("List of Devices is empty");

            return new None();
        }

        logger.LogInformation("List of Devices successfully got");

        return new Success<List<MeasurementDevice>>(result.ToList());
    }

    public async ValueTask<GetAllSaddlersOfEquestrianAsyncResult> GetAllSaddlerFavouritesAsync(int equestrianId)
    {
        bool personExists = await uow.PersonRepository.PersonExistsAsync(equestrianId);

        if (!personExists)
        {
            logger.LogWarning("equestrian doesnt exist");
            return new NotFound();
        }
        
        IReadOnlyCollection<DataTransfer.SaddlerBasicData> saddlers
            = await uow.PersonRepository.GetAllSaddlerFavouritesOfEquestrianAsync(equestrianId);

        if (saddlers.Count <= 0)
        {
            logger.LogWarning("List of saddlers is empty");
            return new None();
        }

        logger.LogInformation("Successfully got list of saddlers");
        return new Success<List<DataTransfer.SaddlerBasicData>>(saddlers.ToList());
    }

    public async ValueTask<GetAllSaddlersOfEquestrianAsyncResult> GetAllSaddlerContactsAsync(int equestrianId)
    {
        bool personExists = await uow.PersonRepository.PersonExistsAsync(equestrianId);

        if (!personExists)
        {
            logger.LogWarning("equestrian doesnt exist");
            return new NotFound();
        }
        
        IReadOnlyCollection<DataTransfer.SaddlerBasicData> saddlers
            = await uow.PersonRepository.GetAllSaddlerContactsOfEquestrianAsync(equestrianId);

        if (saddlers.Count <= 0)
        {
            logger.LogWarning("List of saddlers is empty");
            return new None();
        }

        logger.LogInformation("Successfully got list of saddlers");
        return new Success<List<DataTransfer.SaddlerBasicData>>(saddlers.ToList());
    }

    public async ValueTask<GetAllSaddlersAsyncResult> GetAllSaddlersAsync()
    {
        IReadOnlyCollection<DataTransfer.SaddlerBasicData> saddlers
            = await uow.PersonRepository.GetAllSaddlersAsync();

        if (saddlers.Count <= 0)
        {
            logger.LogWarning("List of saddlers is empty");
            return new None();
        }

        logger.LogInformation("Successfully got list of saddlers");
        return new Success<List<DataTransfer.SaddlerBasicData>>(saddlers.ToList());
    }

    public async ValueTask<AddPersonAsyncResult> AddPersonAsync(string firstName, string lastName, decimal height,
                                                                decimal weight, LocalDate dateOfBirth,
                                                                string? email, string? websiteLink, string? description,
                                                                Address address, AccountRole role)
    {
        if (height <= 0 || weight <= 0 || dateOfBirth >= dateTimeProvider.GetCurrentDate())
        {
            logger.LogWarning("Data is invalid");

            return new IBaseService.InvalidData();
        }

        if (email != null && await uow.PersonRepository.PersonWithEmailExistsAsync(email))
        {
            logger.LogWarning("Person with Email already exists");

            return new IBaseService.Conflict();
        }

        if (role.Name == RoleName.Equestrian && !(websiteLink is null && description is null))
        {
            logger.LogWarning("Person with Role Equestrian cannot be added with websitelink and description");
            // TODO: eventuell anderer Return type
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
        logger.LogInformation("Person successfully added");

        return new Success<Person>(person);
    }

    public async ValueTask<UpdatePersonAsyncResult> UpdatePersonAsync(int personId, string? firstName, string? lastName,
                                                                      decimal? height, decimal? weight,
                                                                      LocalDate? dateOfBirth,
                                                                      string? email, string? websiteLink,
                                                                      string? description, Address? address,
                                                                      List<PersonRoleAssignment>? roles)
    {
        var person = await uow.PersonRepository.GetPersonByIdAsync(personId);

        if (person is null)
        {
            logger.LogWarning("Person could not be found");

            return new NotFound();
        }

        if (height is <= 0 || weight is <= 0 || (dateOfBirth.HasValue && dateOfBirth >= dateTimeProvider.GetCurrentDate()))
        {
            logger.LogWarning("invalid data");

            return new IBaseService.InvalidData();
        }

        if (email != null && await uow.PersonRepository.IsEmailTakenByAnotherUserAsync(email, personId))
        {
            logger.LogWarning("email is already taken by another user");

            return new IBaseService.Conflict();
        }

        Helper.UpdateIfNotNull(firstName, x => person.FirstName = x);
        Helper.UpdateIfNotNull(lastName, x => person.LastName = x);
        Helper.UpdateIfHasValue(height, x => person.Height = x);
        Helper.UpdateIfHasValue(weight, x => person.Weight = x);
        Helper.UpdateIfHasValue(dateOfBirth, x => person.DateOfBirth = x);
        Helper.UpdateIfNotNull(email, x => person.Email = x);
        Helper.UpdateIfNotNull(websiteLink, x => person.WebsiteLink = x);
        Helper.UpdateIfNotNull(description, x => person.Description = x);

        if (address is not null)
        {
            person.AddressId = address.Id;
            person.Address = address;
        }

        if (roles is not null && roles.Count > 0)
        {
            person.Roles = roles;
        }

        await uow.SaveChangesAsync();
        logger.LogInformation("Person successfully updated");

        return new Success();
    }

    public async ValueTask<DeletePersonAsyncResult> DeletePersonAsync(int personId)
    {
        var person = await uow.PersonRepository.GetPersonByIdAsync(personId);

        if (person == null)
        {
            logger.LogWarning("Person could not be found");

            return new NotFound();
        }

        uow.PersonRepository.RemovePerson(person);
        await uow.SaveChangesAsync();
        logger.LogInformation("Person successfully removed");

        return new Success();
    }
}


