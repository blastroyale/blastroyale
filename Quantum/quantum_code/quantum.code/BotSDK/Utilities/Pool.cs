using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Quantum
{
	public static class Pool<T> where T : new()
	{
		private const int POOL_CAPACITY = 4;

		private static readonly List<T> _pool = new List<T>(POOL_CAPACITY);

		public static int Count => _pool.Count;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Get()
		{
			bool found = false;
			T item = default;

			lock (_pool)
			{
				int index = _pool.Count - 1;
				if (index >= 0)
				{
					found = true;
					item = _pool[index];

					_pool.RemoveAt(index);
				}
			}

			if (found == false)
			{
				item = new T();
			}

			return item;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Return(T item)
		{
			if (item == null)
				return;

			lock (_pool)
			{
				_pool.Add(item);
			}
		}
	}

	public static class Pool
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Get<T>() where T : new()
		{
			return Pool<T>.Get();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Return<T>(T item) where T : new()
		{
			Pool<T>.Return(item);
		}
	}
}