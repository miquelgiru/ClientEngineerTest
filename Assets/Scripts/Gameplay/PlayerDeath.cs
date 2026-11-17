using System.Collections;
using System.Collections.Generic;
using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when the player has died, i.e. when the player's health reaches 0
    /// (scheduled by HealthIsZero).
    /// </summary>
    /// <typeparam name="PlayerDeath"></typeparam>
    public class PlayerDeath : Simulation.Event<PlayerDeath>
    {
        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        /// <summary>
        /// Only run the death sequence if the player's health is actually depleted.
        /// </summary>
        public override bool Precondition() => !model.player.health.IsAlive;

        public override void Execute()
        {
            var player = model.player;
            model.virtualCamera.m_Follow = null;
            model.virtualCamera.m_LookAt = null;
            // player.collider.enabled = false;
            player.controlEnabled = false;
            player.CancelKnockback();
            player.ClearHurtImmunity();

            if (player.audioSource && player.ouchAudio)
                player.audioSource.PlayOneShot(player.ouchAudio);
            player.animator.SetTrigger("hurt");
            player.animator.SetBool("dead", true);
            Simulation.Schedule<PlayerSpawn>(2);
        }
    }
}