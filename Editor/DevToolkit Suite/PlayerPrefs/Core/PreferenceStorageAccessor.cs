using System;
using System.Linq;

#if UNITY_EDITOR_WIN
using Microsoft.Win32;
using System.Text;
#elif UNITY_EDITOR_OSX
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
#elif UNITY_EDITOR_LINUX
using System.IO;
using System.Xml;
using System.Xml.Linq;
#endif

namespace DevToolkitSuite.PreferenceEditor.Core
{
    /// <summary>
    /// Abstract base class for platform-specific preference storage access implementations.
    /// Provides a unified interface for reading preference keys from different operating systems
    /// while supporting real-time change monitoring and caching for performance optimization.
    /// </summary>
    public abstract class PreferenceStorageAccessor
    {
        protected string preferencesPath;
        protected string[] cachedPreferenceKeys = new string[0];

        /// <summary>
        /// Platform-specific implementation for retrieving preference keys from the system.
        /// Must be implemented by each platform-specific derived class.
        /// </summary>
        protected abstract void RetrieveKeysFromSystem();

        /// <summary>
        /// Initializes a new preference storage accessor with the specified path.
        /// </summary>
        /// <param name="pathToPreferences">Platform-specific path to the preference storage location</param>
        protected PreferenceStorageAccessor(string pathToPreferences)
        {
            preferencesPath = pathToPreferences;
        }

        /// <summary>
        /// Retrieves all preference keys, optionally reloading from the system.
        /// </summary>
        /// <param name="forceReload">When true, forces a fresh load from system storage</param>
        /// <returns>Array of preference key names</returns>
        public string[] GetPreferenceKeys(bool forceReload = true)
        {
            if (forceReload || cachedPreferenceKeys.Length == 0)
            {
                RetrieveKeysFromSystem();
            }

            return cachedPreferenceKeys;
        }

        /// <summary>
        /// Event delegate triggered when preference entries are modified externally.
        /// </summary>
        public Action PreferenceChangedDelegate;
        
        protected bool shouldIgnoreNextChange = false;

        /// <summary>
        /// Instructs the accessor to ignore the next change notification.
        /// Useful when making programmatic changes to avoid circular notifications.
        /// </summary>
        public void IgnoreNextChangeNotification()
        {
            shouldIgnoreNextChange = true;
        }

        /// <summary>
        /// Handles preference change notifications, respecting the ignore flag.
        /// </summary>
        protected virtual void OnPreferenceChanged()
        {
            if (shouldIgnoreNextChange)
            {
                shouldIgnoreNextChange = false;
                return;
            }

            PreferenceChangedDelegate?.Invoke();
        }

        /// <summary>
        /// Delegate called when loading operations begin (platform-specific).
        /// </summary>
        public Action LoadingStartedDelegate;
        
        /// <summary>
        /// Delegate called when loading operations complete (platform-specific).
        /// </summary>
        public Action LoadingCompletedDelegate;

        /// <summary>
        /// Starts monitoring the preference storage for external changes.
        /// </summary>
        public abstract void BeginMonitoring();
        
        /// <summary>
        /// Stops monitoring the preference storage for external changes.
        /// </summary>
        public abstract void EndMonitoring();
        
        /// <summary>
        /// Gets a value indicating whether preference monitoring is currently active.
        /// </summary>
        /// <returns>True if monitoring is active, false otherwise</returns>
        public abstract bool IsMonitoringActive();
    }

#if UNITY_EDITOR_WIN

    /// <summary>
    /// Windows-specific preference storage accessor that monitors the Windows Registry.
    /// Uses registry change notifications to detect external modifications to PlayerPrefs.
    /// </summary>
    public class WindowsPreferenceStorage : PreferenceStorageAccessor
    {
        private WindowsRegistryMonitor registryMonitor;

        /// <summary>
        /// Initializes Windows preference storage with registry monitoring.
        /// </summary>
        /// <param name="pathToPreferences">Registry path to monitor for preferences</param>
        public WindowsPreferenceStorage(string pathToPreferences) : base(pathToPreferences)
        {
            registryMonitor = new WindowsRegistryMonitor(RegistryHive.CurrentUser, preferencesPath);
            registryMonitor.RegistryChanged += HandleRegistryChange;
        }

