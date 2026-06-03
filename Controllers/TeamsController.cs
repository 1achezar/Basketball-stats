using BAsketball_stats.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Basketball_stats.Controllers;

[ApiController]
[Route("api/[controller]")] // Automatically maps to /api/teams
public class TeamsController : ControllerBase
{
    private readonly BasketballDbContext _db;

    // Use Dependency Injection to bring in our database context
    public TeamsController(BasketballDbContext db)
    {
        _db = db;
    }

    // GET: api/teams
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Team>>> GetTeams()
    {
        var teams = await _db.Teams.ToListAsync();
        return Ok(teams);
    }

    // GET: api/teams/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Team>> GetTeam(int id)
    {
        var team = await _db.Teams.FindAsync(id);
        if (team is null)
        {
            return NotFound($"Team with ID {id} not found.");
        }
        return Ok(team);
    }

    [HttpPost]
    public async Task<ActionResult<Team>> CreateTeam(Team newTeam)
    {
        _db.Teams.Add(newTeam);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTeam), new ValueTuple<int>(newTeam.Id), newTeam);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTeam(int id)
    {
        var team = await _db.Teams.FindAsync(id);
        if (team is null)
        {
            return NotFound($"Team with ID {id} not found.");
        }

        _db.Teams.Remove(team);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}