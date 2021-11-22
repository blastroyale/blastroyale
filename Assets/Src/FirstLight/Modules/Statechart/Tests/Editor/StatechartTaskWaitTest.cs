using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using FirstLight.Statechart;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.TestTools;

// ReSharper disable CheckNamespace

namespace FirstLightEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartTaskWaitTest
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
		private bool _blocker;
		private bool _done;
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
			_blocker = true;
			_done = false;
		}

		[UnityTest]
		public IEnumerator BasicSetup()
		{
			var statechart = new Statechart(factory => SetupTaskWaitState(factory, TaskWaitAction));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			_blocker = false;
			
			yield return YieldCoroutine();
			
			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[UnityTest]
		public IEnumerator EventTrigger()
		{
			var statechart = new Statechart(factory => SetupTaskWaitState(factory, TaskWaitAction));

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(0);

			statechart.Trigger(_event1);
			
			_blocker = false;
			
			yield return YieldCoroutine();
				
			_caller.Received().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[UnityTest]
		public IEnumerator NestState_OuterEventTriggerAndTaskComplete_NoTransition()
		{
			var statechart = new Statechart(InternalSetupNest);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.Received().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.DidNotReceive().OnTransitionCall(5);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().InitialOnExitCall(1);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnExitCall(0);
			_caller.DidNotReceive().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.DidNotReceive().FinalOnEnterCall(1);

			statechart.Trigger(_event2);

			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().OnTransitionCall(5);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received().FinalOnEnterCall(1);
			
			_blocker = false;
			
			yield return YieldCoroutine();
				
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received(1).OnTransitionCall(5);
			_caller.Received(1).StateOnExitCall(0);
			_caller.Received(1).StateOnExitCall(1);
			_caller.DidNotReceive().FinalOnEnterCall(0);
			_caller.Received(1).FinalOnEnterCall(1);

			void InternalSetupNest(IStateFactory factory)
			{
				SetupNest(factory, _event2, InnerSetupTaskWaitState, true, false);
			}

			void InnerSetupTaskWaitState(IStateFactory factory)
			{
				SetupTaskWaitState(factory, TaskWaitAction);
			}
		}

		[Test]
		public void MissingTaskWaiter_ThrowsException()
		{
			Assert.Throws<MissingMethodException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var waiting = factory.TaskWait("Waiting");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(waiting);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				waiting.OnEnter(() => _caller.StateOnEnterCall(1));
				waiting.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		[Test]
		public void StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var waiting = factory.TaskWait("Waiting");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(waiting);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				waiting.OnEnter(() => _caller.StateOnEnterCall(1));
				waiting.WaitingFor(TaskWaitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(waiting);
				waiting.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		private async Task TaskWaitAction()
		{
			while (_blocker)
			{
				await Task.Yield();
			}

			_done = true;
		}
		
		private IEnumerator YieldCoroutine()
		{
			while (!_done)
			{
				yield return null;
			}
			
			yield return null;
		}

		private void SetupTaskWaitState(IStateFactory factory, Func<Task> waitAction)
		{
			var initial = factory.Initial("Initial");
			var waiting = factory.TaskWait("Task Wait");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(waiting);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			waiting.OnEnter(() => _caller.StateOnEnterCall(0));
			waiting.WaitingFor(waitAction).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			waiting.Event(_event1).OnTransition(() => _caller.OnTransitionCall(2));
			waiting.OnExit(() => _caller.StateOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupNest(IStateFactory factory, IStatechartEvent eventTrigger, Action<IStateFactory> nestSetup,
		                       bool executeExit, bool executeFinal)
		{
			var initial = factory.Initial("Initial");
			var nest = factory.Nest("Nest");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(3)).Target(nest);
			initial.OnExit(() => _caller.InitialOnExitCall(1));

			nest.OnEnter(() => _caller.StateOnEnterCall(1));
			nest.Nest(nestSetup, executeExit, executeFinal).OnTransition(() => _caller.OnTransitionCall(4)).Target(final);
			nest.Event(eventTrigger).OnTransition(() => _caller.OnTransitionCall(5)).Target(final);
			nest.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(1));
		}
	}
}