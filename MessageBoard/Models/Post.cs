namespace MessageBoard.Models;

public class Post : Rateable
{
    public Topic? Topic { get; set; }
}
