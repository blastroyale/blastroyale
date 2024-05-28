using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx.Helpers
{
	public class SpawnVfxOnEnable : MonoBehaviour
	{
		[SerializeField] private VfxId _vfx;

		public void OnEnable()
		{
			var vfx = MainInstaller.ResolveServices().VfxService.Spawn(_vfx);
			var currentTransform = transform;
			vfx.transform.SetPositionAndRotation(currentTransform.position, currentTransform.rotation);
		}
	}
}