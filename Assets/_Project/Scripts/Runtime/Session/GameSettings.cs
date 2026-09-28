using System;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// The player's settings (Docs/06-ui-ux.md, Settings tab), kept in the account file beside the character saves
    /// (Docs/07-technical.md: one shared file for account data). Only the settings with a decided source and a built
    /// system behind them: stick size and dead zone (Docs/01 and Docs/06, accessibility), handedness (Docs/01), the
    /// auto-potion threshold (Docs/01), music and effects volume (Docs/06), reduce flashing and reduce motion
    /// (Docs/06, accessibility). Ranges and defaults are Docs/06's proposed table; the steps are Claude's.
    /// Pure data: <see cref="SettingsDirector"/> loads, saves and hands it out.
    /// </summary>
    [Serializable]
    public sealed class GameSettings
    {
        public const int CurrentVersion = 1;

        public const float MinStickSize = 48f, MaxStickSize = 96f, StickSizeStep = 8f;
        public const float MinDeadZone = 4f, MaxDeadZone = 20f, DeadZoneStep = 2f;
        public const float PotionStep = 5f;
        public const float VolumeStep = 10f;

        public int version = CurrentVersion;

        /// <summary>Stick radius in points.</summary>
        public float stickSize = 64f;

        /// <summary>Dead zone in percent of the stick radius.</summary>
        public float deadZone = 8f;

        /// <summary>Moves the Bag and Portal buttons to the lower left corner.</summary>
        public bool leftHanded;

        /// <summary>Life percent at which the auto-potion fires.</summary>
        public float potionThreshold = AutoPotion.DefaultTriggerFraction * 100f;

        /// <summary>0 to 100. 80 is the level the music was mixed at before this setting existed.</summary>
        public float musicVolume = 80f;

        /// <summary>0 to 100. 100 is the level the effects were mixed at.</summary>
        public float effectsVolume = 100f;

        /// <summary>The boss phase flash becomes a slow, faint fade.</summary>
        public bool reduceFlashing;

        /// <summary>No camera lead and no hit stop (Docs/06; there is no screen shake yet).</summary>
        public bool reduceMotion;

        public float PotionTriggerFraction => potionThreshold / 100f;

        /// <summary>Dead zone as a fraction of the stick radius.</summary>
        public float DeadZoneFraction => deadZone / 100f;

        public float MusicLevel => musicVolume / 100f;

        public float EffectsLevel => effectsVolume / 100f;

        /// <summary>Puts every value back in its range, on its step. A file edited by hand or from a later build
        /// that widened a range cannot push a value the systems do not expect.</summary>
        public void Clamp()
        {
            stickSize = Snap(stickSize, MinStickSize, MaxStickSize, StickSizeStep);
            deadZone = Snap(deadZone, MinDeadZone, MaxDeadZone, DeadZoneStep);
            potionThreshold = Snap(potionThreshold, AutoPotion.MinTriggerFraction * 100f, AutoPotion.MaxTriggerFraction * 100f, PotionStep);
            musicVolume = Snap(musicVolume, 0f, 100f, VolumeStep);
            effectsVolume = Snap(effectsVolume, 0f, 100f, VolumeStep);
        }

        /// <summary>The next or previous step of a value within its range, stopping at the ends.</summary>
        public static float Step(float value, float min, float max, float step, int direction) =>
            Snap(value + step * Math.Sign(direction), min, max, step);

        static float Snap(float value, float min, float max, float step)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                value = min;
            var snapped = min + (float)Math.Round((value - min) / step) * step;
            return Math.Max(min, Math.Min(max, snapped));
        }

        public GameSettings Copy() => (GameSettings)MemberwiseClone();

        public static string ToJson(GameSettings settings) => JsonUtility.ToJson(settings, prettyPrint: true);

        /// <summary>Reads a settings file. A file from a newer build is refused rather than half-read, like a save;
        /// missing fields keep their defaults.</summary>
        public static bool TryParse(string json, out GameSettings settings, out string error)
        {
            settings = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "empty file";
                return false;
            }

            GameSettings parsed;
            try
            {
                parsed = new GameSettings();
                JsonUtility.FromJsonOverwrite(json, parsed);
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }

            if (parsed.version > CurrentVersion)
            {
                error = $"version {parsed.version} is newer than this build's {CurrentVersion}";
                return false;
            }
            if (parsed.version < 1)
            {
                error = "not a settings file";
                return false;
            }

            parsed.version = CurrentVersion;
            parsed.Clamp();
            settings = parsed;
            error = null;
            return true;
        }
    }
}
