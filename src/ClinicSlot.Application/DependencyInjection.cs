using ClinicSlot.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClinicSlot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<SchedulingContext>();
        services.AddScoped<ClinicDirectoryService>();
        services.AddScoped<AvailabilityService>();
        services.AddScoped<BookingService>();
        services.AddValidatorsFromAssemblyContaining<ClinicDirectoryService>(ServiceLifetime.Singleton, includeInternalTypes: false);
        return services;
    }
}
