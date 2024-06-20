using System;
using FirstLight.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace FirstLightEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartTransitionTest
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
		
		[SetUp]
		public void Init()
		{
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		public void BasicSetup()
		{
			var statechart = new Statechart(SetupSimple);

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(0);
			_caller.Received().StateOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void BasicSetup_TransitionWithoutTarget_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("final");
				var transition = factory.Transition("Transition");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0));
				initial.OnExit(() => _caller.InitialOnExitCall(0));
			
				transition.OnEnter(() => _caller.StateOnEnterCall(0));
				transition.Transition().OnTransition(() => _caller.OnTransitionCall(1));
				transition.OnExit(() => _caller.StateOnExitCall(0));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		[Test]
		public void BasicSetup_TransitionWithoutTransition_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("final");
				var transition = factory.Transition("Transition");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0));
				initial.OnExit(() => _caller.InitialOnExitCall(0));
			
				transition.OnEnter(() => _caller.StateOnEnterCall(0));
				transition.OnExit(() => _caller.StateOnExitCall(0));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		[Test]
		public void StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("final");
				var transition = factory.Transition("Transition");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(transition);
				initial.OnExit(() => _caller.InitialOnExitCall(0));
			
				transition.OnEnter(() => _caller.StateOnEnterCall(0));
				transition.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(transition);
				transition.OnExit(() => _caller.StateOnExitCall(0));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}
		
		private void SetupSimple(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var final = factory.Final("final");
			var transition = factory.Transition("Transition");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(transition);
			initial.OnExit(() => _caller.InitialOnExitCall(0));
			
			transition.OnEnter(() => _caller.StateOnEnterCall(0));
			transition.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			transition.OnExit(() => _caller.StateOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}
	}
}