namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents the ownership relationship between a user and a lab on a specific server.
    /// </summary>
    public class UserLabOwnershipModel
    {
        /// <summary>
        /// Gets or sets the identifier of the user who owns the lab.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the server where the lab is located.
        /// </summary>
        public int ServerId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the lab.
        /// </summary>
        public required string LabId { get; set; }
    }

}
