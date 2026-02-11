namespace Task6.Models
{
    public class GameSession
    {
        public string Id { get; set; }
        public string SessionName { get; set; }
        public string CreatorId { get; set; }
        public string CreatorName { get; set; }
        public string PlayerXId { get; set; }
        public string PlayerOId { get; set; }
        public string CurrentPlayerId { get; set; }
        public GameStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string[] Board { get; set; }
        public string Winner { get; set; }
    }
}