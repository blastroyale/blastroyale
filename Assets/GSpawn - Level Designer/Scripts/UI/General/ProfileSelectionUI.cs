#if UNITY_EDITOR
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace GSpawn
{
    public class ProfileSelectionUI<TProfileDb, TProfile> : VisualElement
        where TProfileDb : ProfileDb<TProfile>
        where TProfile : Profile
    {
        private Toolbar             _toolbar;
        private ToolbarMenu         _dropDownMenu;
        private TProfileDb          _profileDb;
        private string              _profileCategory    = string.Empty;

        public string               profileCategory     { get { return _profileCategory; } }

        public void build(TProfileDb profileDb, string profileCategory, VisualElement parent)
        {
            _profileCategory = profileCategory;
            if (string.IsNullOrEmpty(_profileCategory)) _profileCategory = string.Empty;

            _profileDb = profileDb;
            parent.Add(this);

            _toolbar = new Toolbar();
            _toolbar.style.flexShrink = 0.0f;
            Add(_toolbar);

            refreshDropDownMenu();
        }

        public void refresh()
        {
            refreshDropDownMenu();
        }

        private void refreshDropDownMenu()
        {
            if (_dropDownMenu != null)
                _toolbar.Remove(_dropDownMenu);

            _dropDownMenu = new ToolbarMenu();
            //_dropDownMenu.style.width = 130.0f;
            _dropDownMenu.style.flexGrow = 1.0f;
            _toolbar.Add(_dropDownMenu);

            _dropDownMenu.text = _profileDb.activeProfile.profileName;
            for (int profileIndex = 0; profileIndex < _profileDb.numProfiles; ++profileIndex)
                insertProfileItem(profileIndex, _profileDb.getProfile(profileIndex));

            _dropDownMenu.menu.AppendSeparator();
            _dropDownMenu.menu.AppendAction("Create new profile...",
                (p) =>
                {
                    var wndUI               = CreateNewEntityUI.instance;
                    wndUI.headerLabel       = "Create a " + profileCategory + " profile";
                    wndUI.descriptionLabel  = "Enter the name of the profile you want to create.";
                    wndUI.nameFieldLabel    = "Profile Name: ";
                    wndUI.onCreate          = (name) =>
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            var newProfile = _profileDb.createProfile(name);
                            insertProfileItem(_profileDb.numProfiles - 1, newProfile);
                            setActiveProfile(newProfile.profileName);
                        }
                    };
                    var wnd = PluginWindow.showModalUtility<CreateNewEntityWindow>("Create Profile");
                    wnd.Repaint();
                });
            _dropDownMenu.menu.AppendAction("Rename profile...",
                (p) =>
                {
                    var wndUI               = RenameEntityUI.instance;
                    wndUI.headerLabel       = "Rename a " + profileCategory + " profile";
                    wndUI.descriptionLabel  = "Enter the new name you want to give to the profile \'" + _profileDb.activeProfile.profileName + "\'.";
                    wndUI.nameFieldLabel    = "Profile Name: ";
                    wndUI.currentName       = _profileDb.activeProfile.profileName;
                    wndUI.onRename          = (name) =>
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            _profileDb.renameProfile(_profileDb.activeProfile, name);
                            _dropDownMenu.text = _profileDb.activeProfile.profileName;

                            int profileIndex = _profileDb.indexOf(_profileDb.activeProfile);
                            _dropDownMenu.menu.RemoveItemAt(_profileDb.indexOf(_profileDb.activeProfile));
                            insertProfileItem(profileIndex, _profileDb.activeProfile);
                        }
                    };
                    var wnd = PluginWindow.showModalUtility<RenameEntityWindow>("Rename Profile");
                    wnd.Repaint();

                }, !_profileDb.isDefaultProfileActive ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            _dropDownMenu.menu.AppendAction("Delete profile...",
                (p) =>
                {
                    var wndUI           = DeleteEntityUI.instance;
                    wndUI.headerLabel   = "Delete a " + profileCategory + " profile";
                    wndUI.question      = "Are you sure you want to delete the " + profileCategory + " profile \'" + _profileDb.activeProfile.profileName + "\'?\n" + 
                        "This operation can not be undone.";
                    wndUI.onDelete      = () =>
                    { _profileDb.deleteProfile(_profileDb.activeProfile); };
                    var wnd             = PluginWindow.showModalUtility<DeleteEntityWindow>("Delete Profile");
                    wnd.Repaint();

                }, !_profileDb.isDefaultProfileActive ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private void insertProfileItem(int index, TProfile profile)
        {
            _dropDownMenu.menu.InsertAction(index, profile.profileName, (p) => { setActiveProfile(p.name); },
                    (p) => { return p.name == _profileDb.activeProfile.name ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal; });
        }

        private void setActiveProfile(string profileName)
        {
            _profileDb.setActiveProfile(profileName, true); 
            _dropDownMenu.text = profileName;
        }
    }
}
#endif