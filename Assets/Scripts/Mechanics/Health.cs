using System;
using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Represents the current vital statistics of some game entity.
    /// </summary>
    public class Health : MonoBehaviour
    {
        /// <summary>
        /// The maximum hit points for the entity.
        /// </summary>
        [Min(1)]
        public int maxHP = 1;

        /// <summary>
        /// Indicates if the entity should be considered 'alive'.
        /// </summary>
        public bool IsAlive => currentHP > 0;

        /// <summary>
        /// The current hit points of the entity.
        /// </summary>
        public int CurrentHP => currentHP;

        int currentHP;

        /// <summary>
        /// Increment the HP of the entity.
        /// </summary>
        public void Increment()
        {
            currentHP = Mathf.Clamp(currentHP + 1, 0, maxHP);
        }

        /// <summary>
        /// Decrement the HP of the entity by 1.
        /// </summary>
        public void Decrement()
        {
            Decrement(1);
        }

        /// <summary>
        /// Decrement the HP of the entity by the given amount. Will trigger a
        /// HealthIsZero event once, when current HP reaches 0.
        /// </summary>
        public void Decrement(int amount)
        {
            if (amount <= 0 || !IsAlive) return;

            currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
            if (currentHP == 0)
            {
                var ev = Schedule<HealthIsZero>();
                ev.health = this;
            }
        }

        /// <summary>
        /// Decrement the HP of the entity until HP reaches 0.
        /// </summary>
        public void Die()
        {
            Decrement(currentHP);
        }

        /// <summary>
        /// Restore the HP of the entity to its maximum value.
        /// </summary>
        public void ResetToMax()
        {
            currentHP = maxHP;
        }

        /// <summary>
        /// Set the maximum HP (at least 1) and refill the entity to it.
        /// Used to configure the player from the active game mode.
        /// </summary>
        public void SetMaxHP(int value)
        {
            maxHP = Mathf.Max(1, value);
            ResetToMax();
        }

        void Awake()
        {
            currentHP = maxHP;
        }
    }
}
