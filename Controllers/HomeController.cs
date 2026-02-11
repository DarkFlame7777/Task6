using Microsoft.AspNetCore.Mvc;
using Task6.Models;
using Task6.Services;

namespace Task6.Controllers
{
    public class HomeController : Controller
    {
        private readonly GameService _gameService;

        public HomeController(GameService gameService)
        {
            _gameService = gameService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SetPlayerName([FromBody] PlayerNameModel model)
        {
            HttpContext.Session.SetString("PlayerName", model.Name);
            return Ok();
        }

        public IActionResult GetPlayerName()
        {
            return Content(HttpContext.Session.GetString("PlayerName") ?? "");
        }

        public IActionResult GetPlayerStats(string playerId)
        {
            return Json(_gameService.GetPlayerStats(playerId));
        }
    }
}