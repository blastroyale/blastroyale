using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace

namespace FirstLight
{
	/// <summary>
	/// A field with the possibility to observe changes to it's elements defined <see cref="ObservableUpdateType"/> rules
	/// </summary>
	public interface IObservableFieldReader<out T>
	{
		/// <summary>
		/// The field value
		/// </summary>
		T Value { get; }
		
		/// <summary>
		/// Observes this field with the given <paramref name="onUpdate"/> when any data changes
		/// </summary>
		void Observe(Action<T, T> onUpdate);
		
		/// <inheritdoc cref="Observe" />
		/// <remarks>
		/// It invokes the given <paramref name="onUpdate"/> method before starting to observe to this field
		/// </remarks>
		void InvokeObserve(Action<T, T> onUpdate);
		
		/// <summary>
		/// Stops observing this field with the given <paramref name="onUpdate"/> of any data changes
		/// </summary>
		void StopObserving(Action<T, T> onUpdate);
		
		/// <summary>
		/// Stops observing this field from all the given <paramref name="subscriber"/> calls.
		/// If the given <paramref name="subscriber"/> is null then will stop observing from everything.
		/// </summary>
		void StopObservingAll(object subscriber = null);
		
		/// <remarks>
		/// It invokes any update method that is observing to this field
		/// </remarks>
		void InvokeUpdate();
	}

	/// <inheritdoc />
	public interface IObservableField<T> : IObservableFieldReader<T>
	{
		/// <summary>
		/// The field value with possibility to be changed
		/// </summary>
		new T Value { get; set; }
	}
	
	/// <inheritdoc />
	public class ObservableField<T> : IObservableField<T>
	{
		private readonly IList<Action<T, T>> _updateActions = new List<Action<T, T>>();

		private T _value;

		/// <inheritdoc cref="IObservableField{T}.Value" />
		public virtual T Value
		{
			get => _value;
			set
			{
				var previousValue = _value;
				
				_value = value;
				InvokeUpdate(previousValue);
			}
		}

		public ObservableField()
		{
			_value = default;
		}
 
		public ObservableField(T initialValue)
		{
			_value = initialValue;
		}
		
		public static implicit operator T(ObservableField<T> value) => value.Value;

		/// <inheritdoc />
		public void Observe(Action<T, T> onUpdate)
		{
			_updateActions.Add(onUpdate);
		}

		/// <inheritdoc />
		public void InvokeObserve(Action<T, T> onUpdate)
		{
			onUpdate(Value, Value);
			
			Observe(onUpdate);
		}

		/// <inheritdoc />
		public void StopObserving(Action<T, T> onUpdate)
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

			for (var i = _updateActions.Count - 1 ; i > -1; i--)
			{
				if (_updateActions[i].Target == subscriber)
				{
					_updateActions.RemoveAt(i);
				}
			}
		}

		/// <inheritdoc />
		public void InvokeUpdate()
		{
			InvokeUpdate(Value);
		}

		protected void InvokeUpdate(T previousValue)
		{
			for (var i = 0; i < _updateActions.Count; i++)
			{
				_updateActions[i].Invoke(previousValue, Value);
			}
		}
	}
	
	/// <inheritdoc />
	public class ObservableResolverField<T> : ObservableField<T>
	{
		private readonly Func<T> _fieldResolver;
		private readonly Action<T> _fieldSetter;

		/// <inheritdoc cref="IObservableField{T}.Value" />
		public override T Value
		{
			get => _fieldResolver();
			set
			{
				var previousValue = _fieldResolver();
				
				_fieldSetter(value);
				
				InvokeUpdate(previousValue);
			}
		}

		private ObservableResolverField() {}
 
		public ObservableResolverField(Func<T> fieldResolver, Action<T> fieldSetter)
		{
			_fieldResolver = fieldResolver;
			_fieldSetter = fieldSetter;
		}
		
		public static implicit operator T(ObservableResolverField<T> value) => value.Value;
	}
}