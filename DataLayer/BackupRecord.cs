namespace DataLayer
{
    /// <summary>
    /// Represents backup record from local backup storage
    /// </summary>
    public class BackupRecord
    {
        /// <summary>
        /// Gets or sets the type of server (e.g., Cisco, EVE, etc.).
        /// </summary>
        public string ServerType { get; set; }

        /// <summary>
        /// Gets or sets the ID of the lab for which the backup was created.
        /// </summary>
        public string LabId { get; set; }

        /// <summary>
        /// Gets or sets the name of the backup file.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the full path to the backup file.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the backup was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
