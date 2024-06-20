using System;
using FirstLight.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace FirstLightEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartNestTest
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
		public void BasicSetup()
		{
			var nestedStateData = new NestedStateData(SetupSimple, true, false);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void BasicSetup_WithoutTarget()
		{
			var nestedStateData = new NestedStateData(SetupSimple, true, false);
			var statechart = new Statechart(factory => SetupNest_WithoutTarget(factory, _event2, nestedStateData));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);
		}

		[Test]
		public void InnerEventTrigger()
		{
			var nestedStateData = new NestedStateData(SetupSimpleEventState, true, false);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void InnerEventTrigger_ExecuteFinal_SameResult()
		{
			var nestedStateData = new NestedStateData(SetupSimpleEventState, true, false);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void InnerEventTrigger_NotExecuteExit_SameResult()
		{
			var nestedStateData = new NestedStateData(SetupSimpleEventState, false, false);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void OuterEventTrigger()
		{
			var nestedStateData = new NestedStateData(SetupSimpleEventState, true, false);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void OuterEventTrigger_ExecuteFinal()
		{
			var nestedStateData = new NestedStateData(SetupSimpleEventState, true, true);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void OuterEventTrigger_NotExecuteExit()
		{
			var nestedStateData = new NestedStateData(SetupSimpleEventState, false, false);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void OuterEventTrigger_NotExecuteExit_ExecuteFinal()
		{
			var nestedStateData = new NestedStateData(SetupSimpleEventState, false, true);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
		}

		[Test]
		public void NestedStates_InnerEventTrigger()
		{
			var statechart = new Statechart(SetupLayer0);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(1).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.Received(2).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(1).InitialOnExitCall(0);
			_caller.Received(2).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);
			_caller.Received(2).FinalOnEnterCall(1);

			void SetupLayer0(IStateFactory factory)
			{
				SetupNest(factory, _event2, new NestedStateData(SetupLayer1, true, false));
			}

			void SetupLayer1(IStateFactory factory)
			{
				SetupNest(factory, _event2, new NestedStateData(SetupSimpleEventState, true, false));
			}
		}

		[Test]
		public void NestedStates_OuterEventLayer0()
		{
			var statechart = new Statechart(SetupLayer0);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(1).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(1).InitialOnExitCall(0);
			_caller.Received(2).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void SetupLayer0(IStateFactory factory)
			{
				SetupNest(factory, _event2, new NestedStateData(SetupLayer1, true, false));
			}

			void SetupLayer1(IStateFactory factory)
			{
				SetupNest(factory, _event1, new NestedStateData(SetupSimpleEventState, true, false));
			}
		}

		[Test]
		public void InnerEventTrigger_RunResetRun()
		{
			var nestedStateData = new NestedStateData(SetupSimpleEventState, true, false);
			var statechart = new Statechart(factory => SetupNest(factory, _event2, nestedStateData));

			statechart.Run();
			statechart.Trigger(_event1);
			statechart.Reset();
			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.Received(2).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(2).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.Received(2).FinalOnEnterCall(1);
		}

		[Test]
		public void StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var nest = factory.Nest("Nest");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(nest);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				nest.OnEnter(() => _caller.StateOnEnterCall(1));
				nest.Nest(SetupSimpleEventState).OnTransition(() => _caller.OnTransitionCall(4)).Target(nest);
				nest.Event(_event2).OnTransition(() => _caller.OnTransitionCall(5)).Target(nest);
				nest.OnExit(() => _caller.StateOnExitCall(1));
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

		private void SetupNest(IStateFactory factory, IStatechartEvent eventTrigger, NestedStateData nestedStateData)
		{
			var initial = factory.Initial("Initial");
			var nest = factory.Nest("Nest");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(nest);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(nestedStateData).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			nest.Event(eventTrigger).OnTransition(() => _caller.OnTransitionCall(4)).Target(final);
			nest.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		private void SetupNest_WithoutTarget(IStateFactory factory, IStatechartEvent eventTrigger, NestedStateData nestedStateData)
		{
			var initial = factory.Initial("Initial");
			var nest = factory.Nest("Nest");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(nest);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(nestedStateData).OnTransition(() => _caller.OnTransitionCall(3));
			nest.Event(eventTrigger).OnTransition(() => _caller.OnTransitionCall(4));
			nest.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		#endregion
	}
}