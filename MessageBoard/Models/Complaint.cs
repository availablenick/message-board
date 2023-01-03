namespace MessageBoard.Models;

public class Complaint
{
    public int Id { get; set; }
    public string Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? Author { get; set; }
    public Rateable Target { get; set; }

    public bool IsValid()
    {
        if (String.IsNullOrEmpty(Reason))
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
