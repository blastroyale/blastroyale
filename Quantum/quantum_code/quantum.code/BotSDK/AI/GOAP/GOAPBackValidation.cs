using System.Collections.Generic;
using Photon.Deterministic;


namespace Quantum
{
	public abstract partial class GOAPBackValidation
	{
		public abstract void ValidatePlanState(Frame frame, EntityRef entity, GOAPState stateToValidate, GOAPState nextState, FP costToNextState, List<StateBackValidation> validatedStates);
	}

	public abstract class GOAPSingleBackValidation : GOAPBackValidation
	{
		public override sealed void ValidatePlanState(Frame frame, EntityRef entity, GOAPState stateToValidate, GOAPState nextState, FP costToNextState,
			List<StateBackValidation> validatedStates)
		{
			if (ValidatePlanState(frame, entity, ref stateToValidate, nextState, ref costToNextState) == true)
			{
				validatedStates.Add(new StateBackValidation(stateToValidate, costToNextState));
			}
		}

		protected virtual bool ValidatePlanState(Frame frame, EntityRef entity, ref GOAPState stateToValidate, GOAPState nextState, ref FP costToNextState)
		{
			return false;
		}
	}
}