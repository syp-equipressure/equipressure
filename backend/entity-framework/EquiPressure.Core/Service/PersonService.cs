using System.Runtime.InteropServices.ComTypes;
using Saddle = EquiPressure.Core.Model.Saddle;

namespace EquiPressure.Core.Service;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NodaTime.TimeZones;
using OneOf;
using OneOf.Types;
using EquiPressure.Core.Model;

using GetPersonAsEquestrianByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<EquestrianMinimaldata>, OneOf.Types.NotFound>;
using GetPersonAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Model.Address>, OneOf.Types.NotFound>;
using GetPersonAsSaddlerByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<SaddlerMinimaldata>, IBaseService.InvalidData, OneOf.Types.NotFound>;

public interface IPersonService
{
    public ValueTask<GetPersonAsEquestrianByIdAsyncResult> GetPersonAsEquestrianByIdAsync(int id);
    public ValueTask<GetPersonAddressAsyncResult> GetPersonAddressAsync(int id);
    public ValueTask<GetPersonAsSaddlerByIdAsyncResult> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId);
}

public class PersonService(EquiContext context) : IPersonService
{
    public async ValueTask<GetPersonAsEquestrianByIdAsyncResult> GetPersonAsEquestrianByIdAsync(int id)
    {
        var result = await context.PersonRoleAssignments
                                  .Include(pra => pra.Person)
                                    .ThenInclude(p => p.Address)
                                    .ThenInclude(a => a.City)
                                  .Include(pra => pra.Role)
                                  .Where(pra => pra.Role.Name.ToLower() == "equestrian")
                                  .Where(pra => pra.PersonId == id)
                                  .Select(pra => new EquestrianMinimaldata
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

        return result != null 
            ? new Success<EquestrianMinimaldata>(result) 
            : new NotFound();
    }

    public async ValueTask<GetPersonAddressAsyncResult> GetPersonAddressAsync(int id)
    {
        var result = await context.Person
                                  .Include(p => p.Address)
                                  .ThenInclude(a => a.City)
                                  .Where(p => p.Id == id)
                                  .Select(p => p.Address)
                                  .FirstOrDefaultAsync();
        return result != null
            ? new Success<Address>(result)
            : new NotFound();
    }

    public async ValueTask<GetPersonAsSaddlerByIdAsyncResult> GetPersonAsSaddlerByIdAsync(int saddlerId, int equestrianId)
    {
        var equestrian = GetPersonAsEquestrianByIdAsync(equestrianId);
        if (!equestrian.Result.IsT0)
        {
            return new IBaseService.InvalidData();
        }
        
        var result = await  context.PersonRoleAssignments
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
                                                                                   (r.Person1Id == saddlerId
                                                                         && r.Person2Id == equestrianId) 
                                    || (r.Person1Id == equestrianId && r.Person2Id == saddlerId)))})
                            .Select(p => new SaddlerMinimaldata
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
        
        return result != null
            ? new Success<SaddlerMinimaldata>(result)
                : new NotFound();
    }
}

public record EquestrianMinimaldata(string FirstName, string LastName, string? Street, int? HouseNumber, string City,
                                    string PLZ, string Email, decimal Height, decimal Weight);
                                    
public record SaddlerMinimaldata(string FirstName, string LastName, string? Street, int? HouseNumber, string City,
                                 string PLZ, string? Link, string? Description, bool isFavourite);