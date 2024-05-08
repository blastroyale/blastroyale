using System.ComponentModel;
using FirstLight.Game.Utils;

public partial class SROptions
{
	[Category("Controls")]
	public bool EnableAimDeadzone
	{
		get => FeatureFlags.AIM_DEADZONE;
		set => FeatureFlags.AIM_DEADZONE = !FeatureFlags.AIM_DEADZONE;
	}

	[Category("Controls")]
	public bool QuantumPredictedAim
	{
		get => FeatureFlags.QUANTUM_PREDICTED_AIM;
		set => FeatureFlags.QUANTUM_PREDICTED_AIM = !FeatureFlags.QUANTUM_PREDICTED_AIM;
	}

	[Category("Controls")]
	public bool SpecialsUseEnhancedTouch
	{
		get => FeatureFlags.SPECIAL_NEW_INPUT;
		set => FeatureFlags.SPECIAL_NEW_INPUT = !FeatureFlags.SPECIAL_NEW_INPUT;
	}
}