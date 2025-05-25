using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace DevToolkitSuite.AudioTrim.Editor
{
    public class WaveformEditorWindow : EditorWindow
    {
        private AudioClip clip;
        private AudioClip previousClip; // Track previous clip to detect changes
        private Texture2D waveformTexture;
        private float[] processedSamples;
        private bool needsWaveformUpdate = true;

        private float trimStart = 0f;
        private float trimEnd = 1f;

        private float fadeInDuration = 0f;
        private float fadeOutDuration = 0f;

        private AnimationCurve fadeInCurve = AnimationCurve.Linear(0, 0, 1, 1);
        private AnimationCurve fadeOutCurve = AnimationCurve.Linear(0, 1, 1, 0);

        private bool normalize = false;
        private float volume = 1f;

        private Color waveformColor = new Color(0f, 1f, 1f);
        private AudioSource audioSource;

        private GUIStyle gradientButtonStyle;
        private Texture2D gradientTex;
        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle modernBoxStyle;
        private static GUIStyle waveformBoxStyle;
        private static GUIStyle separatorStyle;

        private Vector2 scrollPosition = Vector2.zero;

        [MenuItem("Tools/DevToolkit Suite/AudioClip/Waveform Editor",false,49)]
        public static void ShowWindow()
        {
            var window = GetWindow<WaveformEditorWindow>("Waveform Editor");
            window.minSize = new Vector2(500, 450);
        }

        private void OnEnable()
        {
            // Create temporary GameObject for audio playback
            GameObject tempGO = new GameObject("WaveformEditorAudio");
            tempGO.hideFlags = HideFlags.HideAndDontSave;
            audioSource = tempGO.AddComponent<AudioSource>();

            InitStyles();
            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));
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

        private void OnDisable()
        {
            if (audioSource != null && audioSource.gameObject != null)
                DestroyImmediate(audioSource.gameObject);
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
            EditorGUILayout.LabelField("🎵 Waveform Editor", headerLabelStyle);
            EditorGUILayout.Space(10);

            // Begin scroll view for all content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUI.BeginChangeCheck();
            
            // Audio Clip Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📁 Audio Source", sectionLabelStyle);
            EditorGUILayout.Space(3);
            clip = (AudioClip)EditorGUILayout.ObjectField("", clip, typeof(AudioClip), false, GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            // Explicitly check for clip changes
            if (clip != previousClip)
            {
                previousClip = clip;
                needsWaveformUpdate = true;
                processedSamples = null; // Reset processed samples
                waveformTexture = null; // Reset texture
                Repaint();
            }

            if (clip == null)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.BeginVertical(modernBoxStyle);
                EditorGUILayout.HelpBox("🎧 Please assign an AudioClip to begin editing", MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView(); // End scroll view early if no clip
                return;
            }

            // Check if clip changed (this is now redundant but keeping for safety)
            if (EditorGUI.EndChangeCheck())
            {
                needsWaveformUpdate = true;
                // Force a repaint to ensure waveform appears immediately
                Repaint();
            }

            // Waveform Preview Section
            EditorGUILayout.BeginVertical(waveformBoxStyle);
            EditorGUILayout.LabelField("🌊 Waveform Preview", sectionLabelStyle);
            DrawWaveformPreview();
            EditorGUILayout.EndVertical();

            // Track parameter changes
            EditorGUI.BeginChangeCheck();

            // Trim Controls Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("✂️ Trim Controls", sectionLabelStyle);
            EditorGUILayout.Space(5);
            trimStart = EditorGUILayout.Slider("Start Position", trimStart, 0f, 0.5f);
            trimEnd = EditorGUILayout.Slider("End Position", trimEnd, 0.5f, 1f);
            EditorGUILayout.EndVertical();

            // Fade Effects Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🌅 Fade Effects", sectionLabelStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Fade In", EditorStyles.miniLabel);
            fadeInDuration = EditorGUILayout.Slider(fadeInDuration, 0f, 5f);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Fade Out", EditorStyles.miniLabel);
            fadeOutDuration = EditorGUILayout.Slider(fadeOutDuration, 0f, 5f);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Fade Curves", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("In", EditorStyles.miniLabel);
            fadeInCurve = EditorGUILayout.CurveField(fadeInCurve, GUILayout.Height(40));
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Out", EditorStyles.miniLabel);
            fadeOutCurve = EditorGUILayout.CurveField(fadeOutCurve, GUILayout.Height(40));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Audio Processing Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("⚙️ Audio Processing", sectionLabelStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            normalize = EditorGUILayout.Toggle("🔧 Normalize", normalize);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            volume = EditorGUILayout.Slider("🔊 Volume", volume, 0f, 1f);
            EditorGUILayout.EndVertical();

            // If any parameter changed, update waveform
            if (EditorGUI.EndChangeCheck())
            {
                needsWaveformUpdate = true;
                Repaint();
            }

            // Action Buttons Section
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🎮 Actions", sectionLabelStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GradientButton("🔄 Reset", gradientTex, gradientButtonStyle)) ResetSettings();
            
            // Export button (only show if we have processed audio)
            if (processedSamples != null && processedSamples.Length > 0)
            {
                if (GradientButton("💾 Export", gradientTex, gradientButtonStyle)) ExportProcessedAudio();
            }
            
            // Show different buttons based on audio state
            if (audioSource != null && audioSource.isPlaying)
            {
                if (GradientButton("⏹ Stop", gradientTex, gradientButtonStyle)) StopClip();
            }
            else
            {
                if (GradientButton("▶ Play", gradientTex, gradientButtonStyle)) PlayClip();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // End scroll view
            EditorGUILayout.EndScrollView();
            
            // Repaint when audio state might change
            if (audioSource != null)
            {
                Repaint();
            }
        }

        private void DrawWaveformPreview()
        {
            if (clip == null) return;

            GUILayout.Label("Waveform Preview", EditorStyles.boldLabel);
            Rect waveformRect = GUILayoutUtility.GetRect(position.width - 20, 100);

            // Ensure we have a reasonable rect size before generating texture
            int textureWidth = Mathf.Max((int)waveformRect.width, 400); // Minimum 400px width
            int textureHeight = Mathf.Max((int)waveformRect.height, 100); // Minimum 100px height

            // Update waveform if needed
            bool shouldUpdate = needsWaveformUpdate || waveformTexture == null || processedSamples == null;
            if (shouldUpdate)
            {
                ProcessAudioSamples();
                if (processedSamples != null && processedSamples.Length > 0)
                {
                    waveformTexture = GenerateWaveformTexture(processedSamples, clip.channels, textureWidth, textureHeight, waveformColor);
                }
                else
                {
                }
                needsWaveformUpdate = false;
            }

            if (waveformTexture != null)
            {
                GUI.DrawTexture(waveformRect, waveformTexture);
                
                // Draw trim indicators
                DrawTrimIndicators(waveformRect);
            }
            else
            {
                // Draw placeholder if waveform couldn't be generated
                EditorGUI.DrawRect(waveformRect, new Color(0.15f, 0.15f, 0.15f, 1f));
                GUI.Label(waveformRect, "Loading waveform...", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void ProcessAudioSamples()
        {
            if (clip == null) return;

            // Get original samples
            float[] originalSamples = new float[clip.samples * clip.channels];
            clip.GetData(originalSamples, 0);

            // Apply trim
            int startSample = Mathf.RoundToInt(trimStart * clip.samples) * clip.channels;
            int endSample = Mathf.RoundToInt(trimEnd * clip.samples) * clip.channels;
            int trimmedLength = endSample - startSample;
            
            float[] trimmedSamples = new float[trimmedLength];
            System.Array.Copy(originalSamples, startSample, trimmedSamples, 0, trimmedLength);

            // Apply fades
            ApplyFades(trimmedSamples, clip.channels, clip.frequency);

            // Apply normalize
            if (normalize)
            {
                NormalizeSamples(trimmedSamples);
            }

            // Apply volume
            for (int i = 0; i < trimmedSamples.Length; i++)
            {
                trimmedSamples[i] *= volume;
            }

            processedSamples = trimmedSamples;
        }

        private void ApplyFades(float[] samples, int channels, int frequency)
        {
            int fadeInSamples = Mathf.RoundToInt(fadeInDuration * frequency) * channels;
            int fadeOutSamples = Mathf.RoundToInt(fadeOutDuration * frequency) * channels;

            // Apply fade in
            for (int i = 0; i < fadeInSamples && i < samples.Length; i += channels)
            {
                float t = (float)i / fadeInSamples;
                float fadeMultiplier = fadeInCurve.Evaluate(t);
                
                for (int ch = 0; ch < channels; ch++)
                {
                    if (i + ch < samples.Length)
                        samples[i + ch] *= fadeMultiplier;
                }
            }

            // Apply fade out
            for (int i = samples.Length - fadeOutSamples; i < samples.Length; i += channels)
            {
                if (i < 0) continue;
                
                float t = (float)(i - (samples.Length - fadeOutSamples)) / fadeOutSamples;
                float fadeMultiplier = fadeOutCurve.Evaluate(t);
                
                for (int ch = 0; ch < channels; ch++)
                {
                    if (i + ch < samples.Length)
                        samples[i + ch] *= fadeMultiplier;
                }
            }
        }

        private void NormalizeSamples(float[] samples)
        {
            float maxAmplitude = 0f;
            foreach (float sample in samples)
            {
                maxAmplitude = Mathf.Max(maxAmplitude, Mathf.Abs(sample));
            }

            if (maxAmplitude > 0f)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] /= maxAmplitude;
                }
            }
        }

        private void DrawTrimIndicators(Rect waveformRect)
        {
            // Draw trim start line
            float startX = waveformRect.x + (trimStart * waveformRect.width);
            EditorGUI.DrawRect(new Rect(startX, waveformRect.y, 2, waveformRect.height), Color.red);
            
            // Draw trim end line
            float endX = waveformRect.x + (trimEnd * waveformRect.width);
            EditorGUI.DrawRect(new Rect(endX, waveformRect.y, 2, waveformRect.height), Color.red);
            
            // Draw fade indicators if applicable
            if (fadeInDuration > 0f)
            {
                float fadeInWidth = (fadeInDuration / clip.length) * waveformRect.width * (trimEnd - trimStart);
                EditorGUI.DrawRect(new Rect(startX, waveformRect.y, fadeInWidth, 5), Color.green);
            }
            
            if (fadeOutDuration > 0f)
            {
                float fadeOutWidth = (fadeOutDuration / clip.length) * waveformRect.width * (trimEnd - trimStart);
                EditorGUI.DrawRect(new Rect(endX - fadeOutWidth, waveformRect.y + waveformRect.height - 5, fadeOutWidth, 5), Color.yellow);
            }
        }

        private Texture2D GenerateWaveformTexture(float[] samples, int channels, int width, int height, Color color)
        {
            int packSize = (samples.Length / width) + 1;
            float[] waveform = new float[width];

            for (int i = 0; i < width; i++)
            {
                int start = i * packSize;
                float sum = 0;
                for (int j = 0; j < packSize && start + j < samples.Length; j++)
                {
                    sum += Mathf.Abs(samples[start + j]);
                }
                waveform[i] = sum / packSize;
            }

            Texture2D tex = new Texture2D(width, height);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color bg = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark gray background
            Color[] pixels = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    pixels[y * width + x] = bg;
                }
            }

            for (int x = 0; x < width; x++)
            {
                int h = (int)(waveform[x] * height);
                for (int y = (height / 2) - h; y < (height / 2) + h; y++)
                {
                    if (y >= 0 && y < height)
                        pixels[y * width + x] = color;
                }
            }

            tex.SetPixels(pixels);
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

        private void ResetSettings()
        {
            trimStart = 0f;
            trimEnd = 1f;
            fadeInDuration = 0f;
            fadeOutDuration = 0f;
            fadeInCurve = AnimationCurve.Linear(0, 0, 1, 1);
            fadeOutCurve = AnimationCurve.Linear(0, 1, 1, 0);
            normalize = false;
            volume = 1f;
            needsWaveformUpdate = true;
            Repaint();
        }

        private void PlayClip()
        {
            if (clip == null || audioSource == null) return;

            // Stop any currently playing audio
            audioSource.Stop();

            // Create a temporary AudioClip with processed samples
            if (processedSamples != null && processedSamples.Length > 0)
            {
                int sampleCount = processedSamples.Length / clip.channels;
                AudioClip processedClip = AudioClip.Create("ProcessedPreview", sampleCount, clip.channels, clip.frequency, false);
                processedClip.SetData(processedSamples, 0);
                
                audioSource.clip = processedClip;
                audioSource.Play();
                
                Debug.Log($"Playing processed audio: {sampleCount} samples, {clip.channels} channels");
            }
            else
            {
                // Fallback to original clip
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("Playing original audio clip");
            }
        }

        private void StopClip()
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                Debug.Log("Audio stopped");
            }
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
                // Show gradient on hover, default button otherwise
                if (isHovering)
                {
                    GUI.DrawTexture(rect, hoverTex, ScaleMode.StretchToFill);
                    GUI.Label(rect, content, style); // just draw the text
                }
                else
                {
                    GUI.Button(rect, content, GUI.skin.button); // Unity's default style
                }
            }

            // Cursor
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            // Click detection
            if (e.type == EventType.MouseDown && isHovering && e.button == 0)
            {
                isClicked = true;
                GUI.FocusControl(null);
                e.Use();
            }

            return isClicked;
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

        private void ExportProcessedAudio()
        {
            if (processedSamples == null || processedSamples.Length == 0 || clip == null)
            {
                Debug.LogWarning("No processed audio samples to export.");
                return;
            }

            string defaultName = $"{clip.name}_edited";
            string filePath = EditorUtility.SaveFilePanel("Save Processed Audio", "", defaultName, "wav");
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Export cancelled.");
                return;
            }

            try
            {
                byte[] wavData = ConvertToWav(processedSamples, clip.channels, clip.frequency);
                File.WriteAllBytes(filePath, wavData);
                Debug.Log($"Processed audio exported successfully to: {filePath}");
                
                // Show success message in UI
                EditorUtility.DisplayDialog("Export Complete", 
                    $"Audio exported successfully!\n\nFile: {Path.GetFileName(filePath)}", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error exporting processed audio: {ex.Message}");
                EditorUtility.DisplayDialog("Export Failed", 
                    $"Failed to export audio:\n{ex.Message}", "OK");
            }
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
    }
}
