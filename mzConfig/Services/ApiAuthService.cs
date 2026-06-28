using System.Net.Http;

namespace mzConfigure.Services;

/// <summary>
/// Provides shared API token access for requests from the MAUI app.
/// </summary>
public static class ApiAuthService
{
    private const string ApiTokenPreferenceKey = "mzspecials_api_token";
    private const string DefaultApiToken = "rpbs$best-cinnamon-buns-ever$";
    private const string ApiTokenEnvironmentKey = "MZSPECIALS_API_TOKEN";

    public static string GetApiToken()
    {
        var envToken = Environment.GetEnvironmentVariable(ApiTokenEnvironmentKey);
        if (!string.IsNullOrWhiteSpace(envToken))
            return envToken.Trim();

        return Preferences.Default.Get(ApiTokenPreferenceKey, DefaultApiToken);
    }

    public static void SetApiToken(string token)
    {
        var normalized = string.IsNullOrWhiteSpace(token) ? DefaultApiToken : token.Trim();
        Preferences.Default.Set(ApiTokenPreferenceKey, normalized);
    }

    public static void ApplyTokenHeader(HttpClient httpClient)
    {
        var token = GetApiToken();
        httpClient.DefaultRequestHeaders.Remove("X-API-Token");
        httpClient.DefaultRequestHeaders.Add("X-API-Token", token);
    }
}
