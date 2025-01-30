using System;
using FirstLight.Game.Domains.VFX;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx.Helpers
{
	public class VfxSpawnOnEnableMonoBehaviour : MonoBehaviour
	{
		[SerializeField] private VfxId _vfx;

		public void OnEnable()
		{
			AbstractVfx<VfxId> vfx;
			if (MainInstaller.TryResolve<IMatchServices>(out var matchServices))
			{
				vfx = matchServices.VfxService.Spawn(_vfx);
			}
			else
			{
				throw new Exception("Trying to spawn VFX outside match!");
			}

			var currentTransform = transform;
			vfx.transform.SetPositionAndRotation(currentTransform.position, currentTransform.rotation);
		}
	}
}