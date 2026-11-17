using System;
using UnityEngine;

namespace Platformer.Model
{
    /// <summary>
    /// A game mode: the player configuration used when a level starts.
    /// Designers create these assets (Create > Platformer > Game Mode) and pick the
    /// active one in GameController > Model > Game Mode. No code is needed.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameMode", menuName = "Platformer/Game Mode")]
    public class GameMode : ScriptableObject
    {
        [Header("Start")]
        [Tooltip("If on, the player starts and respawns at Start Position. If off, the scene's SpawnPoint is used.")]
        [SerializeField] private bool overrideStartPosition;
        [Tooltip("World position where the player starts and respawns (only used when Override Start Position is on).")]
        [SerializeField] private Vector2 startPosition;

        [Header("Health")]
        [Tooltip("Player hit points. Enemy hits remove their Contact Damage from this.")]
        [Min(1)]
        [SerializeField] private int maxHP = 3;

        [Header("Movement")]
        [Tooltip("Max horizontal speed of the player.")]
        [Min(0f)]
        [SerializeField] private float maxSpeed = 3f;
        [Tooltip("Initial upward velocity of a jump.")]
        [Min(0f)]
        [SerializeField] private float jumpTakeOffSpeed = 7f;
        [Tooltip("Gravity multiplier applied while the player is falling.")]
        [Min(0f)]
        [SerializeField] private float gravityModifier = 1f;

        [Header("Knockback")]
        [Tooltip("Horizontal speed of the knockback when hurt, opposite to the facing direction.")]
        [Min(0f)]
        [SerializeField] private float knockbackSpeed = 3f;
        [Tooltip("Initial upward speed of the knockback. Gravity turns it into a small arc.")]
        [Min(0f)]
        [SerializeField] private float knockbackLift = 3f;
        [Tooltip("Maximum time (seconds) the knockback overrides player input. It also ends on landing.")]
        [Min(0f)]
        [SerializeField] private float knockbackMaxDuration = 0.75f;

        [Header("Hurt Immunity")]
        [Tooltip("Seconds the player ignores enemies (no damage, no collision) after being hurt.")]
        [Min(0f)]
        [SerializeField] private float hurtImmunityDuration = 1f;
        [Tooltip("Blinks per second once the Hurt animation has finished, until immunity ends. 0 disables blinking.")]
        [Min(0f)]
        [SerializeField] private float immunityBlinkFrequency = 10f;

        [Header("Weapon")]
        [Tooltip("Gun the player carries in this mode (Create > Platformer > Gun). Leave empty for no gun.")]
        [SerializeField] private Gun gun;

        public bool OverrideStartPosition => overrideStartPosition;
        public Vector2 StartPosition => startPosition;
        public int MaxHP => maxHP;
        public float MaxSpeed => maxSpeed;
        public float JumpTakeOffSpeed => jumpTakeOffSpeed;
        public float GravityModifier => gravityModifier;
        public float KnockbackSpeed => knockbackSpeed;
        public float KnockbackLift => knockbackLift;
        public float KnockbackMaxDuration => knockbackMaxDuration;
        public float HurtImmunityDuration => hurtImmunityDuration;
        public float ImmunityBlinkFrequency => immunityBlinkFrequency;
        public Gun Gun => gun;

        /// <summary>
        /// Raised when the asset is edited in the Inspector. OnValidate only runs in
        /// the Editor, so this is used for live tuning while in Play mode.
        /// </summary>
        public event Action Changed;

        void OnValidate()
        {
            Changed?.Invoke();
        }
    }
}
