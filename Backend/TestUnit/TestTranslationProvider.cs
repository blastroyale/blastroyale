using FirstLight.Game.Utils;
using NUnit.Framework;
using Quantum;
using ServerCommon.CommonServices;
using Assert = NUnit.Framework.Assert;

namespace Tests
{
	public class TestTranslationProvider
	{
		private EmbeddedTranslationProvider _provider;

		[SetUp]
		public void SetUp()
		{
			_provider = new EmbeddedTranslationProvider();
		}


		[Test]
		public void GameIdTranslationShouldNotBeEqualsEnum()
		{
			Assert.AreNotEqual(GameId.ApoRifle.ToString(), _provider.GetTranslation(GameId.ApoRifle.GetTranslationTerm()));
		}

		[Test]
		public void NotFoundKeyShouldReturnItSelf()
		{
			var key = "NOT_FOUND_RANDOM_KEY_42";
			Assert.AreEqual(key, _provider.GetTranslation(key));
		}
	}
}