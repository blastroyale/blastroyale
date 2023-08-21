using System.Collections.Generic;
using System.ComponentModel;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
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
		var uiService = MainInstaller.Resolve<IGameServices>().GameUiService;
		uiService.OpenScreen<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
		{
			Rewards = new List<IReward>()
			{
				new EquipmentReward(new Equipment(GameId.ModRifle,
					rarity: EquipmentRarity.Legendary,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				new CurrencyReward(GameId.COIN, 1),
				new EquipmentReward(new Equipment(GameId.ApoShotgun,
					rarity: EquipmentRarity.UncommonPlus,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
			},
			OnFinish = () => { uiService.CloseUi<RewardsScreenPresenter>(true); }
		});
	}

	[Category("Other")]
	public void OpenRewardsTestMany()
	{
		var uiService = MainInstaller.Resolve<IGameServices>().GameUiService;
		uiService.OpenScreen<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
		{
			Rewards = new List<IReward>()
			{
				new EquipmentReward(new Equipment(GameId.ModRifle,
					rarity: EquipmentRarity.Legendary,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				new CurrencyReward(GameId.COIN, 1),
				new CurrencyReward(GameId.COIN, 100),
				new CurrencyReward(GameId.BPP, 300),
				new CurrencyReward(GameId.CS, 20000),
				new EquipmentReward(new Equipment(GameId.SciSniper,
					rarity: EquipmentRarity.EpicPlus,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				new EquipmentReward(new Equipment(GameId.ApoShotgun,
					rarity: EquipmentRarity.Rare,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				new EquipmentReward(new Equipment(GameId.ApoRPG,
					rarity: EquipmentRarity.Uncommon,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
				new EquipmentReward(new Equipment(GameId.ApoShotgun,
					rarity: EquipmentRarity.CommonPlus,
					adjective: EquipmentAdjective.Cool,
					material: EquipmentMaterial.Carbon,
					faction: EquipmentFaction.Chaos)),
			},
			OnFinish = () => { uiService.CloseUi<RewardsScreenPresenter>(true); }
		});
	}
	
	[Category("Other")]
	public void OpenRewardsTestFame()
	{
		var uiService = MainInstaller.Resolve<IGameServices>().GameUiService;
		uiService.OpenScreen<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
		{
			FameRewards = true,
			Rewards = new List<IReward>()
			{
				new CurrencyReward(GameId.BPP, 300),
				new CurrencyReward(GameId.CS, 20000),
				new UnlockReward(UnlockSystem.ShopScreen)
			},
			OnFinish = () => { uiService.CloseUi<RewardsScreenPresenter>(true); }
		});
	}
}