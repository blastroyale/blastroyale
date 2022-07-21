// ReSharper disable CheckNamespace

using System;

namespace FirstLight.Statechart.Internal
{
	/// <inheritdoc cref="INestState"/>
	internal class NestState : SplitState, INestState
	{
		public NestState(string name, IStateFactoryInternal factory) : base(name, factory)
		{
		}

		/// <inheritdoc />
		public override void Validate()
		{
#if UNITY_EDITOR || DEBUG
			if (_innerStates.Count != 1)
			{
				throw new MissingMemberException($"This state {Name} doesn't have any nested setup defined.");
			}
#endif
			OnValidate();
		}

		/// <inheritdoc />
		public ITransition Nest(Action<IStateFactory> data)
		{
			return Split(data);
		}

		/// <inheritdoc />
		public ITransition Nest(NestedStateData data)
		{
			return Split(data);
		}
	}
}