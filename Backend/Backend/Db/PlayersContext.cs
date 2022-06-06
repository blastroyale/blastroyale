using Backend.Db;
using Microsoft.EntityFrameworkCore;

namespace Login.Db;

/// <summary>
/// Class represents our database in Entity Framework. It defines which Db Sets (AKA Tables) we have access to
/// for this given context.
/// </summary>
public class PlayersContext : DbContext
{
	/// <summary>
	/// Entity Framework reference to our Players table.
	/// </summary>
	public DbSet<FlgPlayer> Players { get; set; } = null!;
	
	public PlayersContext(DbContextOptions<PlayersContext> options) : base(options)
	{
		Init();
	}

	private void Init()
	{
		Database.EnsureCreated();
	}

}