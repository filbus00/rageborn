using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Shows the player as its baked, layered sprites (<see cref="LayeredCharacterSprite"/>) wearing what is equipped
    /// (<see cref="AppearanceRules"/>), once the character's sheets exist; until then the placeholder body stays. Plays
    /// idle and the locomotion from the movement (<see cref="LocomotionRules"/>: the run at the actual speed, a turn clip
    /// for a one-row change of direction, a backward run facing the target when retreating from a fight), and standing
    /// still the basic attack fitted to the attack rate, a skill's animation (Hew, Hurl Axe, Ground Breaker), and hit;
    /// Bull Rush plays for as long as the dash lasts, and death is held. An animation the sheets lack falls back to the
    /// attack, then to idle. Added to the player by <see cref="PlayerController"/>.
    /// </summary>
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        public const string DefaultCharacter = "wrathborn";

        const float HitSeconds = 0.3f;
        const float RunThreshold = 0.5f;

        // How long the character must stand before the idle shows, so a quick reversal does not flash it.
        const float IdleGraceSeconds = 0.1f;

        // How long after an attack, a skill or a hit the character still counts as fighting (for the backward run).
        const float CombatMemorySeconds = 1.2f;

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
        bool turning;
        int turnTo;
        float runRecordedSpeed;
        float lastCombatTime = -100f;
        float stillFor;
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

        // While the character moves, attacks and skills do not play on its body: a full-body swing over a body sliding
        // along the ground is what made it look like running on ice (the user, 2026-09-27). The run keeps going and the
        // slash effect shows the hit; standing still, the swing plays. Bull Rush is its own movement and always plays.
        bool Moving => player != null && player.GroundVelocity.magnitude > RunThreshold;

        void OnBasicAttack(Vector2 direction, float interval)
        {
            lastCombatTime = Time.time;
            aim = direction;
            if (!showing || dead || Moving)
                return;
            var length = sprite.LengthOf("attack");
            PlayAction("attack", length > 0f ? Mathf.Min(length, interval) : 0f);
        }

        void OnSkillCast(SkillDefinition skill, Vector2 direction)
        {
            lastCombatTime = Time.time;
            aim = direction;
            if (!showing || dead)
                return;
            if (skill.Kind == SkillKind.Charge)
            {
                dashing = true;
                turning = false;
                if (sprite.Has("bull_rush"))
                {
                    inAction = true;
                    sprite.Play("bull_rush", true);
                }
                return;
            }
            if (Moving)
                return;
            PlayAction(skill.Kind == SkillKind.Sweep ? "hew" : skill.Kind == SkillKind.Projectile ? "hurl_axe" : "ground_breaker", 0f);
        }

        void OnHit(float damage)
        {
            lastCombatTime = Time.time;
            if (!showing || dead || inAction || Moving || !sprite.Has("hit"))
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
            turning = false;
            sprite.Rate = 1f;
            sprite.Play(name, false, seconds);
            sprite.Face(aim);
        }

        void Update()
        {
            if (!showing || dead || player == null)
                return;

            var velocity = player.GroundVelocity;
            var speed = velocity.magnitude;

            if (dashing)
            {
                if (player.IsDashing)
                {
                    sprite.Face(velocity);
                    sprite.Rate = LocomotionRules.PlaybackRate(speed, sprite.CurrentRecordedSpeed);
                    return;
                }
                dashing = false;
                inAction = false;
            }

            if (inAction)
            {
                // Starting to move cuts a swing short rather than sliding it across the floor.
                if (!sprite.Finished && speed <= RunThreshold)
                    return;
                inAction = false;
            }

            if (speed <= RunThreshold)
            {
                // A reversal passes through standing for a frame or two; only a real stop shows the idle.
                stillFor += Time.deltaTime;
                if (stillFor < IdleGraceSeconds && sprite.Animation != "idle")
                    return;
                turning = false;
                sprite.Rate = 1f;
                sprite.Loop("idle");
                return;
            }
            stillFor = 0f;

            // Retreating from what it fights, the character faces it and runs backward.
            var fighting = Time.time - lastCombatTime < CombatMemorySeconds;
            if (fighting && LocomotionRules.IsBackpedal(aim, velocity) && sprite.Has("run_back"))
            {
                turning = false;
                sprite.FaceRow(LocomotionRules.ChooseRow(sprite.Row, aim));
                sprite.Loop("run_back");
                sprite.Rate = LocomotionRules.PlaybackRate(speed, sprite.CurrentRecordedSpeed);
                return;
            }

            if (turning)
            {
                if (!sprite.Finished)
                    return;
                turning = false;
                sprite.FaceRow(turnTo);
                sprite.Play("run", true);
            }

            var row = LocomotionRules.ChooseRow(sprite.Row, velocity);
            var step = sprite.Animation == "run" ? LocomotionRules.TurnStep(sprite.Row, row) : 0;
            var turn = step > 0 ? "run_turn_right" : "run_turn_left";
            if (step != 0 && sprite.Has(turn))
            {
                // The turn clip curves the body about 40 degrees from the row it starts in, which is where the new row is:
                // it plays in the old row, as fast as the run would at this speed, then the run goes on in the new one.
                turning = true;
                turnTo = row;
                var runRate = LocomotionRules.PlaybackRate(speed, runRecordedSpeed > 0f ? runRecordedSpeed : sprite.CurrentRecordedSpeed);
                sprite.Rate = 1f;
                sprite.Play(turn, false, sprite.LengthOf(turn) / runRate);
                return;
            }

            sprite.FaceRow(row);
            sprite.Loop("run");
            if (sprite.CurrentRecordedSpeed > 0f)
                runRecordedSpeed = sprite.CurrentRecordedSpeed;
            sprite.Rate = LocomotionRules.PlaybackRate(speed, sprite.CurrentRecordedSpeed);
        }
    }
}
