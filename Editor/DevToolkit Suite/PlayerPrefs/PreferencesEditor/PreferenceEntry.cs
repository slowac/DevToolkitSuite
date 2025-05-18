namespace DevToolkit_Suite.PlayerPrefsEditor
{
    [System.Serializable]
    public class PreferenceEntry
    {
        public enum PlayerPrefTypes
        {
            String = 0,
            Int = 1,
            Float = 2
        }

        public PlayerPrefTypes typeSelection;
        public string key;

        // Need diffrend ones for auto type selection of serilizedProerty
        public string strValue;
        public int intValue;
        public float floatValue;

        public string ValueAsString()
        {
            switch(typeSelection)
            {
                case PlayerPrefTypes.String:
                    return strValue;
                case PlayerPrefTypes.Int:
                    return intValue.ToString();
                case PlayerPrefTypes.Float:
                    return floatValue.ToString();
                default:
                    return string.Empty;
            }
        }
    }
}