namespace MessageBoard.Models;

public class Topic : Rateable
{
    public string Title { get; set; }
    public List<Post> Posts { get; set; }
}
