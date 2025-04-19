using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiEVE
{
    /// <summary>
    /// Provides methods to interact with the EVE API for lab management, including retrieving, exporting, importing, and deleting labs.
    /// </summary>
    public class ApiEVELab
    {
        /// <summary>
        /// Retrieves a list of all labs from the EVE API.
        /// </summary>
        /// <param name="client">An authenticated <see cref="ApiEVEHttpClient"/> instance.</param>
        /// <returns>A string containing the list of labs in JSON format, or null if the request fails.</returns>
        public async Task<string?> GetLabs(ApiEVEHttpClient client)
        {
            var url = $"{client.Url}folders/";
            var response = await client.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        /// <summary>
        /// Retrieves detailed information about a specific lab.
        /// </summary>
        /// <param name="client">An authenticated <see cref="ApiEVEHttpClient"/> instance.</param>
        /// <param name="labName">The name of the lab to retrieve information for.</param>
        /// <returns>A string containing the lab information in JSON format, or null if the request fails.</returns>
        public async Task<string?> GetLabInfo(ApiEVEHttpClient client, string labName)
        {
            var url = $"{client.Url}labs/{labName.Trim()}";
            var response = await client.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
                return null;
        }

        /// <summary>
        /// Exports a lab's data to a downloadable link.
        /// </summary>
        /// <param name="client">An authenticated <see cref="ApiEVEHttpClient"/> instance.</param>
        /// <param name="jsonData">The JSON data to specify which lab to export.</param>
        /// <returns>A string containing the lab export download data, or null if the request fails.</returns>
        public async Task<string?> ExportLab(ApiEVEHttpClient client, string jsonData)
        {
            var url = $"{client.Url}export";
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await client.Client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var downloadLink = await response.Content.ReadAsStringAsync();
                var downloadLinkJson = JsonDocument.Parse(downloadLink).RootElement.GetProperty("data");
                var downloadUrl = $"{client.Url.TrimEnd('/').Replace("/api", "", StringComparison.OrdinalIgnoreCase)}{downloadLinkJson}";
                var downloadResponse = await client.Client.GetAsync(downloadUrl);
                if (downloadResponse.IsSuccessStatusCode)
                {
                    return await downloadResponse.Content.ReadAsStringAsync();
                }
                else
                    return null;
            }
            else
                return null;
        }

        /// <summary>
        /// Imports a lab from a specified file.
        /// </summary>
        /// <param name="user">An authenticated <see cref="ApiEVEHttpClient"/> instance.</param>
        /// <param name="file">The file content as a byte array.</param>
        /// <param name="fileName">The name of the file being uploaded.</param>
        /// <returns>A boolean indicating whether the import was successful.</returns>
        public async Task<bool> ImportLab(ApiEVEHttpClient user, byte[] file, string fileName)
        {
            var url = user.Url + "import";

            using (var content = new MultipartFormDataContent())
            {

                content.Add(new StringContent("/"), "path");
                var fileContent = new ByteArrayContent(file);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-zip-compressed");

                // Attach the file with the correct name
                content.Add(fileContent, "file", fileName);

                var response = await user.Client.PostAsync(url.Trim(), content);
                return response.IsSuccessStatusCode;
            }
        }

        /// <summary>
        /// Deletes a specific lab.
        /// </summary>
        /// <param name="user">An authenticated <see cref="ApiEVEHttpClient"/> instance.</param>
        /// <param name="labName">The name of the lab to delete.</param>
        /// <returns>A boolean indicating whether the lab deletion was successful.</returns>
        public async Task<bool> DeleteLab(ApiEVEHttpClient user, string labName)
        {
            var url = $"{user.Url}labs/{labName.Trim()}";
            var response = await user.Client.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
    }

}
