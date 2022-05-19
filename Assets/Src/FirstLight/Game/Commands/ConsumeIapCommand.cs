using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Services;
using PlayFab;
using PlayFab.CloudScriptModels;
using Quantum;
using UnityEngine;
using UnityEngine.Purchasing;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Consumes the purchased already validated IAP item from the store to award the player the IAP reward
	/// </summary>
	public struct ConsumeIapCommand : IGameCommand
	{
		public bool IsNotIap;
		public ProductData Product;
		public PurchaseFailureReason? FailureReason;

		/// <inheritdoc />
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			var cacheCommand = this;
			var request = new ExecuteFunctionRequest
			{
				FunctionName = "ConsumeValidatedPurchaseCommand",
				GeneratePlayStreamEvent = true,
				FunctionParameter = new LogicRequest
				{
					Command = nameof(ConsumeIapCommand),
					Platform = Application.platform.ToString(),
					Data = new Dictionary<string, string>
					{
						{"item_id", Product.Id },
					}
				},
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			if (FailureReason.HasValue)
			{
				OnPurchaseFailed(gameLogic, FailureReason.Value.ToString());
				return;
			}

			if (IsNotIap)
			{
				ConsumePurchaseSuccess(null);
				return;
			}
			

			PlayFabCloudScriptAPI.ExecuteFunction(request, ConsumePurchaseSuccess, ConsumePurchaseFailed);
			
			void ConsumePurchaseSuccess(ExecuteFunctionResult result)
			{
				cacheCommand.ConsumeAndAwardItem(gameLogic);
			}

			void ConsumePurchaseFailed(PlayFabError error)
			{
				cacheCommand.FailureReason = PurchaseFailureReason.Unknown;
				
				cacheCommand.OnPurchaseFailed(gameLogic, error.ErrorMessage);
				GameCommandService.OnPlayFabError(error);
			}
		}

		private void ConsumeAndAwardItem(IGameLogic gameLogic)
		{
			var rewardData = new RewardData(Product.Data.RewardGameId, (int) Product.Data.RewardValue);

			if (Product.Data.PriceGameId != GameId.RealMoney)
			{
				gameLogic.CurrencyLogic.DeductCurrency(Product.Data.PriceGameId, (ulong) Product.Data.PriceValue);
			}
			
			var reward = gameLogic.RewardLogic.ClaimReward(rewardData);

			gameLogic.MessageBrokerService.Publish(new IapPurchaseSucceededMessage
			{
				Product = Product,
				ProductReward = reward
			});
		}

		private void OnPurchaseFailed(IGameLogic gameLogic, string failReason)
		{
			var dictionary = new Dictionary<string, object>
			{
				{ "reason", failReason },
#if UNITY_IOS
				{ "store", AppleAppStore.Name },
#else
				{ "store", GooglePlay.Name },
#endif
				{ "product_id", Product.Id },
				{ "product_price_id", Product.Data.PriceGameId },
				{ "product_reward_id", Product.Data.RewardGameId },
				{ "product_data", Newtonsoft.Json.JsonConvert.SerializeObject(Product.Data) },
				{ "product_metadata", Newtonsoft.Json.JsonConvert.SerializeObject(Product.Metadata) },
			};
			
			Debug.LogError($"Failed to buy {Product.Id} - {failReason}");
			
			gameLogic.AnalyticsService.LogEvent("purchase_failed", dictionary);
			gameLogic.MessageBrokerService.Publish(new IapPurchaseFailedMessage
			{
				Product = Product,
				FailureReason = FailureReason.Value
			});
		}
	}
}