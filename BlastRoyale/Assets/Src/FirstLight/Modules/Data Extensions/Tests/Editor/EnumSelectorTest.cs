using FirstLight;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.DataExtensions.Tests
{
	[TestFixture]
	public class EnumSelectorTest
	{
		public enum EnumExample
		{
			Value1,
			Value2
		}
	
		public class EnumSelectorExample : EnumSelector<EnumExample>
		{
			public EnumSelectorExample() : base(EnumExample.Value1)
			{
			}
		
			public EnumSelectorExample(EnumExample data) : base(data)
			{
			}
		
			public static implicit operator EnumSelectorExample(EnumExample value)
			{
				return new EnumSelectorExample(value);
			}
		}

		private EnumSelectorExample _enumSelector;
		
		[SetUp]
		public void Init()
		{
			_enumSelector = new EnumSelectorExample(EnumExample.Value1);
		}
		
		[Test]
		public void ValueCheck()
		{
			Assert.AreEqual(EnumExample.Value1, _enumSelector.GetSelection());
			Assert.AreEqual((int) EnumExample.Value1, _enumSelector.GetSelectedIndex());
			Assert.AreEqual(EnumExample.Value1.ToString(), _enumSelector.GetSelectionString());
			Assert.IsTrue(_enumSelector.HasValidSelection());
		}
		
		[Test]
		public void ValueSetCheck()
		{
			_enumSelector.SetSelection(EnumExample.Value2);
			
			Assert.AreEqual(EnumExample.Value2, _enumSelector.GetSelection());
			Assert.AreEqual((int) EnumExample.Value2, _enumSelector.GetSelectedIndex());
			Assert.AreEqual(EnumExample.Value2.ToString(), _enumSelector.GetSelectionString());
			Assert.IsTrue(_enumSelector.HasValidSelection());
		}
	}
}