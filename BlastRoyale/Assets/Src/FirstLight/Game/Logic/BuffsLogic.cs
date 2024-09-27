
using System.Collections.Generic;
using System.Linq;
using BuffSystem;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK.Models;
using Quantum;

namespace FirstLight.Game.Logic
{
	public interface IBuffLogic
	{
		public BuffVirtualEntity CalculateMetaEntity();

		public List<BuffConfig> GetMetaBuffs(ItemData item);
	}
	
	public class BuffsLogic : AbstractBaseLogic<CollectionData>, IBuffLogic, IGameLogicInitializer
	{
		
		public BuffsLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		// TODO: Cache buffs dictionary
		private IReadOnlyDictionary<BuffId, BuffConfig> GetBuffConfigs()
		{
			var d = new Dictionary<BuffId, BuffConfig>();
			foreach (var b in GameLogic.ConfigsProvider.GetConfig<BuffConfigs>().Configs)
			{
				d[b.Id] = b;
			}
			return d;
		}

		public BuffVirtualEntity CalculateMetaEntity()
		{
			var buffConfigs = GetBuffConfigs();
			var e = new BuffVirtualEntity();
			if (!Data.OwnedCollectibles.TryGetValue(CollectionCategories.PLAYER_SKINS, out var skins))
			{
				return e;
			}
			// Only skins gives buffs for now
			var skinIds = skins.Select(skin => skin.Id).ToHashSet();
			foreach (var buffSource in GameLogic.ConfigsProvider.GetConfig<BuffConfigs>().Settings.Sources)
			{
				if (skinIds.Contains(buffSource.Source.SimpleGameId))
				{
					e.AddBuff(buffConfigs[buffSource.Buff]);
				}
			}
			return e;
		}

		public List<BuffConfig> GetMetaBuffs(ItemData item)
		{
			var buffConfigs = GetBuffConfigs();
			var buffs = new List<BuffConfig>();
			foreach (var buffSource in GameLogic.ConfigsProvider.GetConfig<BuffConfigs>().Settings.Sources)
			{
				if (item.Id == buffSource.Source.SimpleGameId)
				{
					buffs.Add(buffConfigs[buffSource.Buff]);
				}
			}
			return buffs;
		}

		public void Init()
		{
			
		}

		public void ReInit()
		{
			
		}
	}
}