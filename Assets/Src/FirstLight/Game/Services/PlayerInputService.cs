using FirstLight.Game.Input;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service handles Player Input general interaction for spell controls  
	/// </summary>
	public interface IPlayerInputService
	{
		/// <summary>
		/// Enable accessor for the player spell control input 
		/// </summary>
		LocalInput Input { get; }
		
		/// <summary>
		/// Enable Player spell control input 
		/// </summary>
		void EnableInput();
		
		/// <summary>
		/// Disable Player spell control input
		/// </summary>
		void DisableInput();

	}

	
	public class PlayerInputService : IPlayerInputService
	{

		public LocalInput Input { get; }
		
		public PlayerInputService()
		{
			Input = new LocalInput();
		}
		
		public void DisposeInput()
		{
			Input.Dispose();
		}

		public void EnableInput()
		{
			Input.Enable();
		}

		public void DisableInput()
		{
			Input.Disable();
		}
	}
	
}