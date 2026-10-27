using BusinessLayer.Models;
using SimpleLogger;
using System.Text;

namespace BusinessLayer.Services
{
    /// <summary>
    /// Service responsible for parsing and bulk importing users from CSV and TXT files.
    /// </summary>
    /// <remarks>
    /// Supported formats:
    /// <list type="bullet">
    /// <item>TXT – one user per line: <c>login</c>, or <c>login password</c> (separated by space, tab, ';' or ',').</item>
    /// <item>CSV – login column detected from the header (or the first column without header);
    /// password column detected from the header (heslo/password) or the column right after the login column.</item>
    /// </list>
    /// Passwords are required only for the <c>localhost</c> authentication type.
    /// </remarks>
    public class UserImportService
    {
        /// <summary>
        /// Maximum password length allowed by the database column ([User].[Password] VARCHAR(50)).
        /// </summary>
        public const int MaxPasswordLength = 50;

        private static readonly char[] TxtSeparators = { ' ', '\t', ';', ',' };

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
        /// Validates a password for a local (localhost) account.
        /// </summary>
        /// <param name="password">The password to validate.</param>
        /// <returns><c>true</c> if the password is non-empty and fits into the database column; otherwise <c>false</c>.</returns>
        public static bool IsValidPassword(string? password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length <= MaxPasswordLength;
        }

        /// <summary>
        /// Extracts a list of usernames from a CSV or TXT stream.
        /// </summary>
        /// <param name="stream">The file data stream.</param>
        /// <param name="fileName">The original file name to detect format extension.</param>
        /// <returns>A list of unique usernames found in the file.</returns>
        public List<string> ExtractUsernamesFromStream(Stream stream, string fileName)
        {
            return ExtractUserEntriesFromStream(stream, fileName).Select(e => e.Username).ToList();
        }

        /// <summary>
        /// Extracts users (username + optional password) from a CSV or TXT stream.
        /// </summary>
        /// <param name="stream">The file data stream.</param>
        /// <param name="fileName">The original file name to detect format extension.</param>
        /// <returns>A list of unique entries (first occurrence of a username wins).</returns>
        public List<UserImportEntry> ExtractUserEntriesFromStream(Stream stream, string fileName)
        {
            var entries = new List<UserImportEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension != ".csv" && extension != ".txt")
            {
                _logger.LogWarning($"Unsupported file extension for user import: {extension}");
                return entries;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            void AddEntry(string rawUsername, string? rawPassword)
            {
                string candidate = StripEmailDomain(rawUsername);
                if (IsValidUsername(candidate) && seen.Add(candidate))
                {
                    string? password = string.IsNullOrEmpty(rawPassword) ? null : rawPassword;
                    entries.Add(new UserImportEntry(candidate, password));
                }
            }

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

                    // "login" or "login<separator>password" – everything after the first separator is the password
                    int sep = candidate.IndexOfAny(TxtSeparators);
                    if (sep < 0)
                    {
                        AddEntry(candidate, null);
                    }
                    else
                    {
                        string password = candidate.Substring(sep + 1).TrimStart(TxtSeparators).TrimEnd();
                        AddEntry(candidate.Substring(0, sep), password);
                    }
                }
            }
            else if (extension == ".csv")
            {
                string? firstLine = reader.ReadLine();
                if (firstLine == null)
                {
                    return entries;
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
                int passwordColumnIndex = -1;

                string[] primaryKeywords = { "login", "username", "osobní číslo", "osobni cislo", "osobní č.", "osobni c.", "uživatelské jméno", "uzivatelske jmeno", "přihlašovací jméno", "prihlasovaci jmeno", "student" };
                string[] fallbackKeywords = { "user", "id" };
                string[] passwordKeywords = { "heslo", "password", "passwd", "pass" };

                // First pass: look for specific login/username keywords
                for (int i = 0; i < firstRowParts.Length; i++)
                {
                    string headerClean = CleanCell(firstRowParts[i]).ToLowerInvariant();
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
                        string headerClean = CleanCell(firstRowParts[i]).ToLowerInvariant();
                        if (fallbackKeywords.Contains(headerClean))
                        {
                            targetColumnIndex = i;
                            break;
                        }
                    }
                }

                bool isHeader = targetColumnIndex != -1;
                if (isHeader)
                {
                    // Password column by header name (heslo/password), otherwise the column right after the login
                    for (int i = 0; i < firstRowParts.Length; i++)
                    {
                        string headerClean = CleanCell(firstRowParts[i]).ToLowerInvariant();
                        if (i != targetColumnIndex && passwordKeywords.Any(k => headerClean.Contains(k)))
                        {
                            passwordColumnIndex = i;
                            break;
                        }
                    }
                    if (passwordColumnIndex == -1)
                    {
                        passwordColumnIndex = targetColumnIndex + 1;
                    }
                }
                else
                {
                    // No header: login in the first column, password in the second
                    targetColumnIndex = 0;
                    passwordColumnIndex = 1;
                    AddEntry(CleanCell(firstRowParts[targetColumnIndex]), GetCell(firstRowParts, passwordColumnIndex));
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
                        AddEntry(CleanCell(parts[targetColumnIndex]), GetCell(parts, passwordColumnIndex));
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Reads a CSV or TXT stream, parses users, and imports them into the system.
        /// For the <c>localhost</c> authentication type every user must have a valid password,
        /// otherwise nothing is imported and <see cref="UserImportResult.MissingPasswordUsernames"/> is filled.
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
            var entries = ExtractUserEntriesFromStream(stream, fileName);
            result.TotalFound = entries.Count;

            bool isLocal = authType == "localhost";
            if (isLocal)
            {
                // All-or-nothing: a local account without a password could never log in
                result.MissingPasswordUsernames = entries.Where(e => !IsValidPassword(e.Password)).Select(e => e.Username).ToList();
                if (result.MissingPasswordUsernames.Count > 0)
                {
                    _logger.LogWarning($"User import (localhost) aborted: {result.MissingPasswordUsernames.Count} users without a valid password.");
                    return result;
                }
            }

            foreach (var entry in entries)
            {
                string username = entry.Username;

                if (_userService.UserExists(username))
                {
                    result.SkippedCount++;
                    result.SkippedUsernames.Add(username);
                    _logger.Log($"User import: '{username}' already exists, skipped.");
                    continue;
                }

                // Password is used only for local accounts; VŠB accounts authenticate via LDAP
                bool added = _userService.AddUser(username, isLocal ? entry.Password : null, role, authType, active);
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

        private static string CleanCell(string value)
        {
            return value.Trim().Trim('"').Trim();
        }

        private static string? GetCell(string[] parts, int index)
        {
            return index >= 0 && index < parts.Length ? CleanCell(parts[index]) : null;
        }

        private static string StripEmailDomain(string value)
        {
            string candidate = value.Trim();
            int at = candidate.IndexOf('@');
            return at >= 0 ? candidate.Substring(0, at).Trim() : candidate;
        }
    }
}
