using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevToolkitSuite.PreferenceEditor.UI
{
    /// <summary>
    /// Centralized style management system for the Preference Editor interface.
    /// Provides consistent visual elements, colors, and UI components across the editor.
    /// </summary>
    public static class UIStyleManager
    {
        #region Color Definitions
        
        /// <summary>
        /// Collection of predefined colors used throughout the preference editor interface.
        /// </summary>
        public static class ColorPalette 
        {
            public static readonly Color PrimaryDark = new Color(0.09f, 0.09f, 0.09f);
            public static readonly Color SecondaryLight = new Color(0.65f, 0.65f, 0.65f);
            public static readonly Color ErrorRed = new Color(1.00f, 0.00f, 0.00f);
            public static readonly Color WarningYellow = new Color(1.00f, 1.00f, 0.00f);
            public static readonly Color InfoBlue = new Color(0.00f, 0.63f, 0.99f);
        }
        
        #endregion

        #region Texture Management System
        
        /// <summary>
        /// Cache dictionary for storing generated textures to improve performance.
        /// </summary>
        private static readonly Dictionary<long, Texture2D> textureCache = new Dictionary<long, Texture2D>();

        /// <summary>
        /// Retrieves or creates a solid color texture from the cache.
        /// Uses RGBA color value as cache key for efficient texture reuse.
        /// </summary>
        /// <param name="colorRGBA">32-bit RGBA color value packed as long integer</param>
        /// <returns>Generated texture with the specified color</returns>
        public static Texture2D GetSolidColorTexture(long colorRGBA)
        {
            if (textureCache.ContainsKey(colorRGBA) && textureCache[colorRGBA] != null)
                return textureCache[colorRGBA];

            Color32 targetColor = UnpackColorFromLong(colorRGBA);

            var generatedTexture = new Texture2D(4, 4);
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    generatedTexture.SetPixel(x, y, targetColor);
                }
            }
            
            generatedTexture.Apply();
            generatedTexture.Compress(true);

            textureCache[colorRGBA] = generatedTexture;
            return generatedTexture;
        }

        /// <summary>
        /// Unpacks RGBA color components from a packed long integer value.
        /// </summary>
        /// <param name="packedColor">Packed color value in RGBA format</param>
        /// <returns>Color32 structure with extracted RGBA components</returns>
        private static Color32 UnpackColorFromLong(long packedColor)
        {
            byte red = (byte)((packedColor & 0xff000000) >> 24);
            byte green = (byte)((packedColor & 0xff0000) >> 16);
            byte blue = (byte)((packedColor & 0xff00) >> 8);
            byte alpha = (byte)(packedColor & 0xff);

            return new Color32(red, green, blue, alpha);
        }
        
        #endregion

        #region UI Style Definitions

        private static GUIStyle horizontalDividerStyle;
        
        /// <summary>
        /// Gets the cached horizontal divider style, creating it if necessary.
        /// Adapts to Unity's current skin (Pro/Personal) for consistent appearance.
        /// </summary>
        private static GUIStyle HorizontalDividerStyle
        {
            get
            {
                if (horizontalDividerStyle == null)
                {
                    horizontalDividerStyle = new GUIStyle
                    {
                        alignment = TextAnchor.MiddleCenter,
                        stretchWidth = true,
                        fixedHeight = 1,
                        margin = new RectOffset(20, 20, 5, 5)
                    };
                    
                    // Adapt background color based on Unity skin
                    long dividerColor = EditorGUIUtility.isProSkin ? 0xb5b5b5ff : 0x000000ff;
                    horizontalDividerStyle.normal.background = GetSolidColorTexture(dividerColor);
                }
                return horizontalDividerStyle;
            }
        }

        /// <summary>
        /// Renders a horizontal divider line in the current layout.
        /// Useful for visually separating sections in the editor interface.
        /// </summary>
        public static void DrawHorizontalDivider()
        {
            GUILayout.Label("", HorizontalDividerStyle);
        }

        private static GUIStyle iconDisplayStyle;
        
        /// <summary>
        /// Gets the standardized icon display style for consistent icon presentation.
        /// </summary>
        public static GUIStyle IconDisplayStyle
        {
            get
            {
                if (iconDisplayStyle == null)
                {
                    iconDisplayStyle = new GUIStyle
                    {
                        fixedWidth = 15.0f,
                        fixedHeight = 15.0f,
                        margin = new RectOffset(2, 2, 2, 2)
                    };
                }
                return iconDisplayStyle;
            }
        }

        private static GUIStyle compactButtonStyle;
        
        /// <summary>
        /// Gets the compact button style for small interactive elements.
        /// Provides consistent sizing and spacing for toolbar buttons.
        /// </summary>
        public static GUIStyle CompactButtonStyle
        {
            get
            {
                if (compactButtonStyle == null)
                {
                    compactButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fixedWidth = 15.0f,
                        fixedHeight = 15.0f,
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(2, 2, 2, 2)
                    };
                }
                return compactButtonStyle;
            }
        }
        
        #endregion
    }
}