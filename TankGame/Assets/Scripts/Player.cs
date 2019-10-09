using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace TanksMP
{
    /// <summary>
    /// Player class implementing movement control and shooting.
    /// </summary>

    [NetworkSettings(sendInterval = 0.05f)]
    public class Player : NetworkBehaviour
	{
		/// <summary>
		/// Player name
		/// </summary>
		[HideInInspector, SyncVar(hook = "OnChangeName")]
		public string myName;
        
		/// <summary>
		/// UI Text displaying the player name.
		/// </summary>
		public Text label;
        
		/// <summary>
		/// Team value assigned by the game manager.
		/// </summary>
		[HideInInspector, SyncVar(hook = "OnChangeIndex")]
		public int teamIndex;

        /// <summary>
        /// Current health value.
        /// </summary>
        [SyncVar(hook = "OnHealthChange")]
		public int health = 10;
        
		/// <summary>
		/// Maximum health value at game start.
		/// </summary>
		[HideInInspector]
		public int maxHealth;

        /// <summary>
        /// Current turret rotation and shooting direction.
        /// keep track of each player turrent rotation so it can shown to other player,
        /// use the command function cmd_TurretRotation to send the client's own turret rotation to the server
        /// </summary>
        [HideInInspector, SyncVar]
		public int turretRotation = 0;
        
		/// <summary>
		/// Delay between shots.
		/// </summary>
		public float fireRate = 0.75f;
        
		/// <summary>
		/// Movement speed in all directions.
		/// </summary>
		public float moveSpeed = 8f;

		/// <summary>
		/// UI Slider visualizing health value.
		/// </summary>
		public Slider healthSlider;

		/// <summary>
		/// Clip to play when a shot has been fired.
		/// </summary>
		public AudioClip shotClip;
        
		/// <summary>
		/// Clip to play on player death.
		/// </summary>
		public AudioClip explosionClip;
        
		/// <summary>
		/// Object to spawn on shooting.
		/// </summary>
		public GameObject shotFX;
        
		/// <summary>
		/// Object to spawn on player death.
		/// </summary>
		public GameObject explosionFX;

		/// <summary>
		/// Turret to rotate with look direction.
		/// </summary>
		public Transform turret;
        
		/// <summary>
		/// Position to spawn new bullets at.
		/// </summary>
		public Transform shotPos;
      
		/// <summary>
		/// Array of available bullets for shooting.
		/// </summary>
		public GameObject bullet;

        public GameObject HUD;
        
		/// <summary>
		/// MeshRenderers that should be highlighted in team color.
		/// </summary>
		public MeshRenderer[] renderers;

		/// <summary>
		/// Last player gameobject that killed this one.
		/// </summary>
		[HideInInspector]
		public GameObject killedBy;
        
		/// <summary>
		/// Reference to the camera following component.
		/// </summary>
		[HideInInspector]
		public FollowTarget camFollow;
		
		//timestamp when next shot should happen
		private float nextFire;
        
		//reference to this rigidbody
		private Rigidbody rb;

		/// <summary>
		/// Used to prevent player moving whilst respawning
		/// </summary>
		[HideInInspector]
		public bool disableInput = false;

        [HideInInspector, SyncVar(hook = "OnChangeAlive")]
        public bool bAlive = true;

        [HideInInspector, SyncVar]
        public bool DoubleDamage = false;

        //private bool isLocalPlayer;

        void Awake ()
		{
			maxHealth = health;
        }

		void Start ()
		{
            OnChangeName(myName);

            OnHealthChange(health);

            //get components and set camera target
            rb = GetComponent<Rigidbody>();

            if (isLocalPlayer)
            {
                GameManager.GetInstance().localPlayer = this;

                //get the player name stored in player prefs and send it to the server
                Cmd_SendName(ApplicationData.PlayerName);

                //inhereted from NetworkBehaviour
                //isLocalPlayer = true;

                camFollow = Camera.main.GetComponent<FollowTarget>();
                camFollow.target = turret;

                //mute while testing
                AudioListener.volume = 0.0f;

                //need to do it after adding the localplayer, not on gamemanager start()
                GameManager.GetInstance().GamePause();
            }

            //if (isServer)
            //{
            //    //Debug.Log("player: " + GameManager.GetInstance().size.Count);

            //    int newTeamIndex = GameManager.GetInstance().GetTeamFill();
            //    teamIndex = newTeamIndex;

            //    //get spawn position for this team and instantiate the player there
            //    Vector3 startPos = GameManager.GetInstance().GetSpawnPosition(teamIndex);

            //    //gameObject.transform.SetPositionAndRotation(startPos, Quaternion.identity);
            //    rb.MovePosition(startPos);

            //    GameManager.GetInstance().size[teamIndex]++;
            //    GameManager.GetInstance().Rpc_OnChangeSize();
            //}

            if (isServer)
            {
                //GameManager.GetInstance().size[teamIndex]++;
                GameManager.GetInstance().Rpc_OnChangeSize();
            }

            OnChangeAlive(bAlive);
            OnChangeIndex(teamIndex);
        }

        void OnChangeIndex(int newIndex)
        {
            //get corresponding team and colorise renderers in team color
            Team team = GameManager.GetInstance().teams[teamIndex];
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material = team.material;
        }

        void OnEnable ()
		{
			OnHealthChange (health);
		}

		void FixedUpdate ()
		{
            if (isLocalPlayer && !disableInput)
            {
                //check for frozen Y position, regardless of other position constraints
                if ((rb.constraints & RigidbodyConstraints.FreezePositionY) != RigidbodyConstraints.FreezePositionY)
                {
                    //Y position is not locked and the player is above normal height, apply additional gravity
                    if (transform.position.y > 0)
                        rb.AddForce(Physics.gravity * 2f, ForceMode.Acceleration);
                }

                //movement variables
                Vector2 moveDir;
                Vector2 turnDir;

                //reset moving input when no arrow keys are pressed down
                if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
                {
                    moveDir.x = 0;
                    moveDir.y = 0;
                }
                else
                {
                    //read out moving directions and calculate force
                    moveDir.x = Input.GetAxis("Horizontal");
                    moveDir.y = Input.GetAxis("Vertical");
                    Move(moveDir);
                }

                //cast a ray on a plane at the mouse position for detecting where to shoot 
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Plane plane = new Plane(Vector3.up, Vector3.up);
                float distance = 0f;
                Vector3 hitPos = Vector3.zero;
                //the hit position determines the mouse position in the scene
                if (plane.Raycast(ray, out distance))
                {
                    hitPos = ray.GetPoint(distance) - transform.position;
                }

                //we've converted the mouse position to a direction
                turnDir = new Vector2(hitPos.x, hitPos.z);

                //rotate turret to look at the mouse direction
                RotateTurret(turnDir);

                //shoot bullet on left mouse click
                if (Input.GetButton("Fire1"))
                {
                    int newRotation = (int)(Quaternion.LookRotation(new Vector3(turnDir.x, 0, turnDir.y)).eulerAngles.y + camFollow.camTransform.eulerAngles.y);
                    if (isServer)
                        Cmd_Shoot(newRotation);
                    else
                        Shoot(newRotation);
                }            
            }
            else
            {
                turret.rotation = Quaternion.Euler(0, turretRotation, 0);
            }
		}
        
		//moves rigidbody in the direction passed in
		void Move (Vector2 direction = default(Vector2))
		{
			//if direction is not zero, rotate player in the moving direction relative to camera
			if (direction != Vector2.zero)
				transform.rotation = Quaternion.LookRotation (new Vector3 (direction.x, 0, direction.y))
				* Quaternion.Euler (0, camFollow.camTransform.eulerAngles.y, 0);
            
			//create movement vector based on current rotation and speed
			Vector3 movementDir = transform.forward * moveSpeed * Time.deltaTime;
			//apply vector to rigidbody position
			rb.MovePosition (rb.position + movementDir);
		}
        
		//rotates turret to the direction passed in
		void RotateTurret (Vector2 direction = default(Vector2))
		{
			//don't rotate without values
			if (direction == Vector2.zero)
				return;

			//get rotation value as angle out of the direction we received
			int newRotation = (int)(Quaternion.LookRotation (new Vector3 (direction.x, 0, direction.y)).eulerAngles.y + camFollow.camTransform.eulerAngles.y);
            
			turretRotation = newRotation;
            Cmd_SetTurretRotation(newRotation);

            turret.rotation = Quaternion.Euler (0, newRotation, 0);
		}

        [Command(channel = 2)]
        void Cmd_SetTurretRotation(int newRotation)
        {
            turretRotation = newRotation;
        }


        protected void Shoot(int newRotation)
        {
            if (Time.time > nextFire)
            {
                Cmd_Shoot(newRotation);

                //set next shot timestamp
                nextFire = Time.time + fireRate;

                SpawnBullet(newRotation, DoubleDamage);
            }
        }

		//shoots a bullet in the direction passed in
		//we do not rely on the current turret rotation here, because we send the direction
		//along with the shot request to the server to absolutely ensure a synced shot position
        //use the int rotation that was previously used to send less data
        [Command(channel = 0)]
		protected void Cmd_Shoot (int newRotation)
		{            
			//if shot delay is over
			if (Time.time > nextFire)
			{
                Rpc_Shoot(newRotation, DoubleDamage);
                //set next shot timestamp
                nextFire = Time.time + fireRate;

                SpawnBullet(newRotation, DoubleDamage);
            }
		}

        /// <summary>
        /// shoot on all other clients except the local player since we already created that bullet
        /// the doubledmg might get replicated too late for the other clients
        /// </summary>
        /// <param name="newRotation"></param>
        [ClientRpc(channel = 0)]
        protected void Rpc_Shoot (int newRotation, bool LatestDoubledmg)
        {
            if (!isLocalPlayer && !isServer)
            {
                SpawnBullet(newRotation, LatestDoubledmg);
            }
        }

        protected void SpawnBullet(int newRotation, bool DoubleDmg)
        {
            //spawn bullet using pooling, locally
            GameObject obj = PoolManager.Spawn(bullet, shotPos.position, Quaternion.Euler(0, newRotation, 0));
            Bullet blt = obj.GetComponent<Bullet>();
            blt.owner = gameObject;

            Debug.Log("Double Damage is: " + DoubleDmg);
            Debug.LogError("Double Damage is: " + DoubleDmg);
            MeshRenderer meshRenderer = blt.GetComponent<MeshRenderer>();
            Material[] materials = meshRenderer.materials;
            if (DoubleDmg)
                materials[0] = blt.DoubleMat;
            else
                materials[0] = blt.defaultMat;

            meshRenderer.materials = materials;

            blt.DoubleDamage = DoubleDmg;
        }


        //hook for updating health locally
        protected void OnHealthChange (int value)
		{
			health = value;
			healthSlider.value = (float)health / maxHealth;
		}

		/// <summary>
		/// Calculate damage to be taken by the Player,
		/// triggers score increase and respawn workflow on death.
        /// should only run on the server, respawn and score change on server as well
		/// </summary>
		public void TakeDamage (Bullet bullet)
		{
            if (!isServer)
                return;

			//substract health by damage
            int damage = bullet.damage * (bullet.DoubleDamage ? 2 : 1);
			health -= damage;
            //OnHealthChange (health);
            Debug.Log("Server Damage applied to :" + this.ToString() + " by: " + bullet.owner.ToString());


            //bullet killed the player
            if (health <= 0)
			{
                //the game is already over so don't do anything
                if (GameManager.GetInstance ().gameOver)
					return;

                killedBy = bullet.owner;
                //get killer and increase score for that team
                Player other = bullet.owner.GetComponent<Player> ();
				GameManager.GetInstance ().score [other.teamIndex]++;
                GameManager.GetInstance().Rpc_OnChangeScore();

                
                //GameManager.GetInstance ().ui.OnTeamScoreChanged (other.teamIndex);
                //the maximum score has been reached now
                if (GameManager.GetInstance ().IsGameOver ())
				{
                    //tell client the winning team
                    GameManager.GetInstance().Rpc_GameOver(other.teamIndex);
					return;
				}

				//the game is not over yet, reset runtime values
				health = maxHealth;
				OnHealthChange (health);

                Rpc_Killed(killedBy);

                bAlive = false;
                Hide(true);
                StartCoroutine(SpawnRoutine());
            }
		}

		/// <summary>
		/// This is when the respawn delay is over
		/// </summary>
        [ClientRpc]
		public virtual void Rpc_Killed (GameObject killer)
		{
            Debug.Log("Run on client");
            Debug.Log("Client Killby:" + killer.ToString());

            killedBy = killer;

            Hide(true);

            Debug.Log(gameObject.activeInHierarchy);
            //detect whether the current user was responsible for the kill
            //yes, that's my kill: increase local kill counter
            //can't kill ourselves and add that to the kill counter
            if (!isLocalPlayer && killer == GameManager.GetInstance().localPlayer.gameObject)
            {
                GameManager.GetInstance().ui.killCounter[0].text = (int.Parse(GameManager.GetInstance().ui.killCounter[0].text) + 1).ToString();
                GameManager.GetInstance().ui.killCounter[0].GetComponent<Animator>().Play("Animation");
            }

            if (explosionFX)
            {
                //spawn death particles locally using pooling and colorize them in the player's team color
                GameObject particle = PoolManager.Spawn(explosionFX, transform.position, transform.rotation);
                ParticleColor pColor = particle.GetComponent<ParticleColor>();
                if (pColor)
                    pColor.SetColor(GameManager.GetInstance().teams[teamIndex].material.color);
            }

            //play sound clip on player death
            if (explosionClip)
                AudioManager.Play3D(explosionClip, transform.position);


            //further changes only affect the local player
            if (isLocalPlayer)
            {
                StartCoroutine(SpawnRoutine());
                //local player was killed, set camera to follow the killer
                camFollow.target = killedBy.transform;
                //disable input
                disableInput = true;
                //display respawn window (only for local player)
                GameManager.GetInstance().DisplayDeath();
            }
		}

        void Hide(bool bhide)
        {
            if (bhide)
            {
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].enabled = false;
                //get components and set camera target
                rb.Sleep();
                GetComponent<BoxCollider>().enabled = false;
                HUD.SetActive(false);


            }
            else
            {
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].enabled = true;
                //get components and set camera target
                rb.WakeUp();
                GetComponent<BoxCollider>().enabled = true;
                HUD.SetActive(true);
            }
        }

        //coroutine spawning the player after a respawn delay
        IEnumerator SpawnRoutine()
        {
            //calculate point in time for respawn
            float targetTime = Time.time + GameManager.GetInstance().respawnTime;

            //wait for the respawn to be over,
            //while waiting update the respawn countdown
            while (targetTime - Time.time > 0)
            {
                if (isLocalPlayer)
                {
                    GameManager.GetInstance().ui.SetSpawnDelay(targetTime - Time.time);
                }
                
                yield return null;
            }

            //respawn now: send request to the server
            if (isLocalPlayer)
            {
                GameManager.GetInstance().ui.DisableDeath();
            }
            if (isServer)
            {
                bAlive = true;
                Hide(false);

                Rpc_Respawn();
            }
           
        }

        [ClientRpc]
        void Rpc_Respawn()
        {
            Hide(false);

            if (isLocalPlayer)
            {
                ResetPosition();
            }
        }

        void OnChangeAlive(bool newALive)
        {
            Hide(!newALive);
        }

        /// <summary>
        /// Repositions in team area and resets camera & input variables.
        /// This should only be called for the local player.
        /// </summary>
        public void ResetPosition ()
		{
			if (!isLocalPlayer)
				return;
			//start following the local player again
			camFollow.target = turret;

            if (GameManager.GetInstance().CanEnableInput())
                disableInput = false;

            //get team area and reposition it there
            transform.position = GameManager.GetInstance ().GetSpawnPosition (teamIndex);
            
			//reset forces modified by input
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}

        ////called on game end providing the winning team
        //[ClientRpc]
        //void Rpc_GameOver(int teamIndex)
        //{
        //    //display game over window
        //    GameManager.GetInstance().DisplayGameOver(teamIndex);
        //}


        [Command]
        void Cmd_SendName(string PlayerName)
        {
            myName = PlayerName;
        }

        void OnChangeName(string newName)
        {
            label.text = newName;
        }

        [Command]
        public void Cmd_Restart()
        {
            if (GameManager.GetInstance().gameOver)
            {
                NetworkGameManager.GetInstance().ServerChangeScene("Game");
                Debug.Log("NetworkGameManagerFound");
                //obj.GetComponent<NetworkGameManager>().ServerChangeScene("Game");

            }
        }

        /// <summary>
        /// use int as powerup, can also be an enum but since its only double damage right now
        /// </summary>
        /// <param name="powerup"></param>
        public void ActivatePowerup(int powerup, float time)
        {
            if (isServer)
            {
                DoubleDamage = true;
                StartCoroutine(DeactivtePowerup(powerup, time));
            } 
        }

        /// <summary>
        /// deactivate the powerup after the time
        /// </summary>
        /// <param name="powerup"></param>
        /// <returns></returns>
        IEnumerator DeactivtePowerup(int powerup, float time)
        {
            yield return new WaitForSeconds(time);
            DoubleDamage = false;
        }

    }
}