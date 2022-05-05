using System.Collections.Generic;
using FirstLight.Game.MonoComponent.EntityViews;
using UnityEngine;

namespace FirstLight.Game.Views.MapViews
{
	public class BuildingTopRemovalView : MonoBehaviour
	{
		private static readonly int _topAnimatorPlayerInsideParamNameHash = Animator.StringToHash("PlayerInside");
			
		[SerializeField] private Animator _topRemovalAnimator;

		private HashSet<int> _playersInside = new ();

		private void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player) && player.IsLocalPlayer)
			{
				PlayerEnteredBuilding(other.gameObject);
			}
		}
		private void OnTriggerExit(Collider other)
		{
			if (other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out var player) && player.IsLocalPlayer)
			{
				PlayerExitedBuilding(other.gameObject);
			}		
		}

		private void PlayerEnteredBuilding(GameObject player)
		{
			_playersInside.Add(player.GetInstanceID());

			UpdateBuildingTop();
		}

		private void PlayerExitedBuilding(GameObject player)
		{
			_playersInside.Remove(player.GetInstanceID());

			UpdateBuildingTop();
		}

		private void UpdateBuildingTop()
		{
			_topRemovalAnimator.SetBool(_topAnimatorPlayerInsideParamNameHash, _playersInside.Count > 0);
		}
	}
}