using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.SceneManagement;

namespace TanksMP
{

    public class NetworkGameManager : NetworkManager
    {
        //reference to this script instance
        private static NetworkGameManager instance;

        //initialize variables
        void Awake()
        {
            instance = this;
        }

        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static NetworkGameManager GetInstance()
        {
            return instance;
        }

        // Use this for initialization
        void Start()
        {
            //maxDelay = 0.15f;
        }

        // Update is called once per frame
        void Update()
        {

        }


        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
        {
            //base.OnServerAddPlayer(conn, playerControllerId);
            //get the team value for this player
            StartCoroutine(SpawnPlayer(conn, playerControllerId));
            //base.OnServerAddPlayer();
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            GameObject obj = conn.playerControllers[0].gameObject;

            if (obj != null && GameManager.GetInstance().size.Count > 0)
            {
                int teamIndex = obj.GetComponent<Player>().teamIndex;
                //Debug.Log(teamIndex);

                GameManager.GetInstance().size[teamIndex]--;
                GameManager.GetInstance().Rpc_OnChangeSize();
            }

            base.OnServerDisconnect(conn);
        }

        /// <summary>
        /// When the server is shut down
        /// </summary>
        /// <param name="connection"></param>
        public override void OnClientDisconnect(NetworkConnection connection)
        {
            ApplicationData.ServerClosed = true;
            NetworkGameManager NetManager = NetworkGameManager.GetInstance();
            MatchInfo matchInfo = NetManager.matchInfo;
            NetManager.matchMaker.DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, NetManager.OnDropConnection);
            NetManager.StopHost();

            NetworkGameManager.Shutdown();

            SceneManager.LoadScene(0);
            base.OnClientDisconnect(connection);
        }

        public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player)
        {
            base.OnServerRemovePlayer(conn, player);

            //Debug.Log("Removed From Server");
            //GameObject obj = conn.playerControllers[0].gameObject;

            //if (obj != null)
            //{
                
            //    int teamIndex = obj.GetComponent<Player>().teamIndex;
            //    Debug.Log(teamIndex);

            //    GameManager.GetInstance().size[teamIndex]--;
            //    GameManager.GetInstance().Rpc_OnChangeSize();
            //}            
        }


        //need tow ait for the GameManager to Start(), otherwise it causes problems with the player spawning
        private IEnumerator SpawnPlayer(NetworkConnection conn, short playerControllerId)
        {
            while (GameManager.GetInstance() == null || GameManager.GetInstance().size.Count <= 0 || conn.isReady == false)
            {
                yield return null;
            }


            int teamIndex = GameManager.GetInstance().GetTeamFill();
            //get spawn position for this team and instantiate the player there
            Vector3 startPos = GameManager.GetInstance().GetSpawnPosition(teamIndex);
            gameObject.transform.SetPositionAndRotation(startPos, Quaternion.identity);


            GameObject obj = (GameObject)Instantiate(playerPrefab, startPos, Quaternion.identity);
            Player player = obj.GetComponent<Player>();
            player.teamIndex = teamIndex;
            NetworkServer.AddPlayerForConnection(conn, obj, playerControllerId);

            GameManager.GetInstance().size[teamIndex]++;
            GameManager.GetInstance().Rpc_OnChangeSize();
        }


        
        //public void OnDisconnectedFromServer( conn)
        //{
        //    GameObject obj = conn.playerControllers[0].gameObject;
        //    int teamIndex = obj.GetComponent<Player>().teamIndex;
        //    GameManager.GetInstance().size[teamIndex]--;

        //    Debug.Log(teamIndex);

        //    obj.SetActive(false);

        //    base.OnClientDisconnect(conn);
        //}
    }
}
