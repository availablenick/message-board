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
                ModelFactory.CreateSection(context);
            }
        }
    }
}
