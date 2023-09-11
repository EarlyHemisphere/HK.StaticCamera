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
    public class StaticCamera: Mod, ICustomMenuMod, ILocalSettings<LocalSettings> {
        private Menu menuRef;
        public static StaticCamera instance;
        private static CameraController cameraController = null;
        internal static readonly FieldInfo cameraGameplayScene = typeof(CameraController).GetField("isGameplayScene", BindingFlags.Instance | BindingFlags.NonPublic);
        private HashSet<CameraLockArea> activeCameraLockAreas = new HashSet<CameraLockArea>();
        private bool isStatic = false;
        private CamControllerCameraMode? prevCamCtrlMode = null;
        private CamTargetMode? prevCamTargetMode = null;
        private bool wasCameraFollow = false;
        private DebugModInteraction debugModInteraction;
        private bool isDebugModInstalled;

        public StaticCamera(): base ("Static Camera") {
            instance = this;
        }

        public override void Initialize() {
            Log("Initializing");

            ModHooks.HeroUpdateHook += HeroUpdate;
            USceneManager.activeSceneChanged += SceneChanged;
            debugModInteraction = new DebugModInteraction();
            isDebugModInstalled = debugModInteraction.IsDebugModInstalled();

            Log("Initialized");
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public bool ToggleButtonInsideMenu => false;

        public static LocalSettings localSettings { get; private set; } = new();
        public void OnLoadLocal(LocalSettings s) => localSettings = s;
        public LocalSettings OnSaveLocal() => localSettings;

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) {
            menuRef ??= new Menu(
                name: "Key Binds",
                elements: new Element[] {
                    Blueprints.KeyAndButtonBind(
                        name: "Toggle Static Camera",
                        keyBindAction: localSettings.keyBinds.ToggleStaticCamera,
                        buttonBindAction: localSettings.keyBinds.ToggleStaticCamera
                    )
                }
            );

            return menuRef.GetMenuScreen(modListMenu);
        }

        public void HeroUpdate() {
            if (cameraController == null) {
                cameraController = GameManager.instance.cameraCtrl;
            }

            if (localSettings.keyBinds.ToggleStaticCamera.WasPressed) {
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

        public void SceneChanged(Scene from, Scene to) {
            activeCameraLockAreas = new HashSet<CameraLockArea>();
            foreach (CameraLockArea cameraLockArea in Object.FindObjectsOfType<CameraLockArea>()) {
                activeCameraLockAreas.Add(cameraLockArea);
            }

            isStatic = !isStatic;
            ToggleStaticCamera();
        }
    }

    public class LocalSettings {
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds keyBinds = new KeyBinds();
    }

    public class KeyBinds : PlayerActionSet {
        public PlayerAction ToggleStaticCamera;

        public KeyBinds() {
            ToggleStaticCamera = CreatePlayerAction("ToggleStaticCamera");
        }
    }
}