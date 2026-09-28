using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Shows the player as its baked, layered sprites (<see cref="LayeredCharacterSprite"/>) wearing what is equipped
    /// (<see cref="AppearanceRules"/>), once the character's sheets exist; until then the placeholder body stays. Plays
    /// idle and the locomotion from the movement (<see cref="LocomotionRules"/>: the run at the actual speed, a turn clip
    /// for a one-row change of direction, a backward run facing the target when retreating from a fight), the basic
    /// attack fitted to the attack rate and a skill's animation (Hew, Hurl Axe, Ground Breaker), standing or, while the
    /// character moves, their <c>_move</c> variants with running legs, and hit when standing;
    /// Bull Rush plays for as long as the dash lasts, and death is held. An animation the sheets lack falls back to the
    /// attack, then to idle. Added to the player by <see cref="PlayerController"/>.
    /// </summary>
    // After PlayerCombat, so a swing faces the target combat picked this frame, not last frame's.
    [DefaultExecutionOrder(50)]
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        public const string DefaultCharacter = "wrathborn";

        const float HitSeconds = 0.3f;
        const float MaxSkillSeconds = 1.2f;
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
        bool skillPlaying;
        string action;
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

        // While the character moves, attacks and skills play their _move variants, whose legs run (the owner, 2026-09-27:
        // "the player will be moving most of the time. The various animations need to play even when the character is
        // moving"). A full-body swing over a body sliding along the ground had looked like running on ice, so without a
        // _move sheet nothing plays while moving and the slash effect shows the hit. Bull Rush always plays.
        bool Moving => player != null && player.GroundVelocity.magnitude > RunThreshold;

        void OnBasicAttack(Vector2 direction, float interval)
        {
            lastCombatTime = Time.time;
            aim = direction;
            if (!showing || dead)
                return;
            // A skill plays out: the basic attack runs on its own timer and cut Hew off 0.2 s into its swing.
            if (inAction && skillPlaying && !sprite.Finished)
                return;
            if (PlayAction("attack", interval))
                skillPlaying = false;
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
                action = null;
                if (sprite.Has("bull_rush"))
                {
                    inAction = true;
                    sprite.Play("bull_rush", true);
                }
                return;
            }
            var skillAnimation = !string.IsNullOrEmpty(skill.AnimationName) ? skill.AnimationName
                : skill.Kind == SkillKind.Sweep ? "hew" : skill.Kind == SkillKind.Projectile ? "hurl_axe" : "ground_breaker";
            // A buff without its own animation shows nothing on the body (a swing would read as an attack).
            if (skill.Kind == SkillKind.Buff && !sprite.Has(skillAnimation))
                return;
            // Capped, so a skill's animation never holds the character longer than the moment it is for.
            if (PlayAction(skillAnimation, MaxSkillSeconds))
                skillPlaying = true;
        }

        void OnHit(float damage)
        {
            lastCombatTime = Time.time;
            if (!showing || dead || inAction || Moving || !sprite.Has("hit"))
                return;
            inAction = true;
            skillPlaying = false;
            action = null;
            sprite.Play("hit", false, HitSeconds);
        }

        void OnDied()
        {
            dead = true;
            if (showing && sprite.Has("death"))
                sprite.Play("death", false);
        }

        // Plays an action, or its moving variant while the character moves, for its length up to maxSeconds; an action
        // the sheets lack falls back to the attack. False when nothing could play (moving, with no moving sheet).
        bool PlayAction(string animation, float maxSeconds)
        {
            var moving = Moving;
            var baseName = sprite.Has(animation) ? animation : sprite.Has("attack") ? "attack" : null;
            if (baseName == null)
                return false;
            var name = ActionSheetFor(baseName, moving, player.GroundVelocity);
            if (name == null)
                return false;
            var length = sprite.LengthOf(name);
            action = baseName;
            inAction = true;
            turning = false;
            sprite.Rate = 1f;
            sprite.Play(name, false, length > 0f ? Mathf.Min(length, maxSeconds) : 0f);
            sprite.Face(aim);
            return true;
        }

        // The sheet for an action: standing, the action; moving, over the forward run, or over the backward run when the
        // target is behind (falling back to the forward one when that sheet is missing). Null when none exists.
        string ActionSheetFor(string baseName, bool moving, Vector2 velocity)
        {
            var wasBackward = sprite.Animation != null && sprite.Animation.EndsWith(LocomotionRules.MovingBackSuffix);
            if (moving && LocomotionRules.IsActionBackward(aim, velocity, wasBackward))
            {
                var back = LocomotionRules.ActionSheet(baseName, true, true);
                if (sprite.Has(back))
                    return back;
            }
            var name = LocomotionRules.ActionSheet(baseName, moving);
            return sprite.Has(name) ? name : null;
        }

        // The basic swing keeps facing its target as both move, so a circling fight never swings at the air where the
        // enemy was when the swing began. Skills keep their cast direction (Hurl Axe picks its own, farther target).
        void FollowTarget()
        {
            if (action != "attack" || combat == null || combat.Target == null || !combat.Target.IsAlive)
                return;
            var toTarget = combat.Target.GroundPosition - IsoMath.WorldToGround(player.transform.position);
            if (toTarget.sqrMagnitude > 1e-6f)
                aim = toTarget.normalized;
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
                if (!sprite.Finished)
                {
                    // Setting off or stopping mid-swing goes on in the other variant at the same point, so the legs follow
                    // the ground. Without a moving sheet, setting off cuts the swing rather than sliding it along the floor.
                    // A hit reaction (no action) only plays standing.
                    // An action always faces what it strikes; the legs run forward or backward under it.
                    var moving = speed > RunThreshold;
                    FollowTarget();
                    var name = action != null ? ActionSheetFor(action, moving, velocity) : moving ? null : sprite.Animation;
                    if (name != null && name != sprite.Animation)
                        sprite.Switch(name);
                    if (name != null)
                    {
                        if (action != null)
                            sprite.FaceRow(LocomotionRules.ChooseRow(sprite.Row, aim, sprite.DirectionCount));
                        return;
                    }
                }
                inAction = false;
                action = null;
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
            if (fighting && LocomotionRules.IsBackpedal(aim, velocity, sprite.Animation == "run_back") && sprite.Has("run_back"))
            {
                turning = false;
                sprite.FaceRow(LocomotionRules.ChooseRow(sprite.Row, aim, sprite.DirectionCount));
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

            // Turns are judged from the stick, which turns at once: the velocity swings round over the acceleration time
            // and with 16 rows would step through the row between, so a 45 degree change never read as one turn.
            var heading = player.InputDirection.sqrMagnitude > 0f ? player.InputDirection : velocity;
            var row = LocomotionRules.ChooseRow(sprite.Row, heading, sprite.DirectionCount);
            var step = sprite.Animation == "run" ? LocomotionRules.TurnStep(sprite.Row, row, sprite.DirectionCount) : 0;
            // Only the right turn has a clip: the mirrored left turn looked wrong (the owner, 2026-09-28), so turning left
            // switches the row straight away.
            var turn = step > 0 ? "run_turn_right" : null;
            if (turn != null && sprite.Has(turn))
            {
                // The turn clip curves the body about 40 degrees from the row it starts in, where the new row is (one row of
                // 8, two of 16):
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
