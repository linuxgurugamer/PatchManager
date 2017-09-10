
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;


namespace PatchManager
{


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


    public class PM : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Patch Manager"; } }
        public override string DisplaySection { get { return "Patch Manager"; } }
        public override int SectionOrder { get { return 2; } }
        public override bool HasPresets { get { return true; } }


        [GameParameters.CustomParameterUI("Mod Enabled")]
        public bool EnabledForSave = true;      // is enabled for this save file


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }

}
