using Microsoft.AspNetCore.Authentication;

namespace OpenEnds.BackEnd;

public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string ApiKey { get; set; }
}