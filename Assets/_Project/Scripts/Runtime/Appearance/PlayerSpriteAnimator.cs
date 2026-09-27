using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Shows the player as its baked, layered sprites (<see cref="LayeredCharacterSprite"/>) wearing what is equipped
    /// (<see cref="AppearanceRules"/>), once the character's sheets exist; until then the placeholder body stays. Plays
    /// idle and run from the movement, the basic attack fitted to the attack rate, a skill's animation when it fires
    /// (Hew, Hurl Axe, Bull Rush for as long as the dash lasts, Ground Breaker), hit when struck and nothing else is
    /// playing, and death, held. An animation the sheets lack falls back to the attack, then to idle. Facing follows the
    /// movement, and the aim while an attack or skill plays. Added to the player by <see cref="PlayerController"/>.
    /// </summary>
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        public const string DefaultCharacter = "wrathborn";

        const float HitSeconds = 0.3f;
        const float RunThreshold = 0.5f;

        string character = DefaultCharacter;
        PlayerController player;
        PlayerCombat combat;
        PlayerHealth health;
        LayeredCharacterSprite sprite;
        SpriteRenderer placeholder;
        GameSession session;
        bool showing;
        bool flashSet;
        bool dead;
        bool dashing;
        bool inAction;
        Vector2 aim = Vector2.down;

        /// <summary>Which character's sheets to show (Resources/Characters/&lt;name&gt;). Changing it rebuilds the look.</summary>
        public string Character
        {
            get => character;
            set
            {
                if (character == value)
                    return;
                character = value;
                if (sprite != null)
                {
                    Destroy(sprite.gameObject);
                    sprite = null;
                }
                flashSet = false;
                showing = false;
                if (isActiveAndEnabled && player != null)
                    Build();
            }
        }

        public LayeredCharacterSprite Sprite => sprite;

        /// <summary>Whether the baked sprites are showing (the character's sheets exist), rather than the placeholder.</summary>
        public bool ShowingSprites => showing;

        void Start()
        {
            player = GetComponent<PlayerController>();
            combat = FindAnyObjectByType<PlayerCombat>();
            health = FindAnyObjectByType<PlayerHealth>();
            var body = transform.Find("Body");
            placeholder = body != null ? body.GetComponent<SpriteRenderer>() : null;

            if (combat != null)
            {
                combat.BasicAttackStarted += OnBasicAttack;
                combat.SkillCast += OnSkillCast;
            }
            if (health != null)
            {
                health.HitTaken += OnHit;
                health.Died += OnDied;
            }
            Build();
        }

        void OnDestroy()
        {
            if (combat != null)
            {
                combat.BasicAttackStarted -= OnBasicAttack;
                combat.SkillCast -= OnSkillCast;
            }
            if (health != null)
            {
                health.HitTaken -= OnHit;
                health.Died -= OnDied;
            }
            if (session != null)
                session.Changed -= ApplyAppearance;
        }

        void Build()
        {
            sprite = LayeredCharacterSprite.Create(transform, character);
            if (session != GameSession.Current)
            {
                if (session != null)
                    session.Changed -= ApplyAppearance;
                session = GameSession.Current;
                session.Changed += ApplyAppearance;
            }
            ApplyAppearance();
            sprite.Play("idle", true);
        }

        void ApplyAppearance()
        {
            if (sprite == null)
                return;
            sprite.SetAppearance(AppearanceRules.For(session.Equipment));
            var wasShowing = showing;
            showing = sprite.Has("idle");
            sprite.gameObject.SetActive(showing);
            if (placeholder != null)
                placeholder.enabled = !showing;
            // Changed fires for gold and kills too; the flash only needs new renderers when what is drawn switches.
            if (showing != wasShowing || !flashSet)
            {
                flashSet = true;
                player.SetFlashRenderers(showing ? sprite.Renderers : placeholder != null ? new[] { placeholder } : null);
            }
        }

        void OnBasicAttack(Vector2 direction, float interval)
        {
            if (!showing || dead)
                return;
            aim = direction;
            var length = sprite.LengthOf("attack");
            PlayAction("attack", length > 0f ? Mathf.Min(length, interval) : 0f);
        }

        void OnSkillCast(SkillDefinition skill, Vector2 direction)
        {
            if (!showing || dead)
                return;
            aim = direction;
            switch (skill.Kind)
            {
                case SkillKind.Charge:
                    dashing = true;
                    if (sprite.Has("bull_rush"))
                    {
                        inAction = true;
                        sprite.Play("bull_rush", true);
                        return;
                    }
                    PlayAction("attack", 0f);
                    return;
                case SkillKind.Sweep:
                    PlayAction("hew", 0f);
                    return;
                case SkillKind.Projectile:
                    PlayAction("hurl_axe", 0f);
                    return;
                default:
                    PlayAction("ground_breaker", 0f);
                    return;
            }
        }

        void OnHit(float damage)
        {
            if (!showing || dead || inAction || !sprite.Has("hit"))
                return;
            inAction = true;
            sprite.Play("hit", false, HitSeconds);
        }

        void OnDied()
        {
            dead = true;
            if (showing && sprite.Has("death"))
                sprite.Play("death", false);
        }

        void PlayAction(string animation, float seconds)
        {
            var name = sprite.Has(animation) ? animation : sprite.Has("attack") ? "attack" : null;
            if (name == null)
                return;
            inAction = true;
            sprite.Play(name, false, seconds);
        }

        void Update()
        {
            if (!showing || dead || player == null)
                return;

            var velocity = player.GroundVelocity;
            if (dashing && !player.IsDashing)
            {
                dashing = false;
                inAction = false;
            }
            if (inAction && sprite.Finished)
                inAction = false;

            if (inAction)
            {
                sprite.Face(dashing ? velocity : aim);
                return;
            }

            var moving = velocity.magnitude > RunThreshold;
            sprite.Face(velocity);
            sprite.Loop(moving ? "run" : "idle");
        }
    }
}
