using System;
using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using NUnit.Framework;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
// ReSharper disable UnassignedField.Global

namespace FirstLightEditor.GoogleSheetImporter.Tests
{
	public class CsvParserTest
	{
		public enum MockEnum
		{
			MockValue
		}
		public struct MockKeyValuePair
		{
			public int Key;
			public int Value;

			public MockKeyValuePair(int key, int value)
			{
				Key = key;
				Value = value;
			}
		}

		public class MockClass
		{
			[ParseIgnore]
			public string Ignored;
			public string String;
			public int Int;
			public float Float;
			public double Double;
			public MockEnum Enum;
			public int[] Array;
			public List<int> List;
			public KeyValuePair<int,int> Pair;
			public Dictionary<int,int> Dictionary;
		}
		
		[Test]
		public void ConvertCsv_Successfully()
		{
			var csv = "Ignored,String,Int,Float,Double,Enum,Array,List,Pair,Dictionary\r\n" +
			          "Ignored,text,1,1.1,1.1,MockValue,\"1,2\",\"1,2\",\"1:2\",\"1,2\"";
			var dic = CsvParser.ConvertCsv(csv);
			
			Assert.AreEqual(1, dic.Count);
			Assert.AreEqual(10, dic[0].Count);
			Assert.AreEqual("Ignored", dic[0]["Ignored"]);
			Assert.AreEqual("text", dic[0]["String"]);
			Assert.AreEqual("1", dic[0]["Int"]);
			Assert.AreEqual("1.1", dic[0]["Float"]);
			Assert.AreEqual("1.1", dic[0]["Double"]);
			Assert.AreEqual("MockValue", dic[0]["Enum"]);
			Assert.AreEqual("1,2", dic[0]["Array"]);
			Assert.AreEqual("1,2", dic[0]["List"]);
			Assert.AreEqual("1:2", dic[0]["Pair"]);
			Assert.AreEqual("1,2", dic[0]["Dictionary"]);
		}

		[Test]
		public void ConvertCsv_WrongFormatSeparator_EmptyResult()
		{
			var csv = "int,float\n" +
			          "1,1.1";

			var dic = CsvParser.ConvertCsv(csv);
			
			Assert.AreEqual(0, dic.Count);
		}

		[Test]
		public void ConvertCsv_OnlyHeadlines_EmptyResult()
		{
			var csv = "int,float,double,enum,pair";

			var dic = CsvParser.ConvertCsv(csv);
			
			Assert.AreEqual(0, dic.Count);
		}

		[Test]
		public void ConvertCsv_MissMatchColumnsCount_ThrowsException()
		{
			var csv = "int,float,double\r\n" +
			          "1,1.1";

			Assert.Throws<IndexOutOfRangeException>(() => CsvParser.ConvertCsv(csv));
		}

		[Test]
		public void Deserialize_Successfully()
		{
			var csv = "Ignored,String,Int,Float,Double,Enum,Array,List,Pair,Dictionary\r\n" +
			          "Ignored,text,1,1.1,1.1,MockValue,\"1,2\",\"1,2\",\"1:2\",\"1,2\"";
			var dic = CsvParser.ConvertCsv(csv);
			var result = CsvParser.DeserializeTo<MockClass>(dic[0]);

			Assert.AreEqual(null, result.Ignored);
			Assert.AreEqual("text", result.String);
			Assert.AreEqual(1, result.Int);
			Assert.AreEqual(1.1f, result.Float);
			Assert.AreEqual(1.1d, result.Double);
			Assert.AreEqual(MockEnum.MockValue, result.Enum);
			Assert.AreEqual(new[] {1, 2}, result.Array);
			Assert.AreEqual(new List<int> {1, 2}, result.List);
			Assert.AreEqual(new KeyValuePair<int, int>(1, 2), result.Pair);
			Assert.AreEqual(new Dictionary<int, int> {{1,2}} , result.Dictionary);
		}

		[Test]
		public void Deserialize_MissingFields_Successfully()
		{
			var csv = "Int,Float\r\n" +
			          "1,1.1";
			var dic = CsvParser.ConvertCsv(csv);
			var result = CsvParser.DeserializeTo<MockClass>(dic[0]);

			Assert.AreEqual(null, result.String);
			Assert.AreEqual(1, result.Int);
			Assert.AreEqual(1.1f, result.Float);
			Assert.AreEqual(0, result.Double);
			Assert.AreEqual(MockEnum.MockValue, result.Enum);
			Assert.AreEqual(null, result.Array);
			Assert.AreEqual(null, result.List);
			Assert.AreEqual(new KeyValuePair<int, int>(), result.Pair);
			Assert.AreEqual(null, result.Dictionary);
		}

		[Test]
		public void Deserialize_ExtraFields_Successfully()
		{
			var csv = "Int,Float,ExtraField\r\n" +
			          "1,1.1,extraValue";
			var dic = CsvParser.ConvertCsv(csv);
			var result = CsvParser.DeserializeTo<MockClass>(dic[0]);

			Assert.AreEqual(null, result.String);
			Assert.AreEqual(1, result.Int);
			Assert.AreEqual(1.1f, result.Float);
			Assert.AreEqual(0, result.Double);
			Assert.AreEqual(MockEnum.MockValue, result.Enum);
			Assert.AreEqual(null, result.Array);
			Assert.AreEqual(null, result.List);
			Assert.AreEqual(new KeyValuePair<int, int>(), result.Pair);
			Assert.AreEqual(null, result.Dictionary);
		}

		[Test]
		public void ArrayParse_Successfully()
		{
			var result = CsvParser.ArrayParse<int>("1,[2],{3,4},(5),6");
			
			Assert.AreEqual(new [] {1,2,3,4,5,6}, result);
		}

		[Test]
		public void ArrayParsePair_Successfully()
		{
			var result = CsvParser.ArrayParse<KeyValuePair<int,int>>("1:2,(3 < 4),[5 > 6],{7 = 8}");
			var pairArray = new[]
			{
				new KeyValuePair<int,int>(1,2),
				new KeyValuePair<int,int>(3,4),
				new KeyValuePair<int,int>(5,6),
				new KeyValuePair<int,int>(7,8), 
			};
			
			Assert.AreEqual(pairArray, result);
		}

		[Test]
		public void ArrayParsePair_ElementOddAmount_ThrowsException()
		{
			Assert.Throws<IndexOutOfRangeException>(() => CsvParser.ArrayParse<KeyValuePair<int,int>>("1:2,(3 < 4),5"));
		}

		[Test]
		public void DictionaryParse_Successfully()
		{
			var result = CsvParser.DictionaryParse<int,int>("1,2,3,4");
			
			Assert.AreEqual(new Dictionary<int, int> {{1,2},{3,4}} , result);
		}

		[Test]
		public void DictionaryParsePair_Successfully()
		{
			var result = CsvParser.DictionaryParse<int,int>("1:2,(3>4)");
			
			Assert.AreEqual(new Dictionary<int, int> {{1,2},{3,4}} , result);
		}

		[Test]
		public void DictionaryParse_ElementOddAmount_ThrowsException()
		{
			Assert.Throws<IndexOutOfRangeException>(() => CsvParser.DictionaryParse<int,int>("1,2,3"));
		}

		[Test]
		public void ParseInt_Successfully()
		{
			var result = CsvParser.Parse<int>("1");
			
			Assert.AreEqual(1, result);
		}

		[Test]
		public void ParseInt_WithFloat_ThrowsException()
		{
			Assert.Throws<FormatException>(() => CsvParser.Parse<int>("1.1f"));
		}

		[Test]
		public void ParsePair_Successfully()
		{
			var pair1 = CsvParser.Parse<KeyValuePair<int, int>>("1:2");
			var pair2 = CsvParser.Parse<MockKeyValuePair>("1:2");
			var result1 = new KeyValuePair<int, int>(1, 2);
			var result2 = new MockKeyValuePair(1, 2);
			
			Assert.AreEqual(result1, pair1);
			Assert.AreEqual(result2, pair2);
		}

		[Test]
		public void ParsePair_OneElement_ThrowsException()
		{
			Assert.Throws<IndexOutOfRangeException>(() => CsvParser.Parse<KeyValuePair<int, int>>("1"));
		}
	}
}