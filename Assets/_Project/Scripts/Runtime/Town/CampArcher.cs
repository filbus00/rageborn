using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// An archer of the company at the camp's firing range (the owner, 2026-11-18: "add other archers training and
    /// firing at the dummies"): stands at the line in her drawn look (the player's sheets in
    /// Resources/Characters/wild_arrow_drawn: a body, a bow and perhaps a helm, stacked), idles, turns to her target, draws
    /// and looses an arrow that flies to it and stays stuck a while, then rests and does it again. Each has her own look
    /// and her own pace. For show: nothing hits and nothing in the game reads them. Made by <see cref="CampSpot.CreateAll"/>.
    /// </summary>
    public class CampArcher : MonoBehaviour
    {
        const string Folder = "Characters/wild_arrow_drawn/wild_arrow_drawn_";
        const float FramesPerSecond = 12f;
        // The arrow leaves on this share of the draw's frames; it flies this fast (world units a second).
        const float ReleaseShare = 0.62f;
        const float ArrowSpeed = 15f;
        const float ChestHeight = 0.7f;

        sealed class Layer
        {
            public SpriteRenderer Renderer;
            public CharacterSheet Idle;
            public CharacterSheet Shot;
        }

        readonly List<Layer> layers = new List<Layer>();
        Vector2 groundDirection;
        Vector3 targetWorld;
        float clock;
        float nextShot;
        float shotStart;
        bool shooting;
        bool released;

        /// <summary>Stands an archer at a cell of the range, shooting at its target cell. Null when her sheets are missing.</summary>
        public static CampArcher Create(CampLayout.Archer place, string chest, string helm, string bow, int seed)
        {
            var go = new GameObject("Camp Archer");
            go.transform.position = IsoMath.GroundToWorld(IsoMath.CellToGround(place.Cell));
            var archer = go.AddComponent<CampArcher>();
            archer.Add("body_" + chest, 0);
            archer.Add("weapon_" + bow, 1);
            if (helm.Length > 0)
                archer.Add("helm_" + helm, 2);
            if (archer.layers.Count == 0)
            {
                Destroy(go);
                return null;
            }
            var from = IsoMath.CellToGround(place.Cell);
            var to = IsoMath.CellToGround(place.Target);
            archer.groundDirection = (to - from).normalized;
            var target = IsoMath.GroundToWorld(to);
            archer.targetWorld = new Vector3(target.x, target.y + ChestHeight * 0.6f, 0f);
            // Each at her own pace, so they never loose together.
            var random = new System.Random(seed);
            archer.clock = (float)random.NextDouble() * 3f;
            archer.nextShot = Time.time + 1f + (float)random.NextDouble() * 4f;
            return archer;
        }

        void Add(string sheet, int order)
        {
            var idle = CharacterSheets.Load(Folder + sheet + "_1h_idle");
            var shot = CharacterSheets.Load(Folder + sheet + "_1h_attack");
            if (idle == null || shot == null)
                return;
            var child = new GameObject(sheet, typeof(SpriteRenderer));
            child.transform.SetParent(transform, false);
            var renderer = child.GetComponent<SpriteRenderer>();
            renderer.sortingLayerName = GameSortingLayers.Entities;
            renderer.sortingOrder = order;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            WorldArt.Lit(renderer);
            layers.Add(new Layer { Renderer = renderer, Idle = idle, Shot = shot });
        }

        void Update()
        {
            clock += Time.deltaTime;
            if (!shooting && Time.time >= nextShot)
            {
                shooting = true;
                released = false;
                shotStart = Time.time;
            }
            foreach (var layer in layers)
            {
                var sheet = shooting ? layer.Shot : layer.Idle;
                var rowIndex = Mathf.Clamp(AppearanceRules.DirectionRow(groundDirection, sheet.Rows.Length), 0, sheet.Rows.Length - 1);
                var frames = sheet.Rows[rowIndex].Length;
                int frame;
                if (shooting)
                {
                    frame = Mathf.FloorToInt((Time.time - shotStart) * FramesPerSecond);
                    if (frame >= frames)
                    {
                        shooting = false;
                        nextShot = Time.time + 2.5f + Hash(clock) * 3.5f;
                        frame = 0;
                        sheet = layer.Idle;
                        rowIndex = Mathf.Clamp(rowIndex, 0, sheet.Rows.Length - 1);
                        frames = sheet.Rows[rowIndex].Length;
                    }
                    else if (!released && frame >= Mathf.FloorToInt(frames * ReleaseShare))
                    {
                        released = true;
                        Loose();
                    }
                }
                else
                    frame = Mathf.FloorToInt(clock * FramesPerSecond * 0.5f) % frames;
                layer.Renderer.sprite = sheet.Rows[rowIndex][Mathf.Clamp(frame, 0, frames - 1)];
            }
        }

        static float Hash(float seed) => Mathf.Abs(Mathf.Sin(seed * 12.9898f) * 43758.5453f) % 1f;

        // The arrow leaves her bow at chest height and flies to the target.
        void Loose()
        {
            var from = transform.position + new Vector3(0f, ChestHeight, 0f);
            CampArrow.Fly(from, targetWorld, ArrowSpeed);
        }
    }

    /// <summary>An arrow shot at the camp's range: flies to its target, sticks there for a few seconds, and goes.</summary>
    public class CampArrow : MonoBehaviour
    {
        Vector3 to;
        float speed;
        float stuckFor = -1f;

        public static void Fly(Vector3 from, Vector3 to, float speed)
        {
            var go = new GameObject("Camp Arrow", typeof(SpriteRenderer));
            go.transform.position = from;
            var direction = to - from;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = ArrowArt.For(ArrowLook.Plain);
            renderer.sortingLayerName = GameSortingLayers.Entities;
            renderer.sortingOrder = 5;
            WorldArt.Lit(renderer);
            var arrow = go.AddComponent<CampArrow>();
            arrow.to = to;
            arrow.speed = speed;
        }

        void Update()
        {
            if (stuckFor >= 0f)
            {
                stuckFor += Time.deltaTime;
                if (stuckFor > 5f)
                    Destroy(gameObject);
                return;
            }
            var step = speed * Time.deltaTime;
            var remaining = Vector3.Distance(transform.position, to);
            if (remaining <= step)
            {
                transform.position = to;
                stuckFor = 0f;
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, to, step);
        }
    }
}
