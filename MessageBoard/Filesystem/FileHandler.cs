using System.IO;

namespace MessageBoard.Filesystem;

public class FileHandler : IFileHandler
{
    private readonly IWebHostEnvironment _env;

    public FileHandler(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string StoreFile(IFormFile file)
    {
        string imageDirectory = $"{_env.WebRootPath}/images";
        if (!Directory.Exists(imageDirectory))
        {
            Directory.CreateDirectory(imageDirectory);
        }

        string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string fileSubpath = $"images/{Guid.NewGuid().ToString()}{fileExtension}";
        string filepath = $"{_env.WebRootPath}/{fileSubpath}";
        using (var fileStream = File.Create(filepath))
        {
            file.CopyTo(fileStream);
        }

        return fileSubpath;
    }
}
