using Microsoft.AspNetCore.Http;

using MessageBoard.Filesystem;

namespace MessageBoard.Tests.Fakes;

public class FileHandlerStub : IFileHandler
{
    private readonly string _projectPath;

    public FileHandlerStub()
    {
        _projectPath = $"{Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName}/";
    }

    public string StoreFile(IFormFile file)
    {
        string imageDirectory = $"{_projectPath}Storage/Images";
        if (!Directory.Exists(imageDirectory))
        {
            Directory.CreateDirectory(imageDirectory);
        }

        string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string fileSubpath = $"Storage/Images/{Guid.NewGuid().ToString()}{fileExtension}";
        string filepath = $"{_projectPath}{fileSubpath}";
        using (var fileStream = File.Create(filepath))
        {
            file.CopyTo(fileStream);
        }

        return fileSubpath;
    }
}
