using System.Collections.Generic;
using FirstLight.Services;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.Services.Tests
{
	[TestFixture]
	public class DataServiceTest
	{
		private DataService _dataService;

		// ReSharper disable once MemberCanBePrivate.Global
		public interface IDataMockup {}

		[SetUp]
		public void Init()
		{
			_dataService = new DataService();
		}

		[Test]
		public void AddData_Successfully()
		{
			var data = Substitute.For<IDataMockup>();
			
			_dataService.AddData(data, false);

			Assert.AreSame(data, _dataService.GetData<IDataMockup>());
		}

		[Test]
		public void GetData_NotFound_ThrowsException()
		{
			Assert.Throws<KeyNotFoundException>(() => _dataService.GetData<IDataMockup>());
		}
	}
}