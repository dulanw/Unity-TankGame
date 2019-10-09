using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace TanksMP
{
    public class UIMainMenu : MonoBehaviour
    {

        public InputField NameTextField;

        public Text servererr;

        // Use this for initialization
        void Start()
        {
            if (ApplicationData.PlayerName.Length <= 0)
            {
                string newname = PlayerPrefs.GetString("PlayerName");
                if (newname.Length > 0)
                {
                    NameTextField.text = newname;
                }
                else
                {
                    int rand = Random.Range(100, 100000);
                    newname = ("Player" + rand.ToString());
                    NameTextField.text = newname;
                    PlayerPrefs.SetString("PlayerName", newname);
                }

                ApplicationData.PlayerName = newname;
            }
            else
            {
                NameTextField.text = ApplicationData.PlayerName;
            }


            if (ApplicationData.ServerClosed)
            {
                servererr.enabled = true;
            }
            else
            {
                servererr.enabled = false;
            }
                
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void UpdateName()
        {
            PlayerPrefs.SetString("PlayerName", NameTextField.text);
            ApplicationData.PlayerName = NameTextField.text;
        }

        public void Quit()
        {
            Application.Quit();
        }
    }

}
