using FirstLight.Game.Input;

namespace FirstLight.Game.Services
{
	public interface IPlayerInputService
	{
		void DisposeInput();
		void EnableInput();
		void DisableInput();

	}

	
	public class PlayerInputService : IPlayerInputService
	{
		private LocalInput _localInput;
		
		public PlayerInputService()
		{
			_localInput = new LocalInput();
		}

		public void DisposeInput()
		{
			_localInput.Dispose();
		}

		public void EnableInput()
		{
			_localInput.Enable();
		}

		public void DisableInput()
		{
			_localInput.Disable();
		}
	}
	
}