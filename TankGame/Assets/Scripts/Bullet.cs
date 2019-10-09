using UnityEngine;
using UnityEngine.Networking;

namespace TanksMP
{
	/// <summary>
	/// Projectile script for player shots with collision/hit logic.
	/// </summary>
	public class Bullet : MonoBehaviour
	{
		/// <summary>
		/// Projectile travel speed in units.
		/// </summary>
		public float speed = 10;
        
		/// <summary>
		/// Damage to cause on a player that gets hit.
		/// </summary>
		public int damage = 3;
        
		/// <summary>
		/// Delay until despawned automatically when nothing gets hit.
		/// </summary>
		public float despawnDelay = 1f;

		/// <summary>
		/// Clip to play when a player gets hit.
		/// </summary>
		public AudioClip hitClip;
        
		/// <summary>
		/// Clip to play when this projectile gets despawned.
		/// </summary>
		public AudioClip explosionClip;
        
		/// <summary>
		/// Object to spawn when a player gets hit.
		/// </summary>
		public GameObject hitFX;
        
		/// <summary>
		/// Object to spawn when this projectile gets despawned.
		/// </summary>
		public GameObject explosionFX;

		//reference to rigidbody component
		private Rigidbody myRigidbody;
		//reference to collider component
		private SphereCollider sphereCol;
        
		/// <summary>
		/// Player gameobject that spawned this projectile.
		/// </summary>
		[HideInInspector]
		public GameObject owner;

        public Material defaultMat;
        public Material DoubleMat;

        public bool DoubleDamage = false;

		//get component references
		void Awake ()
		{
			myRigidbody = GetComponent<Rigidbody> ();
			sphereCol = GetComponent<SphereCollider> ();
		}


		//set initial travelling velocity
		void OnSpawn ()
		{
			myRigidbody.velocity = speed * transform.forward;
        }

        public void PlayFX()
        {
            Player player = owner.GetComponent<Player>();
            if (player.shotFX)
            {
                PoolManager.Spawn(player.shotFX, player.shotPos.position, Quaternion.identity);
            }

            if (player.shotClip)
            {
                AudioManager.Play3D(player.shotClip, player.shotPos.position, 0.1f);
            }
        }
        
		//check what was hit on collisions
		void OnTriggerEnter (Collider col)
		{
            //cache corresponding gameobject that was hit
            GameObject obj = col.gameObject;
            //try to get a player component out of the collided gameobject
            Player player = obj.GetComponent<Player>();

            //we actually hit a player
            //do further checks
            if (player != null)
            {
                //ignore ourselves & disable friendly fire (same team index)
                if (player.gameObject == owner || player.gameObject == null)
                    return;
                else if (player.teamIndex == owner.GetComponent<Player>().teamIndex)
                    return;

                //create clips and particles on hit
                if (hitFX)
                    PoolManager.Spawn(hitFX, transform.position, Quaternion.identity);
                if (hitClip)
                    AudioManager.Play3D(hitClip, transform.position);

                //on the player that was hit, set the killing player to the owner of this bullet
                //maybe this owner really killed the player, but that check is done in the Player script
                //if (NetworkServer.active == true)
                //    player.killedBy = owner;
            }


            if (NetworkServer.active == true)
            {
                //apply bullet damage to the collided player
                //no need to check if the player is team mate since it will return above if it is a team mate.
                if (player)
                    player.TakeDamage(this);
            }

            PoolManager.Despawn(gameObject);
        }


		//set despawn effects and reset variables
		void OnDespawn ()
		{
            myRigidbody.velocity = Vector3.zero;
            myRigidbody.angularVelocity = Vector3.zero;

            //create clips and particles on despawn
            if (explosionFX)
                PoolManager.Spawn(explosionFX, transform.position, transform.rotation);
            if (explosionClip)
                AudioManager.Play3D(explosionClip, transform.position);
        }
    }
}