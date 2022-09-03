using System;
using System.Collections;
using System.Collections.Generic;

namespace FirstLight.Server.SDK.Modules.GameConfiguration
{
	/// <inheritdoc />
	/// <remarks>
	/// Extends the <see cref="IConfigsProvider"/> behaviour by allowing it to add configs to the provider
	/// </remarks>
	public interface IConfigsAdder : IConfigsProvider
	{
		/// <summary>
		/// Adds the given unique single <paramref name="config"/> to the container.
		/// </summary>
		void AddSingletonConfig<T>(T config);

		/// <summary>
		/// Adds the given <paramref name="configList"/> to the container.
		/// The configuration will use the given <paramref name="referenceIdResolver"/> to map each config to it's defined id.
		/// </summary>
		void AddConfigs<T>(Func<T, int> referenceIdResolver, IList<T> configList) where T : struct;

		/// <summary>
		/// Adds the given dictionary of configuration lists to the config.
		/// </summary>
		public void AddAllConfigs(IDictionary<Type, IEnumerable> configs);
	}
}