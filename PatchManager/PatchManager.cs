using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using KSP.UI.Screens;

using KSP.Localization;

using ClickThroughFix;
using ToolbarControl_NS;


namespace PatchManager
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(PatchManagerClass.MODID, PatchManagerClass.MODNAME);
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public partial class PatchManagerClass : MonoBehaviour
    {
        internal static PatchManagerClass Instance;

        const string CONFIG_NODENAME = "PatchManager";

        string KSP_DIR = KSPUtil.ApplicationRootPath;
        internal string DEFAULT_PATCH_DIRECTORY;
        string CFG_DIR;
        public static List<String> installedMods = null;

        //private ApplicationLauncherButton Button;
        bool visible = false;
        bool restartMsg = false;
        Rect windowPosition;
        const int WIDTH = 900;
        const int HEIGHT = 600;
        Vector2 fileSelectionScrollPosition = new Vector2();

        static List<PatchInfo> availablePatches = new List<PatchInfo>();
        static List<String> installedPatches = new List<String>();

        internal Settings settings = new Settings();

        PatchInfo pi;

        public void Start()
        {
            Instance = this;
            DEFAULT_PATCH_DIRECTORY = "GameData/PatchManager/ActiveMMPatches";
            CFG_DIR = "GameData/PatchManager/PluginData";
            LoadAllPatches();
            settings.LoadSettings(CFG_DIR);

            windowPosition = new Rect((Screen.width - WIDTH) / 2, (Screen.height - HEIGHT) / 2, WIDTH, HEIGHT);
            
            if (HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave)
                CreateButton();
        }
        ToolbarControl toolbarControl;
        internal const string MODID = "Patchmanger_NS";
        internal const string MODNAME = "Patch Manager";

        void CreateButton()
        {
            if (!settings.alwaysShow && (availablePatches == null || availablePatches.Count() == 0))
                return;
#if false
            Texture2D Image = GameDatabase.Instance.GetTexture("PatchManager/Resources/PatchManager", false);
            Button = ApplicationLauncher.Instance.AddModApplication(onTrue, onFalse, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER, Image);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(Destroy);
#endif
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(onTrue, onFalse,
                ApplicationLauncher.AppScenes.SPACECENTER ,
                MODID,
                "patchManagerButton﻿",
                "PatchManager/Resources/PatchManager-38",
                "PatchManager/Resources/PatchManager-24",
                MODNAME
            );

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
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
                toolbarControl = null;
#if false
                ApplicationLauncher.Instance.RemoveModApplication(Button);
                GameEvents.onGUIApplicationLauncherUnreadifying.Remove(Destroy);
                Button = null;
#endif
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
            //Button.SetFalse();
            toolbarControl.SetFalse(true);
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
            //string s = "This overrides the function of the button being hidden if there are no patches due to dependencies";
            string s = Localizer.Format("pm_overrideinfo");
            GUILayout.TextField(s);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            //s = "This does not disble/enable the mod, that you can do in the standard settings:";
            s = Localizer.Format("pm_doesNot");
            GUILayout.TextField(s);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            //settings.alwaysShow = GUILayout.Toggle(settings.alwaysShow, "Always show toolbar button");
            settings.alwaysShow = GUILayout.Toggle(settings.alwaysShow, Localizer.Format("pm_alwaysShow"));
            GUILayout.EndHorizontal();
            GUILayout.Space(25);
            GUILayout.BeginHorizontal();
            //s = "Disable this to store the active patches in the patch's parent mod folder";
            s = Localizer.Format("pm_disablethis");
            GUILayout.TextArea(s);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            //settings.storeActivePatchesInPMFolder = GUILayout.Toggle(settings.storeActivePatchesInPMFolder, "Store active patches in PatchManager folder");
            settings.storeActivePatchesInPMFolder = GUILayout.Toggle(settings.storeActivePatchesInPMFolder, Localizer.Format("pm_storeactive"));
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("pm_ok"), GUILayout.Width(60)))
            {
                showSettings = false;
                settings.SaveSettings(CFG_DIR);
                CheckPatchLocations();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }
        void doQuickShutdown()
        {
            //QExit.Instance.doShutdown = true;
            doShutdown = true;
        }
        //
        // Need to warn the user to restart KSP
        //
        void drawRestartWindow(int windowid)
        {
            CenterLine(" ");
            //CenterLine("The changes you just made by installing/uninstalling one or");
            //CenterLine("more patches will not take effect until the game is restarted");
            CenterLine(Localizer.Format("pm_changesmade1"));
            CenterLine(Localizer.Format("pm_changesmade2"));
            CenterLine(" ");
            CenterLine(Localizer.Format("pm_changesmade3"));
            CenterLine(Localizer.Format("pm_changesmade4"));

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("pm_ack"), GUILayout.Width(150), GUILayout.Height(40)))
                restartMsg = false;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("pm_shutdown"), GUILayout.Width(150), GUILayout.Height(40)))
            {
                restartMsg = false;
                doQuickShutdown();
            }
                
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUI.DragWindow();
        }

        bool expanded = false;
        string expandedMod;

        //
        // Main window 
        //
        void drawPatchWindow(int windowid)
        {
            //Log.Info("===================================================================================================");
            string lastModDisplayed = "";
            GUILayout.BeginHorizontal();
            fileSelectionScrollPosition = GUILayout.BeginScrollView(fileSelectionScrollPosition);
            GUILayout.BeginVertical();
            for (int i = 0; i < availablePatches.Count(); i++)
            {
                pi = availablePatches[i];
                //Log.Info("i: " + i.ToString() + ",  modname: " + pi.modName);

                if (!expanded && pi.modName == lastModDisplayed)
                    continue;

                GUILayout.BeginHorizontal();
                GUIStyle gs = bodyButtonStyle;

                GUILayout.BeginVertical();

                // Disply Mod Buttons
                //string s = "";
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
            if (GUILayout.Button(Localizer.Format("pm_applyall"), GUILayout.Width(90)))
            {
                ApplyAllChanges();
                HideWindow();

            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("pm_cancel"), GUILayout.Width(90)))
            {
                HideWindow();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Localizer.Format("pm_settings")))
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

                if (pi.toggle)
                {
                    if (pi.enabled)
                    {
                        Log.InfoAlways("Removing active patch: " + pi.activePatchName);
                        Log.InfoAlways("activePatchPath: " + pi.activePatchPath + ", inactivePatchPath: " + pi.inactivePatchPath);
                        // Save the file if it doesn't exist at the dstination, otherwise delete it
                        if (!System.IO.File.Exists(pi.activePatchPath))
                        {
                            ScreenMessages.PostScreenMessage("Patch file: " + pi.activePatchPath + " missing", 5);
                            Log.InfoAlways("Patch file: " + pi.activePatchPath + " missing");
                        }
                        else
                        {
                            if (!System.IO.File.Exists(pi.inactivePatchPath))
                                File.Move(pi.activePatchPath, pi.inactivePatchPath);
                            else
                                File.Delete(pi.activePatchPath);
                        }
                    }
                    else
                    {
                        Log.InfoAlways("Activating patch: " + pi.activePatchName);
                        Log.InfoAlways("activePatchPath: " + pi.activePatchPath + ", inactivePatchPath: " + pi.inactivePatchPath);
                        if (System.IO.File.Exists(pi.inactivePatchPath))
                            File.Copy(pi.inactivePatchPath, pi.activePatchPath);
                        else
                            ScreenMessages.PostScreenMessage("Patch file: " + pi.inactivePatchPath + " missing", 5);
                        Log.InfoAlways("Patch file: " + pi.inactivePatchPath + " missing");
                    }
                }
            }
            restartMsg = true;
        }

        private void LateUpdate()
        {
            if (toolbarControl == null && HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave)
                CreateButton();
            if (toolbarControl != null && !HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave)
                OnDestroy();
        }



        bool showSettings = false;
        public void OnGUI()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<PM>().EnabledForSave && settings.alwaysShow || (availablePatches != null && availablePatches.Count() > 0))
            {
                if (!showSettings && visible)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Passive);
                    
                    windowPosition = ClickThruBlocker.GUILayoutWindow(windowId, windowPosition, drawPatchWindow, Localizer.Format("pm_patchmanager"));
                }
                if (restartMsg)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Passive);
                    windowPosition = ClickThruBlocker.GUILayoutWindow(windowId, windowPosition, drawRestartWindow, Localizer.Format("pm_restart"));
                }
                if (showSettings)
                {
                    int windowId = GUIUtility.GetControlID(FocusType.Passive);
                    windowPosition = ClickThruBlocker.GUILayoutWindow(windowId, windowPosition, drawSettingsWindow, Localizer.Format("pm_settingstitle"));
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
                    PatchInfo pi = new PatchInfo(n);
                    
                    if (pi.enabled)
                        installedPatches.Add(pi.activePatchName);
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
            {
                Log.Info("No exclusions");
                return true;
            }
            Log.Info("pi.patchName: " + pi.patchName);
            Log.Info("pi.modName: " + pi.modName);
            Log.Info("pi.exclusions: " + pi.exclusions);
            List<string> stringList = pi.exclusions.Split(',').ToList();
            for (int i = 0; i < stringList.Count; i++)
            {
                var s = stringList[i];
                s = pi.modName + "_" + s;
                s = s.Replace(' ', '_');
                stringList[i] = s;
                Log.Info("stringlist[" + i + "]: " + s);
            }

            for (int i = 0; i < availablePatches.Count(); i++)
            {
                pi = availablePatches[i];
                string s = pi.exclusionPatchName;
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
            Log.Info("No exclusion found");
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
