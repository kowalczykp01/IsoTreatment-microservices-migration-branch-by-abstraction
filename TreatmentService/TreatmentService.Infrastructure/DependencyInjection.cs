using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TreatmentService.Domain.Repositories;
using TreatmentService.Domain.UnitOfWork;
using TreatmentService.Infrastructure.Persistence;
using TreatmentService.Infrastructure.Repositories;

namespace TreatmentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TreatmentDb")
            ?? throw new InvalidOperationException(
                "Missing connection string 'ConnectionStrings:TreatmentDb' "
                + "(environment variable: ConnectionStrings__TreatmentDb).");

        services.AddDbContext<TreatmentDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        return services;
    }
}
