namespace MessageBoard.Models;

public class Post
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? Author { get; set; }
    public Topic Topic { get; set; }
}
