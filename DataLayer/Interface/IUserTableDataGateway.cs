using System.Data;

namespace DataLayer.Interface
{
    /// <summary>
    /// Interface for UserTableDataGateway to support dependency injection and unit testing.
    /// </summary>
    public interface IUserTableDataGateway
    {
        /// <summary>
        /// Retrieves a user by their username from the database.
        /// </summary>
        DataTable GetUserByUsername(string username);

        /// <summary>
        /// Retrieves a user by their ID from the database.
        /// </summary>
        DataTable GetUserById(int id);

        /// <summary>
        /// Retrieves all users from the database.
        /// </summary>
        DataTable GetAllUsers();

        /// <summary>
        /// Adds a new user to the database with password.
        /// </summary>
        void AddUser(string username, string password, string role, string authorizationType, int active);

        /// <summary>
        /// Adds a new user to the database without password.
        /// </summary>
        void AddUser(string username, string role, string authorizationType, int active);

        /// <summary>
        /// Updates the active status of a user.
        /// </summary>
        void UpdateUserActive(int id, int active);

        /// <summary>
        /// Updates the password of a user.
        /// </summary>
        void UpdateUserPassword(int id, string password);

        /// <summary>
        /// Removes a user from the database.
        /// </summary>
        void RemoveUser(int id);
    }
}
