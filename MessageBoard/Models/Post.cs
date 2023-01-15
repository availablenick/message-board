namespace MessageBoard.Models;

public class Post : Rateable
{
    public Discussion? Discussion { get; set; }
}
