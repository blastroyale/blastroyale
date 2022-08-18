using NUnit.Framework;
using Quantum;
using System.Collections.Generic;
using System.IO;

namespace tests
{
	public class TestCustomPlugin
	{
		private CustomQuantumPlugin _plugin;

		[SetUp]
		public void Setup()
		{
			var cfg = new Dictionary<string, string>()
			{
				{ "PlayfabTitle", "ASD" },
				{ "PlayfabKey", "ASD" },
			};
			_plugin = new CustomQuantumPlugin(new CustomQuantumServer(cfg, null));
		}

		[Test]
		public void TestSomething()
		{
		
		}
	}
}
