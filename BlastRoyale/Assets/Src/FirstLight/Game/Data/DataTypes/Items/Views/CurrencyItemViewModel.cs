using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Holder for currency rewards, ex Coins
	/// </summary>
	public class CurrencyItemViewModel : IItemViewModel
	{
		private static IReadOnlyDictionary<GameId, string> _richTextIcons = new Dictionary<GameId, string>()
		{
			{GameId.COIN, "Coinicon"},
			{GameId.BlastBuck, "Blastbuckicon"},
			{GameId.CS, "CraftSpiceicon"},
			{GameId.NOOB, "NOOBIcon"},
			{GameId.PartnerANCIENT8, "PartnerANCIENT8Icon"},
			{GameId.PartnerAPECOIN, "PartnerAPECOINIcon"},
			{GameId.PartnerBEAM, "PartnerBEAMIcon"},
			{GameId.PartnerBLOCKLORDS, "PartnerBLOCKLORDSIcon"},
			{GameId.PartnerBLOODLOOP, "PartnerBLOODLOOPIcon"},
			{GameId.PartnerCROSSTHEAGES, "PartnerCROSSTHEAGESIcon"},
			{GameId.PartnerFARCANA, "PartnerFARCANAIcon"},
			{GameId.PartnerGAM3SGG, "PartnerGAM3SGGIcon"},
			{GameId.PartnerIMMUTABLE, "PartnerIMMUTABLEIcon"},
			{GameId.PartnerMOCAVERSE, "PartnerMOCAVERSEIcon"},
			{GameId.PartnerNYANHEROES, "PartnerNYANHEROESIcon"},
			{GameId.PartnerPIRATENATION, "PartnerPIRATENATIONIcon"},
			{GameId.PartnerPIXELMON, "PartnerPIXELMONIcon"},
			{GameId.PartnerPLANETMOJO, "PartnerPLANETMOJOIcon"},
			{GameId.PartnerSEEDIFY, "PartnerSEEDIFYIcon"},
			{GameId.PartnerWILDERWORLD, "PartnerWILDERWORLDIcon"},
			{GameId.PartnerXBORG, "PartnerXBORGIcon"},
			{GameId.PartnerBREED, "PartnerBREEDIcon"},
			{GameId.PartnerMEME, "PartnerMEMEIcon"},
			{GameId.PartnerYGG, "PartnerYGGIcon"},
			{GameId.FestiveSNOWFLAKE, "FestiveSNOWFLAKEIcon"},
			{GameId.EventTicket, "EventTicketIcon"}
		};

		public ItemData Item { get; }
		public GameId GameId => _gameId;
		public uint Amount => _amount;
		public string DisplayName => GameId.GetCurrencyLocalization(_amount).ToUpperInvariant();
		public string Description => $"X {_amount}";

		public string ItemTypeDisplayName => GameId.GetCurrencyLocalization(_amount).ToUpperInvariant();

		public VisualElement ItemCard => new CurrencyRewardSummaryItemElement()
		{
			pickingMode = PickingMode.Ignore
		}.SetReward(this);

		public string GetRichTextIcon()
		{
			return GetRichTextIcon(_gameId);
		}

		public void DrawIcon(VisualElement icon)
		{
			DrawIconInternal(icon, GameId, Amount);
		}

		private static void DrawIconInternal(VisualElement icon, GameId gameId, uint amount)
		{
			if (MainInstaller.TryResolve<IGameServices>(out var services))
			{
				var config = services.ConfigsProvider.GetConfig<CurrencySpriteConfig>();

				if (config.TryGetConfig(gameId, out var entry))
				{
					var clazz = entry.GetClassForAmount(amount);
					icon.style.backgroundImage = StyleKeyword.Null;
					icon.RemoveSpriteClasses();
					icon.AddToClassList(clazz);
					return;
				}

				throw new Exception("Unable to set icon for currency " + gameId);
			}
		}

		public static VisualElement CreateIcon(GameId id, uint amount = 0)
		{
			var icon = new VisualElement() {name = "icon-" + id.ToString().ToLowerInvariant()};
			icon.AddToClassList("generated-icon");
			DrawIconInternal(icon, id, amount);
			return icon;
		}

		public static string GetRichTextIcon(GameId id)
		{
			if (!_richTextIcons.TryGetValue(id, out var iconName))
			{
				FLog.Error($"Could not read rich text icon for {id}");
				iconName = _richTextIcons[GameId.COIN];
			}

			return $"<sprite name=\"{iconName}\">";
		}

		public override string ToString() => GetRichTextIcon();

		private GameId _gameId;
		private uint _amount;

		public CurrencyItemViewModel(ItemData item)
		{
			Item = item;
			_gameId = item.Id;
			_amount = (uint) item.GetMetadata<CurrencyMetadata>().Amount;
		}
	}
}