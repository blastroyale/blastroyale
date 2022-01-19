using Photon.Deterministic;

namespace Quantum
{
	public abstract unsafe partial class GOAPGoal
	{
		public enum EInterruptionBehavior
		{
			Never,
			Always,
			BasedOnActions,
		}

		// PUBLIC MEMBERS

		public string                Label;

		[BotSDKHidden]
		public GOAPState             StartState;
		[BotSDKHidden]
		public GOAPState             TargetState;
		public EInterruptionBehavior InterruptionBehavior;

		// PUBLIC INTERFACE

		public virtual FP GetRelevancy(Frame frame, GOAPEntityContext context)
		{
			return 1;
		}

		public virtual void InitPlanning(Frame frame, GOAPEntityContext context, ref GOAPState startState, ref GOAPState targetState)
		{
			startState.Merge(StartState);
			targetState.Merge(TargetState);
		}

		public virtual void Activate(Frame frame, GOAPEntityContext context)
		{
		}

		public virtual void Deactivate(Frame frame, GOAPEntityContext context)
		{
		}

		public virtual bool HasFinished(Frame frame, GOAPEntityContext context)
		{
			return context.Agent->CurrentState.Contains(context.Agent->GoalState);
		}

		public bool IsInterruptible(GOAPAction currentAction)
		{
			if (InterruptionBehavior == EInterruptionBehavior.Never)
				return false;

			if (InterruptionBehavior == EInterruptionBehavior.Always)
				return true;

			return currentAction != null && currentAction.Interruptible;
		}

		public virtual FP GetDisableTime(Frame frame, GOAPEntityContext context)
		{
			return 0;
		}
	}
}