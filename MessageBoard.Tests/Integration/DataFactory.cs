using Bogus;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration;

public class DataFactory
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

    public static async Task<Topic> CreateTopic(MessageBoardDbContext dbContext)
    {
        var now = DateTime.Now;
        var topic = new Topic
        {
            Title = _faker.Lorem.Sentence(),
            Content = _faker.Lorem.Paragraph(),
            CreatedAt = now,
            UpdatedAt = now,
            Author = await CreateUser(dbContext),
        };

        await dbContext.Topics.AddAsync(topic);
        await dbContext.SaveChangesAsync();

        return topic;
    }

    public static async Task<Post> CreatePost(MessageBoardDbContext dbContext)
    {
        var now = DateTime.Now;
        var topic = await CreateTopic(dbContext);
        var post = new Post
        {
            Content = _faker.Lorem.Paragraph(),
            CreatedAt = now,
            UpdatedAt = now,
            Author = topic.Author,
            Topic = topic,
        };

        await dbContext.Posts.AddAsync(post);
        await dbContext.SaveChangesAsync();

        return post;
    }

    public static async Task<Rating> CreateRating(int value,
        MessageBoardDbContext dbContext)
    {
        var now = DateTime.Now;
        var post = await CreatePost(dbContext);
        var rating = new Rating
        {
            Value = value,
            CreatedAt = now,
            UpdatedAt = now,
            Owner = post.Author,
            Target = post,
        };

        await dbContext.Ratings.AddAsync(rating);
        await dbContext.SaveChangesAsync();

        return rating;
    }
}
