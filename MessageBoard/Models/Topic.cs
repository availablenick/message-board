namespace MessageBoard.Models;

public class Topic : Discussion
{
    public bool IsPinned { get; set; }
    public bool IsOpen { get; set; }
    public Section Section { get; set; }
}
