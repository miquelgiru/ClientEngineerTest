using Platformer.Core;
using Platformer.Mechanics;
using static Platformer.Core.Simulation;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when a player bullet hits an enemy. Applies the gun's damage to the
    /// enemy's Health; the enemy dies (EnemyDeath) when its health reaches 0.
    /// </summary>
    /// <typeparam name="BulletHitEnemy"></typeparam>
    public class BulletHitEnemy : Simulation.Event<BulletHitEnemy>
    {
        public EnemyController enemy;
        public int damage;

        /// <summary>
        /// The enemy may already have been killed earlier in this frame.
        /// </summary>
        public override bool Precondition() => enemy.IsAlive;

        public override void Execute()
        {
            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth == null)
            {
                // Enemies without Health die in one hit, like a stomp.
                Schedule<EnemyDeath>().enemy = enemy;
                return;
            }

            enemyHealth.Decrement(damage);
            if (enemyHealth.IsAlive)
                enemy.PlayHurt();
            else
                Schedule<EnemyDeath>().enemy = enemy;
        }
    }
}
