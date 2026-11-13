using System.Net;

namespace ApiPNETLab.Client;

public class ApiPNETHttpClient : IDisposable
{
    public string BaseUrl { get; }
    public string ApiUrl => $"{BaseUrl}/api/";
    public string StoreUrl => $"{BaseUrl}/store/public/";
    public Uri BaseUri { get; }

    public CookieContainer CookieContainer { get; }
    public HttpClient Client { get; }

    public ApiPNETHttpClient(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Server URL cannot be empty.", nameof(url));

        string trimmed = url.Trim().TrimEnd('/');
        BaseUrl = (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            ? trimmed
            : $"https://{trimmed}";

        BaseUri = new Uri(BaseUrl);
        CookieContainer = new CookieContainer();

        var httpHandler = new HttpClientHandler
        {
            CookieContainer = CookieContainer,
            UseCookies = true,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        var xsrfHandler = new XsrfTokenHandler(httpHandler, CookieContainer, BaseUri);

        Client = new HttpClient(xsrfHandler, disposeHandler: true)
        {
            BaseAddress = BaseUri,
            Timeout = TimeSpan.FromSeconds(30)
        };

        Client.DefaultRequestHeaders.Add("Accept", "application/json");
        Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
    }

    public string? GetXsrfToken()
    {
        var cookie = CookieContainer.GetCookies(BaseUri)["XSRF-TOKEN"];
        return cookie != null ? Uri.UnescapeDataString(cookie.Value) : null;
    }

    public void Dispose()
    {
        Client.Dispose();
    }

    private sealed class XsrfTokenHandler : DelegatingHandler
    {
        private readonly CookieContainer _cookies;
        private readonly Uri _baseUri;

        public XsrfTokenHandler(HttpMessageHandler innerHandler, CookieContainer cookies, Uri baseUri)
            : base(innerHandler)
        {
            _cookies = cookies;
            _baseUri = baseUri;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains("X-XSRF-TOKEN"))
            {
                var xsrfCookie = _cookies.GetCookies(_baseUri)["XSRF-TOKEN"];
                if (xsrfCookie != null && !string.IsNullOrEmpty(xsrfCookie.Value))
                {
                    request.Headers.TryAddWithoutValidation(
                        "X-XSRF-TOKEN",
                        Uri.UnescapeDataString(xsrfCookie.Value));
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}