using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TanksMP
{
    public static class ApplicationData
    {
        //private static ApplicationData instance;
        public static bool ServerClosed = false;
        public static bool ServerTimeout = false;
        public static string PlayerName = "";

        //void Awake()
        //{
        //    DontDestroyOnLoad(this);
        //    instance = this;
        //}

        //public static ApplicationData GetInstance()
        //{
        //    return instance;
        //}
    }
}
