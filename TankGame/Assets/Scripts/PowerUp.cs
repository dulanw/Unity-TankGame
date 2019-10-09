using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace TanksMP
{
    [NetworkSettings(sendInterval = 0.05f)]
    public class PowerUp : NetworkBehaviour
    {
        /// <summary>
        /// rotation speed
        /// </summary>
        public float RotationSpeed = 45.0f;

        /// <summary>
        /// time to respawn after player picks up
        /// </summary>
        public float RespawnTime = 15.0f;

        /// <summary>
        /// how long the player can use the powerup for
        /// </summary>
        public float UseTime = 5.0f;

        /// <summary>
        /// the model of the powerup, can be changed to whatever
        /// </summary>
        public GameObject Model3d;

        [SyncVar(hook = "OnChangeActive")]
        private bool isActive = true;
        // Use this for initialization
        void Start()
        {
            OnChangeActive(isActive);
        }

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(Vector3.up * (RotationSpeed * Time.deltaTime));
        }

        void OnTriggerEnter(Collider col)
        {
            if (!isServer || !isActive)
                return;

            //cache corresponding gameobject that was hit
            GameObject obj = col.gameObject;
            //try to get a player component out of the collided gameobject
            Player player = obj.GetComponent<Player>();

            if (player != null)
            {
                isActive = false;
                player.ActivatePowerup(0, UseTime);

                OnChangeActive(isActive);
                StartCoroutine(Respawn());
            }
        }

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(RespawnTime);

            Debug.LogError("Powerup Respawned");
            isActive = true;
            OnChangeActive(isActive);
        }

        void OnChangeActive(bool newActive)
        {
            Model3d.SetActive(newActive);
        }
    }
}
