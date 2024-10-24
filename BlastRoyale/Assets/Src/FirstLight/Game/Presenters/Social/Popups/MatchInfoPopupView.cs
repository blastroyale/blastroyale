using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
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
	public class MatchInfoPopupView : UIView
	{
		private const string USS_EVENT = "content--event";
		private const string USS_CUSTOM = "content--custom";
		private const string USS_REWARD_ITEM = "event-reward";
		private const string USS_BPP_XP_SPRITE = "sprite-shared__icon-bpp-xp";
		private readonly SimulationMatchConfig _matchSettings;
		private readonly GameModeInfo _entryInfo;
		private readonly List<string> _friendsPlaying;
		private readonly Action _clickAction;
		private EventGameModeEntry _eventEntry => (EventGameModeEntry) _entryInfo.Entry;

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
		[Q("ActionButton")] private LocalizedButton _actionButton;
		[Q("RewardsListContainer")] private VisualElement _rewardList;
		[Q("MutatorsContainer")] private VisualElement _mutatorsContainer;
		[Q("MutatorsScroll")] private ScrollView _mutatorsScroll;
		[Q("AllowedWeaponsContainer")] private VisualElement _allowedWeaponsContainer;
		[Q("AllowedWeaponsScroll")] private ScrollView _allowedWeapons;
		[Q("CustomThumbnail")] private VisualElement _customThumbnail;

		public MatchInfoPopupView(GameModeInfo info, Action selectAction)
		{
			_clickAction = selectAction;
			_matchSettings = info.Entry.MatchConfig;
			_entryInfo = info;
		}

		public MatchInfoPopupView(SimulationMatchConfig matchSettings, List<string> friendsPlaying, Action selectAction)
		{
			_matchSettings = matchSettings;
			_friendsPlaying = friendsPlaying;
			_clickAction = selectAction;
		}

		public bool IsEvent()
		{
			return _matchSettings.MatchType == MatchType.Matchmaking;
		}

		protected override void Attached()
		{
			Element.RemoveModifiers();
			Element.EnableInClassList(USS_EVENT, _matchSettings.MatchType == MatchType.Matchmaking);
			Element.EnableInClassList(USS_CUSTOM, _matchSettings.MatchType == MatchType.Custom);
			SetupMatchValues();
			_actionButton.clicked += _clickAction;
			if (_matchSettings.MatchType == MatchType.Custom)
			{
				SetupCustom();
				return;
			}

			SetupEvent();
		}

		private void SetupMatchValues()
		{
			_mode.SetValue(_matchSettings.GameModeID);
			_mode.SetEnabled(false);
			_teamSize.SetValue(_matchSettings.TeamSize.ToString());
			_teamSize.SetEnabled(false);
			GameId.TryParse<GameId>(_matchSettings.MapId, out var mapId);
			_map.SetValue(mapId.GetLocalization());
			_map.SetEnabled(false);
			_maxPlayers.SetValue(_matchSettings.GetMaxPlayers().ToString());
			_maxPlayers.SetEnabled(false);
			var mutators = _matchSettings.Mutators.GetSetFlags();
			_mutatorsScroll.Clear();
			_mutatorsContainer.SetDisplay(mutators.Length > 0);

			foreach (var mutator in mutators)
			{
				var mutatorLabel = new LocalizedLabel(mutator.GetLocalizationKey());
				mutatorLabel.RegisterCallback<ClickEvent>((ev) =>
				{
					mutatorLabel.OpenTooltip(Element.panel.visualTree,
						LocalizationManager.GetTranslation(mutator.GetDescriptionLocalizationKey()),
						position: TooltipPosition.Top,
						maxWidth: 350);
				});

				_mutatorsScroll.Add(mutatorLabel);
			}

			var weaponFilter = _matchSettings.WeaponsSelectionOverwrite;
			_allowedWeapons.Clear();
			_allowedWeaponsContainer.SetDisplay(weaponFilter.Length > 0);
			foreach (var weapon in weaponFilter)
			{
				_allowedWeapons.Add(new LocalizedLabel(weapon.GetTranslationKeyGameIdString())); // TODO mihak: Add localization key
			}
		}

		private void CheckCustomImage()
		{
			var url = _eventEntry.ImageURL;
			if (string.IsNullOrWhiteSpace(url))
			{
				return;
			}

			var request = MainInstaller.ResolveServices().RemoteTextureService.RequestTexture(url, cancellationToken: Presenter.GetCancellationTokenOnClose());
			Element.AddToClassList("event-container--custom-image");
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
			_eventTimer.SetTimer(() => _entryInfo.Duration.GetEndsAtDateTime(), "ENDS IN ");
			_eventTitle.text = _entryInfo.Entry.Title.GetText();
			_summary.text = _entryInfo.Entry.LongDescription.GetText();
			_actionButton.LocalizationKey = ScriptTerms.UITGameModeSelection.select_event;
			_actionButton.AddToClassList("button-long--green");
			if (string.IsNullOrWhiteSpace(_eventEntry.ImageURL))
			{
				var config = MainInstaller.ResolveServices().GameModeService.GetTeamSizeFor(_eventEntry);
				_eventImage.AddToClassList($"event-thumbnail__image--{config.EventImageModifierByTeam}");
			}

			CheckCustomImage();
			_rewardList.Clear();
			foreach (var mod in _matchSettings.RewardModifiers)
			{
				if (mod.CollectedInsideGame) continue;
				AddEventReward(mod.Id, $"x{mod.Multiplier.AsFloat:0.##}", true);
			}

			foreach (var gameId in _matchSettings.MetaItemDropOverwrites.Select(a => a.Id).Distinct())
			{
				if (RewardLogic.TryGetRewardCurrencyGroupId(gameId, out var _))
				{
					continue;
				}
				
				AddEventReward(gameId, "", false);
			}

			_rewardList.EnableInClassList("rewards-list-container--single-line", _rewardList.childCount <= 3);
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

			_friendsTitle.SetVisibility(_friendsPlaying.Count > 0);
			_friendsInMatchScrollView.Clear();

			foreach (var friend in _friendsPlaying)
			{
				_friendsInMatchScrollView.Add(new Label(friend));
			}

			_customThumbnail.RemoveSpriteClasses();
			switch (_matchSettings.TeamSize)
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