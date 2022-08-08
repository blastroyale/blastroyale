using System;
using UnityEngine;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Static utility methods for use in build scripts.
	/// </summary>
	public static class FirstLightBuildUtil
	{
		private const string _buildNumberOption = "-flBuildNumber";
		private const string _buildSymbolOption = "-flBuildSymbol";
		private const string _buildServerOption = "-flBuildServer";
		private const string _buildFileNameOption = "-flBuildFileName";

		/// <summary>
		/// Requests the build number from the batch mode command with the flag -flBuildNumber
		/// </summary>
		public static bool TryGetBuildNumberFromCommandLineArgs(out int buildNumber, params string[] args)
		{
			if (!TryGetCommandLineOption(_buildNumberOption, out var buildNumberString, args))
			{
				buildNumber = 0;
				return false;
			}

			try
			{
				buildNumber = Convert.ToInt32(buildNumberString);
			}
			catch (Exception)
			{
				Debug.LogError($"Could not convert build number string to int: {buildNumberString}");
				throw;
			}

			return true;
		}

		/// <summary>
		/// Requests the build number from the batch mode command with the flag -flBuildSymbol
		/// </summary>
		public static bool TryGetBuildSymbolFromCommandLineArgs(out string buildSymbol, params string[] args)
		{
			if (!TryGetCommandLineOption(_buildSymbolOption, out buildSymbol, args))
			{
				return false;
			}

			if (buildSymbol != FirstLightBuildConfig.DevelopmentSymbol &&
			    buildSymbol != FirstLightBuildConfig.ReleaseSymbol && 
			    buildSymbol !=FirstLightBuildConfig.StoreSymbol)
			{
				Debug.LogError($"Build symbol not recognised: {buildSymbol}");
				return false;
			}
			
			return true;
		}
		
		/// <summary>
		/// Requests the build number from the batch mode command with the flag -flBuildFileName
		/// </summary>
		public static bool TryGetBuildFileNameFromCommandLineArgs(out string buildFileName, params string[] args)
		{
			return TryGetCommandLineOption(_buildFileNameOption, out buildFileName, args);
		}
		
		/// <summary>
		/// Requests the build number from the batch mode command with the flag -flBuildServer
		/// </summary>
		public static bool TryGetBuildServerSymbolFromCommandLineArgs(out string buildServer, params string[] args)
		{
			return TryGetCommandLineOption(_buildServerOption, out buildServer, args);
		}

		private static bool TryGetCommandLineOption(string option, out string result, params string[] args)
		{
			var indexOfOption = Array.IndexOf(args, option);
			if (indexOfOption < 0)
			{
				result = string.Empty;
				return false;
			}

			if (indexOfOption >= args.Length - 1)
			{
				result = string.Empty;
				return false;
			}

			result = args[indexOfOption + 1];
			return true;
		}
	}
}