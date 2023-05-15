using System;
using System.Collections.Generic;
using Backend.Game;
using FirstLight.Game.Logic.RPC;
using NUnit.Framework;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using Tests.Stubs;
using Assert = NUnit.Framework.Assert;
using Environment = FirstLight.Game.Services.Environment;

public class TestCommandValidation
{
	private GameServer? _gameServer;
	private ServerState? _state;
	private Dictionary<string, string>? _commandData;
	private IGameCommand? _command;


	private void Setup(string env = "dev")
	{
		var configuration = new StubConfiguration();
		configuration.ApplicationEnvironment = env;
		configuration.MinClientVersion = new Version("1.0.0");
		_state = new ServerState();
		_gameServer = new TestServer(configuration).GetService<GameServer>();
	}

	private void SetupCommand(IGameCommand? cmd = null)
	{
		if (cmd == null) cmd = new DummyCommand();
		_commandData = new Dictionary<string, string>();
		_commandData[CommandFields.Timestamp] = "1";
		_commandData[CommandFields.ClientVersion] = "1.0.0";
		_command = cmd;
		ModelSerializer.SerializeToData(_commandData, _command);
	}


	private void RunAndAssertException(string exceptionMessage)
	{
		var ex = Assert.Throws<LogicException>(() => _gameServer?.ValidateCommand(_state, _command, _commandData));
		Assert.AreEqual(exceptionMessage, ex.Message);
	}

	[Test]
	public void TestSimpleFlowNoExceptions()
	{
		Setup();
		SetupCommand();
		Assert.IsTrue(_gameServer?.ValidateCommand(_state, _command, _commandData));
	}

	[Test]
	public void TestCommandForFutureIsOk()
	{
		Setup();
		SetupCommand();
		_state[CommandFields.Timestamp] = "1";
		_commandData[CommandFields.Timestamp] = "2";
		Assert.IsTrue(_gameServer?.ValidateCommand(_state, _command, _commandData));
	}

	[Test]
	public void TestCommandFromPastErrors()
	{
		Setup();
		SetupCommand();
		_state[CommandFields.Timestamp] = "2";
		_commandData[CommandFields.Timestamp] = "1";
		RunAndAssertException("Outdated command timestamp for command DummyCommand. Command out of order ?");
	}

	[Test]
	public void TestConcurrentTimestampErrors()
	{
		Setup();
		SetupCommand();
		_state[CommandFields.Timestamp] = "2";
		_commandData[CommandFields.Timestamp] = "2";
		RunAndAssertException("Outdated command timestamp for command DummyCommand. Command out of order ?");
	}

	[Test]
	public void TestMissingTimestampValidation()
	{
		Setup();
		SetupCommand();
		_commandData?.Remove(CommandFields.Timestamp);
		RunAndAssertException("Command data requires a timestamp to be ran: Key Timestamp");
	}

	[Test]
	public void TestMissingVersion()
	{
		Setup();
		SetupCommand();
		_commandData?.Remove(CommandFields.ClientVersion);
		RunAndAssertException("Command data requires a version to be ran: Key ClientVersion");
	}

	[Test]
	public void TestOutdatedClient()
	{
		Setup();
		SetupCommand();
		_commandData[CommandFields.ClientVersion] = "0.0.1";
		RunAndAssertException("Outdated client 0.0.1 but expected minimal version 1.0.0");
	}

	[Test]
	public void TestAdminCommand()
	{
		Setup();
		SetupCommand(new AdminOnlyCommand());
		RunAndAssertException("Insuficient permissions to run command");
	}


	[Test]
	public void TestEnvironmentLock()
	{
		Setup("mainnet-prod");
		SetupCommand(new TestNetOnlyCommand());
		RunAndAssertException("Insuficient permissions to run command");
	}


	private class TestNetOnlyCommand : IGameCommand, IEnvironmentLock
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public void Execute(CommandExecutionContext ctx)
		{
		}

		public Enum[] AllowedEnvironments()
		{
			return new Enum[] { Environment.TESTNET };
		}
	}


	private class DummyCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public void Execute(CommandExecutionContext ctx)
		{
		}
	}

	private class AdminOnlyCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Admin;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public void Execute(CommandExecutionContext ctx)
		{
		}
	}
}