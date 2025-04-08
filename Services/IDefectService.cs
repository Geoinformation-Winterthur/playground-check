using System.Security.Claims;
using playground_check.Model;

namespace playground_check.Services;

public interface IDefectService
{
    Defect Get(int tid);
    ErrorMessage Update(Defect[] defects,
                ClaimsPrincipal user, bool dryRun = false);
}
