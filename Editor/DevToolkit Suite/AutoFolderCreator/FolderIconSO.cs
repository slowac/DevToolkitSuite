using System.Collections.Generic;
using UnityEngine;

namespace DevToolkit_Suite
{
    [CreateAssetMenu(fileName = "FolderIcon", menuName = "DevToolkit Suite/Folder Icon SO", order = 1)]
    public class FolderIconSO : ScriptableObject
    {
        public Texture2D icon;
        public List<string> folderNames = new List<string>();

        private void OnValidate()
        {
            // Gerekirse dictionary yeniden oluþturulabilir
        }
    }
}
