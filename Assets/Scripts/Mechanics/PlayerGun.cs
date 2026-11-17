using Platformer.Gameplay;
using Platformer.Model;
using UnityEngine;
using UnityEngine.Pool;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Lets the player carry and fire a Gun (a ScriptableObject). Fire1 fires a
    /// bullet in the facing direction. Bullets come from an object pool.
    /// The active Game Mode chooses the gun at level start (empty = no gun).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerGun : MonoBehaviour
    {
        const int PoolDefaultCapacity = 10;
        const int PoolMaxSize = 50;

        [Tooltip("Gun used when no Game Mode is assigned. The active Game Mode's gun replaces it at level start (empty = no gun).")]
        [SerializeField] private Gun gun;
        [Tooltip("Bullet prefab instantiated by the bullet pool.")]
        [SerializeField] private Bullet bulletPrefab;
        [Tooltip("Renderer that draws the gun model (child of the player).")]
        [SerializeField] private SpriteRenderer gunRenderer;

        PlayerController player;
        SpriteRenderer playerRenderer;
        ObjectPool<Bullet> bulletPool;
        Transform bulletContainer;
        int bulletsLeft;
        float nextFireTime;

        /// <summary>
        /// The gun currently carried, or null when the player has no gun.
        /// </summary>
        public Gun CurrentGun => gun;

        /// <summary>
        /// Remaining bullets when the gun's ammo is limited.
        /// </summary>
        public int BulletsLeft => bulletsLeft;

        bool HasAmmo => gun.UnlimitedBullets || bulletsLeft > 0;

        bool CanFire => gun != null
            && player.controlEnabled
            && player.health.IsAlive
            && !player.IsHurtAnimationPlaying
            && Time.timeScale > 0f          // main menu open
            && Time.time >= nextFireTime
            && HasAmmo;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            playerRenderer = GetComponent<SpriteRenderer>();
            if (bulletPrefab == null) Debug.LogError("PlayerGun: Bullet Prefab is not assigned.", this);
            if (gunRenderer == null) Debug.LogError("PlayerGun: Gun Renderer is not assigned.", this);

            bulletPool = new ObjectPool<Bullet>(CreateBullet, OnGetBullet, OnReleaseBullet, OnDestroyBullet,
                collectionCheck: true, defaultCapacity: PoolDefaultCapacity, maxSize: PoolMaxSize);
            // Only refill here: PlayerController.Awake (which sets player.health) may run
            // after this one. The model is updated every frame in Update.
            RefillAmmo();
        }

        void OnEnable()
        {
            PlayerSpawn.OnExecute += OnPlayerSpawn;
        }

        void OnDisable()
        {
            PlayerSpawn.OnExecute -= OnPlayerSpawn;
        }

        void OnDestroy()
        {
            bulletPool?.Clear();
        }

        /// <summary>
        /// Carry a gun (null = no gun). Refills its ammo.
        /// </summary>
        public void Equip(Gun newGun)
        {
            gun = newGun;
            RefillAmmo();
            UpdateGunModel();
        }

        void OnPlayerSpawn(PlayerSpawn ev)
        {
            RefillAmmo();
        }

        void RefillAmmo()
        {
            bulletsLeft = gun != null ? gun.BulletLimit : 0;
            nextFireTime = 0f;
        }

        void Update()
        {
            UpdateGunModel();
            if (Input.GetButtonDown("Fire1") && CanFire)
                Fire();
        }

        void Fire()
        {
            var direction = player.FacingDirection;
            var origin = (Vector2)transform.position + MirroredHoldOffset(direction);
            bulletPool.Get().Launch(origin, direction, gun, bulletPool);

            if (!gun.UnlimitedBullets) bulletsLeft--;
            if (gun.FireRate > 0f) nextFireTime = Time.time + 1f / gun.FireRate;
        }

        /// <summary>
        /// Draw the gun on the facing side. It is hidden with no gun, when the player
        /// is dead, or while the player sprite is hidden by the immunity blink.
        /// </summary>
        void UpdateGunModel()
        {
            if (gunRenderer == null || player == null) return;

            var visible = gun != null && player.health.IsAlive && playerRenderer.enabled;
            gunRenderer.enabled = visible;
            if (!visible) return;

            var direction = player.FacingDirection;
            gunRenderer.sprite = gun.GunSprite;
            gunRenderer.flipX = direction < 0f;
            gunRenderer.transform.localPosition = MirroredHoldOffset(direction);
        }

        Vector2 MirroredHoldOffset(float direction)
        {
            return new Vector2(gun.HoldOffset.x * direction, gun.HoldOffset.y);
        }

        // Object pool callbacks.
        Bullet CreateBullet()
        {
            if (bulletContainer == null) bulletContainer = new GameObject("Player Bullets").transform;
            return Instantiate(bulletPrefab, bulletContainer);
        }

        void OnGetBullet(Bullet bullet) => bullet.gameObject.SetActive(true);

        void OnReleaseBullet(Bullet bullet) => bullet.gameObject.SetActive(false);

        void OnDestroyBullet(Bullet bullet)
        {
            if (bullet != null) Destroy(bullet.gameObject);
        }
    }
}
