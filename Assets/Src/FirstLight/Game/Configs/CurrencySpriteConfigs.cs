using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct CurrencySpriteConfigEntry
	{
		public string DefaultSprite;
		[SerializeField] public SerializedDictionary<int, string> SpriteByMinAmount;


		public string GetClassForAmount(uint amount)
		{
			var ordered = SpriteByMinAmount.OrderByDescending(entry => entry.Key);
			var clazz = DefaultSprite;
			foreach (var kv in ordered)
			{
				if (amount >= kv.Key)
				{
					return kv.Value;
				}
			}

			return clazz;
		}
	}

	[Serializable]
	[IgnoreServerSerialization]
	public struct CurrencySpriteConfig
	{
		[SerializeField] public List<Pair<GameId, CurrencySpriteConfigEntry>> Values;


		public bool TryGetConfig(GameId id, out CurrencySpriteConfigEntry config)
		{
			foreach (var value in Values.Where(value => value.Key == id))
			{
				config = value.Value;
				return true;
			}

			config = new CurrencySpriteConfigEntry();
			return false;
		}
	}

	/// <summary>
	/// Scriptable Object to store collection of picture profile data
	/// </summary>
	[CreateAssetMenu(fileName = "CurrencySpriteConfigs", menuName = "ScriptableObjects/Configs/CurrencySpriteConfigs")]
	[IgnoreServerSerialization]
	public class CurrencySpriteConfigs : ScriptableObject, ISingleConfigContainer<CurrencySpriteConfig>
	{
		[SerializeField] private CurrencySpriteConfig _config;

		public CurrencySpriteConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}