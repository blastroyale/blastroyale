using System.ComponentModel;
using Lofelt.NiceVibrations;

public partial class SROptions
{
	[Category("Haptics")]
	public void HapticPresetSelection()
	{
		HapticPatterns.PlayPreset(HapticPatterns.PresetType.Selection);
	}

	[Category("Haptics")]
	public void HapticPresetSuccess()
	{
		HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
	}

	[Category("Haptics")]
	public void HapticPresetWarning()
	{
		HapticPatterns.PlayPreset(HapticPatterns.PresetType.Warning);
	}

	[Category("Haptics")]
	public void HapticPresetFailure()
	{
		HapticPatterns.PlayPreset(HapticPatterns.PresetType.Failure);
	}

	[Category("Haptics")]
	public void HapticPresetLightImpact()
	{
		HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
	}

	[Category("Haptics")]
	public void HapticPresetMediumImpact()
	{
		HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
	}

	[Category("Haptics")]
	public void HapticPresetHeavyImpact()
	{
		HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
	}

	[Category("Haptics")]
	public void HapticPresetRigidImpact()
	{
		HapticPatterns.PlayPreset(HapticPatterns.PresetType.RigidImpact);
	}

	[Category("Haptics")]
	public void HapticPresetSoftImpact()
	{
		HapticPatterns.PlayPreset(HapticPatterns.PresetType.SoftImpact);
	}
}