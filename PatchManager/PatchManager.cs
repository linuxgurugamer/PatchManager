using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using KSP.UI.Screens;

namespace PatchManager
{
    //
    // Normally I'd put this into it's own file, but since it's small and has no methods, putting it at the top of this file is ok
    //
    public class PatchInfo
    {
        public bool enabled;
        public bool toggle = false;
        public string fname;

        // Required settings.  
        // srcPath should use forward slashes, and include the full file name.  srcPath should be in a directory 
        // called ".../PatchManager/PluginData"
        public string modName;
        public string patchName;
        public string srcPath;
        public string shortDescr;

        // Optional, but recommended
        public string longDescr;

        //// Optional entries here

        // dependencies, only show this patch if these specified mods are available
        // List either the directory of the mod (as show by ModuleManager), or the 
        // mod DLL (as show by ModuleManager)
        public string dependencies;

        // exclusions, this patch is exclusive with these, in other words, don't install this
        // if a patch listed in the exclusion is installed
        public string exclusions;

        // Path to icon, if desired
        public string icon;

        // Author's name, if desired
        public string author;
        public string destPath;
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PatchManagerClass : MonoBehaviour
    {

        const string CONFIG_NODENAME = "PatchManager";

        string KSP_DIR = KSPUtil.ApplicationRootPath;
        string DEFAULT_PATCH_DIRECTORY;
        public static List<String> installedMods = null;

        private ApplicationLauncherButton Button;
        bool visible = false;
        bool restartMsg = false;
        Rect windowPosition;
        const int WIDTH = 800;
        const int HEIGHT = 300;
        Vector2 fileSelectionScrollPosition = new Vector2();

        static List<PatchInfo> availablePatches = new List<PatchInfo>();
        static List<String> installedPatches = new List<String>();

        PatchInfo pi;

        public void Start()
        {
            DEFAULT_PATCH_DIRECTORY = KSP_DIR + "GameData/PatchManager/ActiveMMPatches";
            LoadAllPatches();
            if (!HighLogic.CurrentGame.Parameters.CustomParams<PM>().alwaysShow && (availablePatches == null || availablePatches.Count() == 0))
                return;


            windowPosition = new Rect((Screen.width - WIDTH) / 2, (Screen.height - HEIGHT) / 2, WIDTH, HEIGHT);
            Texture2D Image = GameDatabase.Instance.GetTexture("PatchManager/Resources/PatchManager", false);

            Button = ApplicationLauncher.Instance.AddModApplication(onTrue, onFalse, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER, Image);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(Destroy);
        }

        public void onTrue()
        {
            Log.Info("Opened PatchManager");
            visible = true;
            LoadAllPatches();
        }

        public void onFalse()
        {
            Log.Info("Closed PatchManager");
            visible = false;
        }

        void ToggleActivation(PatchInfo pi)
        {
            pi.toggle = !pi.toggle;
        }

        private void Destroy(GameScenes scene)
        {
            OnDestroy();
        }

        public void OnDestroy()
        {
            ApplicationLauncher.Instance.RemoveModApplication(Button);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(Destroy);
        }

        #region ButtonStyles
        static GUIStyle bodyButtonStyle = new GUIStyle(HighLogic.Skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        static GUIStyle bodyButtonStyleGreen = new GUIStyle(HighLogic.Skin.button)
        {
            normal =
            {
                textColor = Color.green
            },
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        static GUIStyle bodyButtonStyleRed = new GUIStyle(HighLogic.Skin.button)
        {
            normal =
            {
                textColor = Color.red
            },
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        #endregion

        void HideWindow()
        {
            visible = false;
            Button.SetFalse();
        }

        void CenterLine(string msg)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(msg);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        //
        // Need to warn the user to restart KSP
        //
        void drawRestartWindow(int windowid)
        {
            CenterLine(" ");
            CenterLine("The changes you just made by installing/uninstalling one or");
            CenterLine("more patches will not take effect until the game is restarted");
            CenterLine(" ");
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(" Acknowledged ", GUILayout.Width(150), GUILayout.Height(40)))
                restartMsg = false;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        //
        // Main window 
        //
        void drawWindow(int windowid)
        {
            GUILayout.BeginHorizontal();
            fileSelectionScrollPosition = GUILayout.BeginScrollView(fileSelectionScrollPosition);
            GUILayout.BeginVertical();
            for (int i = 0; i < availablePatches.Count(); i++)
            {
                pi = availablePatches[i];
                Log.Info("i: " + i.ToString());
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUIStyle gs = bodyButtonStyle;

                if (pi.enabled)
                {
                    if (!pi.toggle)
                        gs = bodyButtonStyleGreen;
                    else
                        gs = bodyButtonStyleRed;
                }
                else
                {
                    if (!pi.toggle)
                        gs = bodyButtonStyleRed;
                    else
                        gs = bodyButtonStyleGreen;

                }


                Texture2D Image = null;
                if (pi.icon != null && pi.icon.Length > 0)
                    Image = GameDatabase.Instance.GetTexture(pi.icon, false);
                GUI.enabled = exclusionsOK(pi);

                if (Image == null)
                {
                    Log.Info("No image loaded for button");
                    if (GUILayout.Button("", GUILayout.Width(38), GUILayout.Height(38)))
                        ToggleActivation(pi);
                }
                else
                {
                    if (GUILayout.Button(Image, GUILayout.Width(38), GUILayout.Height(38)))
                        ToggleActivation(pi);
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical();

                if (GUILayout.Button(pi.modName + "\n" + pi.shortDescr, gs, GUILayout.Width(175)))
                {
                    ToggleActivation(pi);
                }
                GUI.enabled = true;
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                GUILayout.Label(pi.longDescr + "\n" + pi.author + "\n", GUILayout.Width(WIDTH - 175 - 38 - 2));
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply All", GUILayout.Width(90)))
            {
                ApplyAllChanges();
                HideWindow();

            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(90)))
            {
                HideWindow();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        void ApplyAllChanges()
        {
            for (int i = 0; i < availablePatches.Count(); i++)
            {
                pi = availablePatches[i];
                // string s = pi.destPath + pi.fname;
                string s = getActivePatchName(pi);
                if (pi.toggle)
                {
                    if (pi.enabled)
                    {
                        // delete the active patch file
                        Log.Info("Deleting patch at: " + s);
                        File.Delete(s);
                    }
                    else
                    {
                        // Copy the file to the dest to make it active
                        Log.Info("Copying patch from: " + KSP_DIR + "/GameData/" + pi.srcPath + "   to: " + s);
                        File.Copy(KSP_DIR + "/GameData/" + pi.srcPath, s);
                    }
                }
            }
            restartMsg = true;
        }

        public void OnGUI()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave || HighLogic.CurrentGame.Parameters.CustomParams<PM>().alwaysShow)
            {
                if (visible)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Native);
                    windowPosition = GUILayout.Window(windowId, windowPosition, drawWindow, "Patch Manager");
                }
                if (restartMsg)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Native);
                    windowPosition = GUILayout.Window(windowId, windowPosition, drawRestartWindow, "Restart Message");
                }
            }
        }

        void LoadAllPatches()
        {
            Log.Info("PatchManager.OnLoad");
            //try {
            availablePatches.Clear();
            installedPatches.Clear();
            var availableNodes = GameDatabase.Instance.GetConfigNodes(CONFIG_NODENAME);
            if (availableNodes.Count() > 0)
            {
                //load
                Log.Info("PatchManager loaded configs, count: " + availableNodes.Count().ToString());
                foreach (var n in availableNodes)
                {
                    PatchInfo pi = new PatchInfo();
                    pi.modName = n.GetValue("modName");
                    pi.patchName = n.GetValue("patchName");
                    pi.srcPath = n.GetValue("srcPath");
                    pi.shortDescr = n.GetValue("shortDescr");
                    pi.longDescr = n.GetValue("longDescr");
                    pi.dependencies = n.GetValue("dependencies");
                    pi.exclusions = n.GetValue("exclusions");
                    pi.icon = n.GetValue("icon");
                    pi.author = n.GetValue("author");
                    pi.destPath = n.GetValue("destPath");

                    if (pi.destPath == null || pi.destPath == "")
                        pi.destPath = DEFAULT_PATCH_DIRECTORY;
                    else
                        pi.destPath = KSP_DIR + "GameData/" + pi.destPath;
                    
                    pi.fname = pi.srcPath.Substring(pi.srcPath.LastIndexOf('/') + 1);

                    bool bd = Directory.Exists(pi.destPath);
                    string activePatchName = getActivePatchName(pi); ;
                    if (bd)
                    {
                        // check for old style filename, if it's there, rename it with the modname in front
                        string oldName = getActivePatchName(pi, false);
                        
                        Log.Info("Checking for old name: " + oldName);
                        if (File.Exists(oldName))
                        {
                            if (!File.Exists(activePatchName))
                                System.IO.File.Move(oldName, activePatchName);
                        }
                        pi.enabled = File.Exists(activePatchName);
                    }
                    else
                    {
                        pi.enabled = false;
                        DirectoryInfo di = Directory.CreateDirectory(pi.destPath);
                        // Shouldn't ever happen, but if it does, create the directory
                    }
                    pi.toggle = false;
                    Log.Info("pi.enabled: " + pi.enabled.ToString());
                    if (pi.enabled)
                        installedPatches.Add(activePatchName);
                    if (dependenciesOK(pi))
                        availablePatches.Add(pi);
                    else
                        Log.Error("Dependencies not satisfied for: " + pi.modName);
                }
            }
            else
            {
                Log.Info("PatchManager no loaded configs");
            }
        }

        string getActivePatchName(PatchInfo pi, bool withModname = true)
        {
            pi.fname = pi.srcPath.Substring(pi.srcPath.LastIndexOf('/') + 1);

            string s;
            if (withModname)
                s = pi.destPath + "/" + pi.modName + "_" + pi.fname;
           else
                return pi.destPath + "/" + pi.fname;
            return s.Replace(' ', '_');
        }

        //
        // Make sure all dependencies are here
        //
        bool dependenciesOK(PatchInfo pi)
        {
            if (pi.dependencies == null || pi.dependencies.Length == 0)
                return true;
            List<string> stringList = pi.dependencies.Split(',').ToList();
            // First check to see if it's a DLL
            for (int i = 0; i < stringList.Count(); i++)
            {
                var s = stringList[i];
                if (s != null && s.Length > 0)
                {
                    var s1 = s.Trim();
                    Log.Info("Checking for dependency: " + s1);
                    if (hasMod(s1))
                        return true;

                    // Now check to see if it's a directory in GameData
                    var s2 = KSP_DIR + "GameData/" + s1;
                    Log.Info("Checking for directory: " + s2);
                    if (Directory.Exists(s2))
                        return true;
                }
            }
            return false;
        }

        bool exclusionsOK(PatchInfo pi)
        {
            if (pi.exclusions == null || pi.exclusions.Count() == 0)
                return true;
            List<string> stringList = pi.exclusions.Split(',').ToList();
            foreach (var s2 in stringList)
            {
                Log.Info("s2: " + s2);
            }
            Log.Info("pi.exclusions: " + pi.exclusions);
            for (int i = 0; i < availablePatches.Count(); i++)
            {
                pi = availablePatches[i];
                // string s = pi.destPath + pi.fname;
                string s = getActivePatchName(pi);
                s = s.Substring(s.LastIndexOf('/') + 1);
                s = s.Substring(0, s.Length - 4);
                Log.Info("Checking Exclusion: " + s + "   enabled: " + pi.enabled.ToString() + "   toggle: " + pi.toggle.ToString());

                if ((pi.enabled && !pi.toggle) || (!pi.enabled && pi.toggle))
                {
                    if (stringList.Contains(s))
                    {
                        Log.Info("exclusion found");
                        return false;
                    } else
                    {
                        Log.Info("stringList does NOT contain [" + s + "]");
                    }
                }
            }

            return true;
        }

        void buildModList()
        {
            Log.Info("buildModList");
            //https://github.com/Xaiier/Kreeper/blob/master/Kreeper/Kreeper.cs#L92-L94 <- Thanks Xaiier!
            foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
            {
                string name = a.name;
                Log.Info(string.Format("Loading assembly: {0}", name));
                installedMods.Add(name);
            }
        }

        bool hasMod(string modIdent)
        {
            if (installedMods == null)
            {
                installedMods = new List<String>();
                buildModList();
            }
            return installedMods.Contains(modIdent);
        }

    }
}
