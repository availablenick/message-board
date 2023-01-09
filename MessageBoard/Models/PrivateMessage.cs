namespace MessageBoard.Models;

public class PrivateMessage : Discussion
{
    public List<User> Users { get; set; }

    public bool IsValid()
    {
        if (String.IsNullOrEmpty(Title))
        {
            return false;
        }

        if (String.IsNullOrEmpty(Content))
        {
            return false;
        }

        if (Users == null)
        {
            return false;
        }

        return true;
    }

    public override bool CanBePostedOn()
    {
        return true;
    }
}
