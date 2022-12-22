using Bogus;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Factories;

public class UserFactory
{
    private static readonly Faker _faker = new Faker("en");

    public static async Task<User> CreateUser(MessageBoardDbContext dbContext)
    {
        var now = DateTime.Now;
        var user = new User
        {
            Username = _faker.Internet.UserName(),
            Email = _faker.Internet.Email(),
            PasswordHash = "AQAAAAEAACcQAAAAEN0ui+14r0IDonYriVB5PTVPK7aW9VqXJGeQsBkEcmXFPTbOR5vFrMtyy1LTOAwWXg==",
            CreatedAt = now,
            UpdatedAt = now,
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}
