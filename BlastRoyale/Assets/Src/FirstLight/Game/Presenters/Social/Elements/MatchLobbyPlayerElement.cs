using FirstLight.Game.Utils;
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
		private const string USS_LINK = USS_BLOCK + "__link";
		private const string USS_LOCAL_ARROW = USS_BLOCK + "__local-arrow";

		private const string USS_LOCAL = USS_BLOCK + "--local";
		private const string USS_HOST = USS_BLOCK + "--host";
		private const string USS_EMPTY = USS_BLOCK + "--empty";
		private const string USS_READY = USS_BLOCK + "--ready";

		private readonly Label _nameLabel;
		private readonly VisualElement _crown;
		private readonly VisualElement _plus;
		private readonly VisualElement _link;

		public MatchLobbyPlayerElement() : this("AVeryWeirdLongName", false, false, true, false)
		{
		}

		public MatchLobbyPlayerElement(string playerName, bool host, bool local, bool link, bool ready)
		{
			AddToClassList(USS_BLOCK);

			Add(_nameLabel = new Label(playerName) {name = "name"});
			_nameLabel.AddToClassList(USS_NAME);

			Add(_crown = new VisualElement {name = "crown"});
			_crown.AddToClassList(USS_CROWN);
			
			Add(_plus = new VisualElement {name = "plus"});
			_plus.AddToClassList(USS_PLUS);

			Add(_link = new VisualElement {name = "link"});
			_link.AddToClassList(USS_LINK);
			_link.SetDisplay(link);

			var localArrow = new VisualElement {name = "local-arrow"};
			Add(localArrow);
			localArrow.AddToClassList(USS_LOCAL_ARROW);

			SetData(playerName, host, local, ready);
		}
		
		public string GetCurrentPlayerName() => _nameLabel.text;

		public void SetData(string playerName, bool host, bool local, bool ready)
		{
			_nameLabel.text = playerName;

			EnableInClassList(USS_HOST, host);
			EnableInClassList(USS_LOCAL, local);
			EnableInClassList(USS_READY, ready);
			EnableInClassList(USS_EMPTY, string.IsNullOrEmpty(playerName));
		}

		public new class UxmlFactory : UxmlFactory<MatchLobbyPlayerElement>
		{
		}
	}
}