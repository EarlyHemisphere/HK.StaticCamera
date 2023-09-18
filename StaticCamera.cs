using Modding;
using GlobalEnums;
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
        private bool heroInPositionHookAdded = false;
        private bool hazardRespawning = false;
        private Vector3 cameraLocationBeforeHazardRespawn;
        private Vector3 camTargetLocationBeforeHazardRespawn;
        // private bool wasStaticBeforeSceneChange = false;
        private DebugModInteraction debugModInteraction;
        private bool isDebugModInstalled;

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
            } else {
                if (isStatic && (cameraController.cam.transform.position != cameraLocationBeforeHazardRespawn || cameraController.camTarget.transform.position != camTargetLocationBeforeHazardRespawn)) {
                    cameraController.cam.transform.position = cameraLocationBeforeHazardRespawn;
                    cameraController.camTarget.transform.position = camTargetLocationBeforeHazardRespawn;
                }
                if (isStatic && (cameraController.mode != CamControllerCameraMode.FROZEN || cameraController.camTarget.mode != CamTargetMode.FREE)) {
                    cameraController.SetMode(CamControllerCameraMode.FROZEN);
                    cameraController.camTarget.mode = CamTargetMode.FREE;
                }
            }

            if (Input.GetKeyDown((KeyCode)System.Enum.Parse(typeof(KeyCode), localSettings.ToggleStaticCameraKey, true))) {
                ToggleStaticCamera();
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
                cameraLocationBeforeHazardRespawn = cameraController.cam.transform.position;
                camTargetLocationBeforeHazardRespawn = cameraController.camTarget.transform.position;

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

        // public int AfterTakeDamage(int hazardType, int damageAmount) {
        //     if (isStatic && hazardType > 1) {
        //         hazardRespawning = true;
        //         cameraLocationBeforeHazardRespawn = cameraController.cam.transform.position;
        //         camTargetLocationBeforeHazardRespawn = cameraController.camTarget.transform.position;
        //     }
        //     return damageAmount;
        // }

        // public void PositionToHero(On.CameraController.orig_PositionToHero orig, CameraController self, bool forceDirect) {
        //     orig(self, forceDirect);

        //     if (hazardRespawning) {
        //         cameraController.cam.transform.position = cameraLocationBeforeHazardRespawn;
        //         cameraController.camTarget.transform.position = camTargetLocationBeforeHazardRespawn;
        //         hazardRespawning = false;
        //         cameraController.FreezeInPlace(true);
        //         cameraGameplayScene.SetValue(cameraController, false);
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