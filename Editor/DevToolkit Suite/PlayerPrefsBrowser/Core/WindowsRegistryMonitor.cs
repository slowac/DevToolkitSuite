#if UNITY_EDITOR_WIN

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DevToolkitSuite.PreferenceEditor.Core
{
    /// <summary>
    /// Windows Registry monitoring service that provides real-time notifications of registry key changes.
    /// Uses Win32 API to efficiently monitor specific registry locations for modifications.
    /// Supports filtering different types of registry changes and operates on a background thread.
    /// </summary>
    public class WindowsRegistryMonitor : IDisposable
    {
        #region Win32 API Declarations

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint options, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, RegistryChangeFilter dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(IntPtr hKey);

        private const int KEY_QUERY_VALUE = 0x0001;
        private const int KEY_NOTIFY = 0x0010;
        private const int STANDARD_RIGHTS_READ = 0x00020000;

        private static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));
        private static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
        private static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
        private static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));
        private static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked((int)0x80000004));
        private static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));
        private static readonly IntPtr HKEY_DYN_DATA = new IntPtr(unchecked((int)0x80000006));

        #endregion

        #region Events and Event Handlers

        /// <summary>
        /// Event triggered when the monitored registry key or its values change.
        /// Provides notification for any modifications detected within the monitoring scope.
        /// </summary>
        public event EventHandler RegistryChanged;

        /// <summary>
        /// Raises the RegistryChanged event to notify subscribers of registry modifications.
        /// </summary>
        protected virtual void OnRegistryChanged()
        {
            EventHandler handler = RegistryChanged;
            if (handler != null)
                handler(this, null);
        }

        /// <summary>
        /// Event triggered when errors occur during registry monitoring operations.
        /// Provides access to exception details for error handling and diagnostics.
        /// </summary>
        public event ErrorEventHandler Error;

        /// <summary>
        /// Raises the Error event to notify subscribers of monitoring exceptions.
        /// </summary>
        /// <param name="exception">Exception that occurred during monitoring operations</param>
        protected virtual void OnError(Exception exception)
        {
            ErrorEventHandler handler = Error;
            if (handler != null)
                handler(this, new ErrorEventArgs(exception));
        }

        #endregion

        #region Private Fields and State Management

        private IntPtr registryHiveHandle;
        private string registrySubKeyPath;
        private readonly object threadSafetyLock = new object();
        private Thread monitoringThread;
        private bool isDisposed = false;
        private readonly ManualResetEvent terminationEvent = new ManualResetEvent(false);

        private RegistryChangeFilter changeFilter = RegistryChangeFilter.Key | RegistryChangeFilter.Attribute | 
                                                   RegistryChangeFilter.Value | RegistryChangeFilter.Security;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new registry monitor for the specified registry key.
        /// </summary>
        /// <param name="registryKey">Registry key to monitor for changes</param>
        public WindowsRegistryMonitor(RegistryKey registryKey)
        {
            InitializeRegistryTarget(registryKey.Name);
        }

        /// <summary>
        /// Initializes a new registry monitor for the specified registry path.
        /// </summary>
        /// <param name="registryPath">Full registry path to monitor (e.g., "HKEY_CURRENT_USER\\Software\\MyApp")</param>
        /// <exception cref="ArgumentNullException">Thrown when registryPath is null or empty</exception>
        public WindowsRegistryMonitor(string registryPath)
        {
            if (string.IsNullOrEmpty(registryPath))
                throw new ArgumentNullException("registryPath");

            InitializeRegistryTarget(registryPath);
        }

        /// <summary>
        /// Initializes a new registry monitor for the specified hive and subkey combination.
        /// </summary>
        /// <param name="registryHive">Registry hive (root key) to monitor</param>
        /// <param name="subKeyPath">Subkey path within the specified hive</param>
        public WindowsRegistryMonitor(RegistryHive registryHive, string subKeyPath)
        {
            InitializeRegistryTarget(registryHive, subKeyPath);
        }

        #endregion

        #region Resource Management

        /// <summary>
        /// Releases all resources used by the registry monitor and stops monitoring operations.
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
            isDisposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Configuration Properties

        /// <summary>
        /// Gets or sets the registry change notification filter.
        /// Determines which types of registry changes trigger notifications.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when attempting to change filter while monitoring is active</exception>
        public RegistryChangeFilter ChangeNotificationFilter
        {
            get { return changeFilter; }
            set
            {
                lock (threadSafetyLock)
                {
                    if (IsMonitoring)
                        throw new InvalidOperationException("Cannot modify change filter while monitoring is active. Stop monitoring first.");

                    changeFilter = value;
                }
            }
        }

        #endregion

        #region Registry Target Initialization

        /// <summary>
        /// Initializes registry monitoring target using hive and subkey path.
        /// </summary>
        /// <param name="hive">Registry hive to target</param>
        /// <param name="subKeyPath">Subkey path within the hive</param>
        /// <exception cref="InvalidEnumArgumentException">Thrown for unsupported registry hive values</exception>
        private void InitializeRegistryTarget(RegistryHive hive, string subKeyPath)
        {
            switch (hive)
            {
                case RegistryHive.ClassesRoot:
                    registryHiveHandle = HKEY_CLASSES_ROOT;
                    break;
                case RegistryHive.CurrentConfig:
                    registryHiveHandle = HKEY_CURRENT_CONFIG;
                    break;
                case RegistryHive.CurrentUser:
                    registryHiveHandle = HKEY_CURRENT_USER;
                    break;
                case RegistryHive.DynData:
                    registryHiveHandle = HKEY_DYN_DATA;
                    break;
                case RegistryHive.LocalMachine:
                    registryHiveHandle = HKEY_LOCAL_MACHINE;
                    break;
                case RegistryHive.PerformanceData:
                    registryHiveHandle = HKEY_PERFORMANCE_DATA;
                    break;
                case RegistryHive.Users:
                    registryHiveHandle = HKEY_USERS;
                    break;
                default:
                    throw new InvalidEnumArgumentException("hive", (int)hive, typeof(RegistryHive));
            }
            registrySubKeyPath = subKeyPath;
        }

        /// <summary>
        /// Initializes registry monitoring target by parsing a full registry path string.
        /// </summary>
        /// <param name="fullRegistryPath">Complete registry path including hive</param>
        /// <exception cref="ArgumentException">Thrown for unsupported or invalid registry root</exception>
        private void InitializeRegistryTarget(string fullRegistryPath)
        {
            string[] pathComponents = fullRegistryPath.Split('\\');
            switch (pathComponents[0])
            {
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                    registryHiveHandle = HKEY_CLASSES_ROOT;
                    break;
                case "HKEY_CURRENT_USER":
                case "HKCU":
                    registryHiveHandle = HKEY_CURRENT_USER;
                    break;
                case "HKEY_LOCAL_MACHINE":
                case "HKLM":
                    registryHiveHandle = HKEY_LOCAL_MACHINE;
                    break;
                case "HKEY_USERS":
                    registryHiveHandle = HKEY_USERS;
                    break;
                case "HKEY_CURRENT_CONFIG":
                    registryHiveHandle = HKEY_CURRENT_CONFIG;
                    break;
                default:
                    registryHiveHandle = IntPtr.Zero;
                    throw new ArgumentException("Unsupported registry root: " + pathComponents[0], "fullRegistryPath");
            }
            registrySubKeyPath = string.Join("\\", pathComponents, 1, pathComponents.Length - 1);
        }

        #endregion

        #region Monitoring Control

        /// <summary>
        /// Gets a value indicating whether registry monitoring is currently active.
        /// </summary>
        public bool IsMonitoring => monitoringThread != null;

        /// <summary>
        /// Starts registry monitoring on a background thread.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the monitor has been disposed</exception>
        public void StartMonitoring()
        {
            if (isDisposed)
                throw new ObjectDisposedException(null, "Registry monitor has been disposed and cannot be restarted.");

            lock (threadSafetyLock)
            {
                if (!IsMonitoring)
                {
                    terminationEvent.Reset();
                    monitoringThread = new Thread(ExecuteMonitoringLoop) { IsBackground = true };
                    monitoringThread.Start();
                }
            }
        }

        /// <summary>
        /// Stops registry monitoring and waits for the monitoring thread to terminate.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the monitor has been disposed</exception>
        public void StopMonitoring()
        {
            if (isDisposed)
                throw new ObjectDisposedException(null, "Registry monitor has been disposed.");

            lock (threadSafetyLock)
            {
                Thread currentThread = monitoringThread;
                if (currentThread != null)
                {
                    terminationEvent.Set();
                    currentThread.Join();
                }
            }
        }

        #endregion

        #region Background Monitoring Implementation

        /// <summary>
        /// Entry point for the monitoring thread. Handles exceptions and cleanup.
        /// </summary>
        private void ExecuteMonitoringLoop()
        {
            try 
            { 
                RunMonitoringLoop(); 
            }
            catch (Exception exception) 
            { 
                OnError(exception); 
            }
            monitoringThread = null;
        }

        /// <summary>
        /// Main monitoring loop that watches for registry changes and processes notifications.
        /// </summary>
        /// <exception cref="Win32Exception">Thrown when Win32 API calls fail</exception>
        private void RunMonitoringLoop()
        {
            IntPtr registryKeyHandle;
            int result = RegOpenKeyEx(registryHiveHandle, registrySubKeyPath, 0, 
                STANDARD_RIGHTS_READ | KEY_QUERY_VALUE | KEY_NOTIFY, out registryKeyHandle);
            
            if (result != 0)
                throw new Win32Exception(result);

            try
            {
                using (AutoResetEvent changeNotificationEvent = new AutoResetEvent(false))
                {
                    WaitHandle[] waitHandles = new WaitHandle[] { changeNotificationEvent, terminationEvent };
                    
                    while (!terminationEvent.WaitOne(0, true))
                    {
                        result = RegNotifyChangeKeyValue(registryKeyHandle, true, changeFilter, 
                            changeNotificationEvent.SafeWaitHandle.DangerousGetHandle(), true);
                        
                        if (result != 0)
                            throw new Win32Exception(result);

                        if (WaitHandle.WaitAny(waitHandles) == 0)
                            OnRegistryChanged();
                    }
                }
            }
            finally
            {
                if (registryKeyHandle != IntPtr.Zero)
                    RegCloseKey(registryKeyHandle);
            }
        }

        #endregion
    }

    /// <summary>
    /// Flags enumeration for specifying which types of registry changes should trigger notifications.
    /// Can be combined using bitwise OR operations to monitor multiple change types.
    /// </summary>
    [Flags]
    public enum RegistryChangeFilter
    {
        /// <summary>Notify when subkeys are added or deleted</summary>
        Key = 1,
        /// <summary>Notify when key attributes change (such as security descriptors)</summary>
        Attribute = 2,
        /// <summary>Notify when key values change (including add, delete, or modify operations)</summary>
        Value = 4,
        /// <summary>Notify when the key's security descriptor changes</summary>
        Security = 8,
    }
}
#endif
