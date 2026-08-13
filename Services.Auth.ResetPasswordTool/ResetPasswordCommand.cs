using Common.Database.Auth;
using Common.Services.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Services.Auth.ResetPasswordTool;

/// <summary>
/// Core logic behind the <c>reset-password</c> CLI, kept separate from <c>Program</c>'s console I/O so it's
/// unit-testable against an in-memory/Sqlite <see cref="AuthContext"/>.
/// </summary>
public static class ResetPasswordCommand
{
    /// <summary>Hashes and stores <paramref name="newPassword"/> as the admin credential, clearing any lockout.</summary>
    public static async Task SetPasswordAsync(AuthContext context, string newPassword, CancellationToken ct)
    {
        DbCredential? existing = await context.Credentials.SingleOrDefaultAsync(ct);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        DbCredential updated = (existing ?? new DbCredential { CreatedAt = now }) with
        {
            PasswordHash = PasswordHasher.Hash(newPassword),
            FailedLoginAttempts = 0,
            LockedUntil = null,
            UpdatedAt = now,
        };

        if (existing is null)
            await context.Credentials.AddAsync(updated, ct);
        else
            context.Entry(existing).CurrentValues.SetValues(updated);

        await context.SaveChangesAsync(ct);
    }

    /// <summary>Deletes the admin credential, if one exists, putting the deployment back into "not configured" state.</summary>
    public static async Task RemoveCredentialAsync(AuthContext context, CancellationToken ct)
    {
        DbCredential? existing = await context.Credentials.SingleOrDefaultAsync(ct);
        if (existing is null)
            return;

        context.Credentials.Remove(existing);
        await context.SaveChangesAsync(ct);
    }
}
