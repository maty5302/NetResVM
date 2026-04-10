namespace BusinessLayer.DTOs
{
    public class LabDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the lab.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the name of the lab.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the description of the lab.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of metadata key-value pairs associated with this instance.
        /// </summary>
        /// <remarks>Keys are case-sensitive. Use this property to store additional information relevant
        /// to the instance, such as custom attributes or descriptive data.</remarks>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}