using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using Quantum;

namespace BlastRoyaleNFTPlugin.Data;


public class NFTCollectionSyncConfigData
{
	public const string COLLECTION_CORPOS_POLYGON = "CorposLegacy";
	public const string COLLECTION_CORPOS_ETH = "Corpos";
	public const string COLLECTION_GAMESGG_GAMERS_ETH = "GamesGGGamers";
	public const string COLLECTION_PLAGUE_DOCTOR_IMX = "PlagueDoctor";
	public const string COLLECTION_PIRATE_NATION_ETH = "PirateNation";

	private Dictionary<string, NFTCollectionSyncConfiguration> NFTCollectionSyncConfigs { get; } = new();
	
	public IReadOnlyList<NFTCollectionSyncConfiguration> NFTCollections { get; }

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
		NFTCollectionSyncConfigs[COLLECTION_CORPOS_POLYGON] = new NFTCollectionSyncConfiguration
        {
            CollectionName = COLLECTION_CORPOS_POLYGON,
			CanSync = true,
			ItemSyncConfiguration = ResolveCorposSyncConfig()
        };
    
        // Add Corpos collection (Ethereum)
		NFTCollectionSyncConfigs[COLLECTION_CORPOS_ETH] = new NFTCollectionSyncConfiguration
        {
            CollectionName = COLLECTION_CORPOS_ETH,
            CanSync = true,
			CanSyncNFTImage = true,
			NFTImagePrefix = "Corpo",
			ItemSyncConfiguration = ResolveCorposSyncConfig()
        };
		
		  
		// Add Plague Doctor collection (IMX)
		NFTCollectionSyncConfigs[COLLECTION_PLAGUE_DOCTOR_IMX] = new NFTCollectionSyncConfiguration
		{
			CollectionName = COLLECTION_PLAGUE_DOCTOR_IMX,
			CanSync = true,
			CanSyncNFTImage = false,
			ItemSyncConfiguration = ResolvePlagueDoctorSyncConfig()
		};
  
     
		// Add GamesGGGamers collection (Ethereum)
		NFTCollectionSyncConfigs[COLLECTION_GAMESGG_GAMERS_ETH] = new NFTCollectionSyncConfiguration
        {
            CollectionName = COLLECTION_GAMESGG_GAMERS_ETH,
            CanSync = false,
			CanSyncNFTImage = false,
			ItemSyncConfiguration = new List<InItemSyncConfiguration>
			{
				new()
				{
					ItemCollectionCategory = CollectionCategories.PLAYER_SKINS,
					TraitRequired = false,
					InGameRewards = new List<GameId> { GameId.PlayerSkinGamer }
				},
			}
        };
		
		// Add PirateNation collection (Ethereum)
		NFTCollectionSyncConfigs[COLLECTION_PIRATE_NATION_ETH] = new NFTCollectionSyncConfiguration
		{
			CollectionName = COLLECTION_PIRATE_NATION_ETH,
			CanSync = false,
			CanSyncNFTImage = false,
			ItemSyncConfiguration =new List<InItemSyncConfiguration>
			{
				new()
				{
					ItemCollectionCategory = CollectionCategories.PLAYER_SKINS,
					TraitRequired = false,
					InGameRewards = new List<GameId> { GameId.PlayerSkinPirateCaptain }
				},
			}
		};

    }

	//
	// Disclaimer!
	// Ideally, this is going to be a remote JSON that can be modified, adding/removing traits and items to be synced and it's going to be served remotely.
	// So we can implement some sort of cache mechanism that would update the JSON config periodically after caching expires, or restarting the server to
	// load a new one instead of waiting for the cache to expire
	//
	private List<InItemSyncConfiguration> ResolvePlagueDoctorSyncConfig()
	{
		
		var characterCategoryItemConfigs = new List<InItemSyncConfiguration>
		{
			new()
			{
				ItemCollectionCategory = CollectionCategories.PLAYER_SKINS,
				TraitRequired = false,
				InGameRewards = new List<GameId> { GameId.PlayerSkinPlagueDoctor }
			},
		};

		var meleeCategoryItemConfigs = new List<InItemSyncConfiguration>
		{
			new()
			{
				ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
				TraitRequired = true,
				TraitName = "item",
				TraitValue = "Plaguebearer Staff",
				InGameRewards = new List<GameId> {GameId.MeleeSkinDoctorStaff}
			}
		};
		
		return characterCategoryItemConfigs.Union(meleeCategoryItemConfigs).ToList();
	}

	
	private List<InItemSyncConfiguration> ResolveCorposSyncConfig()
	{
		//Setup Corpos Character Sync for Traits
		var characterCategoryItemConfigs = new List<InItemSyncConfiguration>
		{
			new()
			{
				ItemCollectionCategory = CollectionCategories.PLAYER_SKINS,
				TraitRequired = true,
				TraitName = "body",
				TraitValue = "masculine",
				InGameRewards = new List<GameId> { GameId.MaleCorpos, GameId.PlayerSkinCorposMaleDark }
			},
			new()
			{
				ItemCollectionCategory = CollectionCategories.PLAYER_SKINS,
				TraitRequired = true,
				TraitName = "body",
				TraitValue = "feminine",
				InGameRewards = new List<GameId> { GameId.FemaleCorpos, GameId.PlayerSkinCorposFemaleDark }
			}
		};

		//Setup Corpos Melee Sync for Traits
		var meleeCategoryItemConfig = new List<InItemSyncConfiguration>
		{
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Wrench",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinWrench }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Mighty Sledge",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinMightySledge }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Gigahammer",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinGigaMelee }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Superdog",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinSausage }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Tv Takedown",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinTvTakedown }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Putter",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinPutter }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Thunder Axe",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinThunderAxe }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Atom Slicer",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinAtomSlicer }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Fatebringer",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinAtomSlicer }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Hatchet",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinHatchet }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Shredders",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinHatchet }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Sir Quacks-a-lot",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinSirQuacks }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Creator Kit",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinWrench, GameId.MeleeSkinAtomSlicer }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Sigma Rifle",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinGigaMelee }
		    },
		    new()
			{
		        ItemCollectionCategory = CollectionCategories.MELEE_SKINS,
		        TraitRequired = true,
		        TraitName = "item",
		        TraitValue = "Marble Rifle",
		        InGameRewards = new List<GameId> { GameId.MeleeSkinGigaMelee }
		    }
		};
		
		//Setup Corpos Avatar Sync for Traits
		var profilePicturesCategoryItemConfigs = new List<InItemSyncConfiguration>
		{
			new()
			{
				ItemCollectionCategory = CollectionCategories.PROFILE_PICTURE,
				TraitRequired = true,
				TraitName = "body",
				TraitValue = "masculine",
				InGameRewards = new List<GameId> { GameId.AvatarCorpomask, GameId.AvatarMalecorpoangryads, GameId.AvatarMalecorpoconcept, GameId.AvatarMalecorposcaredsticker, GameId.AvatarMaleCorpoDark }
			},
			new()
			{
				ItemCollectionCategory = CollectionCategories.PROFILE_PICTURE,
				TraitRequired = true,
				TraitName = "body",
				TraitValue = "feminine",
				InGameRewards = new List<GameId> { GameId.AvatarFemalecorpo, GameId.AvatarFemalecorpoconcept, GameId.AvatarFemalecorpophonesticker, GameId.AvatarFemalecorposticker, GameId.AvatarFemaleCorpoDark }
			},
			new()
			{
				ItemCollectionCategory = CollectionCategories.PROFILE_PICTURE,
				TraitRequired = true,
				TraitName = "eyewear",
				TraitValue = "Bunny Helmet",
				InGameRewards = new List<GameId> { GameId.Avatar4 }
			}
		};
		
		
		return characterCategoryItemConfigs.Union(meleeCategoryItemConfig).Union(profilePicturesCategoryItemConfigs).ToList();
	}
}


/// <summary>
/// Configuration class used to define the properties for synchronizing NFT collections.
/// This class holds metadata related to an NFT collection such as its name, associated categories,
/// game IDs, and whether it can be synchronized.
/// </summary>
[Serializable]
public class NFTCollectionSyncConfiguration
{
	/// <summary>
	/// Gets or sets the name of the NFT collection.
	/// This is typically used to identify the collection in the system.
	/// </summary>
	public string CollectionName { get; set; }
	

	/// <summary>
	/// Gets or sets a value indicating whether the collection can be synchronized.
	/// If true, this collection is eligible for sync operations with external systems such as blockchain or databases.
	/// </summary>
	public bool CanSync { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the collection can be synchronized.
	/// If true, this collection is eligible for sync NFTs Images to be used as Profile Pictures
	/// </summary>
	public bool CanSyncNFTImage { get; set; }
	
	/// <summary>
	/// Gets or sets a filter value used for ProfileImage synchronization.
	/// Since all player NFTs are now retrieved in a single call, there is no built-in mechanism 
	/// to associate responses with specific collections.
	/// This workaround ensures that players can sync their Corpos PFP to the game.
	/// </summary>
	public string NFTImagePrefix { get; set; }
	
	/// <summary>
	/// Gets or sets a list of synchronization configurations that define the validation criteria 
	/// for determining whether a player can own a skin in the collection. Each configuration specifies:
	/// - The category of the item.
	/// - Whether a specific trait is required for validation.
	/// - The trait name and its expected value (e.g., "body" must be "masculine").
	/// - The in-game rewards associated with the configuration.
	/// </summary>
	/// <remarks>
	/// - If no validation rules are defined, skins will be granted without restriction.
	/// - Trait-based validation (e.g., "body", "rarity") ensures that only players meeting the 
	///   specified criteria can own a particular skin.
	/// - This configuration is used during synchronization to enforce ownership rules.
	/// </remarks>
	public List<InItemSyncConfiguration> ItemSyncConfiguration { get; set; } = new();
}

[Serializable]
public class InItemSyncConfiguration
{
	public CollectionCategory ItemCollectionCategory { get; set; }

	public bool TraitRequired { get; set; }

	public string TraitName { get; set; }
	
	public string TraitValue { get; set; }
	
	public List<GameId> InGameRewards { get; set; } = new();
}


[Serializable]
public class AvatarSyncConfiguration
{

	
	
	public bool FetchImageFromUrl { get; set; }


}