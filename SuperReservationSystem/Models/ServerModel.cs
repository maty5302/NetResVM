namespace SuperReservationSystem.Models
{
    public class ServerModel
    {
        //ADD ID for db
        public required string ServerType { get; set; }
        public required string Name { get; set; }
        public required string IpAddress { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
