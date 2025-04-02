using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using playground_check.Model;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    private ClaimsPrincipal _user = null!;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_user ?? _anonymous));
    }

    public void MarkUserAsAuthenticated(User user, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.mailAddress),
            new Claim(ClaimTypes.Role, role),
            new Claim("firstName", user.firstName),
            new Claim("lastName", user.lastName)
        };

        _user = new ClaimsPrincipal(new ClaimsIdentity(claims, "FakeAuth"));

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void MarkUserAsLoggedOut()
    {
        _user = _anonymous;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
