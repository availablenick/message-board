namespace MessageBoard.Models;

public class Section
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Topic> Topics { get; set; }

    public bool IsValid()
    {
        return !String.IsNullOrEmpty(Name);
    }
}
