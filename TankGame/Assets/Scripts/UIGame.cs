using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking.Match;

namespace TanksMP
{
	/// <summary>
	/// UI script for all elements, team events and user interactions in the game scene.
	/// </summary>
	public class UIGame : MonoBehaviour
	{
		/// <summary>
		/// UI sliders displaying team fill for each team using absolute values.
		/// </summary>
		public Slider[] teamSize;
        
		/// <summary>
		/// UI texts displaying kill scores for each team.
		/// </summary>
		public Text[] teamScore;

		/// <summary>
		/// UI texts displaying kill scores for this local player.
		/// [0] = Kill Count, [1] = Death Count
		/// </summary>
		public Text[] killCounter;

		/// <summary>
		/// UI text for indicating player death and who killed this player.
		/// </summary>
		public Text deathText;
        
		/// <summary>
		/// UI text displaying the time in seconds left until player respawn.
		/// </summary>
		public Text spawnDelayText;

		/// <summary>
		/// UI text for indicating game end and which team has won the round.
		/// </summary>
		public Text gameOverText;

		/// <summary>
		/// UI window gameobject activated on game end, offering restart buttons and quit.
		/// </summary>
		public GameObject gameOverMenu;

        public GameObject pauseMenu;

        public GameObject GamePaused;

        public Text GameTime;

		//initialize variables
		IEnumerator Start ()
		{
			//wait until the network is ready
			while (GameManager.GetInstance () == null || GameManager.GetInstance ().localPlayer == null)
				yield return null;

			//play background music
			AudioManager.PlayMusic (0);
		}

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        void TogglePause()
        {
            bool pause = !pauseMenu.activeSelf;
            pauseMenu.SetActive(pause);
            Player player = GameManager.GetInstance().localPlayer;

            if (player != null)
            {
                //if we are unpausing need to check if we are allowed to take input, i.e the player is dead
                if (!pause)
                {
                    player.disableInput = !GameManager.GetInstance().CanEnableInput();

                    //we need to show the game paused screen if we have less than 2 players and we are trying to unpause
                    GamePaused.SetActive(GameManager.GetInstance().GamePaused);
                }
                else
                {
                    player.disableInput = pause;
                    GamePaused.SetActive(false);
                }
                    
            } 
        }

        public void PauseGame(bool wantToPause)
        {
            if (wantToPause && !pauseMenu.activeSelf)
            {
                GamePaused.SetActive(true);
            }

            if (!wantToPause)
            {
                GamePaused.SetActive(false);
            }

        }

		/// <summary>
		/// This is an implementation for changes to the team fill, updating the slider values.
		/// Parameters: index of team which received updates.
		/// </summary>
		public void OnTeamSizeChanged (int index)
		{
			teamSize [index].value = GameManager.GetInstance ().size [index];
		}

		/// <summary>
		/// This is an implementation for changes to the team score, updating the text values.
		/// Parameters: index of team which received updates.
		/// </summary>
		public void OnTeamScoreChanged (int index)
		{
			teamScore [index].text = GameManager.GetInstance ().score [index].ToString ();
			teamScore [index].GetComponent<Animator> ().Play ("Animation");
		}

		/// <summary>
		/// Sets death text showing who killed the player in its team color.
		/// Parameters: killer's name, killer's team
		/// </summary>
		public void SetDeathText (string playerName, Team team)
		{   
			//show killer name and colorize the name converting its team color to an HTML RGB hex value for UI markup
			deathText.text = "KILLED BY\n<color=#" + ColorUtility.ToHtmlStringRGB (team.material.color) + ">" + playerName + "</color>";
		}

		/// <summary>
		/// Set respawn delay value displayed to the absolute time value received.
		/// The remaining time value is calculated in a coroutine by GameManager.
		/// </summary>
		public void SetSpawnDelay (float time)
		{                
			spawnDelayText.text = Mathf.Ceil (time) + "";
		}

		/// <summary>
		/// Hides any UI components related to player death after respawn.
		/// </summary>
		public void DisableDeath ()
		{
			//clear text component values
			deathText.text = string.Empty;
			spawnDelayText.text = string.Empty;
		}

		/// <summary>
		/// Set game end text and display winning team in its team color.
		/// </summary>
		public void SetGameOverText (Team team)
		{
			//show winning team and colorize it by converting the team color to an HTML RGB hex value for UI markup
			gameOverText.text = "TEAM <color=#" + ColorUtility.ToHtmlStringRGB (team.material.color) + ">" + team.name + "</color> WINS!";
		}

        public void SetGameDrawText()
        {
            //show winning team and colorize it by converting the team color to an HTML RGB hex value for UI markup
            gameOverText.text = "DRAW!";
        }

		/// <summary>
		/// Displays the game's end screen. Called by GameManager after few seconds delay.
		/// </summary>
		public void ShowGameOver ()
		{
			//hide text but enable game over window
			gameOverText.gameObject.SetActive (false);
			gameOverMenu.SetActive (true);

            //localPlayer.enabled = false;
            GameManager.GetInstance().localPlayer.disableInput = false;
        }

		/// <summary>
		/// Restarts the scene
		/// </summary>
		public void Restart ()
		{
            Debug.Log("Restart Clicked");
            GameManager.GetInstance().WantRestart();
			//SceneManager.LoadScene (0);
		}

		/// <summary>
		/// Quits the application.
		/// </summary>
		public void Quit ()
		{
            ApplicationData.ServerClosed = false;
            NetworkGameManager NetManager = NetworkGameManager.GetInstance();
            MatchInfo matchInfo = NetManager.matchInfo;
            NetManager.matchMaker.DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, NetManager.OnDropConnection);
            NetManager.StopHost();

            NetworkGameManager.Shutdown();

            SceneManager.LoadScene(0);
            //SceneManager.LoadScene(0);
        }
	}
}