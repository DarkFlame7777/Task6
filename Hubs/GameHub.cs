using Microsoft.AspNetCore.SignalR;
using Task6.Models;
using Task6.Services;

namespace Task6.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameService _gameService;

        public GameHub(GameService gameService)
        {
            _gameService = gameService;
        }

        public async Task RegisterPlayer(string playerName)
        {
            var player = _gameService.GetOrCreatePlayer(playerName, Context.ConnectionId);
            await Clients.Caller.SendAsync("PlayerRegistered", new
            {
                id = player.Id,
                name = player.Name,
                displayName = player.DisplayName
            });
        }

        public async Task CreateGameSession(string sessionName, string creatorId)
        {
            var creator = _gameService.GetPlayerById(creatorId);
            if (creator == null) return;

            var session = _gameService.CreateGameSession(sessionName, creator);
            if (session == null)
            {
                await Clients.Caller.SendAsync("OperationFailed",
                    "Вы уже находитесь в игре. Завершите её, чтобы создать новую.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
            await Clients.Caller.SendAsync("GameSessionCreated", session);
            await UpdateAvailableSessions();
        }

        public List<GameSession> GetAvailableSessions()
        {
            return _gameService.GetAvailableSessions();
        }

        public async Task JoinGameSession(string sessionId, string playerId)
        {
            var player = _gameService.GetPlayerById(playerId);
            if (player == null) return;

            var session = _gameService.JoinGameSession(sessionId, player);
            if (session != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, session.Id);
                _gameService.UpdatePlayerConnection(player.Id, Context.ConnectionId);
                await Clients.Group(sessionId).SendAsync("GameStarted", session);
                await UpdateAvailableSessions();
            }
            else
            {
                await Clients.Caller.SendAsync("JoinFailed",
                    "Не удалось присоединиться: вы уже в игре или пытаетесь войти в свою.");
            }
        }

        public async Task MakeMove(string sessionId, string playerId, int position)
        {
            if (_gameService.MakeMove(sessionId, playerId, position))
            {
                var session = _gameService.GetGameSession(sessionId);
                await Clients.Group(sessionId).SendAsync("MoveMade", session);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _gameService.DisconnectPlayer(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        private async Task UpdateAvailableSessions()
        {
            var sessions = _gameService.GetAvailableSessions();
            await Clients.All.SendAsync("AvailableSessionsUpdated", sessions);
        }
    }
}