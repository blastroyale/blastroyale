using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExitGames.Client.Photon.StructWrapping;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using JetBrains.Annotations;
using Quantum;

namespace FirstLight.Game.Logic
{
	public interface IGameEventsDataProvider
	{
		bool HasPass(string eventId);
		bool HasAnyPass();
		IReadOnlyList<string> GetPasses();
	}

	/// <summary>
	/// ATM: Handle event passes logic AKA Paid to join tournaments
	/// </summary>
	public interface IGameEventsLogic : IGameEventsDataProvider
	{
		void BuyEventPass(string eventId);
		bool ConsumeEventPass(string eventId);
		List<ItemData> RefundEventPasses();
	}

	public class GameEventsLogic : AbstractBaseLogic<EventsData>, IGameEventsLogic
	{
		public GameEventsLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void BuyEventPass(string eventId)
		{
			var events = GameLogic.RemoteConfigProvider.GetConfig<EventGameModesConfig>();
			var now = GameLogic.TimeService.DateTimeUtcNow;
			var ev = events
				.FirstOrDefault(ev => ev.IsPaid && ev.MatchConfig.UniqueConfigId == eventId && ev.Schedule.Any(sc => sc.Contains(now)));
			if (ev == null)
			{
				throw new LogicException("Event not found to buy ticket " + eventId);
			}

			if (Data.EventPassesWithPrice.ContainsKey(eventId))
			{
				throw new LogicException("Player already have ticket for event!");
			}

			var have = GameLogic.CurrencyLogic.GetCurrencyAmount(ev.PriceToJoin.RewardId);
			if (have < (ulong) ev.PriceToJoin.Value)
			{
				throw new LogicException("Player don't have currency to buy ticket");
			}

			GameLogic.CurrencyLogic.DeductCurrency(ev.PriceToJoin.RewardId, (ulong) ev.PriceToJoin.Value);
			Data.EventPassesWithPrice[eventId] = ev.PriceToJoin;
		}

		public bool ConsumeEventPass(string eventId)
		{
			return Data.EventPassesWithPrice.Remove(eventId);
		}

		public List<ItemData> RefundEventPasses()
		{
			var refunded = new List<ItemData>();
			foreach (var price in Data.EventPassesWithPrice.Values)
			{
				refunded.Add(ItemFactory.Legacy(price));
				GameLogic.CurrencyLogic.AddCurrency(price.RewardId, (ulong) price.Value);
			}

			Data.EventPassesWithPrice.Clear();
			return refunded;
		}

		public bool HasPass(string eventId)
		{
			return Data.EventPassesWithPrice.ContainsKey(eventId);
		}

		public bool HasAnyPass()
		{
			return Data.EventPassesWithPrice.Count > 0;
		}

		public IReadOnlyList<string> GetPasses()
		{
			return Data.EventPassesWithPrice.Keys.ToList();
		}
	}
}