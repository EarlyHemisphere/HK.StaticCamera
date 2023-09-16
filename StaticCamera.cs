using Modding;
using System.Reflection;
using UnityEngine;
using CamControllerCameraMode = CameraController.CameraMode;
using CamTargetMode = CameraTarget.TargetMode;

namespace StaticCamera {
    public class StaticCamera: Mod {
        public static StaticCamera instance;
        private static CameraController cameraController = null;
        internal static readonly FieldInfo cameraGameplayScene = typeof(CameraController).GetField("isGameplayScene", BindingFlags.Instance | BindingFlags.NonPublic);
        private bool isStatic = false;
        private CamControllerCameraMode? prevCamCtrlMode;
        private CamTargetMode? prevCamTargetMode;
        private bool wasCameraFollow = false;
        // private bool wasStaticBeforeSceneChange = false;
        private DebugModInteraction debugModInteraction;
        private bool isDebugModInstalled;
        // private bool heroInPositionHookAdded = false;

        public LocalSettings localSettings = new LocalSettings();
        public override ModSettings GlobalSettings
        {
            get => localSettings;
            set => localSettings = (LocalSettings) value;
        }

        public StaticCamera(): base ("Static Camera") {
            instance = this;
        }

        public override int LoadPriority() => 10000; // Load after DebugMod

        public override void Initialize() {
            Log("Initializing");

            ModHooks.Instance.HeroUpdateHook += HeroUpdate;
            ModHooks.Instance.BeforeSceneLoadHook += UnlockForSceneChange;
            debugModInteraction = new DebugModInteraction();
            debugModInteraction.Initialize();
            isDebugModInstalled = debugModInteraction.IsDebugModInstalled();

            Log("Initialized");
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public void HeroUpdate() {
            if (cameraController == null) {
                cameraController = GameManager.instance.cameraCtrl;
            }

            // if (!heroInPositionHookAdded && HeroController.instance) {
            //     HeroController.instance.heroInPosition += HeroInPosition;
            //     heroInPositionHookAdded = true;
            // }

            if (Input.GetKeyDown((KeyCode)System.Enum.Parse(typeof(KeyCode), localSettings.ToggleStaticCameraKey, true))) {
                ToggleStaticCamera();
            }

            if (isStatic && (cameraController.mode != CamControllerCameraMode.FROZEN || cameraController.camTarget.mode != CamTargetMode.FREE)) {
                cameraController.SetMode(CamControllerCameraMode.FROZEN);
                cameraController.camTarget.mode = CamTargetMode.FREE;
            }
        }

        private void ModifyCameraLockAreas(bool enabled = false) {
            foreach (CameraLockArea cameraLockArea in (CameraLockArea[])Resources.FindObjectsOfTypeAll(typeof(CameraLockArea))) {
                cameraLockArea.gameObject.GetComponent<Collider2D>().enabled = enabled;
            }
        }

        public void ToggleStaticCamera() {
            isStatic = !isStatic;

            if (isStatic) {
                prevCamCtrlMode = cameraController.mode;
                prevCamTargetMode = cameraController.camTarget.mode;

                if (isDebugModInstalled && debugModInteraction.IsCameraFollowEnabled()) {
                    wasCameraFollow = true;
                    debugModInteraction.ToggleCameraFollow();
                }

                cameraController.FreezeInPlace(true);
                ModifyCameraLockAreas(enabled: false);
                cameraGameplayScene.SetValue(cameraController, false);
            } else if (prevCamCtrlMode.HasValue && prevCamTargetMode.HasValue) {
                ModifyCameraLockAreas(enabled: true);
                cameraGameplayScene.SetValue(cameraController, true);
                cameraController.camTarget.mode = prevCamTargetMode.Value;
                cameraController.SetMode(prevCamCtrlMode.Value);

                if (wasCameraFollow) {
                    wasCameraFollow = false;
                    debugModInteraction.ToggleCameraFollow();
                }
            }
        }

        // public void HeroInPosition(bool _) {
        //     if (wasStaticBeforeSceneChange) {
        //         ToggleStaticCamera();
        //         wasStaticBeforeSceneChange = false;
        //     }
        // }

        public string UnlockForSceneChange(string sceneName) {
            if (isStatic) {
                ToggleStaticCamera();
                // wasStaticBeforeSceneChange = true;
            }

            return sceneName;
        }
    }

    public class LocalSettings: ModSettings {
        public string ToggleStaticCameraKey = KeyCode.G.ToString();
    }
}