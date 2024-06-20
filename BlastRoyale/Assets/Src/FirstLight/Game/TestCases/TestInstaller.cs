using FirstLight.Game.TestCases.Helpers;
using FirstLight.Services;

namespace FirstLight.Game.TestCases
{
	public class TestInstaller : Installer
	{
		public void OnGameAwaken()
		{
			foreach (var bindingsValue in this._bindings.Values)
			{
				if (bindingsValue is TestHelper helper)
				{
					helper.OnGameAwaken();
				}
			}
		}
		
		public override void Bind<T>(T instance) where T : class
		{
			var type = typeof(T);
			_bindings.Add(type, instance);
		}
		
		
	}
	
}