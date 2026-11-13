using System.Net;
using System.Text;
using System.Text.Json;

namespace ApiPNETLab.Client;

public class ApiPNETAuthentication
{
    public async Task<(bool Success, HttpStatusCode StatusCode, string Message)> Authenticate(
        ApiPNETHttpClient client,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        // 1. Krok: Inicializace session a XSRF-TOKEN cookie
        var initResponse = await client.Client.GetAsync(client.BaseUri, cancellationToken);
        if (!initResponse.IsSuccessStatusCode)
        {
            return (false, initResponse.StatusCode, $"Failed to initialize PNETLab session ({initResponse.StatusCode}).");
        }

        if (string.IsNullOrEmpty(client.GetXsrfToken()))
        {
            return (false, initResponse.StatusCode, "XSRF-TOKEN cookie was not returned by the server.");
        }

        // 2. Krok: POST přihlášení (XsrfTokenHandler automaticky doplní X-XSRF-TOKEN hlavičku)
        var loginUrl = $"{client.StoreUrl}auth/login/login";
        var authData = new
        {
            username = username,
            password = password,
            html = "0",
            captcha = ""
        };

        var content = new StringContent(
            JsonSerializer.Serialize(authData),
            Encoding.UTF8,
            "application/json");

        var response = await client.Client.PostAsync(loginUrl, content, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            return (false, response.StatusCode, "Invalid credentials.");
        }

        if ((int)response.StatusCode == 419)
        {
            return (false, response.StatusCode, "CSRF token validation failed (HTTP 419).");
        }

        if ((int)response.StatusCode == 422)
        {
            return (false, response.StatusCode, $"Validation failed (check if Captcha is disabled): {ExtractMessage(body)}");
        }

        if (!response.IsSuccessStatusCode)
        {
            return (false, response.StatusCode, $"Authentication failed ({response.StatusCode}).");
        }

        // Kontrola JSON odpovědi pro případ, že server vrátí 200 OK s chybovým příznakem v těle
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("result", out var resultProp) &&
                    resultProp.ValueKind == JsonValueKind.False)
                {
                    return (false, HttpStatusCode.Unauthorized, ExtractMessage(body) ?? "Invalid credentials.");
                }

                if (root.TryGetProperty("status", out var statusProp) &&
                    statusProp.ValueKind == JsonValueKind.String &&
                    (statusProp.GetString()?.Equals("fail", StringComparison.OrdinalIgnoreCase) == true ||
                     statusProp.GetString()?.Equals("error", StringComparison.OrdinalIgnoreCase) == true))
                {
                    return (false, HttpStatusCode.Unauthorized, ExtractMessage(body) ?? "Authentication failed.");
                }
            }
            catch (JsonException)
            {
                // Pokud server při úspěchu nevrací JSON (např. prázdné tělo nebo HTML redirect mimo XHR),
                // ale HttpStatusCode je 2xx a cookies jsou nastaveny, pokračujeme.
            }
        }

        return (true, response.StatusCode, "OK");
    }

    public async Task<HttpResponseMessage> Logout(ApiPNETHttpClient client, CancellationToken cancellationToken = default)
    {
        return await client.Client.GetAsync($"{client.ApiUrl}auth/logout", cancellationToken);
    }

    private static string? ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msgProp) &&
                msgProp.ValueKind == JsonValueKind.String)
            {
                return msgProp.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}