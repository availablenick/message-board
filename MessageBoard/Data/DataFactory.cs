using Bogus;
using Microsoft.AspNetCore.Identity;

using MessageBoard.Models;

namespace MessageBoard.Data;

public class DataFactory
{
    private static readonly Faker _faker = new Faker("en");

    public static User CreateUser(MessageBoardDbContext dbContext,
        string username = null, string email = null, string role = null)
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
        dbContext.SaveChanges();

        return user;
    }
}
