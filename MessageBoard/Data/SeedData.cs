using MessageBoard.Models;

namespace MessageBoard.Data;

public class SeedData
{
    public static void Initialize(MessageBoardDbContext context)
    {
        if (!context.Users.Any())
        {
            for (int i = 0; i < 5; ++i)
            {
                ModelFactory.CreateUser(context);
            }

            var bannedUser = ModelFactory.CreateUser(context, "ban", "ban@ban.com", null, true);
            var mod = ModelFactory.CreateUser(context, "mod", "mod@mod.com", "Moderator");
            var user = ModelFactory.CreateUser(context, "user", "user@user.com");
            for (int i = 0; i < 5; ++i)
            {
                ModelFactory.CreatePrivateMessage(context, mod, new List<User>()
                {
                    mod,
                    user,
                });
            }

            ModelFactory.CreatePrivateMessage(context, user, new List<User>()
            {
                user,
                mod,
            });

            ModelFactory.CreatePrivateMessage(context, user, new List<User>()
            {
                user,
                bannedUser,
            });
        }

        if (!context.Sections.Any())
        {
            var deletedUser = ModelFactory.CreateUser(context, "deletedUser", "deleted@deleted.com", null, false, true);
            for (int i = 0; i < 5; ++i)
            {
                var section = ModelFactory.CreateSection(context);
                for (int j = 0; j < 5; ++j)
                {
                    var topic = ModelFactory.CreateTopic(context, false, true, section);
                    for (int k = 0; k < 10; ++k)
                    {
                        ModelFactory.CreatePost(context, topic);
                    }

                    ModelFactory.CreatePost(context, topic, deletedUser);
                }

                ModelFactory.CreateTopic(context, false, true, section, deletedUser);
                ModelFactory.CreateTopic(context, true, true, section);
                ModelFactory.CreateTopic(context, true, false, section);
                ModelFactory.CreateTopic(context, false, false, section);
            }
        }
    }
}
