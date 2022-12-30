namespace MessageBoard.Filesystem;

public interface IFileHandler
{
    string StoreFile(IFormFile file);
}
