namespace MessageBoard.Data;

public class SeedData
{
    public static void Initialize(MessageBoardDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }

        for (int i = 0; i < 5; ++i)
        {
            DataFactory.CreateUser(context);
        }

        DataFactory.CreateUser(context, "mod", "mod@mod.com", "Moderator");
    }
}
