using System.Security.Claims;
using playground_check.Model;

namespace playground_check.Services;

public interface IPlaygroundService
{
    Task<PlaygroundFeature[]> GetFeaturesInCollection();
    IEnumerable<Playground> GetOnlyNames(string inspectionType, ClaimsPrincipal user);
    Task<PlaygroundFeature> GetPlaygroundAsFeature(string uuid);
    Playground GetByName(string name, string inspectionType, bool minimal);
}
