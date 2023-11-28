using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Backend;
using Backend.Game;
using ServerCommon.Cloudscript;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PlayFab;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using GameLogicService;


public class TestLogicServer: TestService<Program>
{

}
