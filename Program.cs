using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BasketballDbContext>(options =>
    options.UseSqlite("Data Source=basketball.db"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BasketballDbContext>();
    await db.Database.EnsureCreatedAsync();

    string dataDir = Directory.GetCurrentDirectory();
    await LocalDataSeeder.SeedData(db, dataDir);
}

app.Run();

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Conference { get; set; } = string.Empty;
}

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int PointsPerGame { get; set; }
    public int TeamId { get; set; }
    public Team? Team { get; set; }
}

public class PlayerStat
{
    public int Id { get; set; }
    public string GameId { get; set; } = string.Empty;
    public string GameDate { get; set; } = string.Empty;
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int TeamId { get; set; }
    public string TeamAbbreviation { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Assists { get; set; }
    public int Rebounds { get; set; }
    public int Steals { get; set; }
    public int Blocks { get; set; }
    public int Turnovers { get; set; }
    public double FieldGoalPct { get; set; }
    public double ThreePointPct { get; set; }
    public double FreeThrowPct { get; set; }
    public int PlusMinus { get; set; }
    public bool Won { get; set; }
    public bool IsHome { get; set; }
    public string? Comment { get; set; }
}

public class BasketballDbContext : DbContext
{
    public BasketballDbContext(DbContextOptions<BasketballDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerStat> PlayerStats => Set<PlayerStat>();
}

public static class LocalDataSeeder
{
    public static async Task SeedData(BasketballDbContext db, string dataDir)
    {
        if (!db.Teams.Any())
        {
            var conferences = new Dictionary<int, string>
            {
                {1610612737, "East"}, {1610612738, "East"}, {1610612739, "East"},
                {1610612740, "West"}, {1610612741, "East"}, {1610612742, "West"},
                {1610612743, "West"}, {1610612744, "West"}, {1610612745, "West"},
                {1610612746, "West"}, {1610612747, "West"}, {1610612748, "East"},
                {1610612749, "East"}, {1610612750, "West"}, {1610612751, "East"},
                {1610612752, "East"}, {1610612753, "East"}, {1610612754, "East"},
                {1610612755, "East"}, {1610612756, "West"}, {1610612757, "West"},
                {1610612758, "West"}, {1610612759, "West"}, {1610612760, "West"},
                {1610612761, "East"}, {1610612762, "West"}, {1610612763, "West"},
                {1610612764, "East"}, {1610612765, "East"}, {1610612766, "East"}
            };

            var items = new Dictionary<int, Team>();

            // Primary source: TeamHistories.csv
            string historiesPath = Path.Combine(dataDir, "TeamHistories.csv");
            if (File.Exists(historiesPath))
            {
                var lines = await File.ReadAllLinesAsync(historiesPath);
                foreach (var line in lines.Skip(1))
                {
                    var c = SplitCsv(line);
                    if (c.Length >= 7 && int.TryParse(c[0], out int id))
                    {
                        if (c[5] == "2100" && c[6] == "NBA")
                        {
                            conferences.TryGetValue(id, out var conf);
                            items[id] = new Team
                            {
                                Id = id,
                                City = c[1].Trim(),
                                Name = c[2].Trim(),
                                Conference = conf ?? "Unknown"
                            };
                        }
                    }
                }
            }

            // Fallback: build teams from TeamStatistics.csv if TeamHistories.csv is missing or empty
            if (!items.Any())
            {
                string statsPath = Path.Combine(dataDir, "TeamStatistics.csv");
                if (File.Exists(statsPath))
                {
                    var lines = await File.ReadAllLinesAsync(statsPath);
                    foreach (var line in lines.Skip(1))
                    {
                        var c = SplitCsv(line);
                        // teamId=col4, teamCity=col2, teamName=col3
                        if (c.Length >= 5 && int.TryParse(c[4], out int id) && id > 0 && !items.ContainsKey(id))
                        {
                            conferences.TryGetValue(id, out var conf);
                            items[id] = new Team
                            {
                                Id = id,
                                City = c[2].Trim(),
                                Name = c[3].Trim(),
                                Conference = conf ?? "Unknown"
                            };
                        }
                    }
                }
            }

            if (items.Any())
            {
                await db.Teams.AddRangeAsync(items.Values);
                await db.SaveChangesAsync();
            }
        }

        if (!db.Players.Any())
        {
            string path = Path.Combine(dataDir, "Players.csv");
            if (File.Exists(path))
            {
                var lines = await File.ReadAllLinesAsync(path);
                var players = new List<Player>();

                var realLifeStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "LeBron James", 27 }, { "Kareem Abdul-Jabbar", 25 },
                    { "Michael Jordan", 30 }, { "Ja Morant", 22 },
                    { "Stephen Curry", 25 }, { "Kevin Durant", 27 },
                    { "Kobe Bryant", 25 }, { "Shaquille O'Neal", 24 },
                    { "Luka Doncic", 28 }
                };

                // Known player→team assignments based on real-life rosters
                var knownTeams = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "LeBron James",        1610612747 }, // Lakers
                    { "Anthony Davis",       1610612747 }, // Lakers
                    { "Stephen Curry",       1610612744 }, // Warriors
                    { "Klay Thompson",       1610612742 }, // Mavericks
                    { "Draymond Green",      1610612744 }, // Warriors
                    { "Kevin Durant",        1610612756 }, // Suns
                    { "Devin Booker",        1610612756 }, // Suns
                    { "Luka Doncic",         1610612747 }, // Lakers (traded 2025)
                    { "Kyrie Irving",        1610612742 }, // Mavericks
                    { "Ja Morant",           1610612763 }, // Grizzlies
                    { "Giannis Antetokounmpo", 1610612749 }, // Bucks
                    { "Damian Lillard",      1610612749 }, // Bucks
                    { "Joel Embiid",         1610612755 }, // 76ers
                    { "Tyrese Maxey",        1610612755 }, // 76ers
                    { "Jayson Tatum",        1610612738 }, // Celtics
                    { "Jaylen Brown",        1610612738 }, // Celtics
                    { "Nikola Jokic",        1610612743 }, // Nuggets
                    { "Jamal Murray",        1610612743 }, // Nuggets
                    { "Shai Gilgeous-Alexander", 1610612760 }, // Thunder
                    { "Chet Holmgren",       1610612760 }, // Thunder
                    { "Karl-Anthony Towns",  1610612752 }, // Knicks
                    { "Jalen Brunson",       1610612752 }, // Knicks
                    { "Donovan Mitchell",    1610612739 }, // Cavaliers
                    { "Darius Garland",      1610612739 }, // Cavaliers
                    { "Trae Young",          1610612737 }, // Hawks
                    { "Dejounte Murray",     1610612737 }, // Hawks
                    { "Zion Williamson",     1610612740 }, // Pelicans
                    { "Brandon Ingram",      1610612740 }, // Pelicans
                    { "Pascal Siakam",       1610612754 }, // Pacers
                    { "Tyrese Haliburton",   1610612754 }, // Pacers
                    { "Paolo Banchero",      1610612753 }, // Magic
                    { "Franz Wagner",        1610612753 }, // Magic
                    { "Victor Wembanyama",   1610612759 }, // Spurs
                    { "Michael Jordan",      1610612741 }, // Bulls (historical)
                    { "Scottie Pippen",      1610612741 }, // Bulls (historical)
                    { "Kareem Abdul-Jabbar", 1610612747 }, // Lakers (historical)
                    { "Kobe Bryant",         1610612747 }, // Lakers (historical)
                    { "Shaquille O'Neal",    1610612747 }, // Lakers (historical)
                    { "Magic Johnson",       1610612747 }, // Lakers (historical)
                    { "Larry Bird",          1610612738 }, // Celtics (historical)
                    { "Kevin Garnett",       1610612750 }, // Timberwolves (historical)
                    { "Tim Duncan",          1610612759 }, // Spurs (historical)
                    { "Tony Parker",         1610612759 }, // Spurs (historical)
                    { "Manu Ginobili",       1610612759 }, // Spurs (historical)
                    { "Dirk Nowitzki",       1610612742 }, // Mavericks (historical)
                    { "Allen Iverson",       1610612755 }, // 76ers (historical)
                    { "Charles Barkley",     1610612755 }, // 76ers (historical)
                    { "Hakeem Olajuwon",     1610612745 }, // Rockets (historical)
                    { "James Harden",        1610612745 }, // Rockets (historical)
                };

