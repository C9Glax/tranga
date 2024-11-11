using Microsoft.EntityFrameworkCore;

namespace API.Schema;

public class PgsqlContext(DbContextOptions<PgsqlContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
    }
}