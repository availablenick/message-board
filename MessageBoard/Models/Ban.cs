namespace MessageBoard.Models;

public class Ban
{
    public int Id { get; set; }
    public string Reason { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }

    public bool IsValid()
    {
        if (String.IsNullOrEmpty(Reason))
        {
            return false;
        }

        if (ExpiresAt.CompareTo(DateTime.Now) < 0)
        {
            return false;
        }

        return true;
    }
}
