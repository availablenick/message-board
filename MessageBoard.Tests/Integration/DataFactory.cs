using Bogus;
using Microsoft.AspNetCore.Identity;

using MessageBoard.Data;
using MessageBoard.Models;

namespace MessageBoard.Tests.Integration;

public class DataFactory
{
    private static readonly Faker _faker = new Faker("en");

    public static async Task<User> CreateUser(MessageBoardDbContext dbContext,
        string? role = null)
    {
        var now = DateTime.Now;
        var user = new User
        {
            Username = _faker.Internet.UserName(),
            Email = _faker.Internet.Email(),
            Role = role,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, "password");

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    public static async Task<Section> CreateSection(MessageBoardDbContext dbContext)
    {
        var now = DateTime.Now;
        var section = new Section
        {
            Name = _faker.Lorem.Word(),
            Description = _faker.Lorem.Sentence(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        await dbContext.Sections.AddAsync(section);
        await dbContext.SaveChangesAsync();

        return section;
    }

    public static async Task<Topic> CreateTopic(MessageBoardDbContext dbContext,
        bool isOpen = true)
    {
        var now = DateTime.Now;
        var user = await CreateUser(dbContext);
        var section = await CreateSection(dbContext);
        var topic = new Topic
        {
            Title = _faker.Lorem.Sentence(),
            Content = _faker.Lorem.Paragraph(),
            IsPinned = false,
            IsOpen = isOpen,
            CreatedAt = now,
            UpdatedAt = now,
            Author = user,
            Section = section,
        };

        await dbContext.Topics.AddAsync(topic);
        await dbContext.SaveChangesAsync();

        return topic;
    }

    public static async Task<PrivateMessage> CreatePrivateMessage(
        MessageBoardDbContext dbContext)
    {
        var author = await CreateUser(dbContext);
        var users = new List<User> { author, await CreateUser(dbContext) };
        var now = DateTime.Now;
        var message = new PrivateMessage
        {
            Title = _faker.Lorem.Sentence(),
            Content = _faker.Lorem.Paragraph(),
            CreatedAt = now,
            UpdatedAt = now,
            Author = author,
            Users = users,
        };

        await dbContext.PrivateMessages.AddAsync(message);
        await dbContext.SaveChangesAsync();

        return message;
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
            Discussion = topic,
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

    public static async Task<Complaint> CreateComplaint(MessageBoardDbContext dbContext)
    {
        var now = DateTime.Now;
        var post = await CreatePost(dbContext);
        var complaint = new Complaint
        {
            Reason = _faker.Lorem.Sentence(),
            CreatedAt = now,
            UpdatedAt = now,
            Author = post.Author,
            Target = post,
        };

        await dbContext.Complaints.AddAsync(complaint);
        await dbContext.SaveChangesAsync();

        return complaint;
    }

    public static async Task<Ban> CreateBan(MessageBoardDbContext dbContext,
        User user = null)
    {
        var now = DateTime.Now;
        var bannedUser = user ?? await CreateUser(dbContext);
        var ban = new Ban
        {
            Reason = _faker.Lorem.Sentence(),
            ExpiresAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            User = bannedUser,
        };

        await dbContext.Bans.AddAsync(ban);
        await dbContext.SaveChangesAsync();

        return ban;
    }
}
