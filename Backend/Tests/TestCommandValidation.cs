
using System.Collections.Generic;
using Backend.Game;
using Backend.Game.Services;
using Backend.Models;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Quantum;
using Tests.Stubs;

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
}