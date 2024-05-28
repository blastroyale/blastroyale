using System;
using FirstLight.Services;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.Services.Tests
{
	public class InstallerTest
	{
		private interface IInterface {}
		private class Implementation : IInterface {}

		private Installer _installer;
		
		[SetUp]
		public void Init()
		{
			_installer = new Installer();
		}

		[Test]
		public void Bind_Resolve_Successfully()
		{
			_installer.Bind<IInterface>(new Implementation());
			
			var instance = _installer.Resolve<IInterface>();
			
			Assert.IsNotNull(instance);
			Assert.AreSame(typeof(Implementation), instance.GetType());
		}

		[Test]
		public void Bind_NotInterface_ThrowsException()
		{
			Assert.Throws<ArgumentException>(() => _installer.Bind(new Implementation()));
		}

		[Test]
		public void Resolve_NotBinded_ThrowsException()
		{
			Assert.Throws<ArgumentException>(() => _installer.Resolve<IInterface>());
		}
	}
}