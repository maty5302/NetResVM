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
        DataTable GetUserByUsername(string Username);

        /// <summary>
        /// Retrieves a user by their ID from the database.
        /// </summary>
        DataTable GetUserById(int Id);

        /// <summary>
        /// Retrieves all users from the database.
        /// </summary>
        DataTable GetAllUsers();

        /// <summary>
        /// Adds a new user to the database with password.
        /// </summary>
        void AddUser(string Username, string Password, string Role, string AuthorizationType, int Active);

        /// <summary>
        /// Adds a new user to the database without password.
        /// </summary>
        void AddUser(string Username, string Role, string AuthorizationType, int Active);

        /// <summary>
        /// Updates the active status of a user.
        /// </summary>
        void UpdateUserActive(int Id, int Active);

        /// <summary>
        /// Updates the password of a user.
        /// </summary>
        void UpdateUserPassword(int Id, string Password);

        /// <summary>
        /// Removes a user from the database.
        /// </summary>
        void RemoveUser(int Id);
    }
}
