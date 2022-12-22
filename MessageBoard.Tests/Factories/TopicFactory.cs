using Bogus;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Factories;

public class TopicFactory
{
    private static readonly Faker _faker = new Faker("en");

    public static async Task<Topic> CreateTopic(User author, MessageBoardDbContext dbContext)
    {
        var now = DateTime.Now;
        var topic = new Topic
        {
            Title = _faker.Lorem.Sentence(),
            Content = _faker.Lorem.Paragraph(),
            CreatedAt = now,
            UpdatedAt = now,
            Author = author,
        };

        await dbContext.Topics.AddAsync(topic);
        await dbContext.SaveChangesAsync();

        return topic;
    }
}
