using UnityEngine;

namespace ARPG
{
    /// <summary>
    /// Shows the player as its baked, layered sprites (<see cref="LayeredCharacterSprite"/>) wearing what is equipped
    /// (<see cref="AppearanceRules"/>), once the character's sheets exist; until then the placeholder body stays. Plays
    /// idle and the locomotion from the movement (<see cref="LocomotionRules"/>: the run at the actual speed, a backward run facing the target when retreating from a fight), the basic
    /// attack fitted to the attack rate and a skill's animation (Hew, Hurl Axe, Ground Breaker), standing or, while the
    /// character moves, their <c>_move</c> variants with running legs, and hit when standing;
    /// Bull Rush plays for as long as the dash lasts, and death is held. An animation the sheets lack falls back to the
    /// attack, then to idle. Added to the player by <see cref="PlayerController"/>.
    /// </summary>
    // After PlayerCombat, so a swing faces the target combat picked this frame, not last frame's.
    [DefaultExecutionOrder(50)]
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        public const string WildArrowCharacter = "wild_arrow";
        public const string WrathbornCharacter = "wrathborn";

        static string defaultCharacter;

        // Domain reload is off in the editor, so a bake between plays must be seen on the next play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDefault() => defaultCharacter = null;

        /// <summary>
        /// The character whose sheets the player shows: the Wild Arrow once its stand-in is baked (Tools > ARPG > Sprite
        /// Bake > Bake Wild Arrow, Docs/09 4.6); until then the retired Wrathborn's sheets, whose axe and shield layers no
        /// longer show (bows only, 2026-09-30).
        /// </summary>
        public static string DefaultCharacter => defaultCharacter ??=
            CharacterSheets.Load($"Characters/{WildArrowCharacter}/" +
                                 AppearanceRules.SheetName(WildArrowCharacter, AppearanceLayer.Body, AppearanceRules.BareBody, CharacterGrip.OneHand, "idle")) != null
                ? WildArrowCharacter
                : WrathbornCharacter;

        const float HitSeconds = 0.3f;
        const float MaxSkillSeconds = 1.2f;
        const float RunThreshold = 0.5f;

        bool diving;

        // How long the character must stand before the idle shows, so a quick reversal does not flash it.
        const float IdleGraceSeconds = 0.1f;

        // The shortest time a moving action keeps one set of legs.
        const float LegsHoldSeconds = 0.25f;

        // How long after an attack, a skill or a hit the character still counts as fighting (for the backward run).
        const float CombatMemorySeconds = 1.2f;

        // Chosen in Start, not here: DefaultCharacter loads a sheet, and Resources may not be read from a field
        // initializer (it runs in the MonoBehaviour's constructor).
        string character;
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
        float lastCombatTime = -100f;
        float stillFor;
        Vector2 aim = Vector2.down;
        EnemyController swingTarget;
        float legsSince = -1f;

        /// <summary>Which character's sheets to show (Resources/Characters/&lt;name&gt;). Changing it rebuilds the look.</summary>
        public string Character
        {
            get => character ?? DefaultCharacter;
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
            if (string.IsNullOrEmpty(character))
                character = DefaultCharacter;
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
            // The swing follows the enemy it was aimed at, not whichever one combat picks next frame: in a crowd the
            // pick jumped between enemies and flipped the legs every few frames.
            swingTarget = combat != null ? combat.Target : null;
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
            sprite.Rate = 1f;
            sprite.Play(name, false, length > 0f ? Mathf.Min(length, maxSeconds) : 0f);
            sprite.Face(aim);
            return true;
        }

        // The sheet for an action: standing, the action; moving, over the legs that match the motion relative to the
        // facing (forward, strafing right or left, backward), falling back to the forward run when a sheet is missing.
        // Null when none exists.
        string ActionSheetFor(string baseName, bool moving, Vector2 velocity)
        {
            if (moving)
            {
                // Judged from the stick, which is steady, rather than the velocity, which enemies bumping the character
                // swing around; and a set of legs is kept at least LegsHoldSeconds.
                var motion = player.InputDirection.sqrMagnitude > 0f ? player.InputDirection : velocity;
                var current = LocomotionRules.LegsOf(sprite.Animation);
                var legs = LocomotionRules.LegsFor(aim, motion, current);
                if (legs != current && sprite.Animation != null && sprite.Animation.StartsWith(baseName) &&
                    Time.time - legsSince < LegsHoldSeconds)
                    legs = current;
                if (legs != current)
                    legsSince = Time.time;
                var sheet = LocomotionRules.ActionSheet(baseName, true, legs);
                if (sprite.Has(sheet))
                    return sheet;
            }
            var name = LocomotionRules.ActionSheet(baseName, moving);
            return sprite.Has(name) ? name : null;
        }

        // The basic swing keeps facing its target as both move, so a circling fight never swings at the air where the
        // enemy was when the swing began. Skills keep their cast direction (Hurl Axe picks its own, farther target).
        void FollowTarget()
        {
            if (action != "attack" || swingTarget == null || !swingTarget.IsAlive)
                return;
            var toTarget = swingTarget.GroundPosition - IsoMath.WorldToGround(player.transform.position);
            if (toTarget.sqrMagnitude > 1e-6f)
                aim = toTarget.normalized;
        }

        void Update()
        {
            if (!showing || dead || player == null)
                return;

            var velocity = player.GroundVelocity;
            var speed = velocity.magnitude;

            // The dodge's dive (the owner, 2026-10-10): once, facing the way she dives, over whatever was playing. Without
            // a baked dive she just runs, fast, the usual way.
            if (player.IsDodging && sprite.Has("dive"))
            {
                if (!diving)
                {
                    diving = true;
                    inAction = false;
                    action = null;
                    sprite.Face(velocity);
                    sprite.Play("dive", false, DodgeRules.AnimationSeconds);
                }
                return;
            }
            diving = false;

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

            // Disengaging cuts a draw or a hit reaction short: she turns and runs at once.
            if (inAction && player.Disengaged)
            {
                inAction = false;
                action = null;
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
                sprite.Rate = 1f;
                sprite.Loop("idle");
                return;
            }
            stillFor = 0f;

            // Retreating from what it fights, the character faces it and runs backward.
            var fighting = Time.time - lastCombatTime < CombatMemorySeconds && !player.Disengaged;
            if (fighting && LocomotionRules.IsBackpedal(aim, velocity, sprite.Animation == "run_back") && sprite.Has("run_back"))
            {
                sprite.FaceRow(LocomotionRules.ChooseRow(sprite.Row, aim, sprite.DirectionCount));
                sprite.Loop("run_back");
                sprite.Rate = LocomotionRules.PlaybackRate(speed, sprite.CurrentRecordedSpeed);
                return;
            }

            // No turn clips: both leaned the body into the turn, which looked wrong (the owner, 2026-09-28); a change of
            // direction switches the row at once. Turns are judged from the stick, which turns at once.
            var heading = player.InputDirection.sqrMagnitude > 0f ? player.InputDirection : velocity;
            var row = LocomotionRules.ChooseRow(sprite.Row, heading, sprite.DirectionCount);
            sprite.FaceRow(row);
            // Moving slowly she walks (when the walk is baked), else she runs.
            sprite.Loop(sprite.Has("walk") && LocomotionRules.Walks(speed, sprite.Animation == "walk") ? "walk" : "run");
            sprite.Rate = LocomotionRules.PlaybackRate(speed, sprite.CurrentRecordedSpeed);
        }
    }
}
