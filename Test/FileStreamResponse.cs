namespace Tests.TestModels;

public class FileStreamResponse
{
    public FileStreamResponse(Stream fileStream, string contentType, string fileName)
    {
        FileStream = fileStream;
        ContentType = contentType;
        FileName = fileName;
    }

    public Stream FileStream { get; }
    public string ContentType { get; }
    public string FileName { get; }
}