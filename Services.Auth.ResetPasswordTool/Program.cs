using System.Text;
using Common.Database.Auth;
using Common.Database;
using Common.Services.Authentication;
using Microsoft.EntityFrameworkCore;
using Services.Auth.ResetPasswordTool;

if (Console.IsInputRedirected)
{
    Console.Error.WriteLine("This tool needs an interactive terminal - re-run with:");
    Console.Error.WriteLine("  docker exec -it <container> /app/reset-password");
    return 1;
}

Console.WriteLine("Tranga admin password reset");
Console.WriteLine("Leave the password empty to remove it instead - the app will show the first-run setup screen again.");
Console.WriteLine();

string? password = PromptForNewPassword();
if (password is null)
    return 1;

DbContextOptionsBuilder<AuthContext> optionsBuilder = new();
optionsBuilder.Configure(DatabaseContextOptionsBuilder.DbType.Postgresql);
await using AuthContext context = new(optionsBuilder.Options);

try
{
    if (password.Length == 0)
    {
        Console.Write("This removes the password entirely; anyone will be able to run setup again. Continue? [y/N] ");
        string? confirm = Console.ReadLine();
        if (!string.Equals(confirm?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Aborted, nothing changed.");
            return 1;
        }

        await ResetPasswordCommand.RemoveCredentialAsync(context, CancellationToken.None);
        Console.WriteLine("Password removed. The app will prompt for first-run setup on next load.");
    }
    else
    {
        await ResetPasswordCommand.SetPasswordAsync(context, password, CancellationToken.None);
        Console.WriteLine("Password updated.");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to update the database: {ex.Message}");
    return 1;
}

return 0;

static string? PromptForNewPassword()
{
    const int maxTries = 3;
    for (int attempt = 1; attempt <= maxTries; attempt++)
    {
        string first = ReadMaskedLine("New admin password (leave empty to remove it): ");
        if (first.Length == 0)
            return first;

        if (first.Length < PasswordHasher.MinPasswordLength)
        {
            Console.WriteLine($"Password must be at least {PasswordHasher.MinPasswordLength} characters.");
            continue;
        }

        string confirm = ReadMaskedLine("Confirm password: ");
        if (first != confirm)
        {
            Console.WriteLine("Passwords did not match.");
            continue;
        }

        return first;
    }

    Console.Error.WriteLine("Too many failed attempts.");
    return null;
}

static string ReadMaskedLine(string prompt)
{
    Console.Write(prompt);
    StringBuilder buffer = new();
    while (true)
    {
        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return buffer.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (buffer.Length > 0)
            {
                buffer.Length--;
                Console.Write("\b \b");
            }
            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            buffer.Append(key.KeyChar);
            Console.Write('*');
        }
    }
}
