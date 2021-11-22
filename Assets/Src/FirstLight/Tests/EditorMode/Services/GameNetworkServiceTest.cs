using System.Collections;
using FirstLight.Game.Services;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace FirstLight.Tests.EditorMode.Services
{
	public class GameNetworkServiceTest
	{
		private GameNetworkService _gameNetworkService;

		[SetUp]
		public void Init()
		{
			_gameNetworkService = new GameNetworkService();
		}
	}
}