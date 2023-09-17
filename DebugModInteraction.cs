using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Modding;

namespace StaticCamera {
    internal class DebugModInteraction {
        private DebugMod.DebugMod debugModInstance = null;
        private static readonly FieldInfo cameraFollow = typeof(DebugMod.DebugMod).GetField("cameraFollow", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

        public void Initialize() {
            try {
                FileInfo dllFile = new FileInfo(@"hollow_knight_Data/Managed/Assembly-CSharp.dll");
                Assembly assembly = Assembly.LoadFrom(dllFile.FullName);
                Type modLoaderType = assembly.GetType("Modding.ModLoader");
                FieldInfo loadedModsFieldInfo = modLoaderType.GetField("LoadedMods", BindingFlags.Public | BindingFlags.Static);
                List<IMod> loadedMods = (List<IMod>)loadedModsFieldInfo.GetValue(null);
                debugModInstance = (DebugMod.DebugMod)loadedMods.FirstOrDefault(mod => mod.GetName().Equals("DebugMod"));
            } catch (Exception e) {
                Logger.Log("Failed to access DebugMod instance");
                Logger.Log(e);
            }
        }
        public bool IsDebugModInstalled() => debugModInstance != null;

        public bool IsCameraFollowEnabled() => (bool)cameraFollow.GetValue(debugModInstance);

        public void ToggleCameraFollow() => DebugMod.BindableFunctions.ForceCameraFollow();
    }
}