using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ApiCisco
{
    //class for api authentication and logout 
    public class Authentication
    {
        public async Task<HttpResponseMessage?> Authenticate(UserHttpClient client, string username, string password)
        {
            var url = client.Url + "authenticate";
            var authData = new
            {
                username = username,
                password = password
            };
            string jsondata = JsonSerializer.Serialize(authData);

            try
            {
                var content = new StringContent(jsondata, Encoding.UTF8, "application/json");
                var response = await client.Client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    client.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                        response.Content.ReadAsStringAsync().Result.Replace("\"", ""));
                    return response;
                }

                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message); //somehow pass this message to the user
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.ServiceUnavailable };
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine(e.Message);
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.RequestTimeout };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new HttpResponseMessage(){StatusCode = HttpStatusCode.InternalServerError};
            }
        }

        public async Task<HttpResponseMessage> Logout(UserHttpClient client)
        {
            var url = client.Url + "logout";
            try
            {
                var response = await client.Client.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    client.Client.DefaultRequestHeaders.Authorization = null;
                    return response;
                }
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
                HttpResponseMessage responseMessage = new HttpResponseMessage();
                responseMessage.StatusCode = System.Net.HttpStatusCode.ServiceUnavailable;
                return responseMessage;
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine(e.Message);
                HttpResponseMessage responseMessage = new HttpResponseMessage();
                responseMessage.StatusCode = HttpStatusCode.RequestTimeout;
                return responseMessage;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new HttpResponseMessage(){StatusCode = HttpStatusCode.InternalServerError};
            }
            
            
        }

    }

}
