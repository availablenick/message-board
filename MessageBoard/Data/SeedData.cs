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

            ModelFactory.CreateUser(context, "mod", "mod@mod.com", "Moderator");
            ModelFactory.CreateUser(context, "ban", "ban@ban.com", null, true);
        }

        if (!context.Sections.Any())
        {
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
                }

                ModelFactory.CreateTopic(context, true, true, section);
                ModelFactory.CreateTopic(context, true, false, section);
                ModelFactory.CreateTopic(context, false, false, section);
            }
        }
    }
}
