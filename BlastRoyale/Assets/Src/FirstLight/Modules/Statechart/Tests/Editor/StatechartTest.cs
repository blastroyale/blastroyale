using System;
using FirstLight.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace FirstLightEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartTest
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
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void BasicSetup_TransitionWithoutTarget_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0));
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		[Test]
		public void InitialState_StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(initial);
			}));
		}

		[Test]
		public void InitialState_MultipleTransitions_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final = factory.Final("Final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(final);
				initial.Transition().OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		[Test]
		public void NoInitialState_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(factory =>
			{
				var final = factory.Final("final");

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		[Test]
		public void MultipleInitialStates_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial1 = factory.Initial("Initial1");
				var initial2 = factory.Initial("Initial2");

				initial1.OnExit(() => _caller.InitialOnExitCall(1));
				initial2.OnExit(() => _caller.InitialOnExitCall(2));
			}));
		}

		[Test]
		public void MultipleFinalState_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var final1 = factory.Final("final1");
				var final2 = factory.Final("final2");

				initial.OnExit(() => _caller.InitialOnExitCall(0));

				final1.OnEnter(() => _caller.FinalOnEnterCall(1));
				final2.OnEnter(() => _caller.FinalOnEnterCall(2));
			}));
		}
		
		private void SetupSimple(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(final);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}
	}
}