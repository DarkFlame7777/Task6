using System.Collections.Concurrent;
using Task6.Models;

namespace Task6.Services
{
    public class GameService
    {
        private readonly ConcurrentDictionary<string, Player> _players = new();
        private readonly ConcurrentDictionary<string, GameSession> _gameSessions = new();
        private readonly ConcurrentDictionary<string, GameStats> _playerStats = new();

        public Player GetOrCreatePlayer(string playerName, string connectionId)
        {
            var existingPlayers = _players.Values
                .Where(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var playerId = Guid.NewGuid().ToString();
            var displayName = existingPlayers.Count > 0
                ? $"{playerName} #{existingPlayers.Count + 1}"
                : playerName;

            var player = new Player
            {
                Id = playerId,
                Name = playerName,
                DisplayName = displayName,
                ConnectionId = connectionId,
                IsConnected = true,
                LastActivity = DateTime.UtcNow
            };

            _players[playerId] = player;
            return player;
        }

        public void UpdatePlayerConnection(string playerId, string connectionId)
        {
            if (_players.TryGetValue(playerId, out var player))
            {
                player.ConnectionId = connectionId;
                player.IsConnected = true;
                player.LastActivity = DateTime.UtcNow;
            }
        }

        public void DisconnectPlayer(string connectionId)
        {
            var player = _players.Values.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (player != null)
                player.IsConnected = false;
        }

        public Player GetPlayerById(string playerId)
        {
            _players.TryGetValue(playerId, out var player);
            return player;
        }

        public Player GetPlayerByName(string playerName)
        {
            return _players.Values.FirstOrDefault(p =>
                p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
        }

        public GameSession CreateGameSession(string sessionName, Player creator)
        {
            if (IsPlayerInActiveGame(creator.Id))
                return null;

            var session = new GameSession
            {
                Id = Guid.NewGuid().ToString(),
                SessionName = sessionName,
                CreatorId = creator.Id,
                CreatorName = creator.DisplayName,
                PlayerXId = creator.Id,
                Status = GameStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                Board = new string[9],
                CurrentPlayerId = creator.Id
            };

            _gameSessions[session.Id] = session;
            return session;
        }

        public List<GameSession> GetAvailableSessions()
        {
            return _gameSessions.Values
                .Where(s => s.Status == GameStatus.Waiting)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();
        }

        public GameSession JoinGameSession(string sessionId, Player player)
        {
            if (IsPlayerInActiveGame(player.Id))
                return null;

            if (_gameSessions.TryGetValue(sessionId, out var session) &&
                session.Status == GameStatus.Waiting &&
                session.PlayerXId != player.Id)
            {
                session.PlayerOId = player.Id;
                session.Status = GameStatus.InProgress;
                return session;
            }

            return null;
        }

        public bool MakeMove(string sessionId, string playerId, int position)
        {
            if (!_gameSessions.TryGetValue(sessionId, out var session) ||
                session.Status != GameStatus.InProgress ||
                session.CurrentPlayerId != playerId ||
                position < 0 || position >= 9 ||
                !string.IsNullOrEmpty(session.Board[position]))
            {
                return false;
            }

            session.Board[position] = playerId == session.PlayerXId ? "X" : "O";

            if (CheckWinner(session.Board, out var winnerSymbol))
            {
                session.Status = GameStatus.Finished;
                session.Winner = winnerSymbol == "X" ? session.PlayerXId : session.PlayerOId;
                UpdateStats(session.PlayerXId, session.PlayerOId, session.Winner);
            }
            else if (session.Board.All(cell => !string.IsNullOrEmpty(cell)))
            {
                session.Status = GameStatus.Finished;
                session.Winner = "Draw";
                UpdateStats(session.PlayerXId, session.PlayerOId, "Draw");
            }
            else
            {
                session.CurrentPlayerId = session.CurrentPlayerId == session.PlayerXId
                    ? session.PlayerOId
                    : session.PlayerXId;
            }

            return true;
        }

        private bool CheckWinner(string[] board, out string winner)
        {
            winner = null;
            int[,] winConditions = new int[,]
            {
                {0,1,2}, {3,4,5}, {6,7,8},
                {0,3,6}, {1,4,7}, {2,5,8},
                {0,4,8}, {2,4,6}
            };

            for (int i = 0; i < 8; i++)
            {
                int a = winConditions[i, 0];
                int b = winConditions[i, 1];
                int c = winConditions[i, 2];

                if (!string.IsNullOrEmpty(board[a]) &&
                    board[a] == board[b] &&
                    board[b] == board[c])
                {
                    winner = board[a];
                    return true;
                }
            }
            return false;
        }

        public bool IsPlayerInActiveGame(string playerId)
        {
            return _gameSessions.Values.Any(s =>
                (s.Status == GameStatus.Waiting || s.Status == GameStatus.InProgress) &&
                (s.PlayerXId == playerId || s.PlayerOId == playerId));
        }

        private void UpdateStats(string playerXId, string playerOId, string winnerId)
        {
            if (!_playerStats.ContainsKey(playerXId))
                _playerStats[playerXId] = new GameStats { PlayerId = playerXId, PlayerName = playerXId };
            if (!_playerStats.ContainsKey(playerOId))
                _playerStats[playerOId] = new GameStats { PlayerId = playerOId, PlayerName = playerOId };

            if (winnerId == "Draw")
            {
                if (_playerStats.TryGetValue(playerXId, out var statsX))
                    statsX.Draws++;
                if (_playerStats.TryGetValue(playerOId, out var statsO))
                    statsO.Draws++;
            }
            else if (winnerId == playerXId)
            {
                if (_playerStats.TryGetValue(playerXId, out var statsX))
                    statsX.Wins++;
                if (_playerStats.TryGetValue(playerOId, out var statsO))
                    statsO.Losses++;
            }
            else if (winnerId == playerOId)
            {
                if (_playerStats.TryGetValue(playerOId, out var statsO))
                    statsO.Wins++;
                if (_playerStats.TryGetValue(playerXId, out var statsX))
                    statsX.Losses++;
            }
        }

        public GameSession GetGameSession(string sessionId)
        {
            _gameSessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public GameStats GetPlayerStats(string playerId)
        {
            if (!_playerStats.TryGetValue(playerId, out var stats))
            {
                stats = new GameStats { PlayerId = playerId, PlayerName = playerId };
                _playerStats[playerId] = stats;
            }
            return stats;
        }

        public void RemoveGameSession(string sessionId)
        {
            _gameSessions.TryRemove(sessionId, out _);
        }
    }
}