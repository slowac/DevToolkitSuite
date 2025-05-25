#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DevToolkitSuite.PreferenceEditor.UI
{
    /// <summary>
    /// Centralized resource management system for loading and caching editor icons and textures.
    /// Automatically locates resource directory and provides strongly-typed access to UI assets.
    /// </summary>
    public static class ResourceManager
    {
        // Unique identifier for locating the correct ResourceManager among potential duplicates
        private static readonly string RESOURCE_MANAGER_ID = "[PreferenceEditor] com.devtoolkitsuite.preference_editor_resources";

        private static string resourceDirectoryPath;
        
        /// <summary>
        /// Locates and returns the path to the resource directory containing editor assets.
        /// Uses the unique identifier to ensure correct ResourceManager is found.
        /// </summary>
        /// <returns>Full path to the resource directory</returns>
        /// <exception cref="Exception">Thrown when ResourceManager.cs cannot be located in the project</exception>
        private static string GetResourceDirectory()
        {
            if (resourceDirectoryPath != null)
                return resourceDirectoryPath;

            foreach (string assetGuid in AssetDatabase.FindAssets("ResourceManager"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                string fileName = Path.GetFileName(assetPath);

                if (fileName.Equals("ResourceManager.cs"))
                {
                    // Verify this is the correct ResourceManager by checking for unique identifier
                    if (File.ReadLines(Path.GetFullPath(assetPath)).Any(line => line.Contains(RESOURCE_MANAGER_ID)))
                    {
                        resourceDirectoryPath = Path.GetDirectoryName(assetPath) + Path.DirectorySeparatorChar;
                        return resourceDirectoryPath;
                    }
                }
            }
            throw new Exception("Cannot locate ResourceManager.cs in the project. Please ensure all resource files are properly imported.");
        }

        #region Platform-Specific Operating System Icons

        /// <summary>
        /// Returns the appropriate operating system icon based on the current Unity editor platform.
        /// </summary>
        /// <returns>Texture2D representing the current platform's OS icon</returns>
        public static Texture2D GetPlatformIcon()
        {
#if UNITY_EDITOR_WIN
            return WindowsIcon;
#elif UNITY_EDITOR_OSX
            return MacOSIcon;
#elif UNITY_EDITOR_LINUX
            return LinuxIcon;
#endif
        }

        private static Texture2D linuxIcon;
        /// <summary>
        /// Gets the Linux operating system icon, loading it on first access.
        /// </summary>
        public static Texture2D LinuxIcon
        {
            get
            {
                if (linuxIcon == null)
                {
                    linuxIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "linux_icon.png", typeof(Texture2D));
                }
                return linuxIcon;
            }
        }

        private static Texture2D windowsIcon;
        /// <summary>
        /// Gets the Windows operating system icon, loading it on first access.
        /// </summary>
        public static Texture2D WindowsIcon
        {
            get
            {
                if (windowsIcon == null)
                {
                    windowsIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "win_icon.png", typeof(Texture2D));
                }
                return windowsIcon;
            }
        }

        private static Texture2D macOSIcon;
        /// <summary>
        /// Gets the macOS operating system icon, loading it on first access.
        /// </summary>
        public static Texture2D MacOSIcon
        {
            get
            {
                if (macOSIcon == null)
                {
                    macOSIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "mac_icon.png", typeof(Texture2D));
                }
                return macOSIcon;
            }
        }

        #endregion

        #region Animation and Loading Icons

        private static GUIContent[] loadingSpinnerFrames;
        /// <summary>
        /// Gets an array of GUI content for creating loading spinner animations.
        /// Uses Unity's built-in WaitSpin icons for consistent appearance.
        /// </summary>
        public static GUIContent[] LoadingSpinnerFrames
        {
            get
            {
                if (loadingSpinnerFrames == null)
                {
                    loadingSpinnerFrames = new GUIContent[12];
                    for (int frameIndex = 0; frameIndex < 12; frameIndex++)
                        loadingSpinnerFrames[frameIndex] = EditorGUIUtility.IconContent("WaitSpin" + frameIndex.ToString("00"));
                }
                return loadingSpinnerFrames;
            }
        }

        #endregion

        #region Action and State Icons

        private static Texture2D refreshIcon;
        /// <summary>
        /// Gets the refresh/reload action icon.
        /// </summary>
        public static Texture2D RefreshIcon
        {
            get
            {
                if (refreshIcon == null)
                {
                    refreshIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "refresh.png", typeof(Texture2D));
                }
                return refreshIcon;
            }
        }

        private static Texture2D deleteIcon;
        /// <summary>
        /// Gets the delete/trash action icon.
        /// </summary>
        public static Texture2D DeleteIcon
        {
            get
            {
                if (deleteIcon == null)
                {
                    deleteIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "trash.png", typeof(Texture2D));
                }
                return deleteIcon;
            }
        }

        private static Texture2D warningIcon;
        /// <summary>
        /// Gets the warning/attention notification icon.
        /// </summary>
        public static Texture2D WarningIcon
        {
            get
            {
                if (warningIcon == null)
                {
                    warningIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "attention.png", typeof(Texture2D));
                }
                return warningIcon;
            }
        }

        private static Texture2D infoIcon;
        /// <summary>
        /// Gets the informational message icon.
        /// </summary>
        public static Texture2D InfoIcon
        {
            get
            {
                if (infoIcon == null)
                {
                    infoIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "info.png", typeof(Texture2D));
                }
                return infoIcon;
            }
        }

        #endregion

        #region Monitoring State Icons

        private static Texture2D monitoringActiveIcon;
        /// <summary>
        /// Gets the icon indicating active monitoring state.
        /// </summary>
        public static Texture2D MonitoringActiveIcon
        {
            get
            {
                if (monitoringActiveIcon == null)
                {
                    monitoringActiveIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "watching.png", typeof(Texture2D));
                }
                return monitoringActiveIcon;
            }
        }

        private static Texture2D monitoringInactiveIcon;
        /// <summary>
        /// Gets the icon indicating inactive monitoring state.
        /// </summary>
        public static Texture2D MonitoringInactiveIcon
        {
            get
            {
                if (monitoringInactiveIcon == null)
                {
                    monitoringInactiveIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "not_watching.png", typeof(Texture2D));
                }
                return monitoringInactiveIcon;
            }
        }

        #endregion

        #region Sorting State Icons

        private static Texture2D sortNeutralIcon;
        /// <summary>
        /// Gets the icon for neutral/unsorted state.
        /// </summary>
        public static Texture2D SortNeutralIcon
        {
            get
            {
                if (sortNeutralIcon == null)
                {
                    sortNeutralIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "sort.png", typeof(Texture2D));
                }
                return sortNeutralIcon;
            }
        }

        private static Texture2D sortAscendingIcon;
        /// <summary>
        /// Gets the icon for ascending sort order.
        /// </summary>
        public static Texture2D SortAscendingIcon
        {
            get
            {
                if (sortAscendingIcon == null)
                {
                    sortAscendingIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "sort_asc.png", typeof(Texture2D));
                }
                return sortAscendingIcon;
            }
        }

        private static Texture2D sortDescendingIcon;
        /// <summary>
        /// Gets the icon for descending sort order.
        /// </summary>
        public static Texture2D SortDescendingIcon
        {
            get
            {
                if (sortDescendingIcon == null)
                {
                    sortDescendingIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(GetResourceDirectory() + "sort_desc.png", typeof(Texture2D));
                }
                return sortDescendingIcon;
            }
        }

        #endregion
    }
}
#endif
