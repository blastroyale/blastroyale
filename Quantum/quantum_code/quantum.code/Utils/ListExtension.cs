using System;
using System.Collections;
using System.Collections.Generic;

namespace Quantum
{
	/// <summary>
	/// Container for extension functions for the System.Collections.Generic.IList{T} and System.Collections.IList
	/// interfaces that inserts elements lists that are presumed to be already sorted such that sort ordering is preserved
	/// </summary>
	/// <author>Jackson Dunstan, http://JacksonDunstan.com/articles/3189</author>
	/// <license>MIT</license>
	/// <remarks>
	/// IMPORTANT: This sorting might not be deterministic. AVOID using it in the quantum simulation update
	/// </remarks>
	public static class ListExtensions
	{
		/// <summary>
		/// Insert a value into an IList{T} that is presumed to be already sorted such that sort
		/// ordering is preserved
		/// </summary>
		/// <param name="list">List to insert into</param>
		/// <param name="value">Value to insert</param>
		/// <typeparam name="T">Type of element to insert and type of elements in the list</typeparam>
		internal static void InsertIntoSortedList<T>(this IList<T> list, T value) where T : IComparable<T>
		{
			InsertIntoSortedList(list, value, (a,b) => a.CompareTo(b));
		}
	 
		/// <summary>
		/// Insert a value into an IList{T} that is presumed to be already sorted such that sort
		/// ordering is preserved
		/// </summary>
		/// <param name="list">List to insert into</param>
		/// <param name="value">Value to insert</param>
		/// <param name="comparison">Comparison to determine sort order with</param>
		/// <typeparam name="T">Type of element to insert and type of elements in the list</typeparam>
		internal static void InsertIntoSortedList<T>(this IList<T> list, T value, Comparison<T> comparison)
		{
			var startIndex = 0;
			var endIndex = list.Count;
			
			while (endIndex > startIndex)
			{
				var windowSize = endIndex - startIndex;
				var middleIndex = startIndex + (windowSize / 2);
				var middleValue = list[middleIndex];
				var compareToResult = comparison(middleValue, value);
				
				if (compareToResult == 0)
				{
					list.Insert(middleIndex, value);
					return;
				}
				
				if (compareToResult < 0)
				{
					startIndex = middleIndex + 1;
				}
				else
				{
					endIndex = middleIndex;
				}
			}
			
			list.Insert(startIndex, value);
		}
	 
		/// <summary>
		/// Insert a value into an IList{T} that is presumed to be already sorted such that sort
		/// ordering is preserved
		/// </summary>
		/// <param name="list">List to insert into</param>
		/// <param name="value">Value to insert</param>
		/// <param name="comparer">Comparer to determine sort order with</param>
		/// <typeparam name="T">Type of element to insert and type of elements in the list</typeparam>
		internal static void InsertIntoSortedList<T>(this IList<T> list, T value, IComparer<T> comparer)
		{
			var startIndex = 0;
			var endIndex = list.Count;
			
			while (endIndex > startIndex)
			{
				var windowSize = endIndex - startIndex;
				var middleIndex = startIndex + (windowSize / 2);
				var middleValue = list[middleIndex];
				var compareToResult = comparer.Compare(middleValue, value);
				
				if (compareToResult == 0)
				{
					list.Insert(middleIndex, value);
					return;
				}
				
				if (compareToResult < 0)
				{
					startIndex = middleIndex + 1;
				}
				else
				{
					endIndex = middleIndex;
				}
			}
			
			list.Insert(startIndex, value);
		}
	 
		/// <summary>
		/// Insert a value into an IList that is presumed to be already sorted such that sort ordering is preserved
		/// </summary>
		/// <param name="list">List to insert into</param>
		/// <param name="value">Value to insert</param>
		internal static void InsertIntoSortedList(this IList list, IComparable value)
		{
			InsertIntoSortedList(list, value, (a,b) => a.CompareTo(b));
		}
	 
		/// <summary>
		/// Insert a value into an IList that is presumed to be already sorted such that sort ordering is preserved
		/// </summary>
		/// <param name="list">List to insert into</param>
		/// <param name="value">Value to insert</param>
		/// <param name="comparison">Comparison to determine sort order with</param>
		internal static void InsertIntoSortedList(this IList list, IComparable value, Comparison<IComparable> comparison)
		{
			var startIndex = 0;
			var endIndex = list.Count;
			while (endIndex > startIndex)
			{
				var windowSize = endIndex - startIndex;
				var middleIndex = startIndex + (windowSize / 2);
				var middleValue = (IComparable)list[middleIndex];
				var compareToResult = comparison(middleValue, value);
				if (compareToResult == 0)
				{
					list.Insert(middleIndex, value);
					return;
				}
				if (compareToResult < 0)
				{
					startIndex = middleIndex + 1;
				}
				else
				{
					endIndex = middleIndex;
				}
			}
			list.Insert(startIndex, value);
		}
	}
}