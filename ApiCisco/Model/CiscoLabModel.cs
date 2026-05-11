using System.Text.Json.Serialization;

namespace ApiCisco.Model
{
    public class CiscoLabModel
    {
        /// <summary>
        /// Gets or sets the unique identifier of the lab.
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the lab.
        /// </summary>
        [JsonPropertyName("lab_title")]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the lab.
        /// </summary>
        [JsonPropertyName("lab_description")]
        public required string Description { get; set; }

        /// <summary>
        /// Gets or sets the count of nodes in the lab.
        /// </summary>
        [JsonPropertyName("node_count")]
        public int NodeCount { get; set; }

        /// <summary>
        /// Gets or sets the count of links in the lab.
        /// </summary>
        [JsonPropertyName("link_count")]
        public int LinkCount { get; set; }

        /// <summary>
        /// Gets or sets the current state of the lab (e.g., started, stopped).
        /// </summary>
        [JsonPropertyName("state")]
        public required string State { get; set; }

        /// <summary>
        /// Gets or sets the last modified date and time of the lab.
        /// </summary>
        [JsonPropertyName("modified")]
        public DateTime LastModified { get; set; }
    }
}