                // All 30 real NBA team IDs for distributing unknown players
                var allTeamIds = new int[]
                {
                    1610612737, 1610612738, 1610612739, 1610612740, 1610612741,
                    1610612742, 1610612743, 1610612744, 1610612745, 1610612746,
                    1610612747, 1610612748, 1610612749, 1610612750, 1610612751,
                    1610612752, 1610612753, 1610612754, 1610612755, 1610612756,
                    1610612757, 1610612758, 1610612759, 1610612760, 1610612761,
                    1610612762, 1610612763, 1610612764, 1610612765, 1610612766
                };

                foreach (var line in lines.Skip(1))
                {
                    var c = SplitCsv(line);
                    if (c.Length >= 12 && int.TryParse(c[0], out int id))
                    {
                        string fn = c[1].Trim();
                        string ln = c[2].Trim();
                        string name = $"{fn} {ln}".Trim();

                        if (string.IsNullOrEmpty(fn) && string.IsNullOrEmpty(ln)) continue;

                        string pos = "SG";
                        if (c[9] == "1") pos = "PG";
                        else if (c[10] == "1") pos = "SF";
                        else if (c[11] == "1") pos = "C";

                        int ppg = realLifeStats.TryGetValue(name, out int realPPG) ? realPPG : (Math.Abs(id) % 15) + 4;

                        // Use known team if available, otherwise spread across all 30 teams by player ID
                        int teamId = knownTeams.TryGetValue(name, out int knownTeamId)
                            ? knownTeamId
                            : allTeamIds[Math.Abs(id) % allTeamIds.Length];

                        players.Add(new Player
                        {
                            Id = id,
                            Name = name,
                            Position = pos,
                            PointsPerGame = ppg,
                            TeamId = teamId
                        });
                    }
                }

