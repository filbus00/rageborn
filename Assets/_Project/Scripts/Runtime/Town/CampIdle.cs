using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// A camp keeper's idle (2026-11-24, the owner: "give them idle animations"): the frames camp_art.py draws
    /// (`camp_brunhilde_idle_0` to `_3`: still, a breath, their own small move, a blink) played in a loop that breathes
    /// often, moves now and then and blinks once. Each keeper starts at its own point of the loop.
    /// </summary>
    public class CampIdle : MonoBehaviour
    {
        public const float FrameSeconds = 0.4f;

        /// <summary>The loop, as frame numbers: breathing, the keeper's own move, breathing, a blink.</summary>
        public static readonly int[] Loop = { 0, 1, 0, 1, 0, 2, 2, 0, 1, 0, 1, 0, 3 };

        SpriteRenderer body;
        Sprite[] frames;
        float clock;

        /// <summary>Plays the idle frames of a placed keeper when they exist; without them the picture stays still.</summary>
        public static void Attach(SpriteRenderer body, string name)
        {
            if (body == null)
                return;
            var found = new Sprite[4];
            for (var i = 0; i < found.Length; i++)
            {
                found[i] = WorldArt.Get(name + "_idle_" + i);
                if (found[i] == null)
                    return;
            }
            var idle = body.gameObject.AddComponent<CampIdle>();
            idle.body = body;
            idle.frames = found;
            idle.clock = Random.Range(0f, Loop.Length * FrameSeconds);
        }

        void Update()
        {
            clock += Time.deltaTime;
            var step = (int)(clock / FrameSeconds) % Loop.Length;
            var sprite = frames[Loop[step]];
            if (body.sprite != sprite)
                body.sprite = sprite;
        }
    }
}
