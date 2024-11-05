using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using Quantum;

namespace BlastRoyaleNFTPlugin.Data;


public class NFTCollectionSyncConfigData
{
	public const string COLLECTION_CORPOS_POLYGON = "CorposLegacy";
	public const string COLLECTION_CORPOS_ETH = "Corpos";
	public const string COLLECTION_GAMESGG_GAMERS_ETH = "GamesGGGamers";
	public const string COLLECTION_PLAGUE_DOCTOR_IMX = "PlagueDoctor";
	public const string COLLECTION_PIRATE_NATION_ETH = "PirateNation";

	private Dictionary<string, NFTCollectionSyncConfigModel> NFTCollectionSyncConfigs { get; } = new();
	
	public IReadOnlyList<NFTCollectionSyncConfigModel> NFTCollections { get; }

	public NFTCollectionSyncConfigData()
	{
		LoadConfiguredCollections();

		NFTCollections = NFTCollectionSyncConfigs.Values.ToList();
	}

	/// <summary>
	/// Manually sets up the configured NFT collections for synchronization.
	/// Currently, this method is populated manually, but in the future, it would be ideal to fetch 
	/// collection data dynamically from an external service, such as the Blockchain Service, 
	/// to ensure up-to-date and accurate data for synchronization.
	/// </summary>
	/// <summary>
    /// Sets up the NFTCollectionSyncConfig instances and adds them to the dictionary.
    /// </summary>
    public  void LoadConfiguredCollections()
    {
        // Add Corpos Legacy collection (Polygon)
		NFTCollectionSyncConfigs[COLLECTION_CORPOS_POLYGON] = new NFTCollectionSyncConfigModel
        {
            CollectionName = COLLECTION_CORPOS_POLYGON,
            CollectionSyncCategories = new[] { CollectionCategories.PLAYER_SKINS },
            CanSync = true,
			CollectionItems = new Dictionary<GameId, Dictionary<string, string>>
			{
				{GameId.MaleCorpos, new Dictionary<string, string> {{"body", "masculine"}}},		
				{GameId.FemaleCorpos, new Dictionary<string, string> {{"body", "feminine"}}}		
			}
        };
    
        // Add Corpos collection (Ethereum)
		NFTCollectionSyncConfigs[COLLECTION_CORPOS_ETH] = new NFTCollectionSyncConfigModel
        {
            CollectionName = COLLECTION_CORPOS_ETH,
            CollectionSyncCategories = new[] { CollectionCategories.PLAYER_SKINS, CollectionCategories.PROFILE_PICTURE },
            CanSync = true,
			CollectionItems = new Dictionary<GameId, Dictionary<string, string>>
			{
				{GameId.MaleCorpos, new Dictionary<string, string> {{"body", "masculine"}}},		
				{GameId.FemaleCorpos, new Dictionary<string, string> {{"body", "feminine"}}}		
			}
        };
    
        // Add GamesGGGamers collection (Ethereum)
		NFTCollectionSyncConfigs[COLLECTION_GAMESGG_GAMERS_ETH] = new NFTCollectionSyncConfigModel
        {
            CollectionName = COLLECTION_GAMESGG_GAMERS_ETH,
            CollectionSyncCategories = new[] { CollectionCategories.PLAYER_SKINS },
            CanSync = false,
			CollectionItems = new Dictionary<GameId, Dictionary<string, string>>
			{
				{GameId.PlayerSkinGamer, null}		
			}
        };
		
		// Add PirateNation collection (Ethereum)
		NFTCollectionSyncConfigs[COLLECTION_PIRATE_NATION_ETH] = new NFTCollectionSyncConfigModel
		{
			CollectionName = COLLECTION_PIRATE_NATION_ETH,
			CollectionSyncCategories = new[] { CollectionCategories.PLAYER_SKINS },
			CanSync = false,
			CollectionItems = new Dictionary<GameId, Dictionary<string, string>>
			{
				{GameId.PlayerSkinPirateCaptain, null}		
			}
		};
  
        // Add Plague Doctor collection (IMX)
		NFTCollectionSyncConfigs[COLLECTION_PLAGUE_DOCTOR_IMX] = new NFTCollectionSyncConfigModel
        {
            CollectionName = COLLECTION_PLAGUE_DOCTOR_IMX,
            CollectionSyncCategories = new[] { CollectionCategories.PLAYER_SKINS },
            CanSync = false,
			CollectionItems = new Dictionary<GameId, Dictionary<string, string>>
			{
				{GameId.PlayerSkinPlagueDoctor, null}		
			}
        };
    
    }

}


/// <summary>
/// Configuration class used to define the properties for synchronizing NFT collections.
/// This class holds metadata related to an NFT collection such as its name, associated categories,
/// game IDs, and whether it can be synchronized.
/// </summary>
public class NFTCollectionSyncConfigModel
{
	/// <summary>
	/// Gets or sets the name of the NFT collection.
	/// This is typically used to identify the collection in the system.
	/// </summary>
	public string CollectionName { get; set; }

	/// <summary>
	/// Gets or sets an array of categories that the collection belongs to.
	/// These categories are used to classify the collection (e.g., "Player Skins", "Profile Pictures").
	/// Each category may have its own specific synchronization method, meaning that collections
	/// in different categories will be synced using different processes or strategies.
	/// </summary>
	/// <remarks>
	/// - Categories help determine how collections are classified and synced.
	/// - For example, a collection categorized as "Player Skins" may use a different sync method
	///   compared to a collection categorized as "Profile Pictures."
	/// - During the synchronization process, the system checks the category and uses the appropriate
	///   method to sync the collection.
	/// </remarks>
	public CollectionCategory[] CollectionSyncCategories { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the collection can be synchronized.
	/// If true, this collection is eligible for sync operations with external systems such as blockchain or databases.
	/// </summary>
	public bool CanSync { get; set; }

	/// <summary>
	/// Gets or sets a dictionary that maps a <see cref="GameId"/> to the necessary traits required to validate
	/// if the player can own the skin in the collection. The dictionary contains trait names and their corresponding
	/// required values (e.g., {"body": "masculine"}), and it is used during synchronization to determine ownership.
	/// </summary>
	/// <remarks>
	/// - If no dictionary is provided or the dictionary is empty, no special validation will be applied,
	///   and the skin will be added to the player without any additional checks.
	/// - This dictionary allows for the validation of traits such as "body" or "rarity" to determine if a player
	///   is eligible to own a particular skin.
	/// </remarks>
	public Dictionary<GameId, Dictionary<string, string>> CollectionItems { get; set; } = new();
}
