using System.Text.Json.Serialization;

namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents a model for an EVE node with its configuration and status.
    /// </summary>
    public class EVENodeModel
    {
        /// <summary>
        /// Gets or sets the unique identifier of the node.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the node.
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the status of the node (e.g., running, stopped).
        /// </summary>
        [JsonPropertyName("status")]
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the number of CPUs allocated to the node.
        /// </summary>
        [JsonPropertyName("cpu")]
        public int NumberOfCPU { get; set; }

        /// <summary>
        /// Gets or sets the amount of memory (RAM) allocated to the node.
        /// </summary>
        [JsonPropertyName("ram")]
        public int Memory { get; set; }
        /// <summary>
        /// Gets or sets the number of Ethernet interfaces available on the node.
        /// </summary>
        [JsonPropertyName("ethernet")]
        public int NumberOfEthernet { get; set; }

        /// <summary>
        /// Gets or sets the URL used to connect to the node.
        /// </summary>
        [JsonPropertyName("url")]
        public required string UrlConnect { get; set; }
    }
}
