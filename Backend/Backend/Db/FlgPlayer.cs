using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Db;

/// <summary>
/// Class that represents a registered player in our services.
/// This object is to be used as an Entity on entity framework, it reflects the columns of our Player's table.
/// Changing property names or adding properties on this class might require a migration.
/// </summary>
[Index(nameof(Wallet))]
public class FlgPlayer
{
	[Key] public string PlayfabId { get; set; } = null!;
	public string? Wallet { get; set; }
}