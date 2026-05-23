using EquiApi.Core.Services;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;
using Microsoft.Extensions.DependencyInjection;

namespace EquiApi.Core.Util;

public static class CoreSetup
{
    public static void ConfigureCore(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IRocketService, RocketService>();
        services.AddScoped<IHorseService, HorseService>();
        services.AddScoped<IDeviceService, DeviceService>();

    }
}
