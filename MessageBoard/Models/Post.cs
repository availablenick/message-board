namespace MessageBoard.Models;

public class Post : Rateable
{
    public Topic? Topic { get; set; }

    public bool IsValid()
    {
        return !String.IsNullOrEmpty(Content);
    }
}
