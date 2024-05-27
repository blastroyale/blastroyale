using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Models.Collection;
using FirstLight.Server.SDK.Models;
using NUnit.Framework;
using UnityEngine;

namespace FirstLight.Tests.EditorMode.Models
{
	public class TestRemoteCollectionSerializer
	{
		[Test]
		public void TestCorposSerialization()
		{
			var response = new CollectionFetchResponse()
			{
				Owned = new List<RemoteCollectionItem>(Enumerable.Range(0, 100).Select(a => new Corpos() {Identifier = $"{a}"}).ToList())
			};

			var serialized = CollectionSerializer.Serialize(response);

			var deserialize = CollectionSerializer.Deserialize(serialized);

			Assert.AreEqual(100,deserialize.Owned.Count());
			foreach (var remoteCollectionItem in deserialize.Owned)
			{
				Assert.AreEqual(typeof(Corpos), remoteCollectionItem.GetType());
			}
		}
	}
}