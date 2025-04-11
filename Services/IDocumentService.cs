namespace playground_check.Services;
public interface IDocumentService
{
    Task<byte[]> GetDocumentAsync(int documentFid, string type);
}
