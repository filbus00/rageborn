using System.Collections.Generic;
using UnityEngine;

namespace ARPG
{
    /// <summary>What an enemy throws to make a pool: a molotov for fire, a bottle for poison, a glob of web.</summary>
    public enum FlaskKind
    {
        Molotov,
        Poison,
        Web,

        /// <summary>A mortar's shell (2026-11-16): the bomb picture, lobbed high and turning slowly.</summary>
        Mortar,
    }

    /// <summary>The arc of a thrown flask. Pure.</summary>
    public static class FlaskRules
    {
        /// <summary>How high the arc rises at its middle, in world units: higher for a longer throw.</summary>
        public static float Peak(float groundDistance) => 0.7f + 0.12f * groundDistance;

        /// <summary>The height over the ground at a share of the flight (0 thrown, 1 landed): a parabola.</summary>
        public static float Height(float share, float peak)
        {
            share = Mathf.Clamp01(share);
            return 4f * peak * share * (1f - share);
        }

        /// <summary>Turns a second while it flies (degrees).</summary>
        public const float SpinDegrees = 600f;

        /// <summary>Under this flight time a throw is not drawn (a pool that lands at once has nothing to show).</summary>
        public const float LeastSeconds = 0.2f;

        /// <summary>A mortar's shell goes up higher than a thrown bottle, in a long lob (2026-11-16, the owner: the
        /// mortar enemies "should shoot a mortar... A projectile should fly through the air").</summary>
        public const float MortarLift = 2.2f;

        /// <summary>The arc's peak for a throw of a kind.</summary>
        public static float PeakOf(FlaskKind kind, float groundDistance) => Peak(groundDistance) * (kind == FlaskKind.Mortar ? MortarLift : 1f);

        /// <summary>How fast a kind turns in the air (degrees a second): a shell hardly turns.</summary>
        public static float SpinOf(FlaskKind kind) => kind == FlaskKind.Mortar ? 120f : SpinDegrees;

        public static string PictureOf(FlaskKind kind) =>
            kind == FlaskKind.Poison ? "flask_poison" : kind == FlaskKind.Web ? "projectile_web" :
            kind == FlaskKind.Mortar ? "projectile_bomb" : "flask_molotov";
    }

    /// <summary>
    /// The bottles enemies throw at her (2026-11-10, the owner: "The enemies that have pool attacks, make it so that they
    /// are throwing grenades. So like fire pools are molotovs, poison are poison bottles. They should fly in an arc towards
    /// the character and their landing zone be marked like the pool warnings"): a pixel picture from the thrower to where
    /// the pool will be, in an arc, turning as it flies, landing as the pool's warning fills. Only the picture: the pool
    /// and its warning are the hazard's. Pooled, ticked by the <see cref="EnemyManager"/>.
    /// </summary>
    public sealed class Flasks
    {
        sealed class Flight
        {
            public SpriteRenderer Renderer;
            public Sprite Picture;
            public Vector2 From, To;
            public float Seconds, Time, Peak, Spin, SpinRate;
            public bool Active;
        }

        readonly Transform parent;
        readonly List<Flight> flights = new List<Flight>();

        public Flasks(Transform parent) => this.parent = parent;

        /// <summary>A flask from one ground point to another, landing after <paramref name="seconds"/>.</summary>
        public void Throw(Vector2 from, Vector2 to, float seconds, FlaskKind kind)
        {
            if (seconds < FlaskRules.LeastSeconds)
                return;
            var picture = WorldArt.Get(FlaskRules.PictureOf(kind));
            if (picture == null)
                return;
            Flight flight = null;
            foreach (var f in flights)
                if (!f.Active)
                {
                    flight = f;
                    break;
                }
            if (flight == null)
            {
                var go = GroundMarker.NewSprite("Flask", picture, Color.white, parent, 70);
                var renderer = go.GetComponent<SpriteRenderer>();
                // Over the characters while it flies.
                renderer.sortingLayerName = GameSortingLayers.Effects;
                flight = new Flight { Renderer = renderer };
                flights.Add(flight);
            }
            flight.Picture = picture;
            flight.From = from;
            flight.To = to;
            flight.Seconds = seconds;
            flight.Time = 0f;
            flight.Peak = FlaskRules.PeakOf(kind, Vector2.Distance(from, to));
            flight.SpinRate = FlaskRules.SpinOf(kind);
            flight.Spin = Random.Range(0f, 360f) * (Random.value < 0.5f ? 1f : -1f);
            flight.Active = true;
            flight.Renderer.gameObject.SetActive(true);
            Place(flight);
        }

        public void Tick(float deltaTime)
        {
            for (var i = 0; i < flights.Count; i++)
            {
                var flight = flights[i];
                if (!flight.Active)
                    continue;
                flight.Time += deltaTime;
                if (flight.Time >= flight.Seconds)
                {
                    flight.Active = false;
                    flight.Renderer.gameObject.SetActive(false);
                    Sfx.Play(SoundId.FireHit, 0.35f);
                    continue;
                }
                Place(flight);
            }
        }

        void Place(Flight flight)
        {
            var share = flight.Time / flight.Seconds;
            var ground = Vector2.Lerp(flight.From, flight.To, share);
            var world = Elevation.World2(ground);
            flight.Renderer.transform.position = new Vector3(world.x, world.y + FlaskRules.Height(share, flight.Peak), 0f);
            // Turned in whole pixels (TurnedArt), not by the transform, so it stays pixel art.
            var degrees = flight.Spin + flight.SpinRate * flight.Time * Mathf.Sign(flight.Spin == 0f ? 1f : flight.Spin);
            var turned = TurnedArt.Toward(flight.Picture, degrees);
            flight.Renderer.sprite = turned != null ? turned : flight.Picture;
        }

        public void Clear()
        {
            foreach (var flight in flights)
            {
                flight.Active = false;
                if (flight.Renderer != null)
                    flight.Renderer.gameObject.SetActive(false);
            }
        }
    }
}
