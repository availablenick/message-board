using Microsoft.EntityFrameworkCore;

using MessageBoard.Models;

namespace MessageBoard.Data;

public class MessageBoardDbContext : DbContext
{
    public MessageBoardDbContext(DbContextOptions<MessageBoardDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}
