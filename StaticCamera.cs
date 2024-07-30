using Modding;
using Modding.Converters;
using Newtonsoft.Json;
using Satchel.BetterMenus;
using System.Reflection;
using System.Collections.Generic;
using InControl;
using UnityEngine;
using CamControllerCameraMode = CameraController.CameraMode;
using CamTargetMode = CameraTarget.TargetMode;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace StaticCamera {
    public class StaticCamera: Mod, ICustomMenuMod, IGlobalSettings<GlobalSettings> {
        private Menu menuRef;
        public static StaticCamera instance;
        private static CameraController cameraController = null;
        internal static readonly FieldInfo cameraGameplayScene = typeof(CameraController).GetField("isGameplayScene", BindingFlags.Instance | BindingFlags.NonPublic);
        private bool isStatic = false;
        private CamControllerCameraMode? prevCamCtrlMode = null;
        private CamTargetMode? prevCamTargetMode = null;
        private bool wasCameraFollow = false;
        // private bool wasStaticBeforeSceneChange = false;
        private DebugModInteraction debugModInteraction;
        private bool isDebugModInstalled;
        // private bool heroInPositionHookAdded = false;

        public StaticCamera(): base ("Static Camera") {
            instance = this;
        }

        public override void Initialize() {
            Log("Initializing");

            ModHooks.HeroUpdateHook += HeroUpdate;
            ModHooks.BeforeSceneLoadHook += UnlockForSceneChange;
            debugModInteraction = new DebugModInteraction();
            isDebugModInstalled = debugModInteraction.IsDebugModInstalled();

            Log("Initialized");
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public bool ToggleButtonInsideMenu => false;

        public static GlobalSettings globalSettings { get; private set; } = new();
        public void OnLoadGlobal(GlobalSettings s) => globalSettings = s;
        public GlobalSettings OnSaveGlobal() => globalSettings;

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) {
            menuRef ??= new Menu(
                name: "Key Binds",
                elements: new Element[] {
                    Blueprints.KeyAndButtonBind(
                        name: "Toggle Static Camera",
                        keyBindAction: globalSettings.keyBinds.ToggleStaticCameraKey,
                        buttonBindAction: globalSettings.keyBinds.ToggleStaticCameraBtn
                    )
                }
            );

            return menuRef.GetMenuScreen(modListMenu);
        }

        public void HeroUpdate() {
            if (cameraController == null) {
                cameraController = GameManager.instance.cameraCtrl;
            }

            // if (!heroInPositionHookAdded && HeroController.instance) {
            //     HeroController.instance.heroInPosition += HeroInPosition;
            //     heroInPositionHookAdded = true;
            // }

            if (globalSettings.keyBinds.ToggleStaticCameraKey.WasPressed || globalSettings.keyBinds.ToggleStaticCameraBtn.WasPressed) {
                ToggleStaticCamera();
            }

            if (isStatic && (cameraController.mode != CamControllerCameraMode.FROZEN || cameraController.camTarget.mode != CamTargetMode.FREE)) {
                cameraController.SetMode(CamControllerCameraMode.FROZEN);
                cameraController.camTarget.mode = CamTargetMode.FREE;
            }
        }

        private void ModifyCameraLockAreas(bool enabled = false) {
            foreach (CameraLockArea cameraLockArea in Object.FindObjectsOfType<CameraLockArea>(true)) {
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

    public class GlobalSettings {
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds keyBinds = new KeyBinds();
    }

    public class KeyBinds : PlayerActionSet {
        public PlayerAction ToggleStaticCameraKey;
        public PlayerAction ToggleStaticCameraBtn;

        public KeyBinds() {
            ToggleStaticCameraKey = CreatePlayerAction("ToggleStaticCameraKey");
            ToggleStaticCameraBtn = CreatePlayerAction("ToggleStaticCameraBtn");
        }
    }
}