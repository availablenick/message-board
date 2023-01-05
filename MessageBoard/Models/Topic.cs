namespace MessageBoard.Models;

public class Topic : Rateable
{
    public string Title { get; set; }
    public bool IsPinned { get; set; }
    public bool IsOpen { get; set; }
    public Section Section { get; set; }
    public List<Post> Posts { get; set; }
}
