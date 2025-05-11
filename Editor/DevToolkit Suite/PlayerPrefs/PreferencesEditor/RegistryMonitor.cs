#if UNITY_EDITOR_WIN

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace EPU.PlayerPrefsEditor
{
    /// <summary>
    /// Belirtilen kayıt defteri anahtarını izleyen sınıf.
    /// </summary>
    public class RegistryMonitor : IDisposable
    {
        #region P/Invoke

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint options, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, RegChangeNotifyFilter dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

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

        #region Olaylar

        /// <summary>
        /// Belirtilen kayıt defteri anahtarı değiştiğinde tetiklenir.
        /// </summary>
        public event EventHandler RegChanged;

        /// <summary>
        /// <see cref="RegChanged"/> olayını tetikler.
        /// </summary>
        protected virtual void OnRegChanged()
        {
            EventHandler handler = RegChanged;
            if (handler != null)
                handler(this, null);
        }

        /// <summary>
        /// Kayıt defterine erişim sırasında hata oluştuğunda tetiklenir.
        /// </summary>
        public event ErrorEventHandler Error;

        /// <summary>
        /// <see cref="Error"/> olayını tetikler.
        /// </summary>
        /// <param name="e">İzleme sırasında oluşan istisna.</param>
        protected virtual void OnError(Exception e)
        {
            ErrorEventHandler handler = Error;
            if (handler != null)
                handler(this, new ErrorEventArgs(e));
        }

        #endregion

        #region Özel değişkenler

        private IntPtr _registryHive;
        private string _registrySubName;
        private object _threadLock = new object();
        private Thread _thread;
        private bool _disposed = false;
        private ManualResetEvent _eventTerminate = new ManualResetEvent(false);

        private RegChangeNotifyFilter _regFilter = RegChangeNotifyFilter.Key | RegChangeNotifyFilter.Attribute | RegChangeNotifyFilter.Value | RegChangeNotifyFilter.Security;

        #endregion

        /// <summary>
        /// Yeni bir <see cref="RegistryMonitor"/> örneği başlatır.
        /// </summary>
        /// <param name="registryKey">İzlenecek kayıt anahtarı.</param>
        public RegistryMonitor(RegistryKey registryKey)
        {
            InitRegistryKey(registryKey.Name);
        }

        /// <summary>
        /// Yeni bir <see cref="RegistryMonitor"/> örneği başlatır.
        /// </summary>
        /// <param name="name">Kayıt anahtarı adı.</param>
        public RegistryMonitor(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            InitRegistryKey(name);
        }

        /// <summary>
        /// Yeni bir <see cref="RegistryMonitor"/> örneği başlatır.
        /// </summary>
        /// <param name="registryHive">Kayıt defteri kökü.</param>
        /// <param name="subKey">Alt anahtar adı.</param>
        public RegistryMonitor(RegistryHive registryHive, string subKey)
        {
            InitRegistryKey(registryHive, subKey);
        }

        /// <summary>
        /// Bu nesneyi temizler.
        /// </summary>
        public void Dispose()
        {
            Stop();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// İzleme filtresini alır veya ayarlar.
        /// </summary>
        public RegChangeNotifyFilter RegChangeNotifyFilter
        {
            get { return _regFilter; }
            set
            {
                lock (_threadLock)
                {
                    if (IsMonitoring)
                        throw new InvalidOperationException("İzleme işlemi zaten çalışıyor.");

                    _regFilter = value;
                }
            }
        }

        #region Başlatma

        private void InitRegistryKey(RegistryHive hive, string name)
        {
            switch (hive)
            {
                case RegistryHive.ClassesRoot:
                    _registryHive = HKEY_CLASSES_ROOT;
                    break;
                case RegistryHive.CurrentConfig:
                    _registryHive = HKEY_CURRENT_CONFIG;
                    break;
                case RegistryHive.CurrentUser:
                    _registryHive = HKEY_CURRENT_USER;
                    break;
                case RegistryHive.DynData:
                    _registryHive = HKEY_DYN_DATA;
                    break;
                case RegistryHive.LocalMachine:
                    _registryHive = HKEY_LOCAL_MACHINE;
                    break;
                case RegistryHive.PerformanceData:
                    _registryHive = HKEY_PERFORMANCE_DATA;
                    break;
                case RegistryHive.Users:
                    _registryHive = HKEY_USERS;
                    break;
                default:
                    throw new InvalidEnumArgumentException("hive", (int)hive, typeof(RegistryHive));
            }
            _registrySubName = name;
        }

        private void InitRegistryKey(string name)
        {
            string[] nameParts = name.Split('\\');
            switch (nameParts[0])
            {
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                    _registryHive = HKEY_CLASSES_ROOT;
                    break;
                case "HKEY_CURRENT_USER":
                case "HKCU":
                    _registryHive = HKEY_CURRENT_USER;
                    break;
                case "HKEY_LOCAL_MACHINE":
                case "HKLM":
                    _registryHive = HKEY_LOCAL_MACHINE;
                    break;
                case "HKEY_USERS":
                    _registryHive = HKEY_USERS;
                    break;
                case "HKEY_CURRENT_CONFIG":
                    _registryHive = HKEY_CURRENT_CONFIG;
                    break;
                default:
                    _registryHive = IntPtr.Zero;
                    throw new ArgumentException("Desteklenmeyen kayıt defteri kökü: " + nameParts[0], "value");
            }
            _registrySubName = string.Join("\\", nameParts, 1, nameParts.Length - 1);
        }

        #endregion

        /// <summary>
        /// İzleme işlemi aktifse <c>true</c>, değilse <c>false</c> döner.
        /// </summary>
        public bool IsMonitoring => _thread != null;

        /// <summary>
        /// İzlemeyi başlatır.
        /// </summary>
        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, "Bu nesne zaten yok edildi.");

            lock (_threadLock)
            {
                if (!IsMonitoring)
                {
                    _eventTerminate.Reset();
                    _thread = new Thread(MonitorThread) { IsBackground = true };
                    _thread.Start();
                }
            }
        }

        /// <summary>
        /// İzlemeyi durdurur.
        /// </summary>
        public void Stop()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, "Bu nesne zaten yok edildi.");

            lock (_threadLock)
            {
                Thread thread = _thread;
                if (thread != null)
                {
                    _eventTerminate.Set();
                    thread.Join();
                }
            }
        }

        private void MonitorThread()
        {
            try { ThreadLoop(); }
            catch (Exception e) { OnError(e); }
            _thread = null;
        }

        private void ThreadLoop()
        {
            IntPtr registryKey;
            int result = RegOpenKeyEx(_registryHive, _registrySubName, 0, STANDARD_RIGHTS_READ | KEY_QUERY_VALUE | KEY_NOTIFY, out registryKey);
            if (result != 0)
                throw new Win32Exception(result);

            try
            {
                AutoResetEvent _eventNotify = new AutoResetEvent(false);
                WaitHandle[] waitHandles = new WaitHandle[] { _eventNotify, _eventTerminate };
                while (!_eventTerminate.WaitOne(0, true))
                {
                    result = RegNotifyChangeKeyValue(registryKey, true, _regFilter, _eventNotify.SafeWaitHandle.DangerousGetHandle(), true);
                    if (result != 0)
                        throw new Win32Exception(result);

                    if (WaitHandle.WaitAny(waitHandles) == 0)
                        OnRegChanged();
                }
            }
            finally
            {
                if (registryKey != IntPtr.Zero)
                    RegCloseKey(registryKey);
            }
        }
    }

    /// <summary>
    /// <see cref="RegistryMonitor"/> tarafından bildirilen değişiklikler için bildirim filtresi.
    /// </summary>
    [Flags]
    public enum RegChangeNotifyFilter
    {
        /// <summary>Alt anahtar eklenirse veya silinirse bildir.</summary>
        Key = 1,
        /// <summary>Anahtarın özniteliklerinde yapılan değişiklikleri bildir (örneğin güvenlik tanımlayıcısı bilgisi).</summary>
        Attribute = 2,
        /// <summary>Anahtarın değerlerinde yapılan değişiklikleri bildir. Bu; ekleme, silme ya da güncelleme olabilir.</summary>
        Value = 4,
        /// <summary>Anahtarın güvenlik tanımlayıcısındaki değişiklikleri bildir.</summary>
        Security = 8,
    }
}
#endif
