using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TanksMP
{
    public class ServerManager : MonoBehaviour
    {
        public Text Status;

        public Button FindMatchButton;

        public Button StartMatchButton;

        Coroutine timeoutRoutine;

        NetworkGameManager NetManager;
        // Use this for initialization

        void Start()
        {
            NetManager = NetworkGameManager.GetInstance();

            if (NetManager.matchMaker == null)
            {
                NetManager.StartMatchMaker();
            }

            if (ApplicationData.ServerTimeout)
            {
                Status.text = ("Server Connection Timed Out");
            }
            else
            {
                Status.text = "";
            }
            
            FindMatchButton.enabled = true;

            StartMatchButton.enabled = true; 


        }

        // Update is called once per frame
        void Update()
        {

        }

        public void StartServer()
        {
            int rand = UnityEngine.Random.Range(100, 100000);
            string roomname = ("MpRoom" + rand.ToString());

            Status.text = ("Start Match: " + roomname);

            FindMatchButton.enabled = false;
            StartMatchButton.enabled = false;

            NetManager.matchMaker.CreateMatch(roomname, (uint)NetManager.maxConnections, true, "", "", "", 0, 0, OnMatchCreate);
        }


        private void OnMatchCreate(bool success, string extendedInfo, MatchInfo responseData)
        {
            if (success)
            {
                Status.text = ("");

                FindMatchButton.enabled = false;
                StartMatchButton.enabled = false;
            }
            else
            {
                Status.text = ("Failed to Create Game Server");

                FindMatchButton.enabled = true;
                StartMatchButton.enabled = true;
            }

            NetManager.OnMatchCreate(success, extendedInfo, responseData);
        }

        public void JoinServer()
        {
            ApplicationData.ServerTimeout = false;
            Status.text = ("Searching for Match");

            FindMatchButton.enabled = false;
            StartMatchButton.enabled = false;

            NetManager.matchMaker.ListMatches(0, 100, "", true, 0, 0, MatchListFound);
        }

        //called when a match is found
        private void MatchListFound(bool success, string extendedInfo, List<MatchInfoSnapshot> responseData)
        {
            if (!success || responseData.Count <= 0)
            {
                Status.text = ("Could Not Find Match");
                FindMatchButton.enabled = true;
                StartMatchButton.enabled = true;
                return;
            }

            MatchInfoSnapshot JoinableSnap = new MatchInfoSnapshot();
            bool bfound = false;
            foreach (MatchInfoSnapshot snap in responseData)
            {
                if (!snap.isPrivate && snap.currentSize < snap.maxSize && snap.currentSize > 0)
                {
                    //get the first joinable snap
                    JoinableSnap = snap;
                    bfound = true;
                    break;
                }
            }

            //found a joinable match
            if (bfound)
            {
                NetManager.matchMaker.JoinMatch(JoinableSnap.networkId, "", "", "", 0, 0, OnMatchJoined);
            }
            else
            {
                Status.text = ("No Joinable Servers Found");
                FindMatchButton.enabled = true;
                StartMatchButton.enabled = true;
            }
        }

        private void OnMatchJoined(bool success, string extendedInfo, MatchInfo responseData)
        {
            if (success)
            {
                Status.text = ("Connecting to Server");
                FindMatchButton.enabled = false;
                StartMatchButton.enabled = false;

                timeoutRoutine = StartCoroutine(TimeOut(responseData));
            }
            else
            {
                Status.text = ("Could Not Connect");
                FindMatchButton.enabled = true;
                StartMatchButton.enabled = true;
            }

            NetManager.OnMatchJoined(success, extendedInfo, responseData);
        }

        void OnDestroy()
        {
            if (timeoutRoutine != null)
            {
                StopCoroutine(timeoutRoutine);
            }
        }

        //need tow ait for the GameManager to Start(), otherwise it causes problems with the player spawning
        private IEnumerator TimeOut(MatchInfo responseData)
        {
            float timeout = 5.0f; // 3 seconds you can change this to
                                 //to whatever you want
            float totalTime = 0;
            while (totalTime < timeout)
            {
                totalTime += Time.deltaTime;
                yield return null;
            }

            //NetManager.matchMaker.DropConnection(responseData.networkId, responseData.nodeId, 0, NetManager.OnDropConnection);
            //NetManager.StopHost();

            ApplicationData.ServerTimeout = true;
            MatchInfo matchInfo = NetManager.matchInfo;
            NetManager.matchMaker.DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, NetManager.OnDropConnection);
            NetManager.StopHost();
            NetworkManager.Shutdown();

            SceneManager.LoadScene(0);

  

            FindMatchButton.enabled = true;
            StartMatchButton.enabled = true;
        }



    }
}
