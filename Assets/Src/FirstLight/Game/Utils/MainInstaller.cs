using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
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

		/// <inheritdoc cref="IInstaller.TryResolve{T}"/>
		public static bool TryResolve<T>(out T instance)
		{
			return _installer.TryResolve(out instance);
		}

		/// <inheritdoc cref="IInstaller.Resolve{T}"/>
		public static T Resolve<T>()
		{
			return _installer.Resolve<T>();
		}

		/// <inheritdoc cref="IInstaller.Clean"/>
		public static bool Clean<T>() where T : class
		{
			return _installer.Clean<T>();
		}

		/// <inheritdoc cref="IInstaller.Clean"/>
		public static bool CleanDispose<T>() where T : class, IDisposable
		{
			_installer.Resolve<T>().Dispose();

			return _installer.Clean<T>();
		}

		/// <inheritdoc cref="IInstaller.Clean"/>
		public static void Clean()
		{
			_installer.Clean();
		}

		/// <summary>
		/// Helper to resolve services
		/// </summary>
		public static IGameServices ResolveServices()
		{
			return Resolve<IGameServices>();
		}

		/// <summary>
		/// Helper to resolve match services
		/// </summary>
		public static IMatchServices ResolveMatchServices()
		{
			return Resolve<IMatchServices>();
		}

		/// <summary>
		/// Helper to resolve data provider
		/// </summary>
		public static IGameDataProvider ResolveData()
		{
			return Resolve<IGameDataProvider>();
		}
	}
}