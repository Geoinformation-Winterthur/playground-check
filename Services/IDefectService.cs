using System.Security.Claims;
using playground_check.Model;

namespace playground_check.Services;

public interface IDefectService
{
    Defect Get(int tid);
    ErrorMessage Create(Defect defect, ClaimsPrincipal user, bool dryRun = false);
    ErrorMessage Update(Defect defect, ClaimsPrincipal user, bool dryRun = false);
    Task<string> GetPictureAsync(int tid, bool thumb, bool dryrun = false);
    Task PutPictureAsync(DefectPicture defectPic, int defectTid, bool dryRun);
}
