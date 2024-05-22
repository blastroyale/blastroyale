using System.Collections.Generic;
using System.ComponentModel;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;

public partial class SROptions
{
	[Category("Other")]
	public void ClearRemoteTextureCache()
	{
		MainInstaller.Resolve<IGameServices>().RemoteTextureService.ClearCache();
	}

	[Category("Other")]
	public void OpenBattlePassBanner()
	{
		MainInstaller.ResolveServices().UIService.OpenScreen<BattlePassSeasonBannerPresenter>().Forget();
	}

	[Category("Other")]
	public void OpenReviewAndRateDialog()
	{
		MainInstaller.Resolve<IGameServices>().MessageBrokerService.Publish(new OpenRateAndReviewPromptMessage());
	}

	[Category("Other")]
	public void OpenButtonDialog()
	{
		var button = new GenericDialogButton
		{
			ButtonText = "Confirm",
			ButtonOnClick = CallbackConfirm
		};

		MainInstaller.Resolve<IGameServices>().GenericDialogService.OpenButtonDialog("THIS IS TITLE!", "THE PLAYERS WON'T READ ANY OF THIS!",
			true, button, CallbackClose);

		void CallbackConfirm()
		{
			FLog.Warn("Confirm callback.");
		}

		void CallbackClose()
		{
			FLog.Warn("Close callback.");
		}
	}

	[Category("Other")]
	public void OpenInputDialog()
	{
		var button = new GenericDialogButton<string>
		{
			ButtonText = "Confirm",
			ButtonOnClick = CallbackConfirm
		};

		MainInstaller.Resolve<IGameServices>().GenericDialogService.OpenInputDialog("THIS IS TITLE!",
			"THE PLAYERS WON'T READ ANY OF THIS!",
			"Input", button, true, CallbackClose);

		void CallbackConfirm(string input)
		{
			FLog.Warn("Confirm callback - " + input);
		}

		void CallbackClose(string input)
		{
			FLog.Warn("Close callback - " + input);
		}
	}

	[Category("Other")]
	public void OpenRewardsTest()
	{
		var uiService = MainInstaller.Resolve<IGameServices>().UIService;
		uiService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData()
		{
			Items = new List<ItemData>()
			{
				ItemFactory.Equipment(new Equipment(GameId.ModRifle,
					rarity: EquipmentRarity.Legendary,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				ItemFactory.Collection(GameId.FemaleAssassin),
				ItemFactory.Collection(GameId.Avatar3),
				ItemFactory.Equipment(new Equipment(GameId.ApoShotgun,
					rarity: EquipmentRarity.UncommonPlus,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
			},
			OnFinish = () => { uiService.CloseScreen<RewardsScreenPresenter>().Forget(); }
		}).Forget();
	}

	[Category("Other")]
	public void OpenRewardsTestForFame()
	{
		var uiService = MainInstaller.Resolve<IGameServices>().UIService;
		uiService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData()
		{
			FameRewards = true,
			Items = new List<ItemData>()
			{
				ItemFactory.Equipment(new Equipment(GameId.ModRifle,
					rarity: EquipmentRarity.Legendary,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				ItemFactory.Collection(GameId.FemaleAssassin),
				ItemFactory.Equipment(new Equipment(GameId.ApoShotgun,
					rarity: EquipmentRarity.UncommonPlus,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
			},
			OnFinish = () => { uiService.CloseScreen<RewardsScreenPresenter>().Forget(); }
		}).Forget();
	}

	[Category("Other")]
	public void OpenRewardsTestMany()
	{
		var uiService = MainInstaller.Resolve<IGameServices>().UIService;
		uiService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData()
		{
			Items = new List<ItemData>()
			{
				ItemFactory.Equipment(new Equipment(GameId.ModRifle,
					rarity: EquipmentRarity.Legendary,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				ItemFactory.Currency(GameId.COIN, 1),
				ItemFactory.Currency(GameId.COIN, 100),
				ItemFactory.Currency(GameId.BPP, 300),
				ItemFactory.Currency(GameId.CS, 20000),
				ItemFactory.Equipment(new Equipment(GameId.SciSniper,
					rarity: EquipmentRarity.EpicPlus,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				ItemFactory.Equipment(new Equipment(GameId.ApoShotgun,
					rarity: EquipmentRarity.Rare,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				ItemFactory.Equipment(new Equipment(GameId.ApoRPG,
					rarity: EquipmentRarity.Uncommon,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				ItemFactory.Equipment(new Equipment(GameId.ApoShotgun,
					rarity: EquipmentRarity.CommonPlus,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
			},
			OnFinish = () => { uiService.CloseScreen<RewardsScreenPresenter>().Forget(); }
		}).Forget();
	}

	[Category("Other")]
	public void OpenRewardsTestFame()
	{
		var uiService = MainInstaller.Resolve<IGameServices>().UIService;
		uiService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData()
		{
			FameRewards = true,
			Items = new List<ItemData>()
			{
				ItemFactory.Currency(GameId.BPP, 300),
				ItemFactory.Currency(GameId.CS, 20000),
				ItemFactory.Unlock(UnlockSystem.Shop)
			},
			OnFinish = () => { uiService.CloseScreen<RewardsScreenPresenter>().Forget(); }
		}).Forget();
	}
}