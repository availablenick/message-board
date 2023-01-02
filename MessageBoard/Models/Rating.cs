namespace MessageBoard.Models;

public class Rating
{
    public int Id { get; set; }
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User Owner { get; set; }
    public Rateable Target { get; set; }

    public bool IsValid()
    {
        if (Value != 1 && Value != -1)
        {
            return false;
        }

        if (Target == null)
        {
            return false;
        }

        return true;
    }
}