                // Only keep players whose TeamId actually exists in the Teams table
                var validTeamIds = db.Teams.Select(t => t.Id).ToHashSet();
                players = players.Where(p => validTeamIds.Contains(p.TeamId)).ToList();

                if (players.Any())
                {
                    await db.Players.AddRangeAsync(players);
                    await db.SaveChangesAsync();
                }
            }
        }

        if (!db.PlayerStats.Any())
        {
            string path = Path.Combine(dataDir, "TeamStatistics.csv");
            if (File.Exists(path))
            {
                var lines = await File.ReadAllLinesAsync(path);
                var stats = new List<PlayerStat>();

                foreach (var line in lines.Skip(1))
                {
                    var c = SplitCsv(line);
                    if (c.Length >= 30)
                    {
                        int.TryParse(c[4], out int tId);
                        int.TryParse(c[10], out int pts);
                        int.TryParse(c[12], out int ast);
                        int.TryParse(c[13], out int blk);
                        int.TryParse(c[14], out int stl);
                        int.TryParse(c[26], out int reb);
                        int.TryParse(c[28], out int tov);
                        double.TryParse(c[17], out double fgPct);
                        double.TryParse(c[20], out double threePct);
                        double.TryParse(c[23], out double ftPct);
                        double.TryParse(c[29], out double pm);

                        bool home = c[8] == "1";
                        bool won = c[9] == "1";
                        bool isDnp = c.Length > 31 && c[31] == "1";

                        stats.Add(new PlayerStat
                        {
                            GameId = c[0].Trim(),
                            GameDate = c[1].Trim(),
                            PlayerId = 0,
                            PlayerName = "Team Log Record",
                            TeamId = tId,
                            TeamAbbreviation = c[3].Trim(),
                            Points = pts,
                            Assists = ast,
                            Rebounds = reb,
                            Steals = stl,
                            Blocks = blk,
                            Turnovers = tov,
                            FieldGoalPct = fgPct,
                            ThreePointPct = threePct,
                            FreeThrowPct = ftPct,
                            PlusMinus = (int)pm,
                            Won = won,
                            IsHome = home,
                            Comment = isDnp && c.Length > 36 ? c[36] : null
                        });
                    }
                }

                foreach (var chunk in stats.Chunk(1000))
                {
                    await db.PlayerStats.AddRangeAsync(chunk);
                    await db.SaveChangesAsync();
                }
            }
        }
    }

    private static string[] SplitCsv(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        foreach (char ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}