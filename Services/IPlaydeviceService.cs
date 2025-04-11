namespace playground_check.Services;

public interface IPlaydeviceService
{
    Task<string> GetPictureAsync(int playdeviceFid, bool dryRun = false);
    Task PutPictureAsync(int playdeviceFid, string pictureBase64String, bool dryRun);
}
