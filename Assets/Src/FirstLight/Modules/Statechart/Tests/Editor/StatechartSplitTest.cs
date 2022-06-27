using System;
using FirstLight.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace FirstLightEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartSplitTest
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

		private IMockCaller _caller;
		private readonly IStatechartEvent _event1 = new StatechartEvent("Event1");
		private readonly IStatechartEvent _event2 = new StatechartEvent("Event2");
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		public void BasicSetup()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimple, true, false),
				           new ISplitState.StateData(SetupSimple, true, false)
				           );
			}
		}

		[Test]
		public void BasicSetup_WithoutTarget()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(1).OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit_WithoutTarget(factory, _event2, SetupSimple, SetupSimple);
			}
		}

		[Test]
		public void InnerEventTrigger()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, true, false),
				           new ISplitState.StateData(SetupSimpleEventState, true, false)
				          );
			}
		}

		[Test]
		public void InnerEventTrigger_ExecuteFinal_SameResult()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, true, true),
				           new ISplitState.StateData(SetupSimpleEventState, true, true)
				          );
			}
		}

		[Test]
		public void InnerEventTrigger_NotExecuteExit_SameResult()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(2).OnTransitionCall(0);
			_caller.Received(2).OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.Received(1).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, false, false),
				           new ISplitState.StateData(SetupSimpleEventState, false, false)
				           );
			}
		}

		[Test]
		public void OuterEventTrigger()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, true, false),
				           new ISplitState.StateData(SetupSimpleEventState, true, false)
				           );
			}
		}

		[Test]
		public void OuterEventTrigger_ExecuteFinal()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.Received(2).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, true, true),
				           new ISplitState.StateData(SetupSimpleEventState, true, true)
				           );
			}
		}

		[Test]
		public void OuterEventTrigger_NotExecuteExit()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, false, false),
				           new ISplitState.StateData(SetupSimpleEventState, false, false)
				          );
			}
		}

		[Test]
		public void OuterEventTrigger_NotExecuteExit_ExecuteFinal()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(1).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).InitialOnExitCall(1);
			_caller.Received(2).StateOnEnterCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.Received(2).FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, false, true),
				           new ISplitState.StateData(SetupSimpleEventState, false, true)
				          );
			}
		}

		[Test]
		public void NestedStates_InnerEventTrigger()
		{
			var statechart = new Statechart(SetupLayer0);

			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(4).OnTransitionCall(0);
			_caller.Received(4).OnTransitionCall(1);
			_caller.Received(3).OnTransitionCall(2);
			_caller.Received(3).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(4).InitialOnExitCall(0);
			_caller.Received(3).InitialOnExitCall(1);
			_caller.Received(4).StateOnEnterCall(0);
			_caller.Received(3).StateOnEnterCall(1);
			_caller.Received(4).StateOnExitCall(0);
			_caller.Received(3).StateOnExitCall(1);
			_caller.Received(4).FinalOnEnterCall(0);
			_caller.Received(3).FinalOnEnterCall(1);

			void SetupLayer0(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupLayer1, true, false),
				           new ISplitState.StateData(SetupLayer1, true, false)
				          );
			}
			
			void SetupLayer1(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, true, false),
				           new ISplitState.StateData(SetupSimpleEventState, true, false)
				          );
			}
		}

		[Test]
		public void NestedStates_OuterEventTrigger()
		{
			var statechart = new Statechart(SetupLayer0);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(4).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(3).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(4).InitialOnExitCall(0);
			_caller.Received(3).InitialOnExitCall(1);
			_caller.Received(4).StateOnEnterCall(0);
			_caller.Received(3).StateOnEnterCall(1);
			_caller.Received(4).StateOnExitCall(0);
			_caller.Received(3).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void SetupLayer0(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupLayer1, true, false),
				           new ISplitState.StateData(SetupLayer1, true, false)
				          );
			}

			void SetupLayer1(IStateFactory factory)
			{
				SetupSplit(factory, _event1, 
				           new ISplitState.StateData(SetupSimpleEventState, true, false),
				           new ISplitState.StateData(SetupSimpleEventState, true, false)
				          );
			}
		}

		[Test]
		public void NestedStates_OuterEventTriggerSameTrigger_ExecutesMostOuter()
		{
			var statechart = new Statechart(SetupLayer0);

			statechart.Run();
			statechart.Trigger(_event2);

			_caller.Received(4).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(3).OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received(1).OnTransitionCall(4);
			_caller.Received(4).InitialOnExitCall(0);
			_caller.Received(3).InitialOnExitCall(1);
			_caller.Received(4).StateOnEnterCall(0);
			_caller.Received(3).StateOnEnterCall(1);
			_caller.Received(4).StateOnExitCall(0);
			_caller.Received(3).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void SetupLayer0(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupLayer1, true, false),
				           new ISplitState.StateData(SetupLayer1, true, false)
				          );
			}

			void SetupLayer1(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, true, false),
				           new ISplitState.StateData(SetupSimpleEventState, true, false)
				          );
			}
		}

		[Test]
		public void StatechartSplit_RunResetRun()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();
			statechart.Trigger(_event1);
			statechart.Reset();
			statechart.Run();
			statechart.Trigger(_event1);

			_caller.Received(4).OnTransitionCall(0);
			_caller.Received(4).OnTransitionCall(1);
			_caller.Received(2).OnTransitionCall(2);
			_caller.Received(2).OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(4).InitialOnExitCall(0);
			_caller.Received(2).InitialOnExitCall(1);
			_caller.Received(4).StateOnEnterCall(0);
			_caller.Received(2).StateOnEnterCall(1);
			_caller.Received(4).StateOnExitCall(0);
			_caller.Received(2).StateOnExitCall(1);
			_caller.Received(4).FinalOnEnterCall(0);
			_caller.Received(2).FinalOnEnterCall(1);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, true, false),
				           new ISplitState.StateData(SetupSimpleEventState, true, false)
				          );
			}
		}

		[Test]
		public void StatechartSplit_HalfFinal_NotFinalized()
		{
			var statechart = new Statechart(Setup);

			statechart.Run();

			_caller.Received(2).OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received(2).InitialOnExitCall(0);
			_caller.Received(1).StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.Received(1).FinalOnEnterCall(0);

			void Setup(IStateFactory factory)
			{
				SetupSplit(factory, _event2, 
				           new ISplitState.StateData(SetupSimpleEventState, true, false),
				           new ISplitState.StateData(SetupSimple, true, false)
				          );
			}
		}

		[Test]
		public void SplitState_StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var split = factory.Split("Nest");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(split);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				split.OnEnter(() => _caller.StateOnEnterCall(1));
				split.Split(SetupSimpleEventState, SetupSimpleEventState).OnTransition(() => _caller.OnTransitionCall(4)).Target(split);
				split.Event(_event2).OnTransition(() => _caller.OnTransitionCall(5)).Target(split);
				split.OnExit(() => _caller.StateOnExitCall(1));
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

		private void SetupSplit(IStateFactory factory, IStatechartEvent eventTrigger, params ISplitState.StateData[] data)
		{
			var initial = factory.Initial("Initial");
			var split = factory.Split("Split");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(split);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			split.OnEnter(() => _caller.StateOnEnterCall(1));
			split.Split(data).OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			split.Event(eventTrigger).OnTransition(() => _caller.OnTransitionCall(4)).Target(final);
			split.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		private void SetupSplit_WithoutTarget(IStateFactory factory, IStatechartEvent eventTrigger, params Action<IStateFactory>[] setups)
		{
			var initial = factory.Initial("Initial");
			var split = factory.Split("Split");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(2)).Target(split);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			split.OnEnter(() => _caller.StateOnEnterCall(1));
			split.Split(setups).OnTransition(() => _caller.OnTransitionCall(3));
			split.Event(eventTrigger).OnTransition(() => _caller.OnTransitionCall(4));
			split.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}

		#endregion
	}
}