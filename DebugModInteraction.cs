using System.Reflection;
using Modding;

namespace StaticCamera {
    public class DebugModInteraction {
        internal static readonly FieldInfo cameraFollow = typeof(DebugMod.DebugMod).GetField("cameraFollow", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
        private IMod debugModInstance = ModHooks.GetMod("DebugMod");
        
        public bool IsDebugModInstalled() => debugModInstance is Mod;

        public bool IsCameraFollowEnabled() => (bool)cameraFollow.GetValue((DebugMod.DebugMod)debugModInstance);

        public void ToggleCameraFollow() => DebugMod.BindableFunctions.ForceCameraFollow();
    }
}