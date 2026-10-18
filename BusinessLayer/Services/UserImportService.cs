using BusinessLayer.Models;
using SimpleLogger;
using System.Text;

namespace BusinessLayer.Services
{
    /// <summary>
    /// Service responsible for parsing and bulk importing users from CSV and TXT files.
    /// </summary>
    public class UserImportService
    {
        private readonly UserService _userService;
        private readonly ILogger _logger;

        public UserImportService(UserService userService, ILogger? logger = null)
        {
            _userService = userService;
            _logger = logger ?? FileLogger.Instance;
        }

        public UserImportService() : this(new UserService())
        {
        }

        /// <summary>
        /// Validates if a string conforms to acceptable username constraints.
        /// </summary>
        /// <param name="username">The username to validate.</param>
        /// <returns><c>true</c> if valid; otherwise <c>false</c>.</returns>
        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            string trimmed = username.Trim();
            if (trimmed.Length < 2 || trimmed.Length > 50)
            {
                return false;
            }

            return trimmed.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-' || c == '.');
        }

        /// <summary>
        /// Extracts a list of usernames from a CSV or TXT stream.
        /// </summary>
        /// <param name="stream">The file data stream.</param>
        /// <param name="fileName">The original file name to detect format extension.</param>
        /// <returns>A list of unique usernames found in the file.</returns>
        public List<string> ExtractUsernamesFromStream(Stream stream, string fileName)
        {
            var usernames = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension != ".csv" && extension != ".txt")
            {
                _logger.LogWarning($"Unsupported file extension for user import: {extension}");
                return usernames;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            if (extension == ".txt")
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string candidate = line.Trim();
                    if (string.IsNullOrWhiteSpace(candidate) || candidate.StartsWith("#") || candidate.StartsWith("//"))
                    {
                        continue;
                    }

                    if (candidate.Contains('@'))
                    {
                        candidate = candidate.Substring(0, candidate.IndexOf('@')).Trim();
                    }

                    if (IsValidUsername(candidate) && seen.Add(candidate))
                    {
                        usernames.Add(candidate);
                    }
                }
            }
            else if (extension == ".csv")
            {
                string? firstLine = reader.ReadLine();
                if (firstLine == null)
                {
                    return usernames;
                }

                // Detect delimiter
                char delimiter = ',';
                if (firstLine.Contains(';'))
                {
                    delimiter = ';';
                }
                else if (firstLine.Contains('\t'))
                {
                    delimiter = '\t';
                }

                string[] firstRowParts = firstLine.Split(delimiter);
                int targetColumnIndex = -1;

                string[] primaryKeywords = { "login", "username", "osobní číslo", "osobni cislo", "osobní č.", "osobni c.", "uživatelské jméno", "uzivatelske jmeno", "přihlašovací jméno", "prihlasovaci jmeno", "student" };
                string[] fallbackKeywords = { "user", "id" };

                // First pass: look for specific login/username keywords
                for (int i = 0; i < firstRowParts.Length; i++)
                {
                    string headerClean = firstRowParts[i].Trim().Trim('"').Trim().ToLowerInvariant();
                    if (primaryKeywords.Any(k => headerClean.Contains(k)))
                    {
                        targetColumnIndex = i;
                        break;
                    }
                }

                // Second pass: if no specific login header found, check generic fallback keywords (exact match)
                if (targetColumnIndex == -1)
                {
                    for (int i = 0; i < firstRowParts.Length; i++)
                    {
                        string headerClean = firstRowParts[i].Trim().Trim('"').Trim().ToLowerInvariant();
                        if (fallbackKeywords.Contains(headerClean))
                        {
                            targetColumnIndex = i;
                            break;
                        }
                    }
                }

                bool isHeader = targetColumnIndex != -1;
                if (!isHeader)
                {
                    targetColumnIndex = 0; // default to first column
                    // Process first line as data row
                    string candidate = firstRowParts[targetColumnIndex].Trim().Trim('"').Trim();
                    if (candidate.Contains('@'))
                    {
                        candidate = candidate.Substring(0, candidate.IndexOf('@')).Trim();
                    }
                    if (IsValidUsername(candidate) && seen.Add(candidate))
                    {
                        usernames.Add(candidate);
                    }
                }

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] parts = line.Split(delimiter);
                    if (targetColumnIndex < parts.Length)
                    {
                        string candidate = parts[targetColumnIndex].Trim().Trim('"').Trim();
                        if (candidate.Contains('@'))
                        {
                            candidate = candidate.Substring(0, candidate.IndexOf('@')).Trim();
                        }
                        if (IsValidUsername(candidate) && seen.Add(candidate))
                        {
                            usernames.Add(candidate);
                        }
                    }
                }
            }

            return usernames;
        }

        /// <summary>
        /// Reads a CSV or TXT stream, parses usernames, and imports them into the system.
        /// </summary>
        /// <param name="stream">The file stream containing user data.</param>
        /// <param name="fileName">The file name with extension.</param>
        /// <param name="role">The role to assign (default: student).</param>
        /// <param name="authType">The authentication type (default: vsb).</param>
        /// <param name="active">Whether users should be active (default: true).</param>
        /// <returns>A summary result containing counts and lists of added/skipped users.</returns>
        public UserImportResult ImportUsersFromStream(Stream stream, string fileName, string role = "student", string authType = "vsb", bool active = true)
        {
            var result = new UserImportResult();
            var usernames = ExtractUsernamesFromStream(stream, fileName);

            foreach (var username in usernames)
            {
                result.TotalFound++;

                if (_userService.UserExists(username))
                {
                    result.SkippedCount++;
                    result.SkippedUsernames.Add(username);
                    _logger.Log($"User import: '{username}' already exists, skipped.");
                    continue;
                }

                bool added = _userService.AddUser(username, null, role, authType, active);
                if (added)
                {
                    result.AddedCount++;
                    result.AddedUsernames.Add(username);
                    _logger.Log($"User import: '{username}' added successfully as {role} ({authType}).");
                }
                else
                {
                    result.InvalidCount++;
                    result.InvalidUsernames.Add(username);
                    _logger.LogWarning($"User import: '{username}' failed to be added.");
                }
            }

            return result;
        }
    }
}
