namespace BusinessLayer.DTOs
{
    public class NodeDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name associated with the object.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current status as a string value.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of CPU cores available for processing.
        /// </summary>
        public int NumberOfCPU { get; set; }

        /// <summary>
        /// Gets or sets the amount of memory allocated or used, in megabytes.
        /// </summary>
        public int Memory { get; set; }

        /// <summary>
        /// Gets or sets the collection of metadata key-value pairs associated with this instance.
        /// </summary>
        /// <remarks>Each entry in the dictionary represents a metadata attribute, where the key is the
        /// attribute name and the value is its corresponding value. Keys are case-sensitive. Modifying this collection
        /// affects the metadata available for this instance.</remarks>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
