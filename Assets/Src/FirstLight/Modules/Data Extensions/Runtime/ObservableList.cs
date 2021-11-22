using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// ReSharper disable once CheckNamespace

namespace FirstLight
{
	/// <summary>
	/// A list with the possibility to observe changes to it's elements defined <see cref="ObservableUpdateType"/> rules
	/// </summary>
	public interface IObservableListReader : IEnumerable
	{
		/// <summary>
		/// Requests the list element count
		/// </summary>
		int Count { get; }
	}
	
	/// <inheritdoc cref="IObservableListReader"/>
	/// <remarks>
	/// Read only observable list interface
	/// </remarks>
	public interface IObservableListReader<T> :IObservableListReader, IEnumerable<T> where T : struct
	{
		/// <summary>
		/// Looks up and return the data that is associated with the given <paramref name="index"/>
		/// </summary>
		T this[int index] { get; }
		
		/// <summary>
		/// Requests this list as a <see cref="IReadOnlyList{T}"/>
		/// </summary>
		IReadOnlyList<T> ReadOnlyList { get; }

		/// <inheritdoc cref="List{T}.Contains"/>
		bool Contains(T value);

		/// <inheritdoc cref="List{T}.IndexOf(T)"/>
		int IndexOf(T value);
		
		/// <summary>
		/// Observes to this list changes with the given <paramref name="onUpdate"/>
		/// </summary>
		void Observe(Action<int, T, T, ObservableUpdateType> onUpdate);
		
		/// <summary>
		/// Observes this list with the given <paramref name="onUpdate"/> when any data changes and invokes it with the given <paramref name="index"/>
		/// </summary>
		void InvokeObserve(int index, Action<int, T, T, ObservableUpdateType> onUpdate);
		
		/// <summary>
		/// Stops observing this dictionary with the given <paramref name="onUpdate"/> of any data changes
		/// </summary>
		void StopObserving(Action<int, T, T, ObservableUpdateType> onUpdate);
		
		/// <summary>
		/// Stops observing this dictionary changes from all the given <paramref name="subscriber"/> calls.
		/// If the given <paramref name="subscriber"/> is null then will stop observing from everything.
		/// </summary>
		void StopObservingAll(object subscriber = null);
	}

	/// <inheritdoc />
	public interface IObservableList<T> : IObservableListReader<T> where T : struct
	{
		/// <summary>
		/// Changes the given <paramref name="index"/> in the list. If the data does not exist it will be added.
		/// It will notify any observer listing to its data
		/// </summary>
		new T this[int index] { get; set; }
		
		/// <inheritdoc cref="List{T}.Remove"/>
		void Add(T data);
		
		/// <inheritdoc cref="List{T}.Remove"/>
		void Remove(T data);
		
		/// <inheritdoc cref="List{T}.RemoveAt"/>
		void RemoveAt(int index);
		
		/// <remarks>
		/// It invokes any update method that is observing to the given <paramref name="index"/> on this list
		/// </remarks>
		void InvokeUpdate(int index);
	}
	
	/// <inheritdoc />
	public class ObservableList<T> : IObservableList<T> where T : struct
	{
		private readonly IList<Action<int, T, T, ObservableUpdateType>> _updateActions = new List<Action<int, T, T, ObservableUpdateType>>();

		/// <inheritdoc cref="IObservableList{T}.this" />
		public T this[int index]
		{
			get => List[index];
			set
			{
				var previousValue = List[index];
				
				List[index] = value;
				
				InvokeUpdate(index, previousValue);
			}
		}
		
		/// <inheritdoc />
		public int Count => List.Count;
		/// <inheritdoc />
		public IReadOnlyList<T> ReadOnlyList => List;
		
		protected virtual List<T> List { get; }
		
		protected ObservableList() {}
		
		public ObservableList(List<T> list)
		{
			List = list;
		}

		/// <inheritdoc cref="List{T}.GetEnumerator"/>
		public List<T>.Enumerator GetEnumerator()
		{
			return List.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return List.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return List.GetEnumerator();
		}

		/// <inheritdoc />
		public bool Contains(T value)
		{
			return List.Contains(value);
		}

		/// <inheritdoc />
		public int IndexOf(T value)
		{
			return List.IndexOf(value);
		}

		/// <inheritdoc />
		public void Add(T data)
		{
			List.Add(data);
			
			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](List.Count - 1, default, data, ObservableUpdateType.Added);
			}
		}

		/// <inheritdoc />
		public void Remove(T data)
		{
			var idx = List.IndexOf(data);

			if (idx >= 0)
			{
				RemoveAt(idx);
			}
		}

		/// <inheritdoc />
		public void RemoveAt(int index)
		{
			var data = List[index];
			
			List.RemoveAt(index);
			
			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](index, data, default, ObservableUpdateType.Removed);
			}
		}

		/// <inheritdoc />
		public void InvokeUpdate(int index)
		{
			InvokeUpdate(index, List[index]);
		}

		/// <inheritdoc />
		public void Observe(Action<int, T, T, ObservableUpdateType> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void InvokeObserve(int index, Action<int, T, T, ObservableUpdateType> onUpdate)
		{
			Observe(onUpdate);
			InvokeUpdate(index);
		}

		/// <inheritdoc />
		public void StopObserving(Action<int, T, T, ObservableUpdateType> onUpdate)
		{
			_updateActions.Remove(onUpdate);
		}

		/// <inheritdoc />
		public void StopObservingAll(object subscriber = null)
		{
			if (subscriber == null)
			{
				_updateActions.Clear();
				return;
			}

			for (var i = _updateActions.Count - 1; i > -1; i--)
			{
				if (_updateActions[i].Target == subscriber)
				{
					_updateActions.RemoveAt(i);
				}
			}
		}
		
		protected void InvokeUpdate(int index, T previousValue)
		{
			var data = List[index];
			
			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](index, previousValue, data, ObservableUpdateType.Updated);
			}
		}
	}

	/// <inheritdoc />
	public class ObservableResolverList<T> : ObservableList<T> where T : struct
	{
		private readonly Func<List<T>> _listResolver;

		protected override List<T> List => _listResolver();

		public ObservableResolverList(Func<List<T>> listResolver)
		{
			_listResolver = listResolver;
		}
	}
}