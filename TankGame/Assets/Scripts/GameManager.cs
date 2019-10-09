using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace TanksMP
{
	/// <summary>
	/// Manages game workflow and functions such as team fill, scores and ending a game
	/// </summary>
	public class GameManager : NetworkBehaviour
	{
		//reference to this script instance
		private static GameManager instance;
        
		/// <summary>
		/// The local player instance spawned for this client.
		/// </summary>
		[HideInInspector]
		public Player localPlayer;
        
		/// <summary>
		/// Reference to the UI script displaying game stats.
		/// </summary>
		public UIGame ui;
        
		/// <summary>
		/// Definition of playing teams with additional properties.
		/// </summary>
		public Team[] teams;

        /// <summary>
        /// List storing team fill for each team.
        /// E.g. if size[0] = 2, there are two players in team 0.
        /// </summary>
        public SyncListInt size = new SyncListInt();
        
		/// <summary>
		/// List storing team scores for each team.
		/// E.g. if score[0] = 2, team 0 scored 2 points.
		/// </summary>
		public SyncListInt score = new SyncListInt();

		/// <summary>
		/// The maximum amount of kills to reach before ending the game.
		/// </summary>
		public int maxScore = 5;

		/// <summary>
		/// The delay in seconds before respawning a player after it got killed.
		/// </summary>
		public int respawnTime = 5;
        
		/// <summary>
		/// The delay in seconds before respawning a player after it got killed.
		/// </summary>
		public GameObject playerPrefab;

		/// <summary>
		/// Keeps track of whether the game has ended.
		/// </summary>
		[HideInInspector]
		public bool gameOver;

        /// <summary>
        /// keep track of the game pause state, if less than 2 players then pause, starts with being paused
        /// </summary>
        [SyncVar]
        public bool GamePaused;

        public int MinimumPlayers = 2;

        /// <summary>
        /// the current time elapsed since the game started
        /// in seconds
        /// </summary>
        [SyncVar]
        public float CurrentGameTime;

        /// <summary>
        /// The max game time in seconds
        /// </summary>
        float MaxGameTime = 60.0f;

		//initialize variables
		void Awake ()
		{
            instance = this;
        }

        void Start()
        {
            if (isServer)
            {
                GamePaused = true;

                size.Clear();
                score.Clear();

                if (size.Count != teams.Length)
                {
                    for (int i = 0; i < teams.Length; i++)
                    {
                        size.Add(0);
                        score.Add(0);
                    }
                }
                //Debug.Log("GameManger: " + size.Count);
            }


            //start the timer here when the server starts
            //Run on both the client and server, updated once when the player actually connects
            CurrentGameTime = MaxGameTime;

            for (int i = 0; i < teams.Length; i++)
            {
                ui.OnTeamSizeChanged(i);
            }

            for (int i = 0; i < teams.Length; i++)
            {
                ui.OnTeamScoreChanged(i);
            }
        }

        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static GameManager GetInstance ()
		{
			return instance;
		}

        void Update()
        {
            if (isServer)
            {
                int numPlayer = 0;
                for (int i = 0; i < size.Count; i++)
                {
                    numPlayer += size[i];
                }

                if (numPlayer < MinimumPlayers && !gameOver)
                {
                    GamePaused = true;
                }
                else
                {
                    GamePaused = false;
                }
            }

            if (isClient)
                GamePause();

            if (CurrentGameTime > 0.0f)
            {
                string minutes = Mathf.Floor(CurrentGameTime / 60).ToString("00");
                string seconds = (CurrentGameTime % 60).ToString("00");
                ui.GameTime.text = string.Format("{0}:{1}", minutes, seconds);

                if (isServer && !GamePaused && !gameOver)
                    CurrentGameTime -= Time.deltaTime;
            }

            if (CurrentGameTime <= 0.0f)
            {
                if (isServer)
                {
                    gameOver = true;
                    int CurrentMaxScore = 0;
                    int MaxScoreTeam = 0;
                    //loop over teams to find the highest score
                    for (int i = 0; i < teams.Length; i++)
                    {
                        //score is greater or equal to max score,
                        //which means the game is finished
                        if (score[i] > CurrentMaxScore)
                        {
                            CurrentMaxScore = score[i];
                            MaxScoreTeam = i;
                        }
                    }

                    bool draw = false;
                    for (int i = 0; i < teams.Length; i++)
                    {
                        //score is greater or equal to max score,
                        //which means the game is finished
                        if (score[i] == CurrentMaxScore && MaxScoreTeam != i)
                        {
                            draw = true;
                        }
                    }


                    if (draw)
                    {
                        Rpc_GameDraw();
                    }
                    else
                    {
                        Rpc_GameOver(MaxScoreTeam);
                    }
                }
            }

        }

        public void GamePause()
        {
            if (isClient && localPlayer != null)
            {
                if (GamePaused)
                {
                    localPlayer.disableInput = true;
                    ui.PauseGame(true);
                }
                else
                {
                    localPlayer.disableInput = !CanEnableInput();
                    ui.PauseGame(false);
                }
            }
        }

        /// <summary>
        /// returns true if the player is allowed to enable input
        /// false if the game is currently paused or there is something blocking the game view, i.e pause menu
        /// </summary>
        /// <returns></returns>
        public bool CanEnableInput()
        {
            if (GamePaused)
                return false;

            if (!localPlayer.bAlive)
                return false;

            if (ui.pauseMenu.activeSelf)
                return false;

            if (gameOver)
                return false;

            return true;
        }

		/// <summary>
		/// Returns the next team index a player should be assigned to.
		/// </summary>
		public int GetTeamFill ()
		{
			//init variables
			int teamNo = 0;

			int min = size [0];
			//loop over teams to find the lowest fill
			for (int i = 0; i < teams.Length; i++)
			{
				//if fill is lower than the previous value
				//store new fill and team for next iteration
				if (size [i] < min)
				{
					min = size [i];
					teamNo = i;
				}
			}
            
			//return index of lowest team
			return teamNo;
		}

        
		/// <summary>
		/// Returns a random spawn position within the team's spawn area.
		/// </summary>
		public Vector3 GetSpawnPosition (int teamIndex)
		{
			//init variables
			Vector3 pos = teams [teamIndex].spawn.position;
			BoxCollider col = teams [teamIndex].spawn.GetComponent<BoxCollider> ();

			if (col != null)
			{
				//find a position within the box collider range, first set fixed y position
				//the counter determines how often we are calculating a new position if out of range
				pos.y = col.transform.position.y;
				int counter = 10;
                
				//try to get random position within collider bounds
				//if it's not within bounds, do another iteration
				do
				{
					pos.x = UnityEngine.Random.Range (col.bounds.min.x, col.bounds.max.x);
					pos.z = UnityEngine.Random.Range (col.bounds.min.z, col.bounds.max.z);
					counter--;
				} while(!col.bounds.Contains (pos) && counter > 0);
			}
            
			return pos;
		}

		/// <summary>
		/// Returns whether a team reached the maximum game score.
		/// </summary>
		public bool IsGameOver ()
		{
			//init variables
			bool isOver = false;
            
			//loop over teams to find the highest score
			for (int i = 0; i < teams.Length; i++)
			{
				//score is greater or equal to max score,
				//which means the game is finished
				if (score [i] >= maxScore)
				{
					isOver = true;
					gameOver = true;
					break;
				}
			}
            
			//return the result
			return isOver;
		}

        
		/// <summary>
		/// Only for this player: sets the death text stating the killer on death.
		/// </summary>
		public void DisplayDeath (bool skipAd = false)
		{   
			//get the player component that killed us
			Player other = localPlayer.killedBy.GetComponent<Player> ();
			//increase local death counter for this game
			ui.killCounter [1].text = (int.Parse (ui.killCounter [1].text) + 1).ToString ();
			ui.killCounter [1].GetComponent<Animator> ().Play ("Animation");

			//set the death text
			//and start waiting for the respawn delay immediately
			ui.SetDeathText (other.myName, teams [other.teamIndex]);
		}
        
                
		/// <summary>
		/// Only for this player: sets game over text stating the winning team.
		/// Disables player movement so no updates are sent through the network.
		/// </summary>
		public void DisplayGameOver (int teamIndex)
		{           
			localPlayer.enabled = false;
			localPlayer.disableInput = true;
			ui.SetGameOverText (teams [teamIndex]);
            
			//starts coroutine for displaying the game over window
			StartCoroutine (DisplayGameOver ());
		}

        [ClientRpc]
        public void Rpc_OnChangeSize()
        {
            for (int i = 0; i < teams.Length; i++)
            {
                ui.OnTeamSizeChanged(i);
            }               
        }

        [ClientRpc]
        public void Rpc_OnChangeScore()
        {
            for (int i = 0; i < teams.Length; i++)
            {
                ui.OnTeamScoreChanged(i);
            }
                
        }
        
		//displays game over window after short delay
		IEnumerator DisplayGameOver ()
		{
			//give the user a chance to read which team won the game
			//before enabling the game over screen
			yield return new WaitForSeconds (3);
            
			//show game over window and disconnect from network
			ui.ShowGameOver ();
		}

        public void WantRestart()
        {
            localPlayer.Cmd_Restart();
        }

        //called on game end providing the winning team
        [ClientRpc]
        public void Rpc_GameOver(int teamIndex)
        {
            //display game over window
            GetInstance().DisplayGameOver(teamIndex);
        }

        [ClientRpc]
        public void Rpc_GameDraw()
        {
            localPlayer.enabled = false;
            localPlayer.disableInput = true;
            ui.SetGameDrawText();

            //starts coroutine for displaying the game over window
            StartCoroutine(DisplayGameOver());
        }
    }



    
	/// <summary>
	/// Defines properties of a team.
	/// </summary>
	[System.Serializable]
	public class Team
	{
		/// <summary>
		/// The name of the team shown on game over.
		/// </summary>
		public string name;
             
		/// <summary>
		/// The color of a team for UI and player prefabs.
		/// </summary>   
		public Material material;
            
		/// <summary>
		/// The spawn point of a team in the scene. In case it has a BoxCollider
		/// component attached, a point within the collider bounds will be used.
		/// </summary>
		public Transform spawn;
	}
}