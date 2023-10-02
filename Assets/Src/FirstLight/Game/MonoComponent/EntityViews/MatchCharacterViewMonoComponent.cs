using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.MonoComponent.EntityViews
{
	/// <inheritdoc />
	public class MatchCharacterViewMonoComponent : CharacterEquipmentMonoComponent
	{
		private FootprinterMonoComponent _footsteps;
		private TemporarySkin _hammerSkin;

		public bool PrintFootsteps
		{
			set
			{
				if (_footsteps == null) return;
				_footsteps.SpawnFootprints = value;
			}
		}

		protected override async Task<GameObject> InstantiateEquipment(GameId gameId)
		{
			// There is a lot of hacks in this project, it is time for I to do some bullshit I WANT BIG SAUSAGE and
			// this assetresolver gameid is pain the arse to add new stuff into the game SAUSAGE
			if (gameId == GameId.Hammer && _hammerSkin != null)
			{
				var opHandle = Addressables.LoadAssetAsync<GameObject>(_hammerSkin.HammerAssetAddress);

				await opHandle.Task;
				if (opHandle.IsDone)
				{
					var asset = opHandle.Result;
					return Instantiate(asset);
				}
			}
			return await base.InstantiateEquipment(gameId);
		}

		private TemporarySkin GetHammerSkin(Frame frame, EntityRef entity)
		{
			var playerCharacter = frame.Get<PlayerCharacter>(entity);
			var playerName = Extensions.GetPlayerName(frame, entity, playerCharacter);
			// With this i don't need to share more stuff in runtimedate this works :D I'm very proud of it 
			return TemporarySkin.GetSkinBasedOnName(playerName);
		}
		
		/// <summary>
		/// Initializes the Adventure character view with the given player data
		/// </summary>
		public async Task Init(EntityView entityView, PlayerLoadout loadout, Frame frame)
		{
			_hammerSkin = GetHammerSkin(frame, entityView.EntityRef);
			_footsteps = gameObject.AddComponent<FootprinterMonoComponent>();
			_footsteps.Init(entityView, loadout);

			var weaponTask = EquipWeapon(loadout.Weapon.GameId);
			var list = new List<Task> {weaponTask};

			foreach (var item in loadout.Equipment)
			{
				if (!item.IsValid())
				{
					continue;
				}

				list.Add(EquipItem(item.GameId));
			}

			var isSkydiving = frame.Get<AIBlackboardComponent>(entityView.EntityRef).GetBoolean(frame, Constants.IsSkydiving);

			if (isSkydiving)
			{
				list.Add(InstantiateItem(loadout.Glider, GameIdGroup.Glider));
			}

			await Task.WhenAll(list);

			var runner = QuantumRunner.Default;

			if (this.IsDestroyed() || runner == null)
			{
				return;
			}

			var weapons = weaponTask.Result;

			for (var i = 0; i < weapons.Count; i++)
			{
				if (weapons[i] == null) continue;

				var components = weapons[i].GetComponents<EntityViewBase>();

				foreach (var entityViewBase in components)
				{
					entityViewBase.SetEntityView(runner.Game, entityView);
				}
			}

			if (isSkydiving)
			{
				HideAllEquipment();
			}
		}
	}
}