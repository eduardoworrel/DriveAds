using Microsoft.EntityFrameworkCore;

namespace webapp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Advertisement> Advertisements { get; set; }

    public DbSet<Track> Tracks { get; set; }
}
