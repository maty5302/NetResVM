namespace BusinessLayer.Models
{
    /// <summary>
    /// A single user parsed from an import file.
    /// </summary>
    /// <param name="Username">The login of the user.</param>
    /// <param name="Password">The password (only used for localhost accounts), or <c>null</c> when the file contains none.</param>
    public sealed record UserImportEntry(string Username, string? Password);
}
