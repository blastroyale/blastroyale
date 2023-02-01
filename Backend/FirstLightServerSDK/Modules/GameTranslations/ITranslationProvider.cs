using System;

namespace FirstLight.Server.SDK.Modules.GameTranslations
{
	/**
	 * Translation provider for Backend services, in the future should be used in the Game as well.
	 */
	public interface ITranslationProvider
	{
		/// <summary>
		/// Get the English translation of the given <paramref name="key"/>
		/// </summary>
		/// <param name="key">used to search the translation</param>
		/// <returns>the translated value</returns>
		String GetTranslation(string key);
	}
}