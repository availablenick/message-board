namespace MessageBoard.Models;

public class Post : Rateable
{
    public Discussion? Discussion { get; set; }

    public bool IsValid()
    {
        return !String.IsNullOrEmpty(Content);
    }
}
