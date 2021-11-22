using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// ReSharper disable once CheckNamespace

namespace FirstLight
{
	/// <summary>
	/// A simple dictionary with the possibility to observe changes to it's elements defined <see cref="ObservableUpdateType"/> rules
	/// </summary>
	public interface IObservableDictionary : IEnumerable
	{
		/// <summary>
		/// Requests the element count of this dictionary
		/// </summary>
		int Count { get; }
	}

	/// <inheritdoc cref="IObservableDictionary"/>
	public interface IObservableDictionaryReader<TKey, TValue> : IObservableDictionary, IEnumerable<KeyValuePair<TKey, TValue>>
	{
		/// <summary>
		/// Looks up and return the data that is associated with the given <paramref name="key"/>
		/// </summary>
		TValue this[TKey key] { get; }
		
		/// <summary>
		/// Requests this dictionary as a <see cref="IReadOnlyDictionary{TKey,TValue}"/>
		/// </summary>
		IReadOnlyDictionary<TKey, TValue> ReadOnlyDictionary { get; }
			
		/// <inheritdoc cref="Dictionary{TKey,TValue}.TryGetValue" />
		bool TryGetValue(TKey key, out TValue value);

		/// <inheritdoc cref="Dictionary{TKey,TValue}.ContainsKey" />
		bool ContainsKey(TKey key);
		
		/// <summary>
		/// Observes to this dictionary changes with the given <paramref name="onUpdate"/>
		/// </summary>
		void Observe(Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate);
		
		/// <summary>
		/// Observes to this dictionary changes with the given <paramref name="onUpdate"/> when the given <paramref name="key"/>
		/// data changes
		/// </summary>
		void Observe(TKey key, Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate);
		
		/// <inheritdoc cref="Observe(TKey,System.Action{TKey,TValue,TValue,FirstLight.ObservableUpdateType})" />
		/// <remarks>
		/// It invokes the given <paramref name="onUpdate"/> method before starting to observe to this dictionary
		/// </remarks>
		void InvokeObserve(TKey key, Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate);
		
		/// <summary>
		/// Stops observing this dictionary with the given <paramref name="onUpdate"/> of any data changes
		/// </summary>
		void StopObserving(Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate);
		
		/// <summary>
		/// Stops observing this dictionary updates for the given <paramref name="key"/>
		/// </summary>
		void StopObserving(TKey key);
		
		/// <summary>
		/// Stops observing this dictionary changes from all the given <paramref name="subscriber"/> calls.
		/// If the given <paramref name="subscriber"/> is null then will stop observing from everything.
		/// </summary>
		void StopObservingAll(object subscriber = null);
	}

	/// <inheritdoc />
	public interface IObservableDictionary<TKey, TValue> : IObservableDictionaryReader<TKey, TValue>
		where TValue : struct
	{
		/// <summary>
		/// Changes the given <paramref name="key"/> in the dictionary.
		/// It will notify any observer listing to its data
		/// </summary>
		new TValue this[TKey key] { get; set; }

		/// <inheritdoc cref="Dictionary{TKey,TValue}.Add" />
		void Add(TKey key, TValue value);

		/// <inheritdoc cref="Dictionary{TKey,TValue}.Remove" />
		bool Remove(TKey key);
		
		/// <remarks>
		/// It invokes any update method that is observing to the given <paramref name="key"/> on this dictionary
		/// </remarks>
		void InvokeUpdate(TKey key);
	}

	/// <inheritdoc />
	public class ObservableDictionary<TKey, TValue> : IObservableDictionary<TKey, TValue>
		where TValue : struct
	{
		private readonly IDictionary<TKey, IList<Action<TKey, TValue, TValue, ObservableUpdateType>>> _keyUpdateActions = 
			new Dictionary<TKey, IList<Action<TKey, TValue, TValue, ObservableUpdateType>>>();
		private readonly IList<Action<TKey, TValue, TValue, ObservableUpdateType>> _updateActions = 
			new List<Action<TKey, TValue, TValue, ObservableUpdateType>>();

		/// <inheritdoc />
		public int Count => Dictionary.Count;
		/// <inheritdoc />
		public IReadOnlyDictionary<TKey, TValue> ReadOnlyDictionary => new ReadOnlyDictionary<TKey, TValue>(Dictionary);

		protected virtual IDictionary<TKey, TValue> Dictionary { get; }
		
		protected ObservableDictionary() {}

		public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
		{
			Dictionary = dictionary;
		}

		/// <inheritdoc cref="Dictionary{TKey,TValue}.this" />
		public TValue this[TKey key]
		{
			get => Dictionary[key];
			set
			{
				var previousValue = Dictionary[key];
				
				Dictionary[key] = value;
				
				InvokeUpdate(key, previousValue);
			}
		}

		/// <inheritdoc />
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return Dictionary.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <inheritdoc />
		public bool TryGetValue(TKey key, out TValue value)
		{
			return Dictionary.TryGetValue(key, out value);
		}

		/// <inheritdoc />
		public bool ContainsKey(TKey key)
		{
			return Dictionary.ContainsKey(key);
		}

		/// <inheritdoc />
		public void Add(TKey key, TValue value)
		{
			Dictionary.Add(key, value);
			
			if (_keyUpdateActions.TryGetValue(key, out var actions))
			{
				for (var i = 0; i < actions.Count; i++)
				{
					actions[i](key, default, value, ObservableUpdateType.Added);
				}
			}

			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](key, default, value, ObservableUpdateType.Added);
			}
		}

		/// <inheritdoc />
		public bool Remove(TKey key)
		{
			if (!Dictionary.TryGetValue(key, out var value))
			{
				return false;
			}
			
			Dictionary.Remove(key);
			
			if (_keyUpdateActions.TryGetValue(key, out var actions))
			{
				for (var i = 0; i < actions.Count; i++)
				{
					actions[i](key, value, default, ObservableUpdateType.Removed);
				}
			}

			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](key, value, default, ObservableUpdateType.Removed);
			}

			return true;
		}

		/// <inheritdoc />
		public void InvokeUpdate(TKey key)
		{
			InvokeUpdate(key, Dictionary[key]);
		}

		/// <inheritdoc />
		public void StopObserving(TKey key)
		{
			_keyUpdateActions.Remove(key);
		}

		/// <inheritdoc />
		public void Observe(Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void Observe(TKey key, Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			var list = new List<Action<TKey, TValue, TValue, ObservableUpdateType>> { onUpdate };

			_keyUpdateActions.Add(key, list);
		}

		/// <inheritdoc />
		public void InvokeObserve(TKey key, Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			Observe(key, onUpdate);
			InvokeUpdate(key);
		}

		/// <inheritdoc />
		public void StopObserving(Action<TKey, TValue, TValue, ObservableUpdateType> onUpdate)
		{
			foreach (var actions in _keyUpdateActions)
			{
				for (var i = actions.Value.Count - 1; i > -1; i--)
				{
					if (actions.Value[i] == onUpdate)
					{
						actions.Value.RemoveAt(i);
					}
				}
			}

			for (var i = _updateActions.Count - 1; i > -1; i--)
			{
				if (_updateActions[i] == onUpdate)
				{
					_updateActions.RemoveAt(i);
				}
			}
		}

		/// <inheritdoc />
		public void StopObservingAll(object subscriber = null)
		{
			if (subscriber == null)
			{
				_keyUpdateActions.Clear();
				_updateActions.Clear();
				return;
			}
			
			foreach (var actions in _keyUpdateActions)
			{
				for (var i = actions.Value.Count - 1; i > -1; i--)
				{
					if (actions.Value[i].Target == subscriber)
					{
						actions.Value.RemoveAt(i);
					}
				}
			}

			for (var i = _updateActions.Count - 1; i > -1; i--)
			{
				if (_updateActions[i].Target == subscriber)
				{
					_updateActions.RemoveAt(i);
				}
			}
		}
		
		protected void InvokeUpdate(TKey key, TValue previousValue)
		{
			var value = Dictionary[key];
			
			if (_keyUpdateActions.TryGetValue(key, out var actions))
			{
				for (var i = 0; i < actions.Count; i++)
				{
					actions[i](key, previousValue, value, ObservableUpdateType.Updated);
				}
			}

			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i](key, previousValue, value, ObservableUpdateType.Updated);
			}
		}
	}

	/// <inheritdoc />
	public class ObservableResolverDictionary<TKey, TValue> : ObservableDictionary<TKey, TValue>
		where TValue : struct
	{
		private readonly Func<IDictionary<TKey, TValue>> _dictionaryResolver;

		protected override IDictionary<TKey, TValue> Dictionary => _dictionaryResolver();

		public ObservableResolverDictionary(Func<IDictionary<TKey, TValue>> dictionaryResolver)
		{
			_dictionaryResolver = dictionaryResolver;
		}
	}
}