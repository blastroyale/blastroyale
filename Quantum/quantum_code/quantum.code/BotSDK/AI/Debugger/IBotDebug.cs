namespace Quantum
{
	// Interface to make the communication between the three solutions involved: quantum_unity, quantum_code and quantum.ai.editor
	// Basically, these are the information that the quantum.ai.editor needs to know that are meant to be filed from Unity
	public interface IBotDebug
	{
		EntityRef EntityRef { get; }
		Frame Frame { get; }

		// The asset names on Unity
		string GetHFSMRootName();
		string GetBTRootName();
		string GetUTRootName();
	}
}