using System;
using System.IO;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Collection;
using FirstLight.Game.Ids;
using Quantum;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.MonoComponent.Collections
{
	public class WeaponSkinMonoComponent : MonoBehaviour
	{
		[InfoBox("If not set will use default animation")] [SerializeField]
		private RuntimeAnimatorController _animatorController;

		public RuntimeAnimatorController AnimatorController => _animatorController;
	}
}