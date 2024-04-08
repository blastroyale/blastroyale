using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Collections
{
	public class WeaponSkinMonoComponent : MonoBehaviour
	{
		[InfoBox("If not set will use default animation")] [SerializeField]
		private RuntimeAnimatorController _animatorController;

		public RuntimeAnimatorController AnimatorController => _animatorController;
	}
}