using System.Collections.Generic;

namespace FirstLight.Game.Ids
{
	public enum SceneId
	{
		MainMenu,
		CollectLootRewardSequence,
		FusionSequence,
		EnhanceSequence,
		FloodCity,
		SmallWilderness,
		FtueDeck,
		FloodCitySimple,
		MainDeck,
		BlimpDeck,
		BRGenesis,
		TestScene,
		MapTestScene,
		NewBRMap,
		District,
		BattlelandsMap,
		IslandsMap,
	}
	
	/// <summary>
	/// Avoids boxing for Dictionary
	/// </summary>
	public class SceneIdComparer : IEqualityComparer<SceneId>
	{
		public bool Equals(SceneId x, SceneId y)
		{
			return x == y;
		}

		public int GetHashCode(SceneId obj)
		{
			return (int)obj;
		}
	}
}