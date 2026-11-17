using UnityEngine;

namespace Platformer.Model
{
    /// <summary>
    /// A gun the player can carry: its look, its bullets, ammo, fire rate and damage.
    /// Designers create these assets (Create > Platformer > Gun) and give one to a
    /// Game Mode (GameMode > Weapon > Gun). No code is needed.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGun", menuName = "Platformer/Gun")]
    public class Gun : ScriptableObject
    {
        [Header("Model")]
        [Tooltip("Sprite of the gun, drawn on the player.")]
        [SerializeField] private Sprite gunSprite;
        [Tooltip("Where the gun sits relative to the player when facing right (mirrored when facing left). Bullets are fired from here.")]
        [SerializeField] private Vector2 holdOffset = new Vector2(0.25f, -0.15f);

        [Header("Bullets")]
        [Tooltip("Sprite of the bullet.")]
        [SerializeField] private Sprite bulletSprite;
        [Tooltip("Bullet speed in units per second.")]
        [Min(0.1f)]
        [SerializeField] private float bulletSpeed = 12f;
        [Tooltip("Seconds a bullet flies before it disappears (if it hits nothing).")]
        [Min(0.05f)]
        [SerializeField] private float bulletLifetime = 1f;

        [Header("Ammo")]
        [Tooltip("If on, the gun never runs out of bullets.")]
        [SerializeField] private bool unlimitedBullets = true;
        [Tooltip("Bullets available when Unlimited Bullets is off. Refilled when the player respawns.")]
        [Min(1)]
        [SerializeField] private int bulletLimit = 10;

        [Header("Firing")]
        [Tooltip("Maximum shots per second. 0 = no limit (one shot per key press, as fast as the player presses).")]
        [Min(0f)]
        [SerializeField] private float fireRate = 0f;

        [Header("Damage")]
        [Tooltip("Damage each bullet deals to an enemy it hits.")]
        [Min(1)]
        [SerializeField] private int damage = 1;

        public Sprite GunSprite => gunSprite;
        public Vector2 HoldOffset => holdOffset;
        public Sprite BulletSprite => bulletSprite;
        public float BulletSpeed => bulletSpeed;
        public float BulletLifetime => bulletLifetime;
        public bool UnlimitedBullets => unlimitedBullets;
        public int BulletLimit => bulletLimit;
        public float FireRate => fireRate;
        public int Damage => damage;
    }
}
