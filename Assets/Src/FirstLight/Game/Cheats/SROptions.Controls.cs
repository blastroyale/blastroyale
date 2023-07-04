using System.ComponentModel;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Playables;

public partial class SROptions
{

#if DEVELOPMENT_BUILD

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
#endif
}