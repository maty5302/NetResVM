using DataLayer;
using BusinessLayer.Models;
using BusinessLayer.MapperDT;
using SimpleLogger;
using System.DirectoryServices.Protocols;
using BusinessLayer.DTOs;
using System.Runtime.InteropServices;
using DataLayer.Interface;

namespace BusinessLayer.Services
{
    /// <summary>
    /// This class provides methods for managing users in the system.
    /// </summary>
    public class UserService
    {
        private readonly IUserTableDataGateway _userTableDataGateway;
        private static ILogger _logger = FileLogger.Instance;

        public UserService(IUserTableDataGateway userTableDataGateway)
        {
            _userTableDataGateway = userTableDataGateway;
        }
        public UserService() : this(new UserTableDataGateway())
        {
        }

        /// <summary>
        /// Retrieves a user by their username.
        /// </summary>
        /// <param name="username">The username of the user to retrieve.</param>
        /// <returns>
        /// A <see cref="UserModel"/> object if a user with the specified username exists; otherwise, <c>null</c>.
        /// </returns>
        public virtual UserModel? GetUser(string username)
        {
            try
            {
                var row = _userTableDataGateway.GetUserByUsername(username);
                if (row.Rows.Count > 0)
                {
                    return UserMapper.Map(row.Rows[0]);
                }
                else
                {
                    _logger.LogWarning($"User with username {username} not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Checks whether a user with the specified username exists in the system.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <returns><c>true</c> if the user exists; otherwise, <c>false</c>.</returns>
        public virtual bool UserExists(string username)
        {
            return GetUser(username) != null;
        }

        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>
        /// A <see cref="UserModel"/> object if a user with the specified ID exists; otherwise, <c>null</c>.
        /// </returns>
        private UserModel? GetUser(int id)
        {
            try
            {
                var row = _userTableDataGateway.GetUserById(id);
                if (row.Rows.Count > 0)
                {
                    return UserMapper.Map(row.Rows[0]);
                }
                else
                {
                    _logger.LogWarning($"User with ID {id} not found.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves information about all users.
        /// </summary>
        /// <returns>
        /// A list of <see cref="UserDTO"/> objects containing user information.
        /// </returns>
        public List<UserDTO> GetAllUsersInfo()
        {
            try
            {
                var all = new List<UserDTO>();
                var table = _userTableDataGateway.GetAllUsers();
                foreach (System.Data.DataRow row in table.Rows)
                {
                    var user = UserMapper.MapToDTO(row);
                    all.Add(user);
                }
                return all;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new List<UserDTO>();
            }

        }

        /// <summary>
        /// Retrieves the authorization type of user by their username.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>
        /// The authorization type as a string if the user exists; otherwise, an empty string.
        /// </returns>
        public string GetAuthorizationType(string username)
        {
            var user = GetUser(username);
            if (user != null)
            {
                return user.AuthorizationType;
            }
            _logger.LogWarning($"User with username {username} not found.");
            return "";
        }

        /// <summary>
        /// Checks whether the user exists and is active.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>
        /// <c>true</c> if the user exists and is active; otherwise, <c>false</c>.
        /// </returns>
        public bool IsUserActive(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }
            var user = GetUser(username);
            return user != null && user.Active;
        }

        /// <summary>
        /// Escapes special characters in an LDAP Distinguished Name (DN) component per RFC 4514.
        /// </summary>
        /// <param name="input">The raw string to escape.</param>
        /// <returns>The escaped string safe for LDAP DN usage.</returns>
        public static string EscapeLdapDn(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder(input.Length * 2);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                switch (c)
                {
                    case '\\':
                    case ',':
                    case '+':
                    case '"':
                    case '<':
                    case '>':
                    case ';':
                    case '=':
                        sb.Append('\\').Append(c);
                        break;
                    case ' ' when (i == 0 || i == input.Length - 1):
                        sb.Append('\\').Append(' ');
                        break;
                    case '#' when (i == 0):
                        sb.Append('\\').Append('#');
                        break;
                    case '\0':
                        sb.Append("\\00");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Validates user credentials against an LDAP server.
        /// </summary>
        /// <param name="username">The username to validate.</param>
        /// <param name="password">The password to validate.</param>
        /// <returns>
        /// <c>true</c> if the credentials are valid; otherwise, <c>false</c>.
        /// </returns>
        private bool ValidateCredentialsLdap(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            string cleanUsername = username.Trim();
            char userContext = cleanUsername.Last();

            // VSB LDAP partitions users by the last character of their login (must be letter or digit)
            if (!char.IsLetterOrDigit(userContext))
            {
                _logger.LogWarning($"Invalid user context character '{userContext}' for LDAP authentication.");
                return false;
            }

            try
            {
                string escapedUsername = EscapeLdapDn(cleanUsername);
                string escapedUserContext = EscapeLdapDn(userContext.ToString());
                var bindDn = $"cn={escapedUsername},ou={escapedUserContext},ou=Users,o=VSB";


                var connection = new LdapConnection(new LdapDirectoryIdentifier("ldap.vsb.cz", 636));
                //setting up the connection
                connection.SessionOptions.SecureSocketLayer = true;

                //Just Windows being Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    connection.SessionOptions.VerifyServerCertificate = (connCon, cer) => true;

                connection.SessionOptions.ProtocolVersion = 3;
                connection.AuthType = AuthType.Basic;
                //authentication
                connection.Bind(new System.Net.NetworkCredential(bindDn, password));
                //if the authentication is successful, the connection will not throw an exception
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// Validates user credentials against the system's authentication mechanism (e.g., database or LDAP).
        /// </summary>
        /// <param name="username">The username to validate.</param>
        /// <param name="password">The password to validate.</param>
        /// <returns>
        /// <c>true</c> if the credentials are valid; otherwise, <c>false</c>.
        /// </returns>
        public bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var user = GetUser(username);
            if (user != null)
            {
                if(user.Active)
                {
                    // Check if the user is localhost or vsb
                    if (user.AuthorizationType == "localhost")
                    {
                        if (user.Password == password)
                            return true;
                    }
                    else if(user.AuthorizationType=="vsb")
                    {
                        //  Check the password against LDAP
                        var res = ValidateCredentialsLdap(username, password);
                        if (res)
                            return true;
                        else                        
                            return false;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid AuthorizationType.");
                        return false;
                    }
                }
                else if(!user.Active)
                {
                    // User is not active
                    _logger.LogWarning($"User {username} is not marked as Active denying access..");
                    return false;
                }
                _logger.LogWarning($"Invalid password for user with username {username}.");
                return false;
            }
            _logger.LogWarning($"User with username {username} not found.");
            return false;
        }


        /// <summary>
        /// Retrieves the username of a user by their unique identifier.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <returns>
        /// The username as a string if the user exists; otherwise, an empty string.
        /// </returns>
        public string GetUsername(int id)
        {
            try
            {
                var user = _userTableDataGateway.GetUserById(id);
                if (user.Rows.Count > 0)
                {
                    return (string)user.Rows[0]["Username"];
                }
                _logger.LogWarning($"User with ID {id} not found.");
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return "";
            }
        }

        /// <summary>
        /// Retrieves the unique identifier (ID) of a user by their username.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>
        /// The user ID as an integer if the user exists; otherwise, <c>-1</c>.
        /// </returns>
        public int GetUserId(string username)
        {
            var user = GetUser(username);
            if (user != null)
            {
                return user.Id;
            }
            _logger.LogWarning($"User with username {username} not found.");
            return -1;
        }

        /// <summary>
        /// Retrieves the role of a user by their username.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>
        /// The role of the user as a string if the user exists; otherwise, an empty string.
        /// </returns>
        public string GetRole(string username)
        {
            var user = GetUser(username);
            if (user != null)
            {
                return user.Role;
            }
            _logger.LogWarning($"User with username {username} not found.");
            return "";
        }

        /// <summary>
        /// Adds a new user to the system with the specified details.
        /// </summary>
        /// <param name="username">The username of the new user.</param>
        /// <param name="password">The password of the new user. Can be <c>null</c> if not required.</param>
        /// <param name="role">The role assigned to the new user (e.g., Admin, User, etc.).</param>
        /// <param name="authorizationType">The type of authorization for the new user (e.g., LDAP, Local).</param>
        /// <param name="active">Indicates whether the user account is active.</param>
        /// <returns>
        /// <c>true</c> if the user was successfully added; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool AddUser(string username, string? password, string role ,string authorizationType, bool active)
        {
            try
            {
                if(GetUser(username) != null)
                {
                    // User already exists
                    _logger.LogWarning($"User with username {username} already exists.");
                    return false;
                }
                int activeInt = active ? 1 : 0;
                if (authorizationType=="localhost" && password!=null)
                {
                    _userTableDataGateway.AddUser(username, password, role, authorizationType, activeInt);
                    return true;
                }
                else if(authorizationType=="vsb")
                {
                    _userTableDataGateway.AddUser(username, role, authorizationType, activeInt);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Invalid AuthorizationType.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Updates the active status of a user by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the user to update.</param>
        /// <param name="active">The new active status of the user. <c>true</c> if the user is active, <c>false</c> otherwise.</param>
        /// <returns>
        /// <c>true</c> if the user's status was successfully updated; otherwise, <c>false</c>.
        /// </returns>
        public bool UpdateUser(int id, bool active)
        {
            try
            {
                var user = _userTableDataGateway.GetUserById(id);
                if (user.Rows.Count == 0)
                {
                    _logger.LogWarning($"User with ID {id} not found.");
                    return false;
                }
                int activeInt = active ? 1 : 0; // 1 for true, 0 for false
                _userTableDataGateway.UpdateUserActive(id, activeInt);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Updates the password of a user by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the user whose password is to be updated.</param>
        /// <param name="password">The new password for the user.</param>
        /// <returns>
        /// <c>true</c> if the password was successfully updated; otherwise, <c>false</c>.
        /// </returns>
        public bool UpdateUser(int id, string password)
        {
            try
            {
                if(password == null)
                {
                    _logger.LogWarning("Password cannot be null.");
                    return false;
                }
                if(password.Length == 0)
                {
                    _logger.LogWarning("Password cannot be empty.");
                    return false;
                }
                if(password.Contains(" "))
                {
                    _logger.LogWarning("Password cannot contain spaces.");
                    return false;
                }

                var user = _userTableDataGateway.GetUserById(id);
                if (user.Rows.Count == 0)
                {
                    _logger.LogWarning($"User with ID {id} not found.");
                    return false;
                }
                _userTableDataGateway.UpdateUserPassword(id, password);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Removes a user from the system by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier (ID) of the user to remove.</param>
        /// <returns>
        /// <c>true</c> if the user was successfully removed; otherwise, <c>false</c>.
        /// </returns>
        public bool RemoveUser(int id)
        {
            try
            {
                var user = GetUser(id);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {id} not found.");
                    return false;
                }                
                _userTableDataGateway.RemoveUser(user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
    }
}
