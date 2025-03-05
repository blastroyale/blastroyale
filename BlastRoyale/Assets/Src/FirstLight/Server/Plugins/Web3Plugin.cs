using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;

namespace Src.FirstLight.Server
{
	public class Web3Plugin : ServerPlugin
	{
		private PluginContext _ctx;

		private KeyValuePair<string, string> _privateSignerData;

		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			var data = new Web3PrivateData()
			{
				Signer = _ctx.ServerConfig!.VoucherSigner
			};
			_privateSignerData = ModelSerializer.Serialize(data);
			_ctx.PluginEventManager!.RegisterEventListener<BeforeCommandRunsEvent>(OnBeforeCommand);
		}

		private Task OnBeforeCommand(BeforeCommandRunsEvent e)
		{
			e.State[_privateSignerData.Key] = _privateSignerData.Value;
			return Task.CompletedTask;
		}
	}
}