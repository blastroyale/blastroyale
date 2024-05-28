using System;

// ReSharper disable CheckNamespace

namespace FirstLight.Statechart
{
	/// <summary>
	/// Events are unique inputs to make the State Chart move forward when defined in the setup.
	/// It requires to call <see cref="IStatechart.Trigger(IStatechartEvent)"/> to move the State Chart forward.
	/// </summary>
	public interface IStatechartEvent : IEquatable<IStatechartEvent>
	{
		/// <summary>
		/// The unique Id of the event.
		/// It is automatically generated on creation.
		/// </summary>
		uint Id { get; }
		
		/// <summary>
		/// The name defined for the event.
		/// Helpful for debugging purposes.
		/// </summary>
		string Name { get; }
	}

	/// <inheritdoc cref="IStatechartEvent"/>
	public class StatechartEvent : IStatechartEvent
	{
		private static uint _idRef;

		/// <inheritdoc />
		public uint Id { get; }
		/// <inheritdoc />
		public string Name { get; }

		private StatechartEvent() {}

		public StatechartEvent(string name)
		{
			Id = ++_idRef;
			Name = name;
		}

		public bool Equals(IStatechartEvent statechartEvent)
		{
			return statechartEvent != null && Id == statechartEvent.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is IStatechartEvent chartEvent && Equals(chartEvent);
		}

		public override int GetHashCode()
		{
			return (int) Id;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}