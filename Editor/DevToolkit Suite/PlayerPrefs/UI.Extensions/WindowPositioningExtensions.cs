using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DevToolkitSuite.PreferenceEditor.UI.Extensions
{
    /// <summary>
    /// Extension methods for Unity EditorWindow positioning and layout management.
    /// Provides utilities for centering windows relative to main editor or other windows.
    /// </summary>
    public static class WindowPositioningExtensions
    {
        /// <summary>
        /// Retrieves all types that derive from the specified base type within the current application domain.
        /// </summary>
        /// <param name="appDomain">Application domain to search within</param>
        /// <param name="baseType">Base type to find derived types for</param>
        /// <returns>Array of types that inherit from the base type</returns>
        private static Type[] GetDerivedTypes(this AppDomain appDomain, Type baseType)
        {
            var derivedTypes = new List<Type>();
            var assemblies = appDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(baseType))
                        derivedTypes.Add(type);
                }
            }
            return derivedTypes.ToArray();
        }

        /// <summary>
        /// Retrieves the screen position of Unity's main editor window with multi-monitor support.
        /// This position can be used as a reference for centering other windows.
        /// </summary>
        /// <param name="referenceWindow">Optional window to use as positioning reference</param>
        /// <returns>Rectangle representing the main editor window's screen position</returns>
        /// <exception cref="MissingMemberException">Thrown when Unity's internal ContainerWindow type cannot be found</exception>
        /// <exception cref="MissingFieldException">Thrown when required internal fields are not accessible</exception>
        /// <exception cref="NotSupportedException">Thrown when the main window cannot be located</exception>
        public static Rect GetMainEditorWindowPosition(EditorWindow referenceWindow = null)
        {
            var containerWindowType = AppDomain.CurrentDomain.GetDerivedTypes(typeof(ScriptableObject))
                .Where(t => t.Name == "ContainerWindow").FirstOrDefault();

            if (containerWindowType == null)
                throw new MissingMemberException("Cannot locate Unity's internal ContainerWindow type. Unity version compatibility issue detected.");

            var showModeField = containerWindowType.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var positionProperty = containerWindowType.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (showModeField == null || positionProperty == null)
                throw new MissingFieldException("Cannot access Unity's internal window fields 'm_ShowMode' or 'position'. Unity version compatibility issue detected.");

            var containerWindows = Resources.FindObjectsOfTypeAll(containerWindowType);
            foreach (var window in containerWindows)
            {
                var displayMode = (int)showModeField.GetValue(window);

                // Look for main editor window (show mode 4 indicates main window)
                if (displayMode == 4)
                {
                    var windowPosition = (Rect)positionProperty.GetValue(window, null);
                    return windowPosition;
                }
            }
            throw new NotSupportedException("Cannot locate Unity's main editor window. This may indicate an unsupported Unity version.");
        }

        /// <summary>
        /// Centers the specified EditorWindow relative to Unity's main editor window.
        /// Maintains the current window size while updating only the position.
        /// </summary>
        /// <param name="window">EditorWindow to center</param>
        public static void CenterOnMainEditor(this EditorWindow window)
        {
            CenterRelativeToWindow(window, null);
        }

        /// <summary>
        /// Centers the specified EditorWindow relative to another EditorWindow or the main editor.
        /// Calculates optimal positioning while respecting current window dimensions.
        /// </summary>
        /// <param name="window">EditorWindow to position</param>
        /// <param name="referenceWindow">Target window to center relative to (null for main editor)</param>
        public static void CenterRelativeToWindow(this EditorWindow window, EditorWindow referenceWindow)
        {
            var targetWindowBounds = GetMainEditorWindowPosition(referenceWindow);

            var currentPosition = window.position;
            float horizontalOffset = (targetWindowBounds.width - currentPosition.width) * 0.5f;
            float verticalOffset = (targetWindowBounds.height - currentPosition.height) * 0.5f;
            
            currentPosition.x = targetWindowBounds.x + horizontalOffset;
            currentPosition.y = targetWindowBounds.y + verticalOffset;
            
            window.position = currentPosition;
        }
    }
}