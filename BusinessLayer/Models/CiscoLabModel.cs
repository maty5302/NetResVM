using BusinessLayer.Interface;
using System.Text.Json.Serialization;

namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents a model for a Cisco lab, which includes details such as the lab's ID, name, description, 
    /// node count, link count, state, and modification time.
    /// </summary>
    public class CiscoLabModel : ILabModel
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
        public int Node_count { get; set; }

        /// <summary>
        /// Gets or sets the count of links in the lab.
        /// </summary>
        [JsonPropertyName("link_count")]
        public int Link_count { get; set; }

        /// <summary>
        /// Gets or sets the current state of the lab (e.g., started, stopped).
        /// </summary>
        [JsonPropertyName("state")]
        public required string State { get; set; }

        /// <summary>
        /// Gets or sets the last modified date and time of the lab.
        /// </summary>
        [JsonPropertyName("modified")]
        public DateTime Last_modified { get; set; }
    }

}
