namespace BAsketball_stats.Models
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Conference { get; set; } = string.Empty;

        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<Game> HomeGames { get; set; } = new List<Game>();
        public ICollection<Game> AwayGames { get; set; } = new List<Game>();
    }

    public class Player
    {
        public int Id { get; set; }
        public int NbaPersonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public double PointsPerGame { get; set; }
        public int GamesPlayed { get; set; }
        public int TeamId { get; set; }
        public Team Team { get; set; } = null!;

        public ICollection<PlayerStat> Stats { get; set; } = new List<PlayerStat>();
    }

    public class Game
    {
        public int Id { get; set; }
        public long NbaGameId { get; set; }
        public DateTime GameDate { get; set; }
        public string GameType { get; set; } = string.Empty;   // "Regular Season" | "Playoffs" | "Preseason"
        public string GameLabel { get; set; } = string.Empty;  // e.g. "West Conf. Finals"
        public int HomeTeamId { get; set; }
        public Team HomeTeam { get; set; } = null!;
        public int AwayTeamId { get; set; }
        public Team AwayTeam { get; set; } = null!;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public string? ArenaName { get; set; }
        public string? ArenaCity { get; set; }
        public int? Attendance { get; set; }

        public ICollection<PlayerStat> PlayerStats { get; set; } = new List<PlayerStat>();
    }

    public class PlayerStat
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        public int GameId { get; set; }
        public Game Game { get; set; } = null!;

        public double? Minutes { get; set; }
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
        public string? Comment { get; set; }  // "DNP - Coach's Decision" etc.
    }
}
