namespace BusinessLayer.Models
{
    /// <summary>
    /// Represents the summary result of a bulk user import operation.
    /// </summary>
    public class UserImportResult
    {
        /// <summary>
        /// Total number of username entries discovered in the input file.
        /// </summary>
        public int TotalFound { get; set; }

        /// <summary>
        /// Number of users successfully created in the system.
        /// </summary>
        public int AddedCount { get; set; }

        /// <summary>
        /// Number of users skipped because they already exist in the system.
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// Number of entries skipped due to invalid username format.
        /// </summary>
        public int InvalidCount { get; set; }

        /// <summary>
        /// List of usernames that were successfully added.
        /// </summary>
        public List<string> AddedUsernames { get; set; } = new();

        /// <summary>
        /// List of usernames that already existed and were skipped.
        /// </summary>
        public List<string> SkippedUsernames { get; set; } = new();

        /// <summary>
        /// List of raw entries that failed validation.
        /// </summary>
        public List<string> InvalidUsernames { get; set; } = new();

        /// <summary>
        /// Usernames without a valid password in a localhost import. When not empty, nothing was imported.
        /// </summary>
        public List<string> MissingPasswordUsernames { get; set; } = new();
    }
}
