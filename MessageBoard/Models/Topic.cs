namespace MessageBoard.Models;

public class Topic : Discussion
{
    public bool IsPinned { get; set; }
    public bool IsOpen { get; set; }
    public Section Section { get; set; }

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

        return true;
    }

    public override bool CanBePostedOn()
    {
        return IsOpen;
    }
}
