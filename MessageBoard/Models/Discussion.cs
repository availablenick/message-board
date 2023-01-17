namespace MessageBoard.Models;

public abstract class Discussion : Rateable
{
    public string Title { get; set; }
    public List<Post> Posts { get; set; }
}
