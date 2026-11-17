using Platformer.Core;
using Platformer.Mechanics;
using Platformer.Model;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Gameplay
{

    /// <summary>
    /// Fired when a Player collides with an Enemy.
    /// </summary>
    /// <typeparam name="EnemyCollision"></typeparam>
    public class PlayerEnemyCollision : Simulation.Event<PlayerEnemyCollision>
    {
        public EnemyController enemy;
        public PlayerController player;

        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        /// <summary>
        /// A dead player can neither be hurt nor stomp enemies.
        /// </summary>
        public override bool Precondition() => player.health.IsAlive;

        public override void Execute()
        {
            var willHurtEnemy = player.Bounds.center.y >= enemy.Bounds.max.y;

            if (willHurtEnemy)
            {
                var enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.Decrement();
                    if (!enemyHealth.IsAlive)
                    {
                        Schedule<EnemyDeath>().enemy = enemy;
                        player.Bounce(2);
                    }
                    else
                    {
                        player.Bounce(7);
                    }
                }
                else
                {
                    Schedule<EnemyDeath>().enemy = enemy;
                    player.Bounce(2);
                }
            }
            else
            {
                HurtPlayer();
            }
        }

        /// <summary>
        /// Apply this enemy's contact damage to the player. Death is handled by
        /// Health (HealthIsZero -> PlayerDeath); surviving schedules PlayerHurt.
        /// </summary>
        void HurtPlayer()
        {
            var damage = enemy.ContactDamage;
            if (damage <= 0) return;

            player.health.Decrement(damage);
            if (player.health.IsAlive)
                Schedule<PlayerHurt>().player = player;
        }
    }
}