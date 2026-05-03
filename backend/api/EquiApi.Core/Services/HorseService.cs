using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using OneOf;
using OneOf.Types;


namespace EquiApi.Core.Services;

public interface IHorseService
{
    public ValueTask<OneOf<IReadOnlyCollection<Horse>, NotFound>> GetAllHorsesOfPersonAsync(int personId);
}

public class HorseService(IUnitOfWork uow, ILogger<HorseService> logger) : IHorseService
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
}
