using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This Mono component is used to attach a animator controller override reference on
	/// a given game object
	/// </summary>
	public class RuntimeAnimatorMonoComponent : MonoBehaviour
	{
		[SerializeField, Required] private RuntimeAnimatorController _animatorController;
		
		public RuntimeAnimatorController AnimatorController => _animatorController;
	}
}