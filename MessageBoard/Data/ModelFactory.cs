using Bogus;
using Microsoft.AspNetCore.Identity;

using MessageBoard.Models;

namespace MessageBoard.Data;

public class ModelFactory
{
    private static readonly Faker _faker = new Faker("en");

    public static User CreateUser(MessageBoardDbContext dbContext,
        string username = null, string email = null, string role = null,
        bool isBanned = false)
    {
        var now = DateTime.Now;
        var user = new User
        {
            Username = username ?? _faker.Internet.UserName(),
            Email = email ?? _faker.Internet.Email(),
            Role = role,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, "password");
        dbContext.Users.Add(user);

        if (isBanned)
        {
            var nowAgain = DateTime.Now;
            var ban = new Ban
            {
                Reason = _faker.Lorem.Sentence(),
                ExpiresAt = nowAgain.AddHours(2),
                CreatedAt = nowAgain,
                UpdatedAt = nowAgain,
                User = user,
            };

            dbContext.Bans.Add(ban);
        }

        dbContext.SaveChanges();

        return user;
    }

    public static Section CreateSection(MessageBoardDbContext dbContext,
        string name = null)
    {
        var now = DateTime.Now;
        var section = new Section
        {
            Name = name ?? _faker.Lorem.Word(),
            Description = _faker.Lorem.Sentence(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Sections.Add(section);
        dbContext.SaveChanges();

        return section;
    }
}
