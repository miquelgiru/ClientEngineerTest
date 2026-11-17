using Platformer.Core;
using Platformer.Mechanics;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when the player takes damage but survives.
    /// </summary>
    /// <typeparam name="PlayerHurt"></typeparam>
    public class PlayerHurt : Simulation.Event<PlayerHurt>
    {
        public PlayerController player;

        /// <summary>
        /// A later hit in the same frame may already have killed the player;
        /// in that case PlayerDeath handles the feedback instead.
        /// </summary>
        public override bool Precondition() => player.health.IsAlive;

        public override void Execute()
        {
            if (player.audioSource && player.ouchAudio)
                player.audioSource.PlayOneShot(player.ouchAudio);
            player.animator.SetTrigger("hurt");
            player.Knockback();
        }
    }
}
