using System.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Input;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <summary>
	/// This Mono component controls the behaviour of the <see cref="PlayerCharacter"/>'s <see cref="Quantum.EntityPrototype"/>
	/// </summary>
	public class PlayerCharacterMonoComponent : HealthEntityBase, LocalInput.IGameplayActions
	{
		[SerializeField] private Transform _emojiAnchor;

		private readonly IIndicator[] _indicators = new IIndicator[(int) IndicatorVfxId.TOTAL];
		private readonly Pair<ITransformIndicator, QuantumSpecialConfig>[] _specialIndicators = 
			new Pair<ITransformIndicator, QuantumSpecialConfig>[Constants.MAX_SPECIALS];

		private PlayerCharacterViewMonoComponent _playerView;
		private LocalInput _localInput;
		private Pair<ITransformIndicator, QuantumSpecialConfig> _specialAimIndicator;
		private ITransformIndicator _shootIndicator;
		private ITransformIndicator _movementIndicator;
		
		/// <summary>
		/// The <see cref="Transform"/> anchor values to attach the avatar emoji
		/// </summary>
		public Transform EmojiAnchor => _emojiAnchor;

		protected override void OnAwake()
		{
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
		}

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			base.OnEntityInstantiated(game);
			
			var frame = game.Frames.Verified;
			
			InstantiateAvatar(game, frame.Get<PlayerCharacter>(EntityView.EntityRef).Player);
		}

		protected override void OnEntityDestroyed(QuantumGame game)
		{
			_localInput?.Dispose();
			
			base.OnEntityDestroyed(game);
		}

		/// <inheritdoc />
		public void OnMove(InputAction.CallbackContext context)
		{
			var direction = context.ReadValue<Vector2>();
			
			_movementIndicator.SetTransformState(direction);
			_movementIndicator.SetVisualState(direction.sqrMagnitude > 0);
		}

		/// <inheritdoc />
		public void OnAim(InputAction.CallbackContext context)
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var direction = context.ReadValue<Vector2>();
			var isEmptied = TryGetComponentData<PlayerCharacter>(game, out var component) && 
			                component.IsAmmoEmpty(frame, EntityView.EntityRef);
			
			_shootIndicator.SetTransformState(direction);
			_shootIndicator.SetVisualState(direction.sqrMagnitude > 0, isEmptied);
		}

		/// <inheritdoc />
		public void OnSpecialAim(InputAction.CallbackContext context)
		{
			_specialAimIndicator.Key?.SetTransformState(context.ReadValue<Vector2>());
		}

		/// <inheritdoc />
		public void OnAimButton(InputAction.CallbackContext context)
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var isDown = context.ReadValueAsButton();
			var isEmptied = TryGetComponentData<PlayerCharacter>(game, out var component) && 
			                component.IsAmmoEmpty(frame, EntityView.EntityRef);
			
			_indicators[(int) IndicatorVfxId.Range].SetVisualState(isDown);
			_playerView.SetMovingState(isDown);
			_shootIndicator.SetVisualState(isDown, isEmptied);
		}

		/// <inheritdoc />
		public void OnSpecialButton0(InputAction.CallbackContext context)
		{
			var isDown = context.ReadValueAsButton();
			var config = _specialIndicators[0].Value;
			
			_specialAimIndicator.Key?.SetVisualState(false);
			
			_specialAimIndicator = isDown ? _specialIndicators[0] : new Pair<ITransformIndicator, QuantumSpecialConfig>();
			
			_specialAimIndicator.Key?.SetVisualState(true);
			_specialAimIndicator.Key?.SetTransformState(Vector2.zero);
			_specialAimIndicator.Key?.SetVisualProperties(config.Radius.AsFloat * GameConstants.RadiusToScaleConversionValue,
			                                              config.MinRange.AsFloat, config.MaxRange.AsFloat);
		}

		/// <inheritdoc />
		public void OnSpecialButton1(InputAction.CallbackContext context)
		{
			var isDown = context.ReadValueAsButton();
			var config = _specialIndicators[1].Value;
			
			_specialAimIndicator.Key?.SetVisualState(false);
			
			_specialAimIndicator = isDown ? _specialIndicators[1] : new Pair<ITransformIndicator, QuantumSpecialConfig>();
			
			
			_specialAimIndicator.Key?.SetVisualState(true);
			_specialAimIndicator.Key?.SetTransformState(Vector2.zero);
			_specialAimIndicator.Key?.SetVisualProperties(config.Radius.AsFloat * GameConstants.RadiusToScaleConversionValue,
			                                              config.MinRange.AsFloat, config.MaxRange.AsFloat);
		}

		private void HandleOnLocalPlayerAmmoEmpty(EventOnLocalPlayerAmmoEmpty callback)
		{
			var shootState = _shootIndicator?.VisualState ?? false;
			
			_shootIndicator?.SetVisualState(shootState, true);
		}

		private void HandleOnConsumablePicked(EventOnConsumablePicked callback)
		{
			var shootState = _shootIndicator?.VisualState ?? false;

			if (callback.Consumable.ConsumableType == ConsumableType.Ammo)
			{
				_shootIndicator?.SetVisualState(shootState);
			}
		}

		private void HandleOnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			SetWeaponIndicators(callback.Weapon.GameId);
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (EntityView.EntityRef != callback.Entity)
			{
				return;
			}
			
			var position = GetComponentData<Transform3D>(callback.Game).Position.ToUnityVector3();
			var aliveVfx = Services.VfxService.Spawn(VfxId.SpawnPlayer);
			
			aliveVfx.transform.position = position;
		}
		
		private async void InstantiateAvatar(QuantumGame quantumGame, PlayerRef player)
		{
			var frame = quantumGame.Frames.Verified;
			var stats = frame.Get<Stats>(EntityView.EntityRef);
			
			GetPlayerEquipmentSet(frame, player, out var skin, out var weapon, out var gear);

			if (quantumGame.PlayerIsLocal(player))
			{
				InstantiatePlayerIndicators(weapon.GameId);
			}
			
			var instance = await Services.AssetResolverService.RequestAsset<GameId, GameObject>(skin, true, true, OnLoaded);

			if (this.IsDestroyed())
			{
				return;
			}
			
			await instance.GetComponent<MatchCharacterViewMonoComponent>().Init(weapon, gear, EntityView);
			
			_playerView = instance.GetComponent<PlayerCharacterViewMonoComponent>();

			if (stats.CurrentStatusModifierType != StatusModifierType.None)
			{
				_playerView.SetStatusModifierEffect(stats.CurrentStatusModifierType, stats.CurrentStatusModifierDuration.AsFloat);
			}
		}

		private async void InstantiatePlayerIndicators(GameId weapon)
		{
			var loader = Services.AssetResolverService;
			var rangeTask = loader.RequestAsset<IndicatorVfxId, GameObject>(IndicatorVfxId.Range);
			var lineTask = loader.RequestAsset<IndicatorVfxId, GameObject>(IndicatorVfxId.Line);
			var coneTask = loader.RequestAsset<IndicatorVfxId, GameObject>(IndicatorVfxId.Cone);
			var radialTask = loader.RequestAsset<IndicatorVfxId, GameObject>(IndicatorVfxId.Radial);
			var movementTask = loader.RequestAsset<IndicatorVfxId, GameObject>(IndicatorVfxId.Movement);
			var scalableLineTask = loader.RequestAsset<IndicatorVfxId, GameObject>(IndicatorVfxId.ScalableLine);

			await Task.WhenAll(rangeTask, lineTask, coneTask, radialTask, movementTask, scalableLineTask);
			
			if(this.IsDestroyed())
			{
				return;
			}

			_localInput = new LocalInput();
			_indicators[(int) IndicatorVfxId.Cone] = coneTask.Result.GetComponent<ConeIndicatorMonoComponent>();
			_indicators[(int) IndicatorVfxId.Line] = lineTask.Result.GetComponent<LineIndicatorMonoComponent>();
			_indicators[(int) IndicatorVfxId.Movement] = _movementIndicator = movementTask.Result.GetComponent<MovementIndicatorMonoComponent>();
			_indicators[(int) IndicatorVfxId.None] = null;
			_indicators[(int) IndicatorVfxId.Radial] = radialTask.Result.GetComponent<RadialIndicatorMonoComponent>();
			_indicators[(int) IndicatorVfxId.Range] = rangeTask.Result.GetComponent<RangeIndicatorMonoComponent>();
			_indicators[(int) IndicatorVfxId.ScalableLine] = scalableLineTask.Result.GetComponent<ScalableLineIndicatorMonoComponent>();

			foreach (var indicator in _indicators)
			{
				indicator?.Init(EntityView);
			}

			_localInput.Gameplay.SetCallbacks(this);
			_localInput.Enable();
			SetWeaponIndicators(weapon);

			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, HandleOnLocalPlayerWeaponChanged);
			QuantumEvent.Subscribe<EventOnConsumablePicked>(this, HandleOnConsumablePicked);
			QuantumEvent.Subscribe<EventOnLocalPlayerAmmoEmpty>(this, HandleOnLocalPlayerAmmoEmpty);
		}

		private void SetWeaponIndicators(GameId weapon)
		{
			var configProvider = Services.ConfigsProvider;
			var specialConfigs = configProvider.GetConfigsDictionary<QuantumSpecialConfig>();
			var config = configProvider.GetConfig<QuantumWeaponConfig>((int) weapon);
			var range = config.AttackRange.AsFloat;
			var shootState = _shootIndicator?.VisualState ?? false;
			var angleInRad = config.AttackAngle;
			var size = Mathf.Max(0.5f, Mathf.Tan(angleInRad * 0.5f * Mathf.Deg2Rad) * range * 2f);
			var indicator = angleInRad > 0 ? IndicatorVfxId.Cone : IndicatorVfxId.Line;
			
			// For a melee weapon with a splash damage we use a separate calculation for an indicator
			if (config.Id == GameId.Hammer && config.SplashRadius > FP._0)
			{
				range += config.SplashRadius.AsFloat;
				size = config.SplashRadius.AsFloat * 2f;
			}
			
			_shootIndicator?.SetVisualState(false);
			
			_shootIndicator = _indicators[(int) indicator] as ITransformIndicator;
			
			_indicators[(int) IndicatorVfxId.Range].SetVisualProperties(range, 0, range);
			_indicators[(int) IndicatorVfxId.Range].SetVisualState(shootState);
			_shootIndicator?.SetVisualProperties(size, 0, range);
			_shootIndicator?.SetVisualState(shootState);

			for (var i = 0; i < Constants.MAX_SPECIALS; i++)
			{
				var pair = new Pair<ITransformIndicator, QuantumSpecialConfig>();
				
				if (specialConfigs.TryGetValue((int) config.Specials[i], out var specialConfig))
				{
					pair.Key = _indicators[(int) specialConfig.Indicator] as ITransformIndicator;
					pair.Value = specialConfig;
				}
				
				_specialIndicators[i] = pair;
			}
		}

		private void GetPlayerEquipmentSet(Frame f, PlayerRef player, out GameId skin, out Equipment weapon, out Equipment[] gear)
		{
			if (f.TryGet<BotCharacter>(EntityView.EntityRef, out var botCharacter))
			{
				skin = botCharacter.Skin;
				weapon = botCharacter.Weapon;
				gear = new Equipment[botCharacter.Gear.Length];

				for (var i = 0; i < botCharacter.Gear.Length; i++)
				{
					gear[i] = botCharacter.Gear[i];
				}

				return;
			}
			
			
			var playerData = f.GetPlayerData(player);
				
			skin = playerData.Skin;
			weapon = playerData.Weapon;
			gear = playerData.Gear;
		}
	}
}