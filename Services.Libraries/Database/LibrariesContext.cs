using Common.Database;
using Microsoft.EntityFrameworkCore;

namespace Services.Notifications.Database;

public sealed class LibrariesContext : TrangaDbContext<LibrariesContext>
{
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
    }
}