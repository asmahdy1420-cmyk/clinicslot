using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClinicSlot.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without starting the web host.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClinicSlotDbContext>
{
    public ClinicSlotDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ClinicSlotDbContext>()
            .UseSqlite("Data Source=clinicslot.db")
            .Options;
        return new ClinicSlotDbContext(options);
    }
}
