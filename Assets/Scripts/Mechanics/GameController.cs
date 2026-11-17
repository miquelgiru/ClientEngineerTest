using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This class exposes the the game model in the inspector, and ticks the
    /// simulation.
    /// </summary> 
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        //This model field is public and can be therefore be modified in the 
        //inspector.
        //The reference actually comes from the InstanceRegister, and is shared
        //through the simulation and events. Unity will deserialize over this
        //shared reference when the scene loads, allowing the model to be
        //conveniently configured inside the inspector.
        public PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        GameMode subscribedGameMode;

        void OnEnable()
        {
            Instance = this;
            SubscribeToGameModeChanges();
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            UnsubscribeFromGameModeChanges();
        }

        void Update()
        {
            if (Instance == this) Simulation.Tick();
        }

        void Start()
        {
            ApplyGameMode();
        }

        /// <summary>
        /// Configure the level from the active game mode: the player's settings and
        /// health, and optionally the start (and respawn) position.
        /// </summary>
        void ApplyGameMode()
        {
            var mode = model.gameMode;
            if (mode == null)
            {
                Debug.LogError("GameController: no Game Mode assigned (Model > Game Mode). " +
                               "The player keeps its default settings.", this);
                return;
            }

            model.player.ApplyGameMode(mode);

            if (mode.OverrideStartPosition)
            {
                // PlayerSpawn respawns at the spawn point, so moving it makes respawns use the mode too.
                var spawnZ = model.spawnPoint.position.z;
                model.spawnPoint.position = new Vector3(mode.StartPosition.x, mode.StartPosition.y, spawnZ);
                MovePlayerToStart(mode.StartPosition);
            }
        }

        /// <summary>
        /// Place the player at the mode's start position and snap the camera there,
        /// instead of letting the damped camera pan across the level.
        /// </summary>
        void MovePlayerToStart(Vector2 position)
        {
            var player = model.player;
            // Move the transform too: the body's new position only reaches the transform
            // on the next physics step, which may come after the camera's first update.
            player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
            player.Teleport(position);
            model.virtualCamera.PreviousStateIsValid = false;
        }

        // Live tuning: when the active asset is edited during Play (Editor only),
        // re-apply the player's tuning. Max HP and start position apply at level start.
        void SubscribeToGameModeChanges()
        {
            subscribedGameMode = model.gameMode;
            if (subscribedGameMode != null) subscribedGameMode.Changed += OnGameModeChanged;
        }

        void UnsubscribeFromGameModeChanges()
        {
            if (subscribedGameMode != null) subscribedGameMode.Changed -= OnGameModeChanged;
            subscribedGameMode = null;
        }

        void OnGameModeChanged()
        {
            model.player.ApplyGameModeTuning(subscribedGameMode);
        }
    }
}