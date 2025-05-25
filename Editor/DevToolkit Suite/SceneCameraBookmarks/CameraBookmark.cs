using UnityEngine;

[System.Serializable]
public class CameraBookmark
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
    public KeyCode hotkey = KeyCode.None; // Hotkey for quick navigation
}
