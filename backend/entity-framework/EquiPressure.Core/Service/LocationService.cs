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

using GetCityAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Model.City>, OneOf.Types.Error>;
using AddAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Model.Address>,
        ILocationService.InvalidData, OneOf.Types.Error>;

public interface ILocationService
{
    public ValueTask<GetCityAsyncResult> GetCityAsync(int length, string nameFilter);
    public ValueTask<AddAddressAsyncResult> AddAddressAsync(string street, int houseNumber
                                                            , string cityName, string plz);
    public readonly record struct InvalidData;
    
    
}

public class LocationService(EquiContext context) : ILocationService
{
    // yo warum geht das nicht mit IsolationLevel.Snapshot als Parameter
    private Task<IDbContextTransaction> BeginTransactionAsync() 
        => context.Database.BeginTransactionAsync();

    private async ValueTask CommitAsync()
    {
        await context.SaveChangesAsync();
        await context.Database.CommitTransactionAsync();
    }

    private Task RollbackAsync() => context.Database.RollbackTransactionAsync();
    
    public async ValueTask<GetCityAsyncResult> GetCityAsync(int length, string nameFilter)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<AddAddressAsyncResult> AddAddressAsync(string street, int houseNumber, string cityName, string plz)
    {
        throw new NotImplementedException();
    }
}
