using Microsoft.EntityFrameworkCore;

using MessageBoard.Models;

namespace MessageBoard.Data;

public class MessageBoardDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public MessageBoardDbContext(DbContextOptions<MessageBoardDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}
