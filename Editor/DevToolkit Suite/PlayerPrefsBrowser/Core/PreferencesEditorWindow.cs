using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DevToolkitSuite.PreferenceEditor.Core;
using DevToolkitSuite.PreferenceEditor.Dialogs;
using DevToolkitSuite.PreferenceEditor.UI;
using DevToolkitSuite.PreferenceEditor.UI.Extensions;

#if (UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX)
using System.Text;
using System.Globalization;
#endif

namespace DevToolkitSuite.PreferenceEditor
{
    public class PreferencesEditorWindow : EditorWindow
    {
#region ErrorValues
        private readonly int ERROR_VALUE_INT = int.MinValue;
        private readonly string ERROR_VALUE_STR = "<epuTool_error_24072017>";
        #endregion //ErrorValues

        private enum PreferencesEntrySortOrder
        {
            None = 0,
            Asscending = 1,
            Descending = 2
        }

        private static string pathToPrefs = String.Empty;
        private static string platformPathPrefix = @"~";

        private string[] userDef;
        private string[] unityDef;
        private bool showSystemGroup = false;

        private PreferencesEntrySortOrder sortOrder = PreferencesEntrySortOrder.None;

        private SerializedObject serializedObject;
        private ReorderableList userDefList;
        private ReorderableList unityDefList;

        private SerializedProperty[] userDefListCache = new SerializedProperty[0];

        private PreferenceDataContainer prefEntryHolder;

        private Vector2 scrollPos;
        private float relSpliterPos;
        private bool moveSplitterPos = false;

        private PreferenceStorageAccessor entryAccessor;

        private MySearchField searchfield;
        private string searchTxt;
        private int loadingSpinnerFrame;

        private bool updateView = false;
        private bool monitoring = false;
        private bool showLoadingIndicatorOverlay = false;

        private readonly List<InputValidator> prefKeyValidatorList = new List<InputValidator>()
        {
            new InputValidator(InputValidator.ErrorType.Error, @"Invalid character detected. Only letters, numbers, space and ,.;:<>_|!§$%&/()=?*+~#-]+$ are allowed", @"(^$)|(^[a-zA-Z0-9 ,.;:<>_|!§$%&/()=?*+~#-]+$)"),
            new InputValidator(InputValidator.ErrorType.Warning, @"The given key already exist. The existing entry would be overwritten!", (key) => { return !PlayerPrefs.HasKey(key); })
        };

#if UNITY_EDITOR_LINUX
        private readonly char[] invalidFilenameChars = { '"', '\\', '*', '/', ':', '<', '>', '?', '|' };
#elif UNITY_EDITOR_OSX
        private readonly char[] invalidFilenameChars = { '$', '%', '&', '\\', '/', ':', '<', '>', '|', '~' };
#endif

        // Modern UI Styling
        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle modernBoxStyle;
        private static GUIStyle pathBoxStyle;
        private static GUIStyle modernToolbarStyle;
        private static GUIStyle gradientButtonStyle;
        private static Texture2D gradientTex;
        [MenuItem("Tools/DevToolkit Suite/PlayerPrefs Browser", false, 11)]
        static void ShowWindow()
        {
            PreferencesEditorWindow window = EditorWindow.GetWindow<PreferencesEditorWindow>(false, "PlayerPrefs Browser");
            window.minSize = new Vector2(350.0f, 400.0f);
            window.name = "PlayerPrefs Browser";

            //window.titleContent = EditorGUIUtility.IconContent("SettingsIcon"); // Icon

            window.Show();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR_WIN
            pathToPrefs = @"SOFTWARE\Unity\UnityEditor\" + PlayerSettings.companyName + @"\" + PlayerSettings.productName;
            platformPathPrefix = @"<CurrentUser>";
            entryAccessor = new WindowsPreferenceStorage(pathToPrefs);
#elif UNITY_EDITOR_OSX
            pathToPrefs = @"Library/Preferences/unity." + MakeValidFileName(PlayerSettings.companyName) + "." + MakeValidFileName(PlayerSettings.productName) + ".plist";
            entryAccessor = new MacOSPreferenceStorage(pathToPrefs);
            entryAccessor.LoadingStartedDelegate = () => { showLoadingIndicatorOverlay = true; };
            entryAccessor.LoadingCompletedDelegate = () => { showLoadingIndicatorOverlay = false; };
#elif UNITY_EDITOR_LINUX
            pathToPrefs = @".config/unity3d/" + MakeValidFileName(PlayerSettings.companyName) + "/" + MakeValidFileName(PlayerSettings.productName) + "/prefs";
            entryAccessor = new LinuxPreferenceStorage(pathToPrefs);
#endif
            entryAccessor.PreferenceChangedDelegate = () => { updateView = true; };

            monitoring = EditorPrefs.GetBool("BGTools.PlayerPrefsEditor.WatchingForChanges", true);
            if(monitoring)
                entryAccessor.BeginMonitoring();

            sortOrder = (PreferencesEntrySortOrder) EditorPrefs.GetInt("BGTools.PlayerPrefsEditor.SortOrder", 0);
            searchfield = new MySearchField();
            searchfield.DropdownSelectionDelegate = () => { PrepareData(); };

            // Fix for serialisation issue of static fields
            if (userDefList == null)
            {
                InitReorderedList();
                PrepareData();
            }
            
            InitModernStyles();
        }

