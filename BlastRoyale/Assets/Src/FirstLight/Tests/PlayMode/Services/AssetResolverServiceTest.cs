using System.Collections;
using FirstLight.Game.Services;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FirstLight.Tests.PlayMode.Services
{
	public class AssetResolverServiceTest
	{
		private AssetResolverService _assetResolverService;

		[SetUp]
		public void Init()
		{
			_assetResolverService = new AssetResolverService();
		}
	}
}