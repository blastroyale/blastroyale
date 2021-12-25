using System;

namespace Quantum
{
	[Serializable]
	public partial struct GOAPState
	{
		// PUBLIC METHODS

		public static GOAPState Merge(GOAPState state, GOAPState mergeState)
		{
			var newState = state;
			newState.Merge(mergeState);
			return newState;
		}

		public static GOAPState Remove(GOAPState state, GOAPState removeState)
		{
			var newState = state;
			newState.Remove(removeState);
			return newState;
		}

		public bool HasPositiveFlag(EWorldState state)
		{
			return (Positive & state) == state;
		}

		public bool HasNegativeFlag(EWorldState state)
		{
			return (Negative & state) == state;
		}

		public void SetFlag(EWorldState flagState, bool value)
		{
			if (value == true)
			{
				Positive = Positive | flagState;
				Negative = Negative & ~Positive;
			}
			else
			{
				Negative = Negative | flagState;
				Positive = Positive & ~Negative;
			}
		}

		public void ClearFlag(EWorldState clearState)
		{
			Positive = Positive & ~clearState;
			Negative = Negative & ~clearState;
		}

		public void Merge(GOAPState mergeState)
		{
			Positive = Positive | mergeState.Positive;
			Negative = Negative & ~Positive;

			Negative = Negative | mergeState.Negative;
			Positive = Positive & ~Negative;

			MergeUserData(mergeState);
		}

		public void Remove(GOAPState removeState)
		{
			Positive = Positive & ~removeState.Positive;
			Negative = Negative & ~removeState.Negative;

			RemoveUserData(removeState);
		}

		public bool Contains(GOAPState state)
		{
			bool result = (state.Positive & Positive) == state.Positive && (state.Negative & Negative) == state.Negative;

			if (result == false)
				return false;

			ContainsUserData(state, ref result);
			return result;
		}

		public bool ContainsAny(GOAPState state)
		{
			bool result = (state.Positive & Positive) != EWorldState.None || (state.Negative & Negative) != EWorldState.None;

			if (result == true)
				return true;

			ContainsAnyUserData(state, ref result);
			return result;
		}

		partial void MergeUserData(GOAPState mergeState);
		partial void RemoveUserData(GOAPState removeState);
		partial void ContainsUserData(GOAPState state, ref bool result);
		partial void ContainsAnyUserData(GOAPState state, ref bool result);
	}
}