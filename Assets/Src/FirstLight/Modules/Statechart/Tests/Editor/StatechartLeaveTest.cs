using System;
using FirstLight.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace FirstLightEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartLeaveTest
	{
		/// <summary>
		/// Mocking interface to check method calls received
		/// </summary>
		public interface IMockCaller
		{
			void InitialOnExitCall(int id);
			void FinalOnEnterCall(int id);
			void StateOnEnterCall(int id);
			void StateOnExitCall(int id);
			void OnTransitionCall(int id);
		}
		
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");
		private readonly IStatechartEvent _event2 = new StatechartEvent("Event2");

		private IMockCaller _caller;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		public void NestStateBasicSetup()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnEnterCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				var state = factory.State("State");

				SetupNest(factory, nestFactory => SetupLeave(nestFactory, state), false);

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}
		}

		[Test]
		public void NestStateBasicSetup_ExecuteFinal_SameResult()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnEnterCall(2);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				var state = factory.State("State");

				SetupNest(factory, nestFactory => SetupLeave(nestFactory, state), true);

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}
		}

		[Test]
		public void SplitStateBasicSetup()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnEnterCall(2);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.Received(1).FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				var state = factory.State("State");

				SetupSplit(factory, true, SetupSimple, nestFactory => SetupLeave(nestFactory, state));

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}
		}

		[Test]
		public void SplitStateBasicSetup_ExecuteFinal_SameResult()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnEnterCall(2);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.Received(1).FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				var state = factory.State("State");

				SetupSplit(factory, true, SetupSimple, nestFactory => SetupLeave(nestFactory, state));

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}
		}

		[Test]
		public void NoSplitFinalized_LeaveOneSplit_LeaveAll()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnEnterCall(2);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				var state = factory.State("State");

				SetupSplit(factory, false, SetupSimpleEventState, 
				           nestFactory => SetupLeave(nestFactory, state));

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}
		}

		[Test]
		public void NoSplitFinalized_LeaveOneSplitExecuteFinal_ExecuteOtherSplitFinal()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnEnterCall(2);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.Received(1).FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				var state = factory.State("State");

				SetupSplit(factory,true, SetupSimpleEventState, nestFactory => SetupLeave(nestFactory, state));

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}
		}

		[Test]
		public void BasicSetup_NestStateTransitionWithoutTarget_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				SetupNest(factory, SetupLeave_WithoutTarget, false);
			}));
		}

		[Test]
		public void BasicSetup_SplitStateTransitionWithoutTarget_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				SetupSplit(factory, false, SetupSimple, SetupLeave_WithoutTarget);
			}));
		}

		[Test]
		public void SameLayerTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var leave = factory.Leave("Leave");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(leave);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				leave.OnEnter(() => _caller.StateOnEnterCall(1));
				leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(initial);
			}));
		}

		[Test]
		public void WrongLayerTarget_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var state = factory.State("State");

				SetupNest(factory, 
				          nestFactory => SetupNest(nestFactory, 
				                                   doubleNestFactory => SetupLeave(doubleNestFactory, state), false), false);

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}));
		}

		[Test]
		public void MultipleTransitions_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var state = factory.State("State");

				SetupNest(factory, nestFactory =>
				{
					var initial = factory.Initial("Initial");
					var leave = factory.Leave("Leave");

					initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(leave);
					initial.OnExit(() => _caller.InitialOnExitCall(0));

					leave.OnEnter(() => _caller.StateOnEnterCall(1));
					leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(state);
					leave.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(state);
				}, false);

				state.OnEnter(() => _caller.StateOnEnterCall(2));
				state.OnExit(() => _caller.StateOnExitCall(2));
			}));
		}

		#region Setups
		
		private void SetupSimple(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(final);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupSimpleEventState(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var state = factory.State("State");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(state);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			state.OnEnter(() => _caller.StateOnEnterCall(0));
			state.Event(_event1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			state.OnExit(() => _caller.StateOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupLeave(IStateFactory factory, IState leaveTarget)
		{
			var initial = factory.Initial("Initial");
			var leave = factory.Leave("Leave");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(leave);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			leave.OnEnter(() => _caller.StateOnEnterCall(0));
			leave.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(leaveTarget);

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupLeave_WithoutTarget(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var leave = factory.Leave("Leave");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(leave);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			leave.OnEnter(() => _caller.StateOnEnterCall(0));
			leave.Transition().OnTransition(() => _caller.OnTransitionCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupNest(IStateFactory factory, Action<IStateFactory> nestSetup, bool executeFinal)
		{
			var initial = factory.Initial("Initial");
			var nest = factory.Nest("Nest");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(nest);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(nestSetup, true, executeFinal).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			nest.Event(_event2).OnTransition(() => _caller.OnTransitionCall(4)).Target(final);
			nest.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		private void SetupSplit(IStateFactory factory, bool executeFinal, params Action<IStateFactory>[] setups)
		{
			var initial = factory.Initial("Initial");
			var split = factory.Split("Split");
			var final = factory.Final("final");
			var data = new ISplitState.StateData[setups.Length];

			for (var i = 0; i < setups.Length; i++)
			{
				data[i] = new ISplitState.StateData(setups[i], true, executeFinal);
			}

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(split);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			split.OnEnter(() => _caller.StateOnEnterCall(1));
			split.Split(data).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			split.Event(_event2).OnTransition(() => _caller.OnTransitionCall(4)).Target(final);
			split.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		#endregion
	}
}