using EquiPressure.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IPersonRepository
{
    public ValueTask<EquestrianMinimalData?> GetPersonAsEquestrianByIdAsync(int id);
    public ValueTask<Address> GetPersonAddressAsync(int id);
    public ValueTask<SaddlerMinimalData?> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId);
    public ValueTask<NameData?> GetNameByIdAsync(int id);
    public ValueTask<IReadOnlyCollection<Person>> GetFavouritesAsync(int id);
    public ValueTask<IReadOnlyCollection<Person>> GetContactsAsync(int id);
    public ValueTask<IReadOnlyCollection<Horse>> GetOwnedHorsesAsync(int id);
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
    public async ValueTask<EquestrianMinimalData?> GetPersonAsEquestrianByIdAsync(int id, bool tracking)
    {
        IQueryable<PersonRoleAssignment> source = tracking ? PersonRoleAssignments : PersonRoleAssignmentsNoTracking;
        var result = await source.Include(pra => pra.Person)
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
        throw new NotImplementedException();
    }

    public ValueTask<Address> GetPersonAddressAsync(int id) => throw new NotImplementedException();

    public ValueTask<SaddlerMinimalData?> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId) => throw new NotImplementedException();

    public ValueTask<NameData?> GetNameByIdAsync(int id) => throw new NotImplementedException();

    public ValueTask<IReadOnlyCollection<Person>> GetFavouritesAsync(int id) => throw new NotImplementedException();

    public ValueTask<IReadOnlyCollection<Person>> GetContactsAsync(int id) => throw new NotImplementedException();

    public ValueTask<IReadOnlyCollection<Horse>> GetOwnedHorsesAsync(int id) => throw new NotImplementedException();

    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetAllDevicesAsync(int personId) => throw new NotImplementedException();

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