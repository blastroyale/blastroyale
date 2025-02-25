using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.UIElements.Kit;
using FirstLight.Game.Utils;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Shows the details of a match / event.
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class MatchInfoPopupPresenter : UIPresenterData<MatchInfoPopupPresenter.StateData>
	{
		public class StateData
		{
			public SimulationMatchConfig MatchSettings;
			public GameModeInfo EntryInfo;
			public List<string> FriendsPlaying;
			public Action ClickAction;
			public string ButtonText;
			public string AboveTextButton;
			public EventGameModeEntry EventInfo => EntryInfo.Entry as EventGameModeEntry;
		}

		private const string USS_EVENT = "content--event";
		private const string USS_EVENT_PAID = "content--event--paid";
		private const string USS_CUSTOM = "content--custom";
		private const string USS_REWARD_ITEM = "event-reward";
		private const string USS_BPP_XP_SPRITE = "sprite-shared__icon-bpp-xp";

		[Q("EventTitle")] private Label _eventTitle;
		[Q("EventTimer")] private Label _eventTimer;
		[Q("EventThumbnail")] private VisualElement _eventThumbnail;
		[Q("EventImage")] private VisualElement _eventImage;
		[Q("FriendsTitle")] private LocalizedLabel _friendsTitle;
		[Q("FriendsInMatchScrollView")] private VisualElement _friendsInMatchScrollView;
		[Q("Summary")] private Label _summary;
		[Q("GameMode")] private MatchSettingsButtonElement _mode;
		[Q("MaxPlayers")] private MatchSettingsButtonElement _maxPlayers;
		[Q("Map")] private MatchSettingsButtonElement _map;
		[Q("SquadSize")] private MatchSettingsButtonElement _teamSize;
		[Q("ActionButton")] private KitButton _actionButton;
		[Q("RewardsListContainer")] private VisualElement _rewardList;
		[Q("MutatorsContainer")] private VisualElement _mutatorsContainer;
		[Q("MutatorsScroll")] private ScrollView _mutatorsScroll;
		[Q("AllowedWeaponsContainer")] private VisualElement _allowedWeaponsContainer;
		[Q("AllowedWeaponsScroll")] private ScrollView _allowedWeapons;
		[Q("CustomThumbnail")] private VisualElement _customThumbnail;
		[Q("AboveButtonLabel")] private Label _aboveButtonLabel;
		[Q("Popup")] private GenericPopupElement _popup;
		[Q("Blocker")] private ImageButton _blocker;
		[Q("Content")] private VisualElement _content;
		[QView("TopCurrenciesBar")] private CurrencyTopBarView _currencyTopBarView;

		protected override void QueryElements()
		{
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_popup.CloseClicked += () => Close().Forget();

			_blocker.clicked += () => Close().Forget();

			_popup.EnablePadding(false)
				.SetGlowEffect(Data.MatchSettings.MatchType == MatchType.Matchmaking);
			_popup.LocalizeTitle(Data.MatchSettings.MatchType == MatchType.Custom
				? ScriptTerms.UITCustomGames.match_info
				: ScriptTerms.UITGameModeSelection.event_info_popup_title);
			_content.RemoveModifiers();
			_content.EnableInClassList(USS_EVENT, Data.MatchSettings.MatchType == MatchType.Matchmaking);
			_content.EnableInClassList(USS_EVENT_PAID, Data.MatchSettings.MatchType == MatchType.Matchmaking && Data.EventInfo.IsPaid);
			_content.EnableInClassList(USS_CUSTOM, Data.MatchSettings.MatchType == MatchType.Custom);
			if (!string.IsNullOrWhiteSpace(Data.AboveTextButton))
			{
				_aboveButtonLabel.text = Data.AboveTextButton;
			}
			else
			{
				_aboveButtonLabel.SetDisplay(false);
			}

			SetupMatchValues();
			_actionButton.clicked += Data.ClickAction;
			if (Data.MatchSettings.MatchType == MatchType.Custom)
			{
				SetupCustom();
				return UniTask.CompletedTask;
			}

			SetupEvent();
			return UniTask.CompletedTask;
		}

		private void SetupMatchValues()
		{
			_mode.SetValue(Data.MatchSettings.GameModeID);
			_mode.SetEnabled(false);
			_teamSize.SetValue(Data.MatchSettings.TeamSize.ToString());
			_teamSize.SetEnabled(false);
			GameId.TryParse<GameId>(Data.MatchSettings.MapId, out var mapId);
			_map.SetValue(mapId.GetLocalization());
			_map.SetEnabled(false);
			_maxPlayers.SetValue(Data.MatchSettings.GetMaxPlayers().ToString());
			_maxPlayers.SetEnabled(false);
			var mutators = Data.MatchSettings.Mutators.GetSetFlags();
			_mutatorsScroll.Clear();
			_mutatorsContainer.SetDisplay(mutators.Length > 0);

			foreach (var mutator in mutators)
			{
				var mutatorLabel = new LocalizedLabel(mutator.GetLocalizationKey());
				mutatorLabel.RegisterCallback<ClickEvent>((ev) =>
				{
					mutatorLabel.OpenTooltip(_content.panel.visualTree,
						LocalizationManager.GetTranslation(mutator.GetDescriptionLocalizationKey()),
						position: TooltipPosition.Top,
						maxWidth: 350);
				});

				_mutatorsScroll.Add(mutatorLabel);
			}

			var weaponFilter = Data.MatchSettings.WeaponsSelectionOverwrite;
			_allowedWeapons.Clear();
			_allowedWeaponsContainer.SetDisplay(weaponFilter.Length > 0);
			foreach (var weapon in weaponFilter)
			{
				_allowedWeapons.Add(new LocalizedLabel(weapon.GetTranslationKeyGameIdString())); // TODO mihak: Add localization key
			}
		}

		private void CheckCustomImage()
		{
			var url = Data.EventInfo.ImageURL;
			if (string.IsNullOrWhiteSpace(url))
			{
				return;
			}

			var request = MainInstaller.ResolveServices().RemoteTextureService
				.RequestTexture(url, cancellationToken: GetCancellationTokenOnClose());
			_content.AddToClassList("event-container--custom-image");
			_eventImage.ListenOnce<GeometryChangedEvent>(() =>
			{
				SetTexture(request).Forget();
			});
		}

		private async UniTaskVoid SetTexture(UniTask<Texture2D> task)
		{
			// Didn't load before geometry change, so add a loading effect
			if (task.Status == UniTaskStatus.Pending)
			{
				_eventImage.AddToClassList("anim-fade");
			}

			var tex = await task;
			_eventImage.style.backgroundImage = new StyleBackground(tex);
			_eventImage.style.opacity = 1;
		}

		private void SetupEvent()
		{
			_eventTimer.SetTimer(() => Data.EntryInfo.Duration.GetEndsAtDateTime(), "ENDS IN ");
			_eventTitle.text = Data.EntryInfo.Entry.Title.GetText();
			_summary.text = Data.EntryInfo.Entry.LongDescription.GetText();

			if (Data.ButtonText != null)
			{
				_actionButton.BtnText = Data.ButtonText;
			}
			else
			{
				_actionButton.Localize(ScriptTerms.UITGameModeSelection.select_event);
				_actionButton.AddToClassList("button-long--green");
			}

			CheckCustomImage();
			_rewardList.Clear();
			foreach (var mod in Data.MatchSettings.RewardModifiers)
			{
				if (mod.CollectedInsideGame) continue;
				AddEventReward(mod.Id, $"x{mod.Multiplier.AsFloat:0.##}", true);
			}

			foreach (var gameId in Data.MatchSettings.MetaItemDropOverwrites.Select(a => a.Id).Distinct())
			{
				if (RewardLogic.TryGetRewardCurrencyGroupId(gameId, out var _))
				{
					continue;
				}

				AddEventReward(gameId, "", false);
			}

			_rewardList.EnableInClassList("rewards-list-container--single-line", _rewardList.childCount <= 3);
			if (Data.EventInfo.IsPaid)
			{
				_currencyTopBarView.Configure(
					_actionButton,
					new List<GameId>() {Data.EventInfo.PriceToJoin.RewardId});
			}
		}

		private void AddEventReward(GameId id, string text, bool modifier)
		{
			var icon = new Label()
			{
				name = id.ToString(),
				text = text,
			};
			icon.AddToClassList(USS_REWARD_ITEM);
			if (id == GameId.BPP && modifier)
			{
				icon.AddToClassList(USS_BPP_XP_SPRITE);
			}
			else
			{
				new CurrencyItemViewModel(ItemFactory.Currency(id, 0))
					.DrawIcon(icon);
			}

			_rewardList.Add(icon);
		}

		private void SetupCustom()
		{
			_summary.SetDisplay(false);

			_actionButton.BtnText = ScriptLocalization.UITCustomGames.join;
			_friendsTitle.SetVisibility(Data.FriendsPlaying.Count > 0);
			_friendsInMatchScrollView.Clear();

			foreach (var friend in Data.FriendsPlaying)
			{
				_friendsInMatchScrollView.Add(new Label(friend));
			}

			_customThumbnail.RemoveSpriteClasses();
			switch (Data.MatchSettings.TeamSize)
			{
				case 1:
					_customThumbnail.AddToClassList("sprite-home__icon-match-solos");
					break;
				case 2:
					_customThumbnail.AddToClassList("sprite-home__icon-match-duos");
					break;
				case 4:
					_customThumbnail.AddToClassList("sprite-home__icon-match-quads");
					break;
				default:
					throw new NotSupportedException("Unsupported squad size");
			}
		}
	}
}