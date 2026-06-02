namespace Looma.Presentation.Services;

public interface IDocumentFilePicker
{
    Task<string?> PickDocumentAsync();
}
