namespace SuperReservationSystem.Models
{
    public class ChangePasswordModel
    {
        public required string oldPassword { get; set; }
        public required string newPassword { get; set; }
        public required string confirmPassword { get; set; }
    }
}
