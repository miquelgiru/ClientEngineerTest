using System.Collections;
using System.Collections.Generic;
using Platformer.Gameplay;
using Platformer.Model;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// A simple controller for enemies. Provides movement control over a patrol path.
    /// </summary>
    [RequireComponent(typeof(AnimationController), typeof(Collider2D))]
    public class EnemyController : MonoBehaviour
    {
        public PatrolPath path;
        public AudioClip ouch;

        [Tooltip("Damage dealt to the player when this enemy hits them. Stomping the enemy is always safe.")]
        [Min(0)]
        [SerializeField] private int contactDamage = 1;

        /// <summary>
        /// Damage dealt to the player on a collision that hurts the player.
        /// </summary>
        public int ContactDamage => contactDamage;

        internal PatrolPath.Mover mover;
        internal AnimationController control;
        internal Collider2D _collider;
        internal AudioSource _audio;
        SpriteRenderer spriteRenderer;

        readonly PlatformerModel model = GetModel<PlatformerModel>();
        bool ignoringPlayer;

        public Bounds Bounds => _collider.bounds;

        void Awake()
        {
            control = GetComponent<AnimationController>();
            _collider = GetComponent<Collider2D>();
            _audio = GetComponent<AudioSource>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                var ev = Schedule<PlayerEnemyCollision>();
                ev.player = player;
                ev.enemy = this;
            }
        }

        void Update()
        {
            UpdatePlayerCollision();
            if (path != null)
            {
                if (mover == null) mover = path.CreateMover(control.maxSpeed * 0.5f);
                control.move.x = Mathf.Clamp(mover.Position.x - transform.position.x, -1, 1);
            }
        }

        /// <summary>
        /// While the player is immune, this enemy and the player pass through each
        /// other (no contacts, no blocking). The pair keeps ignoring each other until
        /// they stop overlapping, so neither gets stuck inside the other.
        /// Dead enemies (collider disabled by EnemyDeath) are skipped.
        /// </summary>
        void UpdatePlayerCollision()
        {
            var player = model.player;
            if (player == null || !_collider.enabled) return;

            var ignore = player.IsImmune
                || (ignoringPlayer && _collider.Distance(player.collider2d).isOverlapped);
            if (ignore == ignoringPlayer) return;

            Physics2D.IgnoreCollision(_collider, player.collider2d, ignore);
            ignoringPlayer = ignore;
        }

    }
}