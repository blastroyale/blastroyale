
/// <summary>
/// References sprites in Assets/Art/UI/Textures/spritesheet-text.png
/// and mapped in UiSpriteAsset.asset
/// </summary>
public static class TextSprites
{
    public static string TagFromSpriteName(string iconName) => $"<sprite name=\"{iconName}\">";

	public static string GREEN_CHECK = TagFromSpriteName("Checked");
	public static string RED_CROSS = TagFromSpriteName("RedCrossIcon");
}