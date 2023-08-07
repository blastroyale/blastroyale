#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace GSpawn
{
    public abstract class ProfileDb<TProfile> : ScriptableObject
        where TProfile : Profile
    {
        public delegate void    ActiveProfileChangedHandler             (TProfile newActiveProfile);
        public delegate void    ProfileRenamedHandler                   (TProfile profile);

        public event            ActiveProfileChangedHandler             activeProfileChanged;
        public event            ProfileRenamedHandler                   profileRenamed;

        private static readonly string                                  _defaultProfileName = "Default";

        [SerializeField]
        private List<TProfile>  _profiles                   = new List<TProfile>();
        [SerializeField]
        private TProfile        _activeProfile;

        public int              numProfiles                { get { return _profiles.Count; } }
        public TProfile         defaultProfile 
        { 
            get 
            {
                // Note: We can't do this in OnEnabled because it causes
                //       issues when importing into another project (i.e. it
                //       overrides the existing default profile).
                initializeIfNecessary();
                return _profiles[0]; 
            } 
        }
        public TProfile         activeProfile 
        { 
            get 
            {
                if (_activeProfile == null)
                {
                    UndoEx.saveEnabledState();
                    UndoEx.enabled = false;
                    _activeProfile = defaultProfile;
                    EditorUtility.SetDirty(this);
                    UndoEx.restoreEnabledState();
                }
                return (TProfile)_activeProfile;
            }
        }
        public bool             isDefaultProfileActive      { get { return defaultProfile == activeProfile; } }
        public abstract string  folderPath                  { get; }

        public static string    defaultProfileName          { get { return _defaultProfileName; } }

        public void onPostProcessAllAssets()
        {
            // Note: Make sure all profiles are loaded. Don't do this in OnEnable. See the defaultProfile property.
            loadProfiles(folderPath);
            createDefaultProfile();

            EditorUtility.SetDirty(this);
        }

        public void initializeIfNecessary()
        {
            if (_profiles.Count == 0)
            {
                loadProfiles(folderPath);
                createDefaultProfile();
            }
        }

        public void setActiveProfile(string profileName, bool allowUndoRedo)
        {
            initializeIfNecessary();
            if (string.IsNullOrEmpty(profileName)) return;

            var newActiveProfile = findProfile(profileName);
            if (newActiveProfile != null && newActiveProfile != _activeProfile)
            {
                if (allowUndoRedo) UndoEx.record(this);
                _activeProfile = newActiveProfile;
                EditorUtility.SetDirty(this);
                onActiveProfileChanged();

                if (activeProfileChanged != null) activeProfileChanged((TProfile)_activeProfile);
            }
        }

        public TProfile createProfile(string profileName)
        {
            if (string.IsNullOrEmpty(profileName)) return null;

            // Note: Avoid calling getProfileNames. It will cause a stack overflow
            //       because of 'initializeIfNecessary'.
            var names = new List<string>();
            foreach (var p in _profiles)
                names.Add(p.profileName);

            profileName = UniqueNameGen.generate(profileName, names);

            TProfile profile = CreateInstance<TProfile>();
            profile.profileName = profileName;
            _profiles.Add(profile);

            AssetDbEx.saveScriptableObject(profile, folderPath + "/" + profile.profileName + ".asset");
            EditorUtility.SetDirty(this);

            return profile;
        }

        public void deleteProfile(TProfile profile)
        {
            if (profile == defaultProfile) return;

            if (containsProfile(profile))
            {
                //UndoEx.record(this);
                bool refreshActiveProfile = profile == _activeProfile;
                _profiles.Remove(profile);

                //UndoEx.destroyObjectImmediate(profile);
                DestroyImmediate(profile, true);
                if (refreshActiveProfile) setActiveProfile(defaultProfile.profileName, true);
                EditorUtility.SetDirty(this);
            }
        }

        public void renameProfile(TProfile profile, string newName)
        {
            if (!string.IsNullOrEmpty(newName) && containsProfile(profile) && profile.profileName != newName)
            {
                var names               = new List<string>();
                getProfileNames(names, profile.profileName);
                newName                 = UniqueNameGen.generate(newName, names);

                // Note: Rename the asset first in order to avoid warning message.
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(profile), newName);
                profile.profileName     = newName;

                if (profileRenamed != null) profileRenamed(profile);
                EditorUtility.SetDirty(this);
            }
        }

        public bool containsProfile(TProfile profile)
        {
            return _profiles.Contains(profile);
        }

        public bool containsProfile(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return _profiles.Find(item => { return item.profileName == name; }) != null;
        }

        public void getProfileNames(List<string> profileNames, string ignoredName)
        {
            initializeIfNecessary();
            profileNames.Clear();

            foreach (var profile in _profiles)
            {
                if (profile.profileName != ignoredName)
                    profileNames.Add(profile.profileName);
            }
        }

        public void getProfiles(List<TProfile> profiles)
        {
            initializeIfNecessary();
            profiles.Clear();
            profiles.AddRange(_profiles);
        }

        public TProfile findProfile(string profileName)
        {
            initializeIfNecessary();
            return _profiles.Find(item => item.profileName == profileName);
        }

        public TProfile getProfile(int index)
        {
            initializeIfNecessary();
            return _profiles[index];
        }

        public int indexOf(TProfile profile)
        {
            initializeIfNecessary();
            return _profiles.IndexOf(profile);
        }

        protected virtual void onActiveProfileChanged   () {}
        protected virtual void onEnabled                () {}
        protected virtual void onDisabled               () {}
        protected virtual void onDestroy                () {}

        private void loadProfiles(string folderPath)
        {
            if (_profiles.Count == 0)
            {
                var profiles = AssetDbEx.loadAssetsInFolder<TProfile>(folderPath);
                foreach (var profile in profiles)
                {
                    if (!containsProfile(profile))
                        _profiles.Add(profile);
                }
                EditorUtility.SetDirty(this);
            }
        }

        private void createDefaultProfile()
        {
            if (!containsProfile(_defaultProfileName))
            {
                createProfile(_defaultProfileName);
                EditorUtility.SetDirty(this);
            }
        }

        private void OnEnable()
        {
            // Note: Doesn't work when exporting to a different project.
            //loadProfiles(folderPath);
            //createDefaultProfile();
            //if (_activeProfile == null) _activeProfile = defaultProfile;
            //EditorUtility.SetDirty(this);

            onEnabled();
        }

        private void OnDisable()
        {
            onDisabled();
        }

        private void OnDestroy()
        {
            onDestroy();
            foreach (var profile in _profiles)
                DestroyImmediate(profile, true);
        }
    }
}
#endif