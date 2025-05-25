using System.Collections.Generic;
using UnityEngine;

namespace DevToolkitSuite.PreferenceEditor.Core
{
    /// <summary>
    /// ScriptableObject container for managing collections of preference entries during editor operations.
    /// Maintains separate lists for user-defined preferences and Unity system preferences.
    /// Configured to avoid persistence to prevent unwanted asset creation.
    /// </summary>
    [System.Serializable]
    public class PreferenceDataContainer : ScriptableObject
    {
        /// <summary>
        /// Collection of user-defined preference entries that can be modified and managed.
        /// These are typically application-specific settings created by developers.
        /// </summary>
        public List<PreferenceEntryData> userDefinedEntries;
        
        /// <summary>
        /// Collection of Unity system preference entries that are read-only for display purposes.
        /// These include Unity engine settings and internal configuration values.
        /// </summary>
        public List<PreferenceEntryData> systemDefinedEntries;

        /// <summary>
        /// Initializes the preference entry collections when the ScriptableObject is enabled.
        /// Ensures collections are properly instantiated and configures object to avoid persistence.
        /// </summary>
        private void OnEnable()
        {
            // Prevent this object from being saved as an asset file
            hideFlags = HideFlags.DontSave;
            
            // Initialize collections if they don't exist
            if (userDefinedEntries == null)
                userDefinedEntries = new List<PreferenceEntryData>();
            if (systemDefinedEntries == null)
                systemDefinedEntries = new List<PreferenceEntryData>();
        }

        /// <summary>
        /// Clears all preference entry collections, effectively resetting the container state.
        /// Useful for refreshing data when reloading preferences from the system.
        /// </summary>
        public void ClearAllEntries()
        {
            if (userDefinedEntries != null)
                userDefinedEntries.Clear();
            if (systemDefinedEntries != null)
                systemDefinedEntries.Clear();
        }
    }
}