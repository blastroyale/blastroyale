using static FirstLight.Editor.Ids.Ids.GroupSource;

namespace FirstLight.Editor.Ids
{
	/// <summary>
	/// Used for generation, NEVER USE THIS FOR REFERENCE
	/// </summary>
	internal class Ids
	{
		/// <summary>
		/// Keep the same ids
		/// </summary>
		internal enum GroupSource : int
		{
			GameDesign = 0,
			Currency = 1,
			ResourcePool = 6,
			Resource = 5,
			Map = 8,
			Helmet = 13,
			Equipment = 12,
			Gear = 24,
			Simple = 25,
			Weapon = 11,
			Melee = 20,
			Deprecated = 34,
			Amulet = 16,
			Armor = 17,
			Shield = 15,
			Collection = 14,
			Assassin = 31,
			Corpo = 32,
			BotItem = 4,
			Punk = 30,
			Superstar = 29,
			Consumable = 19,
			Collectable = 27,
			Ammo = 7,
			Chest = 2,
			Special = 21,
			Destructible = 26,
			DummyCharacter = 3,
			Platform = 37,
			Core = 22,
			IAP = 23,
			GenericCollectionItem = 39,


			#region CollectionGroup

			MeleeSkin = 38,
			PlayerSkin = 18,
			Glider = 28,
			DeathMarker = 10,
			ProfilePicture = 9,
			Footprint = 33,

			#endregion
		}

		internal static GameIdHolder GameIds = new ()
		{
			{"Random", 0, GameDesign},
			{"Any", 126, GameDesign },
			
			{"RealMoney", 125, Currency},
			{"COIN", 61, Currency},
			{"BlastBuck", 105, Currency},
			{"BLST", 14, Currency},
			{"Fragments", 112, Currency},
			{"CS", 12, Currency, ResourcePool},
			{"XP", 3, Resource},
			{"Trophies", 22, Resource},
			{"BPP", 26, Resource, ResourcePool},
			{"FloodCity", 137, Map},
			{"MainDeck", 143, Map},
			{"FtueDeck", 5, Map},
			{"SmallWilderness", 144, Map},
			{"FloodCitySimple", 7, Map},
			{"BlimpDeck", 8, Map},
			{"BRGenesis", 9, Map},
			{"MapTestScene", 63, Map},
			{"TestScene", 11, Map},
			{"NewBRMap", 65, Map},
			{"FtueMiniMap", 133, Map},
			{"District", 108, Map},
			{"TestAssetsMap", 1009, Map},
			{"BattlelandsMap", 113, Map},
			{"IslandsMap", 114, Map},
			{"MazeMayhem", 115, Map},
			{"Cemetery", 197, Map},
			{"Fortress", 198, Map},
			{"IslandOne", 200, Map},
			{"MausHelmet", 24, Helmet, Equipment, Gear, Simple},
			{"SoldierHelmet", 49, Helmet, Equipment, Gear},
			{"RiotHelmet", 50, Helmet, Equipment, Gear},
			{"WarriorHelmet", 51, Helmet, Equipment, Gear},
			{"RoadHelmet", 23, Helmet, Equipment, Gear, Simple},
			{"FootballHelmet", 30, Helmet, Equipment, Gear},
			{"BaseballHelmet", 32, Helmet, Equipment, Gear},
			{"HockeyHelmet", 33, Helmet, Equipment, Gear},
			{"Hammer", 31, Weapon, Equipment, Melee},
			{"ApoCrossbow", 60, Weapon, Equipment, Deprecated},
			{"ApoShotgun", 71, Weapon, Equipment, Deprecated},
			{"ApoSMG", 78, Weapon, Equipment, Simple},
			{"ApoRifle", 79, Weapon, Equipment},
			{"ApoSniper", 84, Weapon, Equipment, Deprecated},
			{"ApoRPG", 86, Weapon, Equipment, Deprecated},
			{"ApoMinigun", 87, Weapon, Equipment},
			{"ModPistol", 88, Weapon, Equipment, Simple},
			{"ModShotgun", 90, Weapon, Equipment},
			{"ModMachineGun", 91, Weapon, Equipment, Deprecated},
			{"ModRifle", 92, Weapon, Equipment, Deprecated},
			{"ModSniper", 93, Weapon, Equipment},
			{"ModLauncher", 94, Weapon, Equipment},
			{"ModHeavyMachineGun", 95, Weapon, Equipment, Deprecated},
			{"SciPistol", 96, Weapon, Equipment, Deprecated},
			{"SciBlaster", 97, Weapon, Equipment, Deprecated},
			{"SciNeedleGun", 98, Weapon, Equipment, Deprecated},
			{"SciRifle", 99, Weapon, Equipment, Deprecated},
			{"SciSniper", 100, Weapon, Equipment, Deprecated},
			{"SciCannon", 101, Weapon, Equipment, Deprecated},
			{"SciMelter", 103, Weapon, Equipment, Deprecated},
			{"MouseAmulet", 38, Amulet, Equipment, Gear, Simple},
			{"RiotAmulet", 39, Amulet, Equipment, Gear},
			{"SoldierAmulet", 40, Amulet, Equipment, Gear},
			{"WarriorAmulet", 41, Amulet, Equipment, Gear},
			{"TikTokAmulet", 28, Amulet, Equipment, Gear, Simple},
			{"MouseArmor", 42, Armor, Equipment, Gear, Simple},
			{"RiotArmor", 43, Armor, Equipment, Gear},
			{"SoldierArmor", 44, Armor, Equipment, Gear},
			{"WarriorArmor", 45, Armor, Equipment, Gear},
			{"RoadSignArmour", 29, Armor, Equipment, Gear, Simple},
			{"BaseballArmor", 34, Armor, Equipment, Gear},
			{"FootballArmor", 35, Armor, Equipment, Gear},
			{"MouseShield", 52, Shield, Equipment, Gear, Simple},
			{"SoldierShield", 53, Shield, Equipment, Gear},
			{"WarriorShield", 54, Shield, Equipment, Gear},
			{"RiotShield", 27, Shield, Equipment, Gear},
			{"RoadShield", 36, Shield, Equipment, Gear, Simple},
			{"Rage", 64, Consumable, Collectable},
			{"Health", 4, Consumable, Collectable},
			{"AmmoSmall", 127, Consumable, Collectable, Ammo},
			{"AmmoLarge", 128},
			{"ShieldSmall", 20, Consumable, Collectable},
			{"ShieldLarge", 21},
			{"ShieldCapacitySmall", 17},
			{"ShieldCapacityLarge", 18},
			{"EnergyCubeSmall", 106},
			{"EnergyCubeLarge", 107, Consumable, Collectable},
			{"ChestCommon", 13},
			{"ChestUncommon", 1},
			{"ChestRare", 16},
			{"ChestEpic", 2},
			{"ChestConsumable", 131, Chest, Collectable},
			{"ChestEquipment", 130, Chest, Collectable},
			{"ChestEquipmentTutorial", 132, Chest, Collectable},
			{"ChestWeapon", 134, Chest, Collectable},
			{"ChestLegendary", 19, Chest, Collectable},
			{"SpecialAimingAirstrike", 10, Special},
			{"SpecialAimingStunGrenade", 85, Special},
			{"SpecialShieldSelf", 89, Special},
			{"SpecialSkyLaserBeam", 110, Special},
			{"SpecialShieldedCharge", 119, Special},
			{"SpecialAimingGrenade", 102, Special},
			{"SpecialDefaultDash", 141, Special},
			{"SpecialRadar", 62, Special},
			{"SpecialLandmine", 666, Special},
			{"TutorialGrenade", 70, Special},
			{"Barrel", 109, Destructible},
			{"Barrier", 72, Destructible},
			{"SkipTutorial", 195, Destructible},
			{"DummyCharacter", 6, DummyCharacter},
			{"WeaponPlatformSpawner", 138, Platform},
			{"ConsumablePlatformSpawner", 140, Platform},
			{"CoreCommon", 15, Core},
			{"CoreUncommon", 46, Core},
			{"CoreRare", 47, Core, IAP, Chest},
			{"CoreEpic", 48, Core, IAP, Chest},
			{"CoreLegendary", 59, Core, IAP, Chest},

			#region Collections

			#region Player Skins

			{"Male01Avatar", 55, Deprecated },
			{"Male02Avatar", 56, Deprecated},
			{"Female01Avatar", 57, Deprecated},
			{"Female02Avatar", 58, Deprecated},
			{"MaleAssassin", 68, PlayerSkin, BotItem, Assassin, Collection},
			{"MaleCorpos", 69, PlayerSkin, Corpo, Collection},
			{"MalePunk", 77, PlayerSkin, BotItem, Punk, Collection},
			{"MaleSuperstar", 80, PlayerSkin, BotItem, Superstar, Collection},
			{"FemaleAssassin", 81, PlayerSkin, BotItem, Assassin, Collection},
			{"FemaleCorpos", 82, PlayerSkin, Corpo, Collection},
			{"FemalePunk", 83, PlayerSkin, BotItem, Punk, Collection},
			{"FemaleSuperstar", 104, PlayerSkin, BotItem, Superstar, Collection},
			{"TestSkin", 122},
			{"PlayerSkinDragonBoxer", 148}, // placeholder ID for the future skin
			{"PlayerSkinTieGuy", 147}, // placeholder ID for the future skin
			{"PlayerSkinFitnessChick", 146}, // placeholder ID for the future skin
			{"PlayerSkinSkellyQueen", 145}, // placeholder ID for the future skin
			{"PlayerSkinXmasSuperstar", 149, PlayerSkin, BotItem, Collection},
			{"PlayerSkinJodie", 135}, // placeholder ID for the future skin
			{"PlayerSkinMontyVonCue", 136}, // placeholder ID for the future skin
			{"PlayerSkinBoudicca", 139}, // placeholder ID for the future skin
			{"PlayerSkinCupid", 142, PlayerSkin, BotItem, Collection},
			{"PlayerSkinPanda", 400, PlayerSkin, Collection},
			{"PlayerSkinLeprechaun", 401}, // placeholder ID for the future skin
			{"PlayerSkinDragon", 402}, // placeholder ID for the future skin
			{"PlayerSkinSnowboarder", 403, PlayerSkin, Collection},
			{"PlayerSkinDunePaul", 404, PlayerSkin, Collection},
			{"PlayerSkinViking", 405, PlayerSkin, Collection},

			#endregion Player skins

			#region Gliders

			{"Divinci", 66, Glider, BotItem, Collection},
			{"Falcon", 67, Glider, BotItem, Collection},
			{"Rocket", 73, Glider, BotItem, Collection},
			{"Turbine", 74, Glider, BotItem, Collection},

			#endregion

			#region Deathmarkers

			{"Tombstone", 37, DeathMarker, BotItem, Collection},
			{"Demon", 25, DeathMarker, BotItem, Collection},
			{"Superstar", 75, DeathMarker, BotItem, Collection},
			{"Unicorn", 76, DeathMarker, BotItem, Collection},

			#endregion

			#region MeleeSkins

			{"MeleeSkinDefault", 300, MeleeSkin, Collection, BotItem},
			{"MeleeSkinSausage", 301, MeleeSkin, Collection},
			{"MeleeSkinCactus", 302, MeleeSkin, Collection},
			{"MeleeSkinAtomSlicer", 303, MeleeSkin, Collection},
			{"MeleeSkinDaggerOfDestiny", 304, MeleeSkin, Collection, BotItem},
			{"MeleeSkinElectricSolo", 305, MeleeSkin, Collection},
			{"MeleeSkinGigaMelee", 306, MeleeSkin, Collection},
			{"MeleeSkinHatchet", 307, MeleeSkin, Collection},
			{"MeleeSkinMicDrop", 308, MeleeSkin, Collection, BotItem},
			{"MeleeSkinMightySledge", 309, MeleeSkin, Collection},
			{"MeleeSkinOutOfThePark", 310, MeleeSkin, Collection},
			{"MeleeSkinPowerPan", 311, MeleeSkin, Collection, BotItem},
			{"MeleeSkinPutter", 312, MeleeSkin, Collection},
			{"MeleeSkinSirQuacks", 313, MeleeSkin, Collection},
			{"MeleeSkinThunderAxe", 314, MeleeSkin, Collection, BotItem},
			{"MeleeSkinToyMelee", 315, MeleeSkin, Collection, BotItem},
			{"MeleeSkinTvTakedown", 316, MeleeSkin, Collection},
			{"MeleeSkinWheelOfPain", 317, MeleeSkin, Collection, BotItem},
			{"MeleeSkinWrench", 318, MeleeSkin, Collection},
			{"MeleeSkinYouGotMail", 319, MeleeSkin, Collection},
			{"MeleeSkinXmas2023", 320, MeleeSkin, Collection, BotItem},
			
			#endregion

			#region ProfilePictures

			{"Avatar1", 116, ProfilePicture, Collection},
			{"Avatar2", 117, ProfilePicture, Collection},
			{"Avatar3", 118, ProfilePicture, Collection},
			{"Avatar4", 120, ProfilePicture, Collection},
			{"Avatar5", 121, ProfilePicture, Collection},
			{"AvatarRemote", 123, ProfilePicture, Collection, GenericCollectionItem},
			{"AvatarNFTCollection", 124, ProfilePicture, Collection, GenericCollectionItem},
			{"AvatarAssasinmask", 150, ProfilePicture, Collection},
			{"AvatarBlastcatads", 151, ProfilePicture, Collection},
			{"AvatarBurgerads", 152, ProfilePicture, Collection},
			{"AvatarCatcupads", 153, ProfilePicture, Collection},
			{"AvatarCorpoads", 154, ProfilePicture, Collection},
			{"AvatarCorpocrossads", 155, ProfilePicture, Collection},
			{"AvatarCorpomask", 156, ProfilePicture, Collection},
			{"AvatarEyesads", 157, ProfilePicture, Collection},
			{"AvatarFemaleassasinwantedads", 158, ProfilePicture, Collection},
			{"AvatarFemaleassassinconcept", 159, ProfilePicture, Collection},
			{"AvatarFemaleassassinwhatsticker", 160, ProfilePicture, Collection},
			{"AvatarFemalecorpo", 161, ProfilePicture, Collection},
			{"AvatarFemalecorpoconcept", 162, ProfilePicture, Collection},
			{"AvatarFemalecorpophonesticker", 163, ProfilePicture, Collection},
			{"AvatarFemalecorposticker", 164, ProfilePicture, Collection},
			{"AvatarFemalehost", 165, ProfilePicture, Collection},
			{"AvatarFemalepunk", 166, ProfilePicture, Collection},
			{"AvatarFemalepunkconcept", 167, ProfilePicture, Collection},
			{"AvatarFemalepunkfunsticker", 168, ProfilePicture, Collection},
			{"AvatarFemalepunkgraffiti", 169, ProfilePicture, Collection},
			{"AvatarFemalesuperstarads", 170, ProfilePicture, Collection},
			{"AvatarFemalesuperstarconcept", 171, ProfilePicture, Collection},
			{"AvatarFemalesuperstardisguststicker", 172, ProfilePicture, Collection},
			{"AvatarFemalesupperstar", 173, ProfilePicture, Collection},
			{"AvatarMaleassasin", 174, ProfilePicture, Collection},
			{"AvatarMaleassasinconcept", 175, ProfilePicture, Collection},
			{"AvatarMaleassasinexcitedsticker", 176, ProfilePicture, Collection},
			{"AvatarMaleassasinwantedads", 177, ProfilePicture, Collection},
			{"AvatarMalecorpoangryads", 178, ProfilePicture, Collection},
			{"AvatarMalecorpoconcept", 179, ProfilePicture, Collection},
			{"AvatarMalecorposcaredsticker", 180, ProfilePicture, Collection},
			{"AvatarMalehost", 181, ProfilePicture, Collection},
			{"AvatarMalepunk", 182, ProfilePicture, Collection},
			{"AvatarMalepunkads", 183, ProfilePicture, Collection},
			{"AvatarMalepunkconcept", 184, ProfilePicture, Collection},
			{"AvatarMalepunkgraffiti", 185, ProfilePicture, Collection},
			{"AvatarMalepunkhahasticker", 186, ProfilePicture, Collection},
			{"AvatarMalesuperstarads", 187, ProfilePicture, Collection},
			{"AvatarMalesuperstarconcept", 188, ProfilePicture, Collection},
			{"AvatarMalesuperstarstopsticker", 189, ProfilePicture, Collection},
			{"AvatarMusic", 190, ProfilePicture, Collection},
			{"AvatarPunklogoads", 191, ProfilePicture, Collection},
			{"AvatarRocketads", 192, ProfilePicture, Collection},
			{"AvatarSuperstarloveads", 193, ProfilePicture, Collection},
			{"AvatarUnicornssticker", 194, ProfilePicture, Collection},

			#endregion

			#region Footprint

			{"FootprintDot", 111, Footprint, BotItem},

			#endregion

			#endregion
		};
	}
}