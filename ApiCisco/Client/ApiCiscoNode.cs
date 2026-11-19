using System.Text.Json;

namespace ApiCisco.Client
{
    /// <summary>
    /// Class used for making requests to server Cisco CML API about nodes of lab
    /// </summary>
    public class ApiCiscoNode
    {
        /// <summary>
        /// Asynchronously retrieves a list of node identifiers within a specified lab on the Cisco CML server.
        /// </summary>
        /// <param name="user">The <see cref="ApiCiscoHttpClient"/> instance used to communicate with the server.</param>
        /// <param name="labId">The unique identifier of the lab to retrieve nodes from.</param>
        /// <returns>
        /// An array of <see cref="string"/> values representing the node identifiers within the lab.
        /// Returns <c>null</c> if the lab does not exist, no nodes are found, or an error occurs.
        /// </returns>
        public async Task<string[]?> GetNodes(ApiCiscoHttpClient user, string labId)
        {
            var url = user.Url + $"labs/{labId}/nodes";
            var response = await user.Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = (await response.Content.ReadAsStringAsync()).Split(",");
                string[] charactersToRemove = { "\n", "\"", "[", "]", " " };
                for (int i = 0; i < data.Length; i++)
                {
                    foreach (string item in charactersToRemove)
                    {
                        data[i] = data[i].Replace(item, string.Empty);
                    }
                }
                return data;
            }
            return null;
        }

        /// <summary>
        /// Asynchronously retrieves detailed information about a specific node within a lab on the Cisco CML server.
        /// </summary>
        /// <param name="user">The <see cref="ApiCiscoHttpClient"/> instance used to send the request.</param>
        /// <param name="labId">The unique identifier of the lab that contains the node.</param>
        /// <param name="nodeId">The unique identifier of the node whose information is being retrieved.</param>
        /// <returns>
        /// A <see cref="string"/> representing the node's information (e.g., in raw JSON or another serialized format).
        /// Returns <c>null</c> if the node or lab does not exist, or if an error occurs while fetching the data.
        /// </returns>
        public async Task<string?> GetNodeInfo(ApiCiscoHttpClient user, string labId, string nodeId)
        {
            var url = user.Url + $"labs/{labId}/nodes/{nodeId}";
            var response = await user.Client.GetAsync(url.Trim());
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return data;
            }

            return null;
        }

        /// <summary>
        /// Asynchronně získá unikátní klíč (UUID) konzolové relace pro zadaný uzel a sériovou linku na serveru Cisco CML.
        /// Nejprve dotazuje per-node endpoint <c>labs/{labId}/nodes/{nodeId}/keys/console?line={line}</c> (CML 2.7+),
        /// a pokud neuspěje, použije záložní endpoint <c>keys/console?lab_id={labId}&amp;node_id={nodeId}&amp;line={line}</c>.
        /// </summary>
        /// <param name="user">Autentizovaná instance <see cref="ApiCiscoHttpClient"/>.</param>
        /// <param name="labId">Unikátní identifikátor laboratoře.</param>
        /// <param name="nodeId">Unikátní identifikátor uzlu.</param>
        /// <param name="line">Index sériové konzolové linky (výchozí je 0).</param>
        /// <param name="cancellationToken">Token pro zrušení asynchronních HTTP požadavků.</param>
        /// <returns>Nalezené UUID konzole jako <see cref="string"/>, nebo <see langword="null"/>, pokud nebylo nalezeno.</returns>
        public async Task<string?> GetConsoleKey(
            ApiCiscoHttpClient user,
            string labId,
            string nodeId,
            int line = 0,
            CancellationToken cancellationToken = default)
        {
            string escapedLab = Uri.EscapeDataString(labId);
            string escapedNode = Uri.EscapeDataString(nodeId);

            string[] candidateEndpoints =
            {
                $"{user.Url}labs/{escapedLab}/nodes/{escapedNode}/keys/console?line={line}",
                $"{user.Url}keys/console?lab_id={escapedLab}&node_id={escapedNode}&line={line}"
            };

            foreach (string endpoint in candidateEndpoints)
            {
                try
                {
                    using var response = await user.Client.GetAsync(endpoint, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                        continue;

                    string body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
                    string? parsedUuid = ExtractConsoleUuid(body, labId, nodeId, line);
                    if (!string.IsNullOrWhiteSpace(parsedUuid))
                        return parsedUuid;
                }
                catch
                {
                    // Zkusíme další kandidátní endpoint
                }
            }

            return null;
        }

        /// <summary>
        /// Pomocná metoda pro extrakci UUID klíče konzole z JSON odpovědi endpointu <c>/api/v0/keys/console</c>.
        /// Podporuje jak přímý JSON řetězec, tak pole UUID či strukturovaný objekt vracející mapování všech aktivních konzolí.
        /// </summary>
        /// <param name="jsonBody">Textové tělo HTTP odpovědi z endpointu <c>/api/v0/keys/console</c>.</param>
        /// <param name="labId">Identifikátor hledané laboratoře.</param>
        /// <param name="nodeId">Identifikátor hledaného uzlu.</param>
        /// <param name="line">Index sériové linky (typicky 0).</param>
        /// <returns>Nalezené UUID konzole, nebo <see langword="null"/>, pokud jej nelze z odpovědi vyčíst.</returns>
        public static string? ExtractConsoleUuid(string jsonBody, string labId, string nodeId, int line = 0)
        {
            if (string.IsNullOrWhiteSpace(jsonBody))
                return null;

            // Pokud API vrátilo přímo prostý řetězec v uvozovkách (např. "550e8400-e29b-41d4-a716-446655440000")
            string trimmed = jsonBody.Trim();
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"") && trimmed.Length > 2)
            {
                string unquoted = trimmed.Trim('"').Trim();
                return string.IsNullOrWhiteSpace(unquoted) ? null : unquoted;
            }

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return ExtractConsoleUuidFromElement(doc.RootElement, labId, nodeId, line);
            }
            catch
            {
                return null;
            }
        }

        private static string? ExtractConsoleUuidFromElement(JsonElement element, string labId, string nodeId, int line)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                {
                    string? val = element.GetString();
                    return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
                }

                case JsonValueKind.Array:
                {
                    int index = 0;
                    string? firstCandidate = null;
                    foreach (var item in element.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            string? val = item.GetString();
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                firstCandidate ??= val.Trim();
                                if (index == line)
                                    return val.Trim();
                            }
                        }
                        else if (item.ValueKind == JsonValueKind.Object)
                        {
                            bool labMatches = !item.TryGetProperty("lab_id", out var lProp) || lProp.GetString() == labId;
                            bool nodeMatches = !item.TryGetProperty("node_id", out var nProp) || nProp.GetString() == nodeId;
                            bool lineMatches = !item.TryGetProperty("line", out var lnProp) || lnProp.ToString() == line.ToString();
                            if (labMatches && nodeMatches && lineMatches)
                            {
                                if (item.TryGetProperty("uuid", out var uProp) && uProp.ValueKind == JsonValueKind.String)
                                    return uProp.GetString();
                                if (item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                                    return idProp.GetString();
                            }
                        }
                        index++;
                    }
                    return firstCandidate;
                }

                case JsonValueKind.Object:
                {
                    if (element.TryGetProperty("uuid", out var uuidProp) && uuidProp.ValueKind == JsonValueKind.String)
                        return uuidProp.GetString();

                    if (element.TryGetProperty("active_console_keys", out var activeKeys))
                    {
                        string? nested = ExtractConsoleUuidFromElement(activeKeys, labId, nodeId, line);
                        if (!string.IsNullOrWhiteSpace(nested))
                            return nested;
                    }

                    foreach (var prop in element.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            bool labMatches = !prop.Value.TryGetProperty("lab_id", out var lProp) || lProp.GetString() == labId;
                            bool nodeMatches = !prop.Value.TryGetProperty("node_id", out var nProp) || nProp.GetString() == nodeId;
                            bool lineMatches = !prop.Value.TryGetProperty("line", out var lnProp) || lnProp.ToString() == line.ToString();
                            if (labMatches && nodeMatches && lineMatches)
                            {
                                return prop.Name;
                            }
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.String &&
                                 string.Equals(prop.Name, line.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            return prop.Value.GetString();
                        }
                    }
                    break;
                }
            }

            return null;
        }
    }
}
