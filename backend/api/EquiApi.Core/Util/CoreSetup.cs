using EquiApi.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EquiApi.Core.Util;

public static class CoreSetup
{
    public static void ConfigureCore(this IServiceCollection services)
    {
        services.AddSingleton<IClock>(SystemClock.Instance);
        
        services.AddScoped<IRocketService, RocketService>();
    }
}
