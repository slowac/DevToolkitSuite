namespace DevToolkitSuite.PreferenceEditor.Core
{
    /// <summary>
    /// Data container representing a single preference entry with type information and value storage.
    /// Supports Unity's PlayerPrefs data types: String, Int, and Float.
    /// Designed for serialization compatibility with Unity's Inspector and Editor systems.
    /// </summary>
    [System.Serializable]
    public class PreferenceEntryData
    {
        /// <summary>
        /// Enumeration of supported PlayerPrefs data types for preference values.
        /// </summary>
        public enum PreferenceDataType
        {
            /// <summary>String text value</summary>
            String = 0,
            /// <summary>32-bit signed integer value</summary>
            Integer = 1,
            /// <summary>Single-precision floating point value</summary>
            Float = 2
        }

        /// <summary>
        /// The data type classification for this preference entry.
        /// Determines which value field contains the actual data.
        /// </summary>
        public PreferenceDataType typeSelection;
        
        /// <summary>
        /// Unique identifier key for this preference entry.
        /// Must match the key used in Unity's PlayerPrefs system.
        /// </summary>
        public string key;

        // Separate value fields are required for Unity's serialization system
        // to properly handle different data types in the Inspector interface

        /// <summary>
        /// String value storage field - used when typeSelection is String.
        /// </summary>
        public string strValue;
        
        /// <summary>
        /// Integer value storage field - used when typeSelection is Integer.
        /// </summary>
        public int intValue;
        
        /// <summary>
        /// Float value storage field - used when typeSelection is Float.
        /// </summary>
        public float floatValue;

        /// <summary>
        /// Retrieves the current value as a string representation regardless of the underlying data type.
        /// Useful for display purposes and generic value handling.
        /// </summary>
        /// <returns>String representation of the stored value</returns>
        public string GetValueAsString()
        {
            switch(typeSelection)
            {
                case PreferenceDataType.String:
                    return strValue;
                case PreferenceDataType.Integer:
                    return intValue.ToString();
                case PreferenceDataType.Float:
                    return floatValue.ToString();
                default:
                    return string.Empty;
            }
        }
    }
}