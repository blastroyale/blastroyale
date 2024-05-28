using System;
using FirstLight.Statechart;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace FirstLightEditor.StateChart.Tests
{
	[TestFixture]
	public class StatechartChoiceTest
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
		private bool _condition1;
		private bool _condition2;
		
		[SetUp]
		public void Init()
		{
			_condition1 = _condition2 = false;
			_caller = Substitute.For<IMockCaller>();
		}

		[Test]
		public void BasicSetup()
		{
			var statechart = new Statechart(SetupChoiceState);

			_condition1 = false;
			_condition2 = true;

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.DidNotReceive().OnTransitionCall(1);
			_caller.Received().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.Received().StateOnExitCall(1);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void BasicSetup_WithoutTarget_ThrowsException()
		{
			Assert.Throws<MissingMemberException>(() => new Statechart(SetupChoiceState_WithoutTarget));
		}

		[Test]
		public void MultipleTrueConditions_PicksFirstTransition()
		{
			var statechart = new Statechart(SetupChoiceState);

			_condition1 = true;
			_condition2 = true;

			statechart.Run();

			_caller.Received().OnTransitionCall(0);
			_caller.Received().OnTransitionCall(1);
			_caller.DidNotReceive().OnTransitionCall(2);
			_caller.DidNotReceive().OnTransitionCall(3);
			_caller.DidNotReceive().OnTransitionCall(4);
			_caller.Received().InitialOnExitCall(0);
			_caller.Received().StateOnEnterCall(1);
			_caller.DidNotReceive().StateOnEnterCall(2);
			_caller.Received().StateOnExitCall(1);
			_caller.DidNotReceive().StateOnExitCall(2);
			_caller.Received().FinalOnEnterCall(0);
		}

		[Test]
		public void StateTransitionsLoop_ThrowsException()
		{
			Assert.Throws<InvalidOperationException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var choice = factory.Choice("Choice");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(choice);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				choice.OnEnter(() => _caller.StateOnEnterCall(1));
				choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1)).Target(choice);
				choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2)).Target(choice);
				choice.Transition().OnTransition(() => _caller.OnTransitionCall(3)).Target(choice);
				choice.OnExit(() => _caller.StateOnExitCall(1));
			}));
		}

		[Test]
		public void MissingTransition_ThrowsException()
		{
			Assert.Throws<MissingMethodException>(() => new Statechart(factory =>
			{
				var initial = factory.Initial("Initial");
				var choice = factory.Choice("Choice");
				var final = factory.Final("final");

				initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(choice);
				initial.OnExit(() => _caller.InitialOnExitCall(0));

				choice.OnEnter(() => _caller.StateOnEnterCall(1));
				choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
				choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
				choice.OnExit(() => _caller.StateOnExitCall(1));

				final.OnEnter(() => _caller.FinalOnEnterCall(0));
			}));
		}

		private void SetupChoiceState(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var choice = factory.Choice("Choice");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(choice);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			choice.OnEnter(() => _caller.StateOnEnterCall(1));
			choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1)).Target(final);
			choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2)).Target(final);
			choice.Transition().OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			choice.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}

		private void SetupChoiceState_WithoutTarget(IStateFactory factory)
		{
			var initial = factory.Initial("Initial");
			var choice = factory.Choice("Choice");
			var final = factory.Final("final");

			initial.Transition().OnTransition(() => _caller.OnTransitionCall(0)).Target(choice);
			initial.OnExit(() => _caller.InitialOnExitCall(0));

			choice.OnEnter(() => _caller.StateOnEnterCall(1));
			choice.Transition().Condition(() => _condition1).OnTransition(() => _caller.OnTransitionCall(1));
			choice.Transition().Condition(() => _condition2).OnTransition(() => _caller.OnTransitionCall(2));
			choice.Transition().OnTransition(() => _caller.OnTransitionCall(3)).Target(final);
			choice.OnExit(() => _caller.StateOnExitCall(1));

			final.OnEnter(() => _caller.FinalOnEnterCall(0));
		}
	}
}