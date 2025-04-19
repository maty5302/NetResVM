using BusinessLayer.Interface;
using System.Text.Json.Serialization;
namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents a model for an EVE lab, which includes details such as the lab's ID, name, description, filename, and modification time.
    /// </summary>
    public class EVELabModel : ILabModel
    {
        /// <summary>
        /// Gets or sets the unique identifier of the lab.
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the lab.
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the lab.
        /// </summary>
        [JsonPropertyName("description")]
        public required string Description { get; set; }

        /// <summary>
        /// Gets or sets the filename associated with the lab.
        /// </summary>
        [JsonPropertyName("filename")]
        public required string Filename { get; set; }

        /// <summary>
        /// Gets or sets the path of the lab (ignored during serialization).
        /// </summary>
        [JsonIgnore]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the last modified date and time of the lab.
        /// </summary>
        [JsonPropertyName("mtime")]
        public DateTime Last_modified { get; set; }
    }

}
