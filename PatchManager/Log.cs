
using System;
using System.Collections;
using System.Diagnostics;
//using UnityEngine;

namespace PatchManager
{


    public static class Log
    {
        public enum LEVEL
        {
            OFF = 0,
            ERROR = 1,
            WARNING = 2,
            INFO = 3,
            DETAIL = 4,
            TRACE = 5
        };

        public static LEVEL level = LEVEL.INFO;

        private static readonly String PREFIX = "PatchManager" + ": ";

        public static LEVEL GetLevel()
        {
            return level;
        }

        public static void SetLevel(LEVEL level)
        {
            UnityEngine.Debug.Log("log level " + level);
            Log.level = level;
        }

        public static LEVEL GetLogLevel()
        {
            return level;
        }

        private static bool IsLevel(LEVEL level)
        {
            return level == Log.level;
        }

        public static bool IsLogable(LEVEL level)
        {
            return level <= Log.level;
        }

        public static void Trace(String msg)
        {
            if (IsLogable(LEVEL.TRACE))
            {
                UnityEngine.Debug.Log(PREFIX + msg);
            }
        }

        public static void Detail(String msg)
        {
            if (IsLogable(LEVEL.DETAIL))
            {
                UnityEngine.Debug.Log(PREFIX + msg);
            }
        }

        [ConditionalAttribute("DEBUG")]
        public static void Info(String msg)
        {
            if (IsLogable(LEVEL.INFO))
            {
                UnityEngine.Debug.Log(PREFIX + msg);
            }
        }

        [ConditionalAttribute("DEBUG")]
        public static void Test(String msg)
        {
            //if (IsLogable(LEVEL.INFO))
            {
                UnityEngine.Debug.LogWarning(PREFIX + "TEST:" + msg);
            }
        }

        static Stack funcStack = new Stack();

        [ConditionalAttribute("DEBUG")]
        public static void PushStackInfo(string funcName, string msg)
        {
            funcStack.Push(funcName);
            //  if (Debug_Level_1_Active)
            Log.Info(msg);
        }

        [ConditionalAttribute("DEBUG")]
        public static void PopStackInfo(string msg)
        {
            if (funcStack.Count > 0)
            {
                string f = (string)funcStack.Pop();
            }
            else
                Log.Info("Pop failed, no values on stack");
            //if (Debug_Level_1_Active)
            Log.Info(msg);
        }
        [ConditionalAttribute("DEBUG")]
        public static void ShowStackInfo()
        {
            int cnt = 0;
            Log.Info("Stack size: " + funcStack.Count.ToString());
            foreach (var obj in funcStack)
            {
                Log.Info("Stack[" + cnt.ToString() + "] = " + (string)obj);
                cnt++;
            }
        }

        public static void Warning(String msg)
        {
            if (IsLogable(LEVEL.WARNING))
            {
                UnityEngine.Debug.LogWarning(PREFIX + msg);
            }
        }

        public static void Error(String msg)
        {
            if (IsLogable(LEVEL.ERROR))
            {
                UnityEngine.Debug.LogError(PREFIX + msg);
            }
        }

        public static void Exception(Exception e)
        {
            Log.Error("exception caught: " + e.GetType() + ": " + e.Message);
        }

    }
}
