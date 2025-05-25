using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace DevToolkitSuite.AudioTrim.Editor
{
    public class BatchProcessClipsWindow : EditorWindow
    {
        private void OnEnable()
        {
            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));
        }
        private List<AudioClip> selectedClips = new();
        private Dictionary<AudioClip, float> originalPeaks = new();
        private Dictionary<string, AudioClip> originalClipMap = new();

        private float volume = 1f;
        private float silenceThreshold = 0.01f;

        private GUIStyle gradientButtonStyle;
        private Texture2D gradientTex;

        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle boxStyle;

        private static GUIStyle modernBoxStyle;
        private static GUIStyle waveformBoxStyle;
        private static GUIStyle separatorStyle;
        private Vector2 scrollPosition = Vector2.zero;

        [MenuItem("Tools/DevToolkit Suite/AudioClip/Batch Process Clips",false,48)]
        public static void ShowWindow()
        {
            var window = GetWindow<BatchProcessClipsWindow>("Batch Process Clips");
            window.minSize = new Vector2(420, 350);
        }

        private void OnGUI()
        {
            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            // Beautiful header with gradient background
            Rect headerRect = new Rect(0, 0, position.width, 50);
            GUI.DrawTexture(headerRect, gradientTex, ScaleMode.StretchToFill);
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🎛️ Batch Process Clips", headerLabelStyle);
            EditorGUILayout.Space(10);

            // Begin scroll view for all content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Selected Clips Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🎵 Selected Audio Clips", sectionLabelStyle);
            DrawClipList();
            EditorGUILayout.EndVertical();

            // Processing Operations Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("⚙️ Processing Operations", sectionLabelStyle);
            DrawClipProcessingButtons();
            EditorGUILayout.EndVertical();

            // Volume & Effects Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🔊 Volume & Effects", sectionLabelStyle);
            DrawVolumeAndSilenceControls();
            EditorGUILayout.EndVertical();

            // End scroll view
            EditorGUILayout.EndScrollView();
        }

        private void InitStyles()
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
                modernBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(15, 15, 12, 12),
                    margin = new RectOffset(5, 5, 5, 8),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.25f, 0.25f, 0.25f, 0.8f)),
                        textColor = Color.white 
                    },
                    border = new RectOffset(1, 1, 1, 1)
                };
            }

            if (waveformBoxStyle == null)
            {
                waveformBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(5, 5, 5, 8),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.1f, 0.1f, 0.1f, 0.9f))
                    },
                    border = new RectOffset(2, 2, 2, 2)
                };
            }

            if (separatorStyle == null)
            {
                separatorStyle = new GUIStyle()
                {
                    normal = { background = CreateSolidTexture(new Color(0.4f, 0.4f, 0.4f, 0.5f)) },
                    margin = new RectOffset(10, 10, 5, 5),
                    fixedHeight = 1
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 10, 10)
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

        private void DrawClipList()
        {
            if (selectedClips == null)
                selectedClips = new();

            EditorGUILayout.Space(5);
            
            // Display selected clips with improved styling
            if (selectedClips.Count > 0)
            {
                for (int i = 0; i < selectedClips.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(30));
                    selectedClips[i] = (AudioClip)EditorGUILayout.ObjectField(selectedClips[i], typeof(AudioClip), false);
                    
                    // Stylish remove button
                    if (GUILayout.Button("✖", GUILayout.Width(30), GUILayout.Height(18)))
                    {
                        selectedClips.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    if (i < selectedClips.Count - 1)
                        EditorGUILayout.Space(2);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("📂 No audio clips selected. Click 'Add Clip' to begin.", MessageType.Info);
            }

            EditorGUILayout.Space(8);
            
            // Add clip button with improved styling
            if (GradientButton("➕ Add New Clip", gradientTex, gradientButtonStyle))
            {
                if (selectedClips == null)
                    selectedClips = new();

                selectedClips.Add(null);
            }

            // Show clip count
            if (selectedClips.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"📊 Total Clips: {selectedClips.Count}", EditorStyles.miniLabel);
            }
        }

        private void DrawClipProcessingButtons()
        {
            if (selectedClips == null || selectedClips.Count == 0 || selectedClips.TrueForAll(c => c == null))
            {
                EditorGUILayout.HelpBox("🎧 Please add at least one valid AudioClip to use processing operations.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🔧 Audio Normalization", EditorStyles.miniLabel);
            EditorGUILayout.Space(3);

            // Normalize and denormalize buttons in a row
            EditorGUILayout.BeginHorizontal();
            if (GradientButton("📈 Normalize Audio", gradientTex, gradientButtonStyle))
            {
                foreach (var clip in selectedClips)
                {
                    if (clip == null) continue;
                    BackupOriginalIfNeeded(clip);
                    var normalized = NormalizeClip(clip);
                    SaveClipAsWav(normalized, "normalized");
                }
            }

            if (GradientButton("📉 Restore Original", gradientTex, gradientButtonStyle))
            {
                foreach (var clip in selectedClips)
                {
                    if (clip == null) continue;
                    var denormalized = DeNormalizeClip(clip);
                    SaveClipAsWav(denormalized, "restored");
                }
            }
            EditorGUILayout.EndHorizontal();

            // Info about processing
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("💡 Processed files will be saved to Assets/ProcessedAudio/", EditorStyles.miniLabel);
        }

        private void DrawVolumeAndSilenceControls()
        {
            if (selectedClips == null || selectedClips.Count == 0 || selectedClips.TrueForAll(c => c == null))
            {
                EditorGUILayout.HelpBox("🎚️ Please assign at least one valid AudioClip to use volume and effects controls.", MessageType.Warning);
                return;
            }

            // Volume Control Section
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🔊 Volume Adjustment", EditorStyles.miniLabel);
            EditorGUILayout.Space(3);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Volume:", GUILayout.Width(60));
            volume = EditorGUILayout.Slider(volume, 0f, 1f);
            EditorGUILayout.LabelField($"{Mathf.RoundToInt(volume * 100)}%", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            if (GradientButton("🎚️ Apply Volume to All Clips", gradientTex, gradientButtonStyle))
            {
                foreach (var clip in selectedClips)
                {
                    if (clip == null) continue;
                    BackupOriginalIfNeeded(clip);
                    var adjusted = SetVolumeToClip(clip, volume);
                    SaveClipAsWav(adjusted, $"vol_{Mathf.RoundToInt(volume * 100)}");
                }
            }

            // Separator
            EditorGUILayout.Space(8);
            GUILayout.Box("", separatorStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(8);

            // Silence Trimming Section
            EditorGUILayout.LabelField("✂️ Silence Trimming", EditorStyles.miniLabel);
            EditorGUILayout.Space(3);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Threshold:", GUILayout.Width(70));
            silenceThreshold = EditorGUILayout.Slider(silenceThreshold, 0f, 1f);
            EditorGUILayout.LabelField($"{Mathf.RoundToInt(silenceThreshold * 100)}%", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            if (GradientButton("✂️ Trim Start/End Silence", gradientTex, gradientButtonStyle))
            {
                ApplyTrim();
            }

            // Separator
            EditorGUILayout.Space(8);
            GUILayout.Box("", separatorStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(8);

            // Reset Section
            EditorGUILayout.LabelField("🔄 Reset Controls", EditorStyles.miniLabel);
            EditorGUILayout.Space(3);
            
            if (GradientButton("🔄 Reset All Clips to Original", gradientTex, gradientButtonStyle))
            {
                foreach (var clip in selectedClips)
                {
                    if (clip == null) continue;
                    string baseName = clip.name.Split('_')[0];
                    if (originalClipMap.TryGetValue(baseName, out var original))
                    {
                        SaveClipAsWav(original, "reset");
                    }
                }
            }

            // Progress info
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("💾 All processed files are automatically saved to the project", EditorStyles.miniLabel);
        }

        private bool GradientButton(string text, Texture2D hoverTex, GUIStyle style, params GUILayoutOption[] options)
        {
            GUIContent content = new GUIContent(text);
            Rect rect = GUILayoutUtility.GetRect(content, style, options);
            Event e = Event.current;

            bool isHovering = rect.Contains(e.mousePosition);
            bool isClicked = false;

            if (e.type == EventType.Repaint)
            {
                // Hover ise gradient çiz, değilse default buton
                if (isHovering)
                {
                    GUI.DrawTexture(rect, hoverTex, ScaleMode.StretchToFill);
                    GUI.Label(rect, content, style); // sadece yazıyı çiz
                }
                else
                {
                    GUI.Button(rect, content, GUI.skin.button); // Unity'nin kendi stiliyle çiz
                }
            }

            // Cursor
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            // Click algılama
            if (e.type == EventType.MouseDown && isHovering && e.button == 0)
            {
                isClicked = true;
                GUI.FocusControl(null);
                e.Use();
            }

            return isClicked;
        }

        private void BackupOriginalIfNeeded(AudioClip clip)
        {
            if (clip == null) return;
            string baseName = clip.name.Split('_')[0];
            if (originalClipMap.ContainsKey(baseName)) return;

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            AudioClip backup = AudioClip.Create(baseName + "_backup", clip.samples, clip.channels, clip.frequency, false);
            backup.SetData(samples, 0);
            originalClipMap[baseName] = backup;
        }

        private AudioClip NormalizeClip(AudioClip clip)
        {
            if (clip == null) return null;
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            float max = 0f;
            foreach (var s in samples)
                max = Mathf.Max(max, Mathf.Abs(s));

            if (max == 0f) return clip;

            for (int i = 0; i < samples.Length; i++)
                samples[i] /= max;

            AudioClip newClip = AudioClip.Create(clip.name + "_normalized", clip.samples, clip.channels, clip.frequency, false);
            newClip.SetData(samples, 0);
            return newClip;
        }

        private AudioClip DeNormalizeClip(AudioClip clip)
        {
            if (clip == null || !originalPeaks.TryGetValue(clip, out float peak)) return clip;

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            for (int i = 0; i < samples.Length; i++)
                samples[i] *= peak;

            AudioClip newClip = AudioClip.Create(clip.name.Replace("_normalized", "_restored"), clip.samples, clip.channels, clip.frequency, false);
            newClip.SetData(samples, 0);
            return newClip;
        }

        private AudioClip SetVolumeToClip(AudioClip clip, float vol)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            for (int i = 0; i < samples.Length; i++)
                samples[i] *= vol;

            AudioClip newClip = AudioClip.Create(clip.name + "_vol" + Mathf.RoundToInt(vol * 100), clip.samples, clip.channels, clip.frequency, false);
            newClip.SetData(samples, 0);
            return newClip;
        }

        private void ApplyTrim()
        {
            foreach (var clip in selectedClips)
            {
                if (clip == null) continue;
                BackupOriginalIfNeeded(clip);
                var trimmed = TrimSilence(clip, silenceThreshold);
                SaveClipAsWav(trimmed, "trimmed");
            }
        }

        private AudioClip TrimSilence(AudioClip clip, float threshold)
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            int start = 0;
            int end = samples.Length - 1;

            while (start < samples.Length && Mathf.Abs(samples[start]) < threshold) start++;
            while (end > start && Mathf.Abs(samples[end]) < threshold) end--;

            int length = end - start + 1;
            float[] trimmed = new float[length];
            System.Array.Copy(samples, start, trimmed, 0, length);

            int newSampleCount = length / clip.channels;
            AudioClip newClip = AudioClip.Create(clip.name + "_trimmed", newSampleCount, clip.channels, clip.frequency, false);
            newClip.SetData(trimmed, 0);
            return newClip;
        }

        private void SaveClipAsWav(AudioClip clip, string suffix)
        {
            if (clip == null) return;

            string folderPath = "Assets/ProcessedAudio";
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets", "ProcessedAudio");

            string path = $"{folderPath}/{clip.name}_{suffix}.wav";
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            byte[] wav = ConvertToWav(samples, clip.channels, clip.frequency);
            File.WriteAllBytes(path, wav);
            AssetDatabase.Refresh();
        }

        private byte[] ConvertToWav(float[] samples, int channels, int freq)
        {
            int byteCount = samples.Length * 2;
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + byteCount);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(freq);
            writer.Write(freq * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(byteCount);

            foreach (var s in samples)
                writer.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));

            return stream.ToArray();
        }

        private Texture2D CreateHorizontalGradient(int width, int height, Color left, Color right)
        {
            Texture2D tex = new Texture2D(width, height);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            for (int x = 0; x < width; x++)
            {
                Color col = Color.Lerp(left, right, x / (float)(width - 1));
                for (int y = 0; y < height; y++) tex.SetPixel(x, y, col);
            }
            tex.Apply();
            return tex;
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}