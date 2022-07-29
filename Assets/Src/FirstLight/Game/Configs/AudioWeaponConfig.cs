using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct AudioWeaponConfig
	{
		public AudioId WeaponShotWindUp;
		public AudioId WeaponShot;
		public AudioId WeaponShotWindDown;
		public AudioId ProjectileFlyTrail;
		public AudioId ProjectileImpact;
	}
}
