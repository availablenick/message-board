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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Topic> Topics { get; set; }
    public List<Post> Posts { get; set; }

    public static bool DataIsValidForCreation(string username, string email,
        string password, string avatarFilename)
    {
        if (!UsernameIsValid(username) ||
            !PasswordIsValid(password) ||
            !EmailIsValid(email) ||
            !AvatarIsValid(avatarFilename))
        {
            return false;
        }

        return true;
    }

    public static bool DataIsValidForUpdate(string username, string email,
        string avatarFilename)
    {
        if (!UsernameIsValid(username) ||
            !EmailIsValid(email) ||
            !AvatarIsValid(avatarFilename))
        {
            return false;
        }

        return true;
    }

    private static bool UsernameIsValid(string username)
    {
        if (String.IsNullOrEmpty(username))
        {
            return false;
        }

        return true;
    }

    private static bool PasswordIsValid(string password)
    {
        if (String.IsNullOrEmpty(password))
        {
            return false;
        }

        return true;
    }

    private static bool EmailIsValid(string email)
    {
        try
        {
            MailAddress address = new MailAddress(email);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    private static bool AvatarIsValid(string filename)
    {
        if (filename == null)
        {
            return true;
        }

        string[] allowedExtensions = new string[] { ".jpg", ".jpeg", ".png" };
        string extension = Path.GetExtension(filename).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
        {
            return false;
        }

        return true;
    }
}
