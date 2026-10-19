using ClinicSlot.Application.Abstractions;
using ClinicSlot.Application.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClinicSlot.Infrastructure.Persistence;

internal sealed class UnitOfWork(ClinicSlotDbContext db) : IUnitOfWork
{
    private const int SqliteConstraintError = 19;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("The appointment was changed by another request. Reload it and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintError } sqlite)
        {
            if (sqlite.Message.Contains("SlotClaims", StringComparison.Ordinal))
            {
                throw new SlotConflictException(ex);
            }

            throw new ConflictException("The change conflicts with existing data.", ex);
        }
    }
}
