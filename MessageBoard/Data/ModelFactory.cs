using Bogus;
using Microsoft.AspNetCore.Identity;

using MessageBoard.Models;

namespace MessageBoard.Data;

public class ModelFactory
{
    private static readonly Faker _faker = new Faker("en");

    public static User CreateUser(MessageBoardDbContext dbContext,
        string username = null, string email = null, string role = null,
        bool isBanned = false, bool isDeleted = false)
    {
        var now = DateTime.Now;
        var user = new User
        {
            Username = username ?? _faker.Internet.UserName(),
            Email = email ?? _faker.Internet.Email(),
            Role = role,
            IsDeleted = isDeleted,
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

    public static Topic CreateTopic(MessageBoardDbContext dbContext,
        bool isPinned = false, bool isOpen = true, Section section = null,
        User author = null)
    {
        var now = DateTime.Now;
        var topic = new Topic
        {
            Title = _faker.Lorem.Sentence(),
            Content = _faker.Lorem.Sentence(),
            IsPinned = isPinned,
            IsOpen = isOpen,
            CreatedAt = now,
            UpdatedAt = now,
            Section = section ?? CreateSection(dbContext),
            Author = author ?? CreateUser(dbContext),
        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        return topic;
    }

    public static PrivateMessage CreatePrivateMessage(MessageBoardDbContext dbContext,
        User author = null, List<User> participants = null)
    {
        var creator = author ?? CreateUser(dbContext);
        var users = participants;
        if (participants == null)
        {
            users = new List<User> { creator, CreateUser(dbContext) };
        }

        var now = DateTime.Now;
        var message = new PrivateMessage
        {
            Title = _faker.Lorem.Sentence(),
            Content = _faker.Lorem.Paragraph(),
            CreatedAt = now,
            UpdatedAt = now,
            Author = creator,
            Users = users,
        };

        dbContext.PrivateMessages.Add(message);
        dbContext.SaveChanges();

        return message;
    }

    public static Post CreatePost(MessageBoardDbContext dbContext,
        Discussion discussion = null, User author = null)
    {
        var now = DateTime.Now;
        var post = new Post
        {
            Content = _faker.Lorem.Sentence(),
            CreatedAt = now,
            UpdatedAt = now,
            Discussion = discussion ?? CreateTopic(dbContext),
            Author = author ?? CreateUser(dbContext),
        };

        dbContext.Posts.Add(post);
        dbContext.SaveChanges();

        return post;
    }
}
