using Platformer.Gameplay;
using Platformer.Model;
using UnityEngine;
using UnityEngine.Pool;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// A pooled bullet fired by PlayerGun. It flies in a straight line and returns
    /// to its pool when it hits an enemy or the level, or when its lifetime ends.
    /// Hitting an enemy schedules a BulletHitEnemy event.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
    public class Bullet : MonoBehaviour
    {
        Rigidbody2D body;
        SpriteRenderer spriteRenderer;

        IObjectPool<Bullet> pool;
        int damage;
        float despawnTime;
        // Guards against releasing twice (e.g. two hits in the same physics step).
        bool isActive;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Fire this bullet from origin in a horizontal direction (1 = right, -1 = left).
        /// </summary>
        public void Launch(Vector2 origin, float direction, Gun gun, IObjectPool<Bullet> ownerPool)
        {
            pool = ownerPool;
            damage = gun.Damage;
            despawnTime = Time.time + gun.BulletLifetime;
            isActive = true;

            spriteRenderer.sprite = gun.BulletSprite;
            spriteRenderer.flipX = direction < 0f;

            transform.position = origin;
            body.position = origin;
            body.linearVelocity = new Vector2(direction * gun.BulletSpeed, 0f);
        }

        void Update()
        {
            if (isActive && Time.time >= despawnTime)
                Despawn();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // Ignore other triggers (tokens, zones, bullets) and the shooter.
            if (!isActive || other.isTrigger) return;
            if (other.GetComponent<PlayerController>() != null) return;

            var enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                var ev = Schedule<BulletHitEnemy>();
                ev.enemy = enemy;
                ev.damage = damage;
            }

            // Enemies and level geometry stop the bullet.
            Despawn();
        }

        void Despawn()
        {
            isActive = false;
            body.linearVelocity = Vector2.zero;
            pool.Release(this);
        }
    }
}
