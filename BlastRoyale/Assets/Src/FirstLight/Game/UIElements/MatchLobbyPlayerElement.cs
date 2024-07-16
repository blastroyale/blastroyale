using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements
{
	/// <summary>
	/// A single player in a match lobby.
	/// </summary>
	public class MatchLobbyPlayerElement : ImageButton
	{
		private const string USS_BLOCK = "match-lobby-player";
		private const string USS_NAME = USS_BLOCK + "__name";
		private const string USS_CROWN = USS_BLOCK + "__crown";
		private const string USS_PLUS = USS_BLOCK + "__plus";

		private const string USS_LOCAL = USS_BLOCK + "--local";
		private const string USS_HOST = USS_BLOCK + "--host";
		private const string USS_EMPTY = USS_BLOCK + "--empty";

		private Label _nameLabel;
		private VisualElement _crown;
		private VisualElement _plus;

		public MatchLobbyPlayerElement() : this("AVeryWeirdLongName", false, false)
		{
		}

		public MatchLobbyPlayerElement(string name, bool host, bool local)
		{
			AddToClassList(USS_BLOCK);
			
			Add(_nameLabel = new Label(name) {name = "name"});
			_nameLabel.AddToClassList(USS_NAME);

			Add(_crown = new VisualElement {name = "crown"});
			_crown.AddToClassList(USS_CROWN);
			
			Add(_plus = new VisualElement {name = "plus"});
			_plus.AddToClassList(USS_PLUS);

			EnableInClassList(USS_HOST, host);
			EnableInClassList(USS_LOCAL, local);
			EnableInClassList(USS_EMPTY, string.IsNullOrEmpty(name));
		}

		public new class UxmlFactory : UxmlFactory<MatchLobbyPlayerElement>
		{
		}
	}
}