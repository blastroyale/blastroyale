using System.Collections.Generic;
using FirstLight.Game.MonoComponent.EntityViews;
using UnityEngine;

namespace FirstLight.Game.Views.MapViews
{
	/// <inheritdoc/>
	/// <remarks>
	/// Responsible of hiding the top of a building when a player enters it
	/// </remarks>
	public class BuildingTopRemovalView : MonoBehaviour
	{
		private static readonly int _topAnimatorPlayerInsideParamNameHash = Animator.StringToHash("PlayerInside");
			
		[SerializeField] private Animator _topRemovalAnimator;

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
			UpdateBuildingTop(true);
		}

		private void PlayerExitedBuilding(GameObject player)
		{
			UpdateBuildingTop(false);
		}

		private void UpdateBuildingTop(bool playerInside)
		{
			_topRemovalAnimator.SetBool(_topAnimatorPlayerInsideParamNameHash, playerInside);
		}
	}
}