using System.Net;

namespace ApiEVE
{
    /// <summary>
    /// Provides an HTTP client for interacting with the EVE API, managing cookies and authentication.
    /// </summary>
    public class ApiEVEHttpClient
    {
        /// <summary>
        /// Gets the base URL of the EVE API.
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// Gets the cookie container used for managing cookies.
        /// </summary>
        internal readonly CookieContainer cookieContainer;

        /// <summary>
        /// Gets the HTTP client used for making requests to the API.
        /// </summary>
        public HttpClient Client { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiEVEHttpClient"/> class with the specified base URL.
        /// </summary>
        /// <param name="url">The base URL for the EVE API.</param>
        /// <exception cref="ArgumentException">Thrown if the provided URL is invalid or null.</exception>
        public ApiEVEHttpClient(string url)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
                this.Url = url.TrimEnd('/') + "/api/";
            else
                this.Url = "http://" + url.TrimEnd('/') + "/api/";

            cookieContainer=new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer,
                UseCookies = true
            };

            this.Client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            Client.DefaultRequestHeaders.Add("Accept", "application/json");
        }
    }
}
