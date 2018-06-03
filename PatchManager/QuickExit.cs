
/* 
QuickExit
Copyright 2017 Malah

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>. 
*/

using System.Collections;
using System.IO;
using KSP.Localization;
using UnityEngine;

namespace PatchManager
{

    public partial class PatchManagerClass
    {

        void MyLog(string s, string method)
        {
            Log.Info(method + ":" + s);
        }
        const string MOD = "PatchManager";
        const string VERSION = "";
        internal static void Warning(string String, string Title = null)
        {
            if (Title == null)
            {
                Title = MOD;
            }
            else
            {
                Title = string.Format("{0}({1})", MOD, Title);
            }
            Log.Warning(string.Format("{0}[{1}]: {2}", Title, VERSION, String));
        }
        //=============

        public static readonly string shipFilename = "Auto-Saved Ship";

        int count = 5;

        Coroutine coroutineTryExit;
        bool IsTryExit
        {
            get
            {
                return coroutineTryExit != null;
            }
            set
            {
                if (coroutineTryExit != null)
                {
                    Log.Info("IsTryExit, stopping tryExit() coroutine");
                    StopCoroutine(coroutineTryExit);
                    coroutineTryExit = null;
                }
                //saveDone = false;
                if (value)
                {
                    Log.Info("IsTryExit, starting tryExit() coroutine");
                    count = 0;
                    coroutineTryExit = StartCoroutine(tryExit());
                }
            }
        }


        //bool saveDone = false;
        bool CanSavegame
        {
            get
            {
                if (!HighLogic.LoadedSceneIsGame)
                {
                    return false;
                }
                string _savegame = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                if (File.Exists(_savegame))
                {
                    FileInfo _info = new FileInfo(_savegame);
                    if (_info.IsReadOnly)
                    {
                        Warning(_savegame + " is read only.", "QExit");
                        return false;
                    }
                }
                
                return true;
            }
        }

        public bool doShutdown = false;
        void Update()
        {
            if (doShutdown)
            {
                Log.Info("QExit, Update, doShutdown is true");
                doShutdown = false;
                IsTryExit = true;
               // if (IsTryExit)
               // {
               //     TryExit(true);
               // }
                
            }
        }


        IEnumerator tryExit()
        {
            Log.Info("tryExit");
           
            if (CanSavegame)
            {
                if (GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE) != string.Empty)
                {
                    //saveDone = true;
                    ScreenMessages.PostScreenMessage(Localizer.Format("pm_gameSaved", MOD), 5);
                    MyLog("Game saved.", "QExit");
                }
                else
                {
                    count = 10;
                    MyLog("Can't save game.", "QExit");
                    ScreenMessages.PostScreenMessage(Localizer.Format("pm_cantSave", MOD), 10);
                }
            }
            else
            {
                count = 10;
                ClearToSaveStatus clearToSaveStatus = FlightGlobals.ClearToSave();
                string _status = FlightGlobals.GetNotClearToSaveStatusReason(clearToSaveStatus, string.Empty);
                MyLog("Can't game saved: " + _status, "QExit");
                ScreenMessages.PostScreenMessage(Localizer.Format("pm_cantSave", MOD) + ": " + _status, 10);
            }
           
            while (count >= 0)
            {
                yield return new WaitForSecondsRealtime(1f);
                MyLog("Exit in " + count, "QExit");
                count--;
            }
            Log.Info("tryExit, ready to exit");
            if (!IsTryExit)
            {
                MyLog("tryExit stopped", "QExit");
                yield break;
            }
            Log.Info("tryExit, before ApplicationQuit");
            Application.Quit();
            MyLog("tryExit ended", "QExit");
        }
    }
}