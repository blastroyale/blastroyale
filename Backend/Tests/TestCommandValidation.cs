
using System;
using System.Collections.Generic;
using Backend;
using Backend.Game;
using Backend.Game.Services;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Quantum;
using ServerSDK.Models;
using Tests.Stubs;
using Assert = NUnit.Framework.Assert;

namespace Tests;

public class TestCommandValidation
{
	private GameServer? _gameServer;
	private ServerState? _state;
	private Dictionary<string, string>? _commandData;
	private IGameCommand? _command;
	
	[SetUp]
	public void Setup()
	{
		_state = new ServerState();
		_gameServer = new TestServer().GetService<GameServer>();
		_commandData = new Dictionary<string, string>();
		_commandData[CommandFields.Timestamp] = "1";
		_commandData[CommandFields.ClientVersion] = ServerConfiguration.GetConfig().MinClientVersion;
		_command = new UpdatePlayerSkinCommand();
		ModelSerializer.SerializeToData(_commandData, _command);
	}

	[Test]
	public void TestSimpleFlowNoExceptions()
	{
		Assert.IsTrue(_gameServer?.ValidateCommand(_state, _command, _commandData));
	}
	
	[Test]
	public void TestCommandForFutureIsOk()
	{
		_state[CommandFields.Timestamp] = "1";
		_commandData[CommandFields.Timestamp] = "2";
		Assert.IsTrue(_gameServer?.ValidateCommand(_state, _command, _commandData));
	}
	
	[Test]
	public void TestCommandFromPastErrors()
	{
		_state[CommandFields.Timestamp] = "2";
		_commandData[CommandFields.Timestamp] = "1";
		Assert.Throws<LogicException>(() => _gameServer?.ValidateCommand(_state, _command, _commandData));
	}
	
	[Test]
	public void TestConcurrentTimestampErrors()
	{
		_state[CommandFields.Timestamp] = "2";
		_commandData[CommandFields.Timestamp] = "2";
		Assert.Throws<LogicException>(() => _gameServer?.ValidateCommand(_state, _command, _commandData));
	}
	
	[Test]
	public void TestMissingTimestampValidation()
	{
		_commandData?.Remove(CommandFields.Timestamp);
		Assert.Throws<LogicException>(() => _gameServer?.ValidateCommand(_state, _command, _commandData));
	}
	
	[Test]
	public void TestMissingVersion()
	{
		_commandData?.Remove(CommandFields.ClientVersion);
		Assert.Throws<LogicException>(() => _gameServer?.ValidateCommand(_state, _command, _commandData));
	}
	
	[Test]
	public void TestOutdatedClient()
	{
		_commandData[CommandFields.ClientVersion] = "0.0.1";
		Assert.Throws<LogicException>(() => _gameServer?.ValidateCommand(_state, _command, _commandData));
	}
}