namespace MessageBoard.Models;

public class PrivateMessage : Discussion
{
    public List<User> Users { get; set; }
}