        /// <summary>
        /// Handles registry change notifications from the monitoring system.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="eventArgs">Event arguments</param>
        private void HandleRegistryChange(object sender, EventArgs eventArgs)
        {
            OnPreferenceChanged();
        }

        /// <summary>
        /// Retrieves preference keys from the Windows Registry, cleaning Unity's key naming scheme.
        /// </summary>
        protected override void RetrieveKeysFromSystem()
        {
            cachedPreferenceKeys = new string[0];

            using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(preferencesPath))
            {
                if (registryKey != null)
                {
                    cachedPreferenceKeys = registryKey.GetValueNames();
                    registryKey.Close();
                }
            }

            // Remove Unity's internal hash suffix from key names (e.g., "MyKey_h3320113488" becomes "MyKey")
            cachedPreferenceKeys = cachedPreferenceKeys.Select(key => 
                key.Substring(0, key.LastIndexOf("_h", StringComparison.Ordinal))).ToArray();

            ConvertEncodingFromAnsiToUtf8();
        }

        /// <summary>
        /// Starts monitoring registry changes for preference modifications.
        /// </summary>
        public override void BeginMonitoring()
        {
            registryMonitor.StartMonitoring();
        }

        /// <summary>
        /// Stops monitoring registry changes.
        /// </summary>
        public override void EndMonitoring()
        {
            registryMonitor.StopMonitoring();
        }

        /// <summary>
        /// Gets the current monitoring state of the registry monitor.
        /// </summary>
        /// <returns>True if registry monitoring is active</returns>
        public override bool IsMonitoringActive()
        {
            return registryMonitor.IsMonitoring;
        }

        /// <summary>
        /// Converts cached preference keys from ANSI encoding to UTF-8.
        /// Addresses Windows Registry encoding issues with international characters.
        /// </summary>
        private void ConvertEncodingFromAnsiToUtf8()
        {
            Encoding utf8Encoding = Encoding.UTF8;
            Encoding ansiEncoding = Encoding.GetEncoding(1252);

            for (int i = 0; i < cachedPreferenceKeys.Length; i++)
            {
                cachedPreferenceKeys[i] = utf8Encoding.GetString(ansiEncoding.GetBytes(cachedPreferenceKeys[i]));
            }
        }
    }

#elif UNITY_EDITOR_LINUX

    /// <summary>
    /// Linux-specific preference storage accessor that monitors Unity's XML preference files.
    /// Uses file system watching to detect changes to the preferences file.
    /// </summary>
    public class LinuxPreferenceStorage : PreferenceStorageAccessor
    {
        private FileSystemWatcher fileSystemWatcher;

        /// <summary>
        /// Initializes Linux preference storage with file system monitoring.
        /// </summary>
        /// <param name="pathToPreferences">Path to Unity's preference file relative to home directory</param>
        public LinuxPreferenceStorage(string pathToPreferences) : base(Path.Combine(Environment.GetEnvironmentVariable("HOME"), pathToPreferences))
        {
            fileSystemWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(preferencesPath),
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite,
                Filter = "prefs"
            };

            fileSystemWatcher.Changed += HandleFileSystemChange;
        }

        /// <summary>
        /// Retrieves preference keys from Unity's XML preference file on Linux.
        /// </summary>
        protected override void RetrieveKeysFromSystem()
        {
            cachedPreferenceKeys = new string[0];

            if (File.Exists(preferencesPath))
            {
                using (XmlReader xmlReader = XmlReader.Create(preferencesPath, new XmlReaderSettings()))
                {
                    XDocument preferencesDocument = XDocument.Load(xmlReader);
                    cachedPreferenceKeys = preferencesDocument.Element("unity_prefs").Elements()
                        .Select(element => element.Attribute("name").Value).ToArray();
                }
            }
        }

        /// <summary>
        /// Starts monitoring the preference file for changes.
        /// </summary>
        public override void BeginMonitoring()
        {
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stops monitoring the preference file for changes.
        /// </summary>
        public override void EndMonitoring()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }

        /// <summary>
        /// Gets the current file monitoring state.
        /// </summary>
        /// <returns>True if file system monitoring is active</returns>
        public override bool IsMonitoringActive()
        {
            return fileSystemWatcher.EnableRaisingEvents;
        }

        /// <summary>
        /// Handles file system change notifications for the preference file.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="eventArgs">File system event arguments</param>
        private void HandleFileSystemChange(object sender, FileSystemEventArgs eventArgs)
        {
            OnPreferenceChanged();
        }
    }

#elif UNITY_EDITOR_OSX

    /// <summary>
    /// macOS-specific preference storage accessor that monitors property list files.
    /// Uses file system watching and plutil command for reading macOS preference files.
    /// Handles macOS-specific file replacement behavior during preference updates.
    /// </summary>
    public class MacOSPreferenceStorage : PreferenceStorageAccessor
    {
        private FileSystemWatcher fileSystemWatcher;
        private DirectoryInfo preferencesDirectory;
        private string basePreferenceFileName;

        /// <summary>
        /// Initializes macOS preference storage with file system monitoring.
        /// </summary>
        /// <param name="pathToPreferences">Path to property list file relative to home directory</param>
        public MacOSPreferenceStorage(string pathToPreferences) : base(Path.Combine(Environment.GetEnvironmentVariable("HOME"), pathToPreferences))
        {
            preferencesDirectory = new DirectoryInfo(Path.GetDirectoryName(preferencesPath));
            basePreferenceFileName = Path.GetFileNameWithoutExtension(preferencesPath);

            fileSystemWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(preferencesPath),
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(preferencesPath)
            };

            // macOS replaces the entire file rather than modifying it in place
            fileSystemWatcher.Created += HandleFileSystemChange;
        }

        /// <summary>
        /// Retrieves preference keys from macOS property list using plutil command.
        /// Handles temporary file detection to avoid incomplete read operations.
        /// </summary>
        protected override void RetrieveKeysFromSystem()
        {
            // Check for temporary files that indicate an incomplete write operation
            foreach (FileInfo fileInfo in preferencesDirectory.GetFiles())
            {
                if (fileInfo.FullName.Contains(basePreferenceFileName) && !fileInfo.FullName.EndsWith(".plist"))
                {
                    LoadingStartedDelegate?.Invoke();
                    return;
                }
            }
            LoadingCompletedDelegate?.Invoke();

            cachedPreferenceKeys = new string[0];

            if (File.Exists(preferencesPath))
            {
                // Escape special characters for shell command execution
                string escapedPath = preferencesPath
                    .Replace("\"", "\\\"")
                    .Replace("'", "\\'")
                    .Replace("`", "\\`");

                var processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    FileName = "plutil",
                    Arguments = string.Format(@"-p '{0}'", escapedPath),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                string standardOutput = string.Empty;
                string errorOutput = string.Empty;

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.OutputDataReceived += (sender, eventArgs) => standardOutput += eventArgs.Data + "\n";
                    process.ErrorDataReceived += (sender, eventArgs) => errorOutput += eventArgs.Data + "\n";

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }

                // Parse plutil output to extract preference key names
                MatchCollection keyMatches = Regex.Matches(standardOutput, @"(?: "")(.*)(?:"" =>.*)");
                cachedPreferenceKeys = keyMatches.Cast<Match>()
                    .Select(match => match.Groups[1].Value).ToArray();
            }
        }

        /// <summary>
        /// Starts monitoring the property list file for changes.
        /// </summary>
        public override void BeginMonitoring()
        {
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stops monitoring the property list file for changes.
        /// </summary>
        public override void EndMonitoring()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }

        /// <summary>
        /// Gets the current file monitoring state.
        /// </summary>
        /// <returns>True if file system monitoring is active</returns>
        public override bool IsMonitoringActive()
        {
            return fileSystemWatcher.EnableRaisingEvents;
        }

        /// <summary>
        /// Handles file system change notifications for the property list file.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="eventArgs">File system event arguments</param>
        private void HandleFileSystemChange(object sender, FileSystemEventArgs eventArgs)
        {
            OnPreferenceChanged();
        }
    }
#endif
}
