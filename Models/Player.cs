namespace Task6.Models
{
    public class Player
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ConnectionId { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastActivity { get; set; }
    }
}