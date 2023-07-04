using System;

namespace FirstLight.Server.SDK.Modules.Commands
{
	public interface IEnvironmentLock
	{

		Enum[] AllowedEnvironments();

	}
}