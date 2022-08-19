using System;
using System.Collections.Generic;
using Backend.Game.Services;
using ServerSDK;
using ServerSDK.Services;

namespace Src.FirstLight.Server
{
    /// <summary>
    /// Main server setup class.
    /// This class namespace and type is referenced on server. Renaming here would require rename on server.
    /// </summary>
    public class FlgServerConfig : IServerSetup
    {
        /// <summary>
        /// Here we define plugins that are client-specific with specific logic.
        /// Plugins run on server can listen to server events and react/modify them.
        /// </summary>
        public ServerPlugin[] GetPlugins()
        {
            return new ServerPlugin[] { };
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
