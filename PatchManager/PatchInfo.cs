using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using KSP.UI.Screens;

namespace PatchManager
{
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
        private string srcPath;
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
        private bool installedWithMod = false;

        private string destPath;

        //
        // Some useful vars, created at load, not in the file
        //
        public string activePatchName
        {
            get
            {
                fname = srcPath.Substring(srcPath.LastIndexOf('/') + 1);
                string s;
                if (!installedWithMod)
                {
                    if (PatchManagerClass.Instance.settings.storeActivePatchesInPMFolder)
                        s = modName + "_" + fname;
                    else
                        s = fname;
                }
                else
                {
                    s = fname;
                }
                s = s.Replace(' ', '_');
                return s;
            }
        }
        public string exclusionPatchName
        {
            get
            {
                fname = srcPath.Substring(srcPath.LastIndexOf('/') + 1);
                fname = fname.Substring(0, fname.Length - 4);
                string s = modName + "_" + fname;

                s = s.Replace(' ', '_');
                return s;
            }
        }
        public string activePatchPath
        {
            get
            {
                fname = srcPath.Substring(srcPath.LastIndexOf('/') + 1);

                string s;
                if (!installedWithMod)
                {
                    if (PatchManagerClass.Instance.settings.storeActivePatchesInPMFolder)
                        s = destPath + "/" + modName + "_" + fname;
                    else
                        s = destPath + "/" + fname;
                }
                else
                {
                    if (!installedWithMod)
                        s = PatchManagerClass.Instance.DEFAULT_PATCH_DIRECTORY + "/" + fname;
                    else
                        s = "GameData/" + modName + "/PatchManager/ActiveMMPatches/" + fname;
                }
                s = s.Replace(' ', '_');

                return KSPUtil.ApplicationRootPath + s;
            }
        }

        public string inactivePatchPath { get { return KSPUtil.ApplicationRootPath + "GameData/" + srcPath; } }


        public PatchInfo(ConfigNode n)
        {
            modName = n.GetValue("modName");
            patchName = n.GetValue("patchName");
            srcPath = n.GetValue("srcPath");
            shortDescr = n.GetValue("shortDescr");
            longDescr = n.GetValue("longDescr");
            dependencies = n.GetValue("dependencies");
            exclusions = n.GetValue("exclusions");
            icon = n.GetValue("icon");
            author = n.GetValue("author");
            string s = n.GetValue("installedWithMod");
            if (s != null && s.Length > 0)
                installedWithMod = Boolean.Parse(s);

            if (!installedWithMod && PatchManagerClass.Instance.settings.storeActivePatchesInPMFolder)
            {
                destPath = PatchManagerClass.Instance.DEFAULT_PATCH_DIRECTORY;
            }
            else
            {
                destPath = "GameData/" + srcPath.Replace("PluginData", "ActiveMMPatches");
                destPath = destPath.Substring(0, destPath.LastIndexOf('/'));
#if DEBUG
                if (installedWithMod)
                    Log.Info("installedWithMod: True,  srcPath: " + srcPath + "    destPath: " + destPath);
                else
                    Log.Info("Storing patches in source mod dir,  srcPath: " + srcPath + "    destPath: " + destPath);
#endif
            }

            fname = srcPath.Substring(srcPath.LastIndexOf('/') + 1);
            bool bd = Directory.Exists(destPath) || File.Exists(destPath);
            if (installedWithMod)
            {
                enabled = File.Exists(destPath + "/" + fname);
            }
            else
            {

                if (bd)
                {
                    enabled = File.Exists(activePatchPath);
                }
                else
                {
                    enabled = false;
                    DirectoryInfo di = Directory.CreateDirectory(destPath);
                    // Shouldn't ever happen, but if it does, create the directory
                }
            }
            toggle = false;

            var s1 = activePatchPath;
            Log.InfoAlways("modname: " + modName + ", patchName: " + patchName + ", srcPath: " + srcPath + ", installedWithMod: " + installedWithMod.ToString() +
                ", destPath: " + destPath +
                ", activePatchPath: " + s1 + ",  enabled: " + enabled.ToString());
        }
    }

}
