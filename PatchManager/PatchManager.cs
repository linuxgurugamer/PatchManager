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

        // installedWithMod, if true, then this patch is active when the mod is installed
        public bool installedWithMod = false;

        public string destPath;
    }

    public class Settings
    {
        const string SETTINGSFILE = "PatchManager.cfg";
        const string NODE = "PatchManager";

        public bool alwaysShow = false;
        public bool storeActivePatchesInPMFolder = true;

        public void LoadSettings(string path)
        {
            Log.Info("LoadSettings, path: " + path + "   settings file: " + SETTINGSFILE);
            if (System.IO.File.Exists(path + "/" + SETTINGSFILE))
            {
                ConfigNode tempNode = ConfigNode.Load(path + "/" + SETTINGSFILE);
                ConfigNode nodeLoad = tempNode.GetNode(NODE);

                string s = nodeLoad.GetValue("alwaysShow");
                if (s != null && s.Length > 0)
                    alwaysShow = Boolean.Parse(s);

                s = nodeLoad.GetValue("storeActivePatchesInPMFolder");
                if (s != null && s.Length > 0)
                    storeActivePatchesInPMFolder = Boolean.Parse(s);
            }
        }
        public void SaveSettings(string path)
        {
            Log.Info("LoadSettings, path: " + path + "   settings file: " + SETTINGSFILE);

            ConfigNode nodeLoad = new ConfigNode(NODE);
            nodeLoad.AddValue("alwaysShow", alwaysShow);
            nodeLoad.AddValue("storeActivePatchesInPMFolder", storeActivePatchesInPMFolder);

            ConfigNode toSave = new ConfigNode("NODE");
            toSave.AddNode(nodeLoad);
            if (!System.IO.Directory.Exists(path))
            {
                DirectoryInfo di = Directory.CreateDirectory(path);
            }
            toSave.Save(path + "/" + SETTINGSFILE);

        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PatchManagerClass : MonoBehaviour
    {

        const string CONFIG_NODENAME = "PatchManager";

        string KSP_DIR = KSPUtil.ApplicationRootPath;
        string DEFAULT_PATCH_DIRECTORY;
        string CFG_DIR;
        public static List<String> installedMods = null;

        private ApplicationLauncherButton Button;
        bool visible = false;
        bool restartMsg = false;
        Rect windowPosition;
        const int WIDTH = 900;
        const int HEIGHT = 600;
        Vector2 fileSelectionScrollPosition = new Vector2();

        static List<PatchInfo> availablePatches = new List<PatchInfo>();
        static List<String> installedPatches = new List<String>();

        Settings settings = new Settings();

        PatchInfo pi;

        public void Start()
        {
            DEFAULT_PATCH_DIRECTORY = "GameData/PatchManager/ActiveMMPatches";
            CFG_DIR = "GameData/PatchManager/PluginData";
            LoadAllPatches();
            settings.LoadSettings(CFG_DIR);

            if (!settings.alwaysShow && (availablePatches == null || availablePatches.Count() == 0))
                return;


            windowPosition = new Rect((Screen.width - WIDTH) / 2, (Screen.height - HEIGHT) / 2, WIDTH, HEIGHT);


            if (HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave)
            {
                CreateButton();
            }
        }

        void CreateButton()
        {
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
            if (Button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(Button);
                GameEvents.onGUIApplicationLauncherUnreadifying.Remove(Destroy);
                Button = null;
            }
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


        void drawSettingsWindow(int windowid)
        {
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            string s = "This overrides the function of the button being hidden if there are no patches due to dependencies";
            GUILayout.TextField(s);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            s = "This does not disble/enable the mod, that you can do in the standard settings:";
            GUILayout.TextField(s);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            settings.alwaysShow = GUILayout.Toggle(settings.alwaysShow, "Always show toolbar button");
            GUILayout.EndHorizontal();
            GUILayout.Space(25);
            GUILayout.BeginHorizontal();
            s = "Disable this to store the active patches in the patch's parent mod folder";
            GUILayout.TextArea(s);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            settings.storeActivePatchesInPMFolder = GUILayout.Toggle(settings.storeActivePatchesInPMFolder, "Store active patches in PatchManager folder");
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK", GUILayout.Width(60)))
            {
                showSettings = false;
                settings.SaveSettings(CFG_DIR);
                CheckPatchLocations();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
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
            GUI.DragWindow();
        }

        bool expanded = false;
        string expandedMod;

        //
        // Main window 
        //
        void drawPatchWindow(int windowid)
        {
            Log.Info("===================================================================================================");
            string lastModDisplayed = "";
            GUILayout.BeginHorizontal();
            fileSelectionScrollPosition = GUILayout.BeginScrollView(fileSelectionScrollPosition);
            GUILayout.BeginVertical();
            for (int i = 0; i < availablePatches.Count(); i++)
            {
                pi = availablePatches[i];
                Log.Info("i: " + i.ToString() + ",  modname: " + pi.modName);

                if (!expanded && pi.modName == lastModDisplayed)
                    continue;

                GUILayout.BeginHorizontal();
                GUIStyle gs = bodyButtonStyle;

                GUILayout.BeginVertical();

                // Disply Mod Buttons
                string s = "";
                //GUILayout.BeginVertical();

                Texture2D Image = null;
                if (pi.icon != null && pi.icon.Length > 0)
                    Image = GameDatabase.Instance.GetTexture(pi.icon, false);
                GUI.enabled = !expanded || exclusionsOK(pi);

                if (Image == null)
                {
                    if (GUILayout.Button("", HighLogic.Skin.label, GUILayout.Width(38), GUILayout.Height(38)))
                    {
                        //ToggleActivation(pi);
                    }
                }
                else
                {
                    if (GUILayout.Button(Image, HighLogic.Skin.label, GUILayout.Width(38), GUILayout.Height(38)))
                    {
                        // ToggleActivation(pi);
                    }
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                if (!expanded || lastModDisplayed != pi.modName)
                {
                    if (GUILayout.Button(pi.modName, GUILayout.Width(175)))
                    {
                        if (expanded && expandedMod == pi.modName)
                            expanded = false;
                        else
                            if (!expanded)
                                expanded = !expanded;
                        expandedMod = pi.modName;
                    }
                }
                else
                    GUILayout.Label(" ", GUILayout.Width(175));
                lastModDisplayed = pi.modName;
                GUILayout.EndVertical();
                // End of Mod button display


                GUI.enabled = exclusionsOK(pi);
                GUILayout.BeginVertical();

                if (expanded && pi.modName == expandedMod )
                {
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
                    if (GUILayout.Button(pi.shortDescr, gs, GUILayout.Width(175)))
                    {
                        ToggleActivation(pi);
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                   
                    GUILayout.Label(pi.longDescr + "\n" + pi.author + "\n", GUILayout.Width(WIDTH - 175*2 - 38 - 2));
                }
                else
                {
                    GUILayout.Label("\n\n", GUILayout.Width(WIDTH - 175 - 38 - 2));
                }
                GUI.enabled = true;

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
            if (GUILayout.Button("Settings"))
            {
                showSettings = true;
            }
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
                    if (!pi.installedWithMod)
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
                    else
                    {
                        if (pi.enabled)
                        {
                            // delete the active patch file
                            Log.Info("Moving patch at: " + s + " to " + KSP_DIR + "/GameData/" + pi.srcPath);
                            File.Move(s, KSP_DIR + "/GameData/" + pi.srcPath);
                        }
                        else
                        {
                            // Copy the file to the dest to make it active
                            Log.Info("Moving patch from: " + KSP_DIR + "/GameData/" + pi.srcPath + "   to: " + s);
                            File.Move(KSP_DIR + "/GameData/" + pi.srcPath, s);
                        }
                    }
                }
            }
            restartMsg = true;
        }
        private void FixedUpdate()
        {
            if (Button == null && HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave)
                CreateButton();
            if (Button != null && !HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave)
                OnDestroy();
        }



        bool showSettings = false;
        public void OnGUI()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave && settings.alwaysShow || (availablePatches != null && availablePatches.Count() > 0))
            {
                if (!showSettings && visible)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Native);
                    windowPosition = GUILayout.Window(windowId, windowPosition, drawPatchWindow, "Patch Manager");
                }
                if (restartMsg)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Native);
                    windowPosition = GUILayout.Window(windowId, windowPosition, drawRestartWindow, "Restart Message");
                }
                if (showSettings)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Native);
                    windowPosition = GUILayout.Window(windowId, windowPosition, drawSettingsWindow, "Patch Manager Settings");
                }
            }
        }

        void LoadAllPatches()
        {
            Log.Info("LoadAllPatches");
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

                    string s = n.GetValue("installedWithMod");
                    if (s != null && s.Length > 0)
                        pi.installedWithMod = Boolean.Parse(s);


                    if (!pi.installedWithMod && settings.storeActivePatchesInPMFolder)
                    {
                        //if (pi.destPath == null || pi.destPath == "")
                        pi.destPath = DEFAULT_PATCH_DIRECTORY;
                        //else
                        //    pi.destPath = "GameData/" + pi.destPath;
                    }
                    else
                    {
                        pi.destPath = "GameData/" + pi.srcPath.Replace("PluginData", "ActiveMMPatches");
                        pi.destPath = pi.destPath.Substring(0, pi.destPath.LastIndexOf('/'));
                        if (pi.installedWithMod)
                            Log.Info("installedWithMod: True,  srcPath: " + pi.srcPath + "    destPath: " + pi.destPath);
                        else
                            Log.Info("Storing patches in source mod dir,  srcPath: " + pi.srcPath + "    destPath: " + pi.destPath);
                    }

                    pi.fname = pi.srcPath.Substring(pi.srcPath.LastIndexOf('/') + 1);

                    bool bd = Directory.Exists(pi.destPath) || File.Exists(pi.destPath);
                    string activePatchName = getActivePatchName(pi); ;
                    if (bd)
                    {
                        // check for old style filename, if it's there, rename it with the modname in front
                        string oldName = getActivePatchName(pi, false);
                        if (!pi.installedWithMod)
                        {
#if false
                            Log.Info("Checking for old name: " + oldName);
                            if (File.Exists(oldName))
                            {
                                if (!File.Exists(activePatchName))
                                    System.IO.File.Move(oldName, activePatchName);
                            }

                            Log.Info("Checking for patch in alternative location");
                            string altPath = getAlternativeActivePatchName(pi);
                            Log.Info("activePatchName: " + activePatchName + ",   alternativePatchName: " + altPath);
                            if (File.Exists(altPath))
                            {
                                Log.Info("Moving patch to correct location");
                                System.IO.File.Move(altPath, activePatchName);
                            }
#endif
                            Log.Info("Checking for file: " + activePatchName);
                            pi.enabled = File.Exists(activePatchName);
                        }
                        else
                        {
                            Log.Info("Checking for file: " + oldName);
                            pi.enabled = File.Exists(oldName);
                        }
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

        void CheckPatchLocations()
        {
            Log.Info("CheckPatchLocations");
            LoadAllPatches();
        }

        string getActivePatchName(PatchInfo pi, bool withModname = true)
        {
            pi.fname = pi.srcPath.Substring(pi.srcPath.LastIndexOf('/') + 1);

            string s;
            if (withModname && /*!pi.installedWithMod && */ settings.storeActivePatchesInPMFolder)
                s = pi.destPath + "/" + pi.modName + "_" + pi.fname;
            else
                s = pi.destPath + "/" + pi.fname;
            s = s.Replace(' ', '_');
            return KSP_DIR + s;
        }

        string getAlternativeActivePatchName(PatchInfo pi)
        {
            string destPath = "";
            if (!pi.installedWithMod && !settings.storeActivePatchesInPMFolder)
            {
                destPath = DEFAULT_PATCH_DIRECTORY;
            }
            else
            {
                destPath = "GameData/" + pi.srcPath.Replace("PluginData", "ActiveMMPatches");
                destPath = destPath.Substring(0, destPath.LastIndexOf('/'));
            }


            pi.fname = pi.srcPath.Substring(pi.srcPath.LastIndexOf('/') + 1);

            string s;
            if (!settings.storeActivePatchesInPMFolder)
                s = destPath + "/" + pi.modName + "_" + pi.fname;
            else
                s = destPath + "/" + pi.fname;
            s = s.Replace(' ', '_');
            return KSP_DIR + s;
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
            Log.Info("pi.patchName: " + pi.patchName);
            Log.Info("pi.modName: " + pi.modName);
            Log.Info("pi.exclusions: " + pi.exclusions);
            List<string> stringList = pi.exclusions.Split(',').ToList();
            for (int i = 0; i < stringList.Count; i++)
            {
                var s = stringList[i];
                s = pi.modName + "_" + s;
                stringList[i] = s;
            }
#if falsae
            foreach (var s2 in stringList)
            {
                Log.Info("s2: " + s2);
            }
#endif
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
                    }
                    else
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
