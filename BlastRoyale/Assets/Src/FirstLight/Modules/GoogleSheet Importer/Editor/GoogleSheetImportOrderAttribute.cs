using System;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.GoogleSheetImporter
{
	/// <summary>
	/// Attribute to set the order of importing the data of a <seealso cref="IGoogleSheetImporter"/>
	/// The smaller the number the sooner is going to be imported. Default = int.MaxValue
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class GoogleSheetImportOrderAttribute : Attribute
	{
		/// <summary>
		/// The order of the sheet to be imported. Less means importing sooner
		/// </summary>
		public int ImportOrder { get; }
		
		public GoogleSheetImportOrderAttribute(int importOrder)
		{
			ImportOrder = importOrder;
		}
	}
}