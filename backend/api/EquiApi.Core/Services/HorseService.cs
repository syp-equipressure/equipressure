using System.Runtime.InteropServices.JavaScript;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using OneOf;
using OneOf.Types;


namespace EquiApi.Core.Services;

public interface IHorseService
{
    /// <summary>
    /// Returns all the horses where one person is the owner
    /// </summary>
    /// <param name="personId">the id of the owner</param>
    /// <returns>
    /// a list of horses which have the personId as Owner of a notFound if the person with the id is not found
    /// </returns>
    public ValueTask<OneOf<IReadOnlyCollection<Horse>, NotFound>> GetAllHorsesOfPersonAsync(int personId);

    /// <summary>
    /// Returns a horse by Id
    /// </summary>
    /// <param name="id">the id of the horse</param>
    /// <returns>
    /// a horse with the id
    /// </returns>
    public ValueTask<OneOf<Horse, NotFound>> GetHorseByIdAsync(int id);
    
    /// <summary>
    /// Adds a new horse
    /// </summary>
    public ValueTask<OneOf<Success<Horse>, IBaseService.InvalidData, NotFound>> AddHorse(string name, LocalDate dob, decimal weight, decimal height, HorseGender gender, Address address, List<HorseBreed> breeds);
}

public class HorseService(IUnitOfWork uow, ILogger<HorseService> logger, IDateTimeProvider dateTimeProvider) : IHorseService
{
    public async ValueTask<OneOf<IReadOnlyCollection<Horse>, NotFound>> GetAllHorsesOfPersonAsync(int personId)
    {
        if (await uow.PersonRepository.PersonExists(personId))
        {
            logger.LogInformation("Person with id {id} could not be found", personId);
            return new NotFound();
        }

        var res = await uow.HorseRepository.GetAllHorsesOfUserAsync(personId);

        return res.ToArray();
    }

    public async ValueTask<OneOf<Horse, NotFound>> GetHorseByIdAsync(int id)
    {

        var horse = await uow.HorseRepository.GetHorseByIdAsync(id);

        if (horse is null)
        {
            logger.LogInformation("Horse with id {id} could not be found", id);

            return new NotFound();
        }

        return horse;
    }


    public async ValueTask<OneOf<Success<Horse>, IBaseService.InvalidData, NotFound>> AddHorse(string name, LocalDate dateOfBirth, decimal weight,
                                                               decimal height, HorseGender gender, Address address,
                                                               List<HorseBreed> breeds)
    {
        
        //TODO Owner ID 
        
        if (height <= 0 || weight <= 0 || dateOfBirth >= dateTimeProvider.GetCurrentDate() || breeds.Count <= 0)
        {
            logger.LogWarning("Data is invalid");

            return new IBaseService.InvalidData();
        }

        var horse = new Horse
        {
            Name = name,
            DateOfBirth = dateOfBirth,
            Weight = weight,
            Height = height,
            Gender = gender,
            HorseBreeds = breeds,
            Address = address
        };

        return new Success<Horse>(horse);
    }

    
}
