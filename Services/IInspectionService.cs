using System.Security.Claims;
using playground_check.Model;

namespace playground_check.Services;

public interface IInspectionService
{
    ErrorMessage SendReport(InspectionReport[] inspectionReports,
                        ClaimsPrincipal user, bool dryRun = false);
}