        private void InitModernStyles()
        {
            if (headerLabelStyle == null)
            {
                headerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                    margin = new RectOffset(0, 0, 10, 15)
                };
            }

            if (sectionLabelStyle == null)
            {
                sectionLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.7f, 0.9f, 1f) },
                    margin = new RectOffset(5, 0, 8, 5)
                };
            }

            if (modernBoxStyle == null)
            {
                modernBoxStyle = new GUIStyle()
                {
                    padding = new RectOffset(15, 15, 12, 12),
                    margin = new RectOffset(5, 5, 5, 8),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.25f, 0.25f, 0.25f, 0.8f)),
                        textColor = Color.white 
                    },
                    border = new RectOffset(0, 0, 0, 0)
                };
            }

            if (pathBoxStyle == null)
            {
                pathBoxStyle = new GUIStyle()
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(5, 5, 2, 5),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.2f, 0.2f, 0.2f, 0.9f))
                    },
                    border = new RectOffset(0, 0, 0, 0)
                };
            }

            if (modernToolbarStyle == null)
            {
                modernToolbarStyle = new GUIStyle(EditorStyles.toolbar)
                {
                    normal = { 
                        background = CreateSolidTexture(new Color(0.3f, 0.3f, 0.3f, 0.95f))
                    }
                };
            }

            if (gradientButtonStyle == null)
            {
                gradientButtonStyle = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                    hover = { textColor = Color.white },
                    active = { textColor = Color.white },
                    focused = { textColor = Color.white },
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 6, 6),
                    margin = new RectOffset(3, 3, 3, 3)
                };
            }

            if (gradientTex == null)
            {
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));
            }
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private Texture2D CreateHorizontalGradient(int width, int height, Color left, Color right)
        {
            Texture2D tex = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                Color color = Color.Lerp(left, right, t);
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            return tex;
        }

        // Handel view updates for monitored changes
        // Necessary to avoid main thread access issue
        private void Update()
        {
            if (showLoadingIndicatorOverlay)
            {
                loadingSpinnerFrame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
                PrepareData();
                Repaint();
            }

            if (updateView)
            {
                updateView = false;
                PrepareData();
                Repaint();
            }
        }

        private void OnDisable()
        {
            entryAccessor.EndMonitoring();
        }

        private void InitReorderedList()
        {
            if (prefEntryHolder == null)
            {
                var tmp = Resources.FindObjectsOfTypeAll<PreferenceDataContainer>();
                if (tmp.Length > 0)
                {
                    prefEntryHolder = tmp[0];
                }
                else
                {
                    prefEntryHolder = ScriptableObject.CreateInstance<PreferenceDataContainer>();
                }
            }

            if (serializedObject == null)
            {
                serializedObject = new SerializedObject(prefEntryHolder);
            }

            userDefList = new ReorderableList(serializedObject, serializedObject.FindProperty("userDefinedEntries"), false, true, true, true);
            unityDefList = new ReorderableList(serializedObject, serializedObject.FindProperty("systemDefinedEntries"), false, true, false, false);

            relSpliterPos = EditorPrefs.GetFloat("BGTools.PlayerPrefsEditor.RelativeSpliterPosition", 100 / position.width);

            userDefList.drawHeaderCallback = (Rect rect) =>
            {
                InitModernStyles();
                EditorGUI.LabelField(rect, "👤 User Defined Preferences", sectionLabelStyle ?? EditorStyles.boldLabel);
            };
            userDefList.drawElementBackgroundCallback = OnDrawElementBackgroundCallback;
            userDefList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = GetUserDefListElementAtIndex(index, userDefList.serializedProperty);

                SerializedProperty key = element.FindPropertyRelative("key");
                SerializedProperty type = element.FindPropertyRelative("typeSelection");

                SerializedProperty value;

                // Load only necessary type
                switch ((PreferenceEntryData.PreferenceDataType)type.enumValueIndex)
                {
                    case PreferenceEntryData.PreferenceDataType.Float:
                        value = element.FindPropertyRelative("floatValue");
                        break;
                    case PreferenceEntryData.PreferenceDataType.Integer:
                        value = element.FindPropertyRelative("intValue");
                        break;
                    case PreferenceEntryData.PreferenceDataType.String:
                        value = element.FindPropertyRelative("strValue");
                        break;
                    default:
                        value = element.FindPropertyRelative("This should never happen");
                        break;
                }

                float spliterPos = relSpliterPos * rect.width;
                rect.y += 2;

                EditorGUI.BeginChangeCheck();
                string prefKeyName = key.stringValue;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, spliterPos - 1, EditorGUIUtility.singleLineHeight), new GUIContent(prefKeyName, prefKeyName));
                GUI.enabled = false;
                EditorGUI.EnumPopup(new Rect(rect.x + spliterPos + 1, rect.y, 60, EditorGUIUtility.singleLineHeight), (PreferenceEntryData.PreferenceDataType)type.enumValueIndex);
                GUI.enabled = !showLoadingIndicatorOverlay;
                switch ((PreferenceEntryData.PreferenceDataType)type.enumValueIndex)
                {
                    case PreferenceEntryData.PreferenceDataType.Float:
                        EditorGUI.DelayedFloatField(new Rect(rect.x + spliterPos + 62, rect.y, rect.width - spliterPos - 60, EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                    case PreferenceEntryData.PreferenceDataType.Integer:
                        EditorGUI.DelayedIntField(new Rect(rect.x + spliterPos + 62, rect.y, rect.width - spliterPos - 60, EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                    case PreferenceEntryData.PreferenceDataType.String:
                        EditorGUI.DelayedTextField(new Rect(rect.x + spliterPos + 62, rect.y, rect.width - spliterPos - 60, EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    entryAccessor.IgnoreNextChangeNotification();

                    switch ((PreferenceEntryData.PreferenceDataType)type.enumValueIndex)
                    {
                        case PreferenceEntryData.PreferenceDataType.Float:
                            PlayerPrefs.SetFloat(key.stringValue, value.floatValue);
                            break;
                        case PreferenceEntryData.PreferenceDataType.Integer:
                            PlayerPrefs.SetInt(key.stringValue, value.intValue);
                            break;
                        case PreferenceEntryData.PreferenceDataType.String:
                            PlayerPrefs.SetString(key.stringValue, value.stringValue);
                            break;
                    }

                    PlayerPrefs.Save();
                }
            };
            userDefList.onRemoveCallback = (ReorderableList l) =>
            {
                userDefList.ReleaseKeyboardFocus();
                unityDefList.ReleaseKeyboardFocus();

                string prefKey = l.serializedProperty.GetArrayElementAtIndex(l.index).FindPropertyRelative("key").stringValue;
                if (EditorUtility.DisplayDialog("Warning!", $"Are you sure you want to delete this entry from PlayerPrefs?\n\nEntry: {prefKey}", "Yes", "No"))
                {
                    entryAccessor.IgnoreNextChangeNotification();

                    PlayerPrefs.DeleteKey(prefKey);
                    PlayerPrefs.Save();

                    ReorderableList.defaultBehaviours.DoRemoveButton(l);
                    PrepareData();
                    GUIUtility.ExitGUI();
                }
            };
            userDefList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
            {
                var menu = new GenericMenu();
                foreach (PreferenceEntryData.PreferenceDataType type in Enum.GetValues(typeof(PreferenceEntryData.PreferenceDataType)))
                {
                    menu.AddItem(new GUIContent(type.ToString()), false, () =>
                    {
                        TextInputDialog.ShowInputDialog("Create new property", "Key for the new property:", prefKeyValidatorList, (key) => {

                            entryAccessor.IgnoreNextChangeNotification();

                            switch (type)
                            {
                                case PreferenceEntryData.PreferenceDataType.Float:
                                    PlayerPrefs.SetFloat(key, 0.0f);

                                    break;
                                case PreferenceEntryData.PreferenceDataType.Integer:
                                    PlayerPrefs.SetInt(key, 0);

                                    break;
                                case PreferenceEntryData.PreferenceDataType.String:
                                    PlayerPrefs.SetString(key, string.Empty);

                                    break;
                            }
                            PlayerPrefs.Save();

                            PrepareData();

                            Focus();
                        }, this);

                    });
                }
                menu.ShowAsContext();
            };

            unityDefList.drawElementBackgroundCallback = OnDrawElementBackgroundCallback;
            unityDefList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = unityDefList.serializedProperty.GetArrayElementAtIndex(index);
                SerializedProperty key = element.FindPropertyRelative("key");
                SerializedProperty type = element.FindPropertyRelative("typeSelection");

                SerializedProperty value;

                // Load only necessary type
                switch ((PreferenceEntryData.PreferenceDataType)type.enumValueIndex)
                {
                    case PreferenceEntryData.PreferenceDataType.Float:
                        value = element.FindPropertyRelative("floatValue");
                        break;
                    case PreferenceEntryData.PreferenceDataType.Integer:
                        value = element.FindPropertyRelative("intValue");
                        break;
                    case PreferenceEntryData.PreferenceDataType.String:
                        value = element.FindPropertyRelative("strValue");
                        break;
                    default:
                        value = element.FindPropertyRelative("This should never happen");
                        break;
                }

                float spliterPos = relSpliterPos * rect.width;
                rect.y += 2;

                GUI.enabled = false;
                string prefKeyName = key.stringValue;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, spliterPos - 1, EditorGUIUtility.singleLineHeight), new GUIContent(prefKeyName, prefKeyName));
                EditorGUI.EnumPopup(new Rect(rect.x + spliterPos + 1, rect.y, 60, EditorGUIUtility.singleLineHeight), (PreferenceEntryData.PreferenceDataType)type.enumValueIndex);

                switch ((PreferenceEntryData.PreferenceDataType)type.enumValueIndex)
                {
                    case PreferenceEntryData.PreferenceDataType.Float:
                        EditorGUI.DelayedFloatField(new Rect(rect.x + spliterPos + 62, rect.y, rect.width - spliterPos - 60, EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                    case PreferenceEntryData.PreferenceDataType.Integer:
                        EditorGUI.DelayedIntField(new Rect(rect.x + spliterPos + 62, rect.y, rect.width - spliterPos - 60, EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                    case PreferenceEntryData.PreferenceDataType.String:
                        EditorGUI.DelayedTextField(new Rect(rect.x + spliterPos + 62, rect.y, rect.width - spliterPos - 60, EditorGUIUtility.singleLineHeight), value, GUIContent.none);
                        break;
                }
                GUI.enabled = !showLoadingIndicatorOverlay;
            };
            unityDefList.drawHeaderCallback = (Rect rect) =>
            {
                InitModernStyles();
                EditorGUI.LabelField(rect, "🔧 Unity System Preferences", sectionLabelStyle ?? EditorStyles.boldLabel);
            };
        }

        private void OnDrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (Event.current.type == EventType.Repaint)
            {
                ReorderableList.defaultBehaviours.elementBackground.Draw(rect, false, isActive, isActive, isFocused);
            }

            Rect spliterRect = new Rect(rect.x + relSpliterPos * rect.width, rect.y, 2, rect.height);
            EditorGUIUtility.AddCursorRect(spliterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && spliterRect.Contains(Event.current.mousePosition))
            {
                moveSplitterPos = true;
            }
            if(moveSplitterPos)
            {
                if (Event.current.mousePosition.x > 100 && Event.current.mousePosition.x<rect.width - 120)
                {
                    relSpliterPos = Event.current.mousePosition.x / rect.width;
                    Repaint();
                }
            }
            if (Event.current.type == EventType.MouseUp)
            {
                moveSplitterPos = false;
                EditorPrefs.SetFloat("BGTools.PlayerPrefsEditor.RelativeSpliterPosition", relSpliterPos);
            }
        }

        void OnGUI()
        {
            // Need to catch 'Stack empty' error on linux
            try
            {
                InitModernStyles();

                if (showLoadingIndicatorOverlay)
                {
                    GUI.enabled = false;
                }

                Color defaultColor = GUI.contentColor;
                if (!EditorGUIUtility.isProSkin)
                {
                    GUI.contentColor = UIStyleManager.ColorPalette.PrimaryDark;
                }

                // Beautiful header with gradient background
                Rect headerRect = new Rect(0, 0, position.width, 50);
                GUI.DrawTexture(headerRect, gradientTex, ScaleMode.StretchToFill);
                EditorGUILayout.Space(15);
                EditorGUILayout.LabelField("🗃️ PlayerPrefs Browser", headerLabelStyle);
                EditorGUILayout.Space(10);

                GUILayout.BeginVertical();

                // Modern Toolbar Section
                EditorGUILayout.BeginVertical(modernBoxStyle);
                EditorGUILayout.LabelField("🔍 Search & Controls", sectionLabelStyle);
                EditorGUILayout.Space(3);

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                searchTxt = EditorGUILayout.TextField(searchTxt, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    PrepareData(false);
                }

                GUILayout.FlexibleSpace();

                EditorGUIUtility.SetIconSize(new Vector2(14.0f, 14.0f));

                GUIContent sortOrderContent;
                switch (sortOrder)
                {
                    case PreferencesEntrySortOrder.Asscending:
                        sortOrderContent = new GUIContent(ResourceManager.SortAscendingIcon, "Ascending sorted");
                        break;
                    case PreferencesEntrySortOrder.Descending:
                        sortOrderContent = new GUIContent(ResourceManager.SortDescendingIcon, "Descending sorted");
                        break;
                    case PreferencesEntrySortOrder.None:
                    default:
                        sortOrderContent = new GUIContent(ResourceManager.SortNeutralIcon, "Not sorted");
                        break;
                }

                if (GUILayout.Button(sortOrderContent, EditorStyles.toolbarButton))
                {
                    
                    sortOrder++;
                    if((int) sortOrder >= Enum.GetValues(typeof(PreferencesEntrySortOrder)).Length)
                    {
                        sortOrder = 0;
                    }
                    EditorPrefs.SetInt("BGTools.PlayerPrefsEditor.SortOrder", (int) sortOrder);
                    PrepareData(false);
                }

                GUIContent watcherContent = (entryAccessor.IsMonitoringActive()) ? new GUIContent(ResourceManager.MonitoringActiveIcon, "Watching changes") : new GUIContent(ResourceManager.MonitoringInactiveIcon, "Not watching changes");
                if (GUILayout.Button(watcherContent, EditorStyles.toolbarButton))
                {
                    monitoring = !monitoring;

                    EditorPrefs.SetBool("BGTools.PlayerPrefsEditor.WatchingForChanges", monitoring);

                    if (monitoring)
                        entryAccessor.BeginMonitoring();
                    else
                        entryAccessor.EndMonitoring();

                    Repaint();
                }
                if (GUILayout.Button(new GUIContent(ResourceManager.RefreshIcon, "Refresh"), EditorStyles.toolbarButton))
                {
                    PlayerPrefs.Save();
                    PrepareData();
                }
                if (GUILayout.Button(new GUIContent(ResourceManager.DeleteIcon, "Delete all"), EditorStyles.toolbarButton))
                {
                    if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete ALL entries from PlayerPrefs?\n\nUse with caution! Unity defined keys are affected too.", "Yes", "No"))
                    {
                        PlayerPrefs.DeleteAll();
                        PrepareData();
                        GUIUtility.ExitGUI();
                    }
                }
                EditorGUIUtility.SetIconSize(new Vector2(0.0f, 0.0f));

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                // Path Information Section  
                EditorGUILayout.BeginVertical(pathBoxStyle);
                EditorGUILayout.LabelField("📂 Storage Location", sectionLabelStyle);
                EditorGUILayout.Space(3);

                GUILayout.BeginHorizontal();
                GUILayout.Box(ResourceManager.GetPlatformIcon(), UIStyleManager.IconDisplayStyle);
                GUILayout.TextField(platformPathPrefix + Path.DirectorySeparatorChar + pathToPrefs, GUILayout.MinWidth(200));
                GUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                // Content Section
                EditorGUILayout.BeginVertical(modernBoxStyle);
                EditorGUILayout.LabelField("⚙️ Player Preferences", sectionLabelStyle);
                EditorGUILayout.Space(3);

                scrollPos = GUILayout.BeginScrollView(scrollPos);
                serializedObject.Update();
                userDefList.DoLayoutList();
                serializedObject.ApplyModifiedProperties();

                GUILayout.FlexibleSpace();

                showSystemGroup = EditorGUILayout.Foldout(showSystemGroup, new GUIContent("🔧 Show System Preferences"));
                if (showSystemGroup)
                {
                    unityDefList.DoLayoutList();
                }
                GUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                GUILayout.EndVertical();

                GUI.enabled = true;

                if (showLoadingIndicatorOverlay)
                {
                    GUILayout.BeginArea(new Rect(position.size.x * 0.5f - 30, position.size.y * 0.5f - 25, 60, 50), GUI.skin.box);
                    GUILayout.FlexibleSpace();

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Box(ResourceManager.LoadingSpinnerFrames[loadingSpinnerFrame], UIStyleManager.IconDisplayStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Loading");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    GUILayout.FlexibleSpace();
                    GUILayout.EndArea();
                }

                GUI.contentColor = defaultColor;
            }
            catch (InvalidOperationException)
            { }
        }

        private void PrepareData(bool reloadKeys = true)
        {
            prefEntryHolder.ClearAllEntries();

            LoadKeys(out userDef, out unityDef, reloadKeys);

            CreatePrefEntries(userDef, ref prefEntryHolder.userDefinedEntries);
            CreatePrefEntries(unityDef, ref prefEntryHolder.systemDefinedEntries);

            // Clear cache
            userDefListCache = new SerializedProperty[prefEntryHolder.userDefinedEntries.Count];
        }

        private void CreatePrefEntries(string[] keySource, ref List<PreferenceEntryData> listDest)
        {
            if (!string.IsNullOrEmpty(searchTxt) && searchfield.SearchMode == MySearchField.SearchModePreferencesEditorWindow.Key)
            {
                keySource = keySource.Where((keyEntry) => keyEntry.ToLower().Contains(searchTxt.ToLower())).ToArray();
            }

            foreach (string key in keySource)
            {
                var entry = new PreferenceEntryData();
                entry.key = key;

                string s = PlayerPrefs.GetString(key, ERROR_VALUE_STR);

                if (s != ERROR_VALUE_STR)
                {
                    entry.strValue = s;
                    entry.typeSelection = PreferenceEntryData.PreferenceDataType.String;
                    listDest.Add(entry);
                    continue;
                }

                float f = PlayerPrefs.GetFloat(key, float.NaN);
                if (!float.IsNaN(f))
                {
                    entry.floatValue = f;
                    entry.typeSelection = PreferenceEntryData.PreferenceDataType.Float;
                    listDest.Add(entry);
                    continue;
                }

                int i = PlayerPrefs.GetInt(key, ERROR_VALUE_INT);
                if (i != ERROR_VALUE_INT)
                {
                    entry.intValue = i;
                    entry.typeSelection = PreferenceEntryData.PreferenceDataType.Integer;
                    listDest.Add(entry);
                    continue;
                }
            }

            if (!string.IsNullOrEmpty(searchTxt) && searchfield.SearchMode == MySearchField.SearchModePreferencesEditorWindow.Value)
            {
                listDest = listDest.Where((preferenceEntry) => preferenceEntry.GetValueAsString().ToLower().Contains(searchTxt.ToLower())).ToList<PreferenceEntryData>();
            }

            switch(sortOrder)
            {
                case PreferencesEntrySortOrder.Asscending:
                    listDest.Sort((PreferenceEntryData x, PreferenceEntryData y) => { return x.key.CompareTo(y.key); });
                    break;
                case PreferencesEntrySortOrder.Descending:
                    listDest.Sort((PreferenceEntryData x, PreferenceEntryData y) => { return y.key.CompareTo(x.key); });
                    break;
            }
        }

        private void LoadKeys(out string[] userDef, out string[] unityDef, bool reloadKeys)
        {
            string[] keys = entryAccessor.GetPreferenceKeys(reloadKeys);

            //keys.ToList().ForEach( e => { Debug.Log(e); } );

            // Seperate keys int unity defined and user defined
            Dictionary<bool, List<string>> groups = keys
                .GroupBy( (key) => key.StartsWith("unity.") || key.StartsWith("UnityGraphicsQuality") )
                .ToDictionary( (g) => g.Key, (g) => g.ToList() );

            unityDef = (groups.ContainsKey(true)) ? groups[true].ToArray() : new string[0];
            userDef = (groups.ContainsKey(false)) ? groups[false].ToArray() : new string[0];
        }

        private SerializedProperty GetUserDefListElementAtIndex(int index, SerializedProperty ListProperty)
        {
            UnityEngine.Assertions.Assert.IsTrue(ListProperty.isArray, "Given 'ListProperts' is not type of array");

            if (userDefListCache[index] == null)
            {
                userDefListCache[index] = ListProperty.GetArrayElementAtIndex(index);
            }
            return userDefListCache[index];
        }

#if (UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX)
        private string MakeValidFileName(string unsafeFileName)
        {
            string normalizedFileName = unsafeFileName.Trim().Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            // We need to use a TextElementEmumerator in order to support UTF16 characters that may take up more than one char(case 1169358)
            TextElementEnumerator charEnum = StringInfo.GetTextElementEnumerator(normalizedFileName);
            while (charEnum.MoveNext())
            {
                string c = charEnum.GetTextElement();
                if (c.Length == 1 && invalidFilenameChars.Contains(c[0]))
                {
                    stringBuilder.Append('_');
                    continue;
                }
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c, 0);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
#endif
        }

    public class MySearchField : SearchField
    {
        public enum SearchModePreferencesEditorWindow { Key, Value }

        public SearchModePreferencesEditorWindow SearchMode { get; private set; }

        public Action DropdownSelectionDelegate;

        public new string OnGUI(
            Rect rect,
            string text,
            GUIStyle style,
            GUIStyle cancelButtonStyle,
            GUIStyle emptyCancelButtonStyle)
        {
            style.padding.left = 17;
            Rect ContextMenuRect = new Rect(rect.x, rect.y, 10, rect.height);

            // Add interactive area
            EditorGUIUtility.AddCursorRect(ContextMenuRect, MouseCursor.Text);
            if (Event.current.type == EventType.MouseDown && ContextMenuRect.Contains(Event.current.mousePosition))
            {
                void OnDropdownSelection(object parameter)
                {
                    SearchMode = (SearchModePreferencesEditorWindow) Enum.Parse(typeof(SearchModePreferencesEditorWindow), parameter.ToString());
                    DropdownSelectionDelegate();
                }

                GenericMenu menu = new GenericMenu();
                foreach(SearchModePreferencesEditorWindow EnumIt in Enum.GetValues(typeof(SearchModePreferencesEditorWindow)))
                {
                    String EnumName = Enum.GetName(typeof(SearchModePreferencesEditorWindow), EnumIt);
                    menu.AddItem(new GUIContent(EnumName), SearchMode == EnumIt, OnDropdownSelection, EnumName);
                }

                menu.DropDown(rect);
            }

            // Render original search field
            String result = base.OnGUI(rect, text, style, cancelButtonStyle, emptyCancelButtonStyle);

            // Render additional images
            GUIStyle ContexMenuOverlayStyle = GUIStyle.none;
            ContexMenuOverlayStyle.contentOffset = new Vector2(9, 5);
            GUI.Box(new Rect(rect.x, rect.y, 5, 5), EditorGUIUtility.IconContent("d_ProfilerTimelineDigDownArrow@2x"), ContexMenuOverlayStyle);

            if (!HasFocus() && String.IsNullOrEmpty(text))
            {
                GUI.enabled = false;
                GUI.Label(new Rect(rect.x + 14, rect.y, 40, rect.height), Enum.GetName(typeof(SearchModePreferencesEditorWindow), SearchMode));
                GUI.enabled = true;
            }
            ContexMenuOverlayStyle.contentOffset = new Vector2();
            return result;
        }

        public new string OnToolbarGUI(string text, params GUILayoutOption[] options) => this.OnToolbarGUI(GUILayoutUtility.GetRect(29f, 200f, 18f, 18f, EditorStyles.toolbarSearchField, options), text);
        public new string OnToolbarGUI(Rect rect, string text) => this.OnGUI(rect, text, EditorStyles.toolbarSearchField, EditorStyles.toolbarButton, EditorStyles.toolbarButton);
    }
}
