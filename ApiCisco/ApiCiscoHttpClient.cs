namespace ApiCisco
{
    /// <summary>
    /// Represents an HTTP client for interacting with the Cisco CML API.
    /// Automatically constructs the base API URL and sets up an HTTP client that ignores SSL certificate validation.
    /// </summary>
    public class ApiCiscoHttpClient
    {
        /// <summary>
        /// Gets the base URL used for API communication, including the versioned path (e.g., /api/v0/).
        /// </summary>
        public string Url { get; private set; }
        /// <summary>
        /// Gets the internal <see cref="HttpClient"/> used for sending HTTP requests.
        /// </summary>
        public HttpClient Client { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiCiscoHttpClient"/> class with the specified server URL.
        /// Adds <c>https://</c> if the protocol is not specified and appends <c>/api/v0/</c> to form the API endpoint.
        /// Configures the client to ignore SSL certificate validation, because of not valid certifications on school servers.
        /// </summary>
        /// <param name="url">The base URL of the Cisco CML server.</param>
        public ApiCiscoHttpClient(string url)
        {
            if(url.StartsWith("http://") || url.StartsWith("https://"))
                this.Url = url + "/api/v0/";
            else
                this.Url = "https://" + url + "/api/v0/";
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            Client = new HttpClient(handler){Timeout = TimeSpan.FromSeconds(20)};
        }
    }
}
