using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        /// <summary>
        /// Max horizontal speed of the player. Set from the active GameMode at level
        /// start; other systems (e.g. speed pads) may change it at runtime.
        /// </summary>
        [HideInInspector]
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump. Set from the active GameMode.
        /// </summary>
        [HideInInspector]
        public float jumpTakeOffSpeed = 7;

        // Knockback and hurt immunity tuning. Set from the active GameMode
        // (see ApplyGameModeTuning); these defaults are only a fallback.
        float knockbackSpeed = 3f;
        float knockbackLift = 3f;
        float knockbackMaxDuration = 3f;
        float hurtImmunityDuration = 1f;
        float immunityBlinkFrequency = 10f;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        [HideInInspector] public Collider2D collider2d { get; private set; }
        [HideInInspector] public AudioSource audioSource { get; private set; }
        [HideInInspector] public Health health { get; private set; }
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        PlayerGun playerGun;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        bool isKnockedBack;
        bool knockbackLeftGround;
        float knockbackDirection;
        float knockbackEndTime;

        float immuneUntil;
        bool hurtAnimationStarted;
        static readonly int HurtState = Animator.StringToHash("Player-Hurt");

        public Bounds Bounds => collider2d.bounds;

        /// <summary>
        /// 1 when the player faces right, -1 when facing left.
        /// </summary>
        public float FacingDirection => spriteRenderer.flipX ? -1f : 1f;

        /// <summary>
        /// True while a knockback is overriding the player's movement.
        /// </summary>
        public bool IsKnockedBack => isKnockedBack;

        /// <summary>
        /// True while the player ignores enemy damage after being hurt.
        /// </summary>
        public bool IsImmune => Time.time < immuneUntil;

        /// <summary>
        /// True while the Hurt animation is playing, including the frame of the hit
        /// before the Animator has entered the Hurt state.
        /// </summary>
        public bool IsHurtAnimationPlaying =>
            animator.GetCurrentAnimatorStateInfo(0).shortNameHash == HurtState
            || (IsImmune && !hurtAnimationStarted);

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            playerGun = GetComponent<PlayerGun>();
        }

        /// <summary>
        /// Configure the player from a game mode at level start: the tuning values and
        /// the Health max HP (which also refills it).
        /// </summary>
        public void ApplyGameMode(GameMode mode)
        {
            ApplyGameModeTuning(mode);
            health.SetMaxHP(mode.MaxHP);
        }

        /// <summary>
        /// Copy the movement, knockback and immunity values from a game mode. Safe to
        /// call at any time; used for live tuning in the Editor.
        /// </summary>
        public void ApplyGameModeTuning(GameMode mode)
        {
            maxSpeed = mode.MaxSpeed;
            jumpTakeOffSpeed = mode.JumpTakeOffSpeed;
            gravityModifier = mode.GravityModifier;
            knockbackSpeed = mode.KnockbackSpeed;
            knockbackLift = mode.KnockbackLift;
            knockbackMaxDuration = mode.KnockbackMaxDuration;
            hurtImmunityDuration = mode.HurtImmunityDuration;
            immunityBlinkFrequency = mode.ImmunityBlinkFrequency;

            // The mode decides which gun the player carries (null = no gun).
            if (playerGun != null && playerGun.CurrentGun != mode.Gun)
                playerGun.Equip(mode.Gun);
        }

        /// <summary>
        /// Push the player backwards (opposite to the facing direction) in a small arc.
        /// The player can't move until it lands; knockbackMaxDuration is only a safety
        /// limit in case it never lands.
        /// </summary>
        public void Knockback()
        {
            knockbackDirection = -FacingDirection;
            knockbackEndTime = Time.time + knockbackMaxDuration;
            knockbackLeftGround = false;
            isKnockedBack = true;

            // Cancel any pending jump input so it can't cut the arc short.
            jump = false;
            stopJump = false;
            jumpState = JumpState.InFlight;
            Bounce(knockbackLift);
        }

        /// <summary>
        /// Stop any active knockback (e.g. when the player dies), so it no longer
        /// drives horizontal movement.
        /// </summary>
        public void CancelKnockback()
        {
            isKnockedBack = false;
        }

        /// <summary>
        /// Start the post-hit immunity window. The sprite blinks once the Hurt
        /// animation has finished, until the window ends.
        /// </summary>
        public void StartHurtImmunity()
        {
            immuneUntil = Time.time + hurtImmunityDuration;
            hurtAnimationStarted = false;
        }

        /// <summary>
        /// End the post-hit immunity window early (e.g. when the player dies).
        /// </summary>
        public void ClearHurtImmunity()
        {
            immuneUntil = 0f;
        }

        protected override void Update()
        {
            if (controlEnabled && !isKnockedBack)
            {
                move.x = Input.GetAxis("Horizontal");
                if (jumpState == JumpState.Grounded && Input.GetButtonDown("Jump"))
                    jumpState = JumpState.PrepareToJump;
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            if (isKnockedBack) UpdateKnockback();
            UpdateJumpState();
            UpdateImmunityBlink();
            base.Update();
        }

        /// <summary>
        /// While immune, blink the sprite once the Hurt animation has played and
        /// left its state; always visible otherwise.
        /// </summary>
        void UpdateImmunityBlink()
        {
            var visible = true;
            if (IsImmune)
            {
                // The hurt trigger is processed by the Animator after this frame's
                // Update, so first wait for the Hurt state to start, then to end.
                if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == HurtState)
                    hurtAnimationStarted = true;
                else if (hurtAnimationStarted && immunityBlinkFrequency > 0f)
                    visible = Mathf.Repeat(Time.time * immunityBlinkFrequency, 1f) < 0.5f;
            }

            if (spriteRenderer.enabled != visible)
                spriteRenderer.enabled = visible;
        }

        void UpdateKnockback()
        {
            // IsGrounded is only refreshed in FixedUpdate, so wait until the player
            // has actually left the ground before treating "grounded" as a landing.
            if (!IsGrounded) knockbackLeftGround = true;

            var landed = knockbackLeftGround && IsGrounded;
            if (landed || Time.time >= knockbackEndTime)
                isKnockedBack = false;
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    // During a knockback, landing is detected once the knockback ends.
                    if (IsGrounded && !isKnockedBack)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
            if (isKnockedBack)
                targetVelocity.x = knockbackDirection * knockbackSpeed;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}