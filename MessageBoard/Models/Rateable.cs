namespace MessageBoard.Models;

public abstract class Rateable
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? Author { get; set; }
    public List<Rating> Ratings { get; set; }
    public List<Complaint> Complaints { get; set; }
}
