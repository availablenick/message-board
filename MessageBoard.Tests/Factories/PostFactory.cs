using Bogus;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Factories;

public class PostFactory
{
    private static readonly Faker _faker = new Faker("en");

    public static async Task<Post> CreatePost(User author, Topic topic,
        MessageBoardDbContext dbContext)
    {
        var now = DateTime.Now;
        var post = new Post
        {
            Content = _faker.Lorem.Paragraph(),
            CreatedAt = now,
            UpdatedAt = now,
            Author = author,
            Topic = topic,
        };

        await dbContext.Posts.AddAsync(post);
        await dbContext.SaveChangesAsync();

        return post;
    }
}
