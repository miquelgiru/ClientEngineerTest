using Platformer.Core;
using Platformer.Mechanics;
using Platformer.Model;
using static Platformer.Core.Simulation;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when a Health component reaches 0 HP. For the player this results
    /// in a PlayerDeath event. Enemy deaths are scheduled by whatever damaged
    /// the enemy (e.g. PlayerEnemyCollision).
    /// </summary>
    /// <typeparam name="HealthIsZero"></typeparam>
    public class HealthIsZero : Simulation.Event<HealthIsZero>
    {
        public Health health;

        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            if (health == model.player.health)
                Schedule<PlayerDeath>();
        }
    }
}