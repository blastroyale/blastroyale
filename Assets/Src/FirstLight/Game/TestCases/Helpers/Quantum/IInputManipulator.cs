using System.Collections;
using Quantum;

namespace FirstLight.Game.TestCases.Helpers
{
	public interface IInputManipulator
	{
		IEnumerator Start();

		void OnAwake();
		
		void Stop();
		void ChangeInput(CallbackPollInput callback, ref Quantum.Input input);
	}
}