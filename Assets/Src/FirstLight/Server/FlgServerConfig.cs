using System;
using System.Collections.Generic;
using System.Reflection;
using FirstLight.Game.Commands;
using FirstLight.Game.Serializers;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Services;
using Src.FirstLight.Server.ServerServices;


namespace Src.FirstLight.Server
{
    /// <summary>
    /// Main server setup class.
    /// This class namespace and type is referenced on server. Renaming here would require rename on server.
    /// </summary>
    public class FlgServerConfig : IServerSetup
    {
        /// <summary>
        /// Will be called on server when instantiating the plugin
        /// </summary>
        public FlgServerConfig()
        {
            FlgCustomSerializers.RegisterSerializers();
        }

        public Assembly GetCommandsAssembly()
        {
            return typeof(EquipCollectionItemCommand).Assembly;
        }

        /// <summary>
        /// Here we define plugins that are client-specific with specific logic.
        /// Plugins run on server can listen to server events and react/modify them.
        /// </summary>
        public ServerPlugin[] GetPlugins()
        {
            return new ServerPlugin[]
            {
                new ServerAnalyticsPlugin(),
                new ServerStatisticsPlugin(),
				new PlayfabAvatarPlugin()
            };
        }
        
        /// <summary>
        /// Get the list of commands that will run after get player data on the server, this is done manually here. In the future
        /// we should auto discover commands
        /// </summary>
        public Type[] GetInitializationCommandTypes()
        {
            return new[]
            {
                typeof(GiveDefaultCollectionItemsCommand),
                typeof(InitializeBattlepassSeasonCommand),
				typeof(CleanupOldDataCommand)
            };
        }

        /// <summary>
        /// Here we define server implementation of how to handle client specific code.
        /// The server uses a singleton dependency injected for defined interfaces. Everytime the server requires the key
        /// type of the dictionary, it will return an singleton instance of the value of that key.
        /// Server interfaces that are required for the service to run (e.g IConfigsProvider) are injected server-side.
        /// </summary>
        public Dictionary<Type, Type> SetupDependencies()
        {
            return new Dictionary<Type, Type>()
            {
                {typeof(IPlayerSetupService), typeof(BlastRoyalePlayerSetup)}
            };
        }
    }
}
