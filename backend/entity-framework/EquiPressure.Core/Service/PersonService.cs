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

using GetPersonByIdAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Model.City[]>, OneOf.Types.Error>;
using GetPersonAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Model.Address>,
        ILocationService.InvalidData, OneOf.Types.Error>;

public interface IPersonService
{
    public ValueTask<GetPersonByIdAsyncResult> GetPersonByIdAsync(int id);
    public ValueTask<GetPersonAddressAsyncResult> GetPersonAddressAsync(int id);
}

public class PersonService : IPersonService
{
    public ValueTask<GetPersonByIdAsyncResult> GetPersonByIdAsync(int id) => throw new NotImplementedException();

    public ValueTask<GetPersonAddressAsyncResult> GetPersonAddressAsync(int id) => throw new NotImplementedException();
}
