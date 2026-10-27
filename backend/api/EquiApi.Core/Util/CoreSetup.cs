using EquiApi.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EquiApi.Core.Util;

public static class CoreSetup
{
    public static void ConfigureCore(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IHorseService, HorseService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IMeasurementService, MeasurementService>();
        services.AddScoped<IAccessService, AccessService>();
    }
}