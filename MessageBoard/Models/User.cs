using System.Net.Mail;

namespace MessageBoard.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string? Avatar { get; set; }
    public bool IsDeleted { get; set; }
    public string? Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Ban? Ban { get; set; }
    public List<Topic> Topics { get; set; }
    public List<Post> Posts { get; set; }
    public List<Rating> Ratings { get; set; }
    public List<Complaint> Complaints { get; set; }
    public List<PrivateMessage> CreatedPrivateMessages { get; set; }
    public List<PrivateMessage> PrivateMessages { get; set; }

    public string GetAvatarPath()
    {
        return Avatar ?? "images/default.jpg";
    }

    public bool HasActiveBan()
    {
        return Ban != null && Ban.ExpiresAt.CompareTo(DateTime.Now) > 0;
    }
}
