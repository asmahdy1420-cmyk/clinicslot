using ClinicSlot.Application.Abstractions;
using ClinicSlot.Infrastructure.Persistence;
using ClinicSlot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicSlot.Infrastructure;

public static class DependencyInjection
{
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">Resolves the SQLite connection string when the first context is created.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        services.AddDbContext<ClinicSlotDbContext>((provider, options) => options.UseSqlite(connectionString(provider)));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IClinicServiceRepository, ClinicServiceRepository>();
        services.AddScoped<IPractitionerRepository, PractitionerRepository>();
        services.AddScoped<ITimeOffRepository, TimeOffRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        return services;
    }
}
