using BAsketball_stats.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Basketball_stats.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly BasketballDbContext _db;

        public PlayersController(BasketballDbContext db)
        {
            _db = db;
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Player>>> SearchPlayers(
            [FromQuery] string? team,
            [FromQuery] string? position,
            [FromQuery] int? minScore)
        {
            var query = _db.Players.AsQueryable();

            if (!string.IsNullOrWhiteSpace(position))
            {
                query = query.Where(p => p.Position.ToLower() == position.ToLower());
            }

            if (minScore.HasValue)
            {
                query = query.Where(p => p.PointsPerGame >= minScore.Value);
            }

            return Ok(await query.Include(p => p.Team).ToListAsync());
        }
    }
}