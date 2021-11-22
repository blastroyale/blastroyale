using FirstLight.Services;

namespace FirstLight.Game.Utils
{
	
	/// <inheritdoc cref="IInstaller"/>
	/// <remarks>
	/// Use this installer for generic Binding interfaces that are available in the entire scope of the game
	/// </remarks>
	public static class MainInstaller
	{
		private static readonly IInstaller _installer = new Installer();
		
		/// <inheritdoc cref="IInstaller.Bind{T}"/>
		public static void Bind<T>(T instance) where T : class
		{
			_installer.Bind(instance);
		}

		/// <inheritdoc cref="IInstaller.Resolve{T}"/>
		public static T Resolve<T>()
		{
			return _installer.Resolve<T>();
		}

		/// <inheritdoc cref="IInstaller.Clean"/>
		public static void Clean()
		{
			_installer.Clean();
		}
	}
}