#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Records what the game actually sounds like (the audio listener's mixed output) to a 16-bit WAV, for checking the
    /// music and effects by ear or by analysis when a report says they sound wrong. Editor and development builds only.
    /// Add it next to the AudioListener with <see cref="Record"/>.
    /// </summary>
    public class AudioCapture : MonoBehaviour
    {
        /// <summary>
        /// Records from the first scene when asked at launch: the environment variable CAPTURE_AUDIO=&lt;seconds&gt; (on the
        /// simulator: <c>SIMCTL_CHILD_CAPTURE_AUDIO=25 xcrun simctl launch booted com.filipbusic.rageborn</c>), or a file
        /// persistentDataPath/capture-audio.txt holding the seconds, which is deleted when read. iOS does not hand launch
        /// arguments to the game, so a command line switch did not work. Writes persistentDataPath/audio-capture.wav, and
        /// every sound effect requested, with its time, to audio-capture.txt.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfAsked()
        {
            var request = System.Environment.GetEnvironmentVariable("CAPTURE_AUDIO");
            var trigger = Path.Combine(Application.persistentDataPath, "capture-audio.txt");
            if (string.IsNullOrEmpty(request) && File.Exists(trigger))
            {
                request = File.ReadAllText(trigger).Trim();
                File.Delete(trigger);
            }
            if (string.IsNullOrEmpty(request) || !float.TryParse(request, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var seconds))
                return;
            var capture = Record(seconds, Path.Combine(Application.persistentDataPath, "audio-capture.wav"));
            if (capture == null)
                return;
            Debug.Log($"AudioCapture: recording {seconds} s");
            // It records on the first scene's camera, so it covers that scene (the town, for a loaded game).
            capture.effects = new List<string>();
            Sfx.Played += capture.OnEffect;
        }

        List<string> effects;

        void OnEffect(SoundId id, float volume) =>
            effects?.Add($"{Time.unscaledTime:F2} {id} volume {volume:F2}");

        void OnDestroy() => Sfx.Played -= OnEffect;

        readonly List<float> samples = new List<float>();
        readonly object gate = new object();
        int channels = 2;
        int sampleRate;
        int remaining;
        string path;

        public bool Done { get; private set; }

        /// <summary>Starts recording <paramref name="seconds"/> of output to <paramref name="file"/>.</summary>
        public static AudioCapture Record(float seconds, string file)
        {
            var listener = FindAnyObjectByType<AudioListener>();
            if (listener == null)
                return null;
            var capture = listener.gameObject.AddComponent<AudioCapture>();
            capture.sampleRate = AudioSettings.outputSampleRate;
            capture.remaining = Mathf.CeilToInt(seconds * capture.sampleRate);
            capture.path = file;
            return capture;
        }

        void OnAudioFilterRead(float[] data, int channelCount)
        {
            lock (gate)
            {
                if (remaining <= 0)
                    return;
                channels = channelCount;
                var frames = Mathf.Min(remaining, data.Length / channelCount);
                for (var i = 0; i < frames * channelCount; i++)
                    samples.Add(data[i]);
                remaining -= frames;
            }
        }

        void Update()
        {
            if (Done)
                return;
            float[] copy;
            lock (gate)
            {
                if (remaining > 0)
                    return;
                copy = samples.ToArray();
            }
            Write(path, copy, channels, sampleRate);
            if (effects != null)
                File.WriteAllLines(Path.ChangeExtension(path, ".txt"), effects);
            Done = true;
            Debug.Log($"AudioCapture: wrote {copy.Length / channels / (float)sampleRate:F1} s ({channels} ch, {sampleRate} Hz) to {path}");
            Destroy(this);
        }

        public static void Write(string file, float[] interleaved, int channelCount, int rate)
        {
            using var stream = new FileStream(file, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            var bytes = interleaved.Length * 2;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + bytes);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channelCount);
            writer.Write(rate);
            writer.Write(rate * channelCount * 2);
            writer.Write((short)(channelCount * 2));
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(bytes);
            foreach (var s in interleaved)
                writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
        }
    }
}
#endif
