using System.Collections;
using System.Linq;
using MelonLoader;
using PlayerList.Config;
using PlayerList.Entries;
using PlayerList.Utilities;
using UnhollowerRuntimeLib;
using UnityEngine;
using VRChatUtilityKit.Components;

[assembly: MelonInfo(typeof(PlayerList.PlayerListMod), "PlayerList", "2.0.5", "Adnezz", "https://github.com/Adnezz/PlayerList")]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonOptionalDependencies("UIExpansionKit", "emmVRC")]

namespace PlayerList
{
    public class PlayerListMod : MelonMod
    {
        public static PlayerListMod Instance { get; private set; }

        public static bool HasUIX => MelonHandler.Mods.Any(x => x.Info.Name.Equals("UI Expansion Kit"));

        public override void OnApplicationStart()
        {
            Instance = this;
            PlayerListConfig.RegisterSettings();
            EntryManager.Init();
            ListPositionManager.Init();
            MenuManager.Init();
            EntrySortManager.Init();
            PlayerEntry.EntryInit();
            LocalPlayerEntry.EntryInit();

            MelonCoroutines.Start(StartUiManagerInitIEnumerator());
        }

        private IEnumerator StartUiManagerInitIEnumerator()
        {
            while (VRCUiManager.prop_VRCUiManager_0 == null)
                yield return null;

            //Wait a little longer
            while (GameObject.Find("UserInterface") == null)
                yield return null;

            //Even longer

            while (GameObject.Find("UserInterface").transform.Find("Canvas_QuickMenu(Clone)") == null)
                yield return null;

            //WAIT SOME MOAR

            while (GameObject.Find("UserInterface").transform.Find("Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_Dashboard") == null)
                yield return null;

            OnUiManagerInit();
        }

        public void OnUiManagerInit()
        {
            // Initialize Constants util
            Constants.UIInit();

            // TODO: Add opacity options, maybe color too, (maybe even for each stage of ping and fps??)
            // TODO: add indicator for those in hearing distance

            MenuManager.LoadAssetBundle();

            // Initialize submenu for the list
            //MenuManager.CreateMainSubMenu();
            MenuManager.OnUiManagerInit();

            // This is kinda a mess but whatever
            MenuManager.AddMenuListeners();
            MenuManager.CreateSortPages();
            MenuManager.CreateSubMenus();
            EntryManager.AddGeneralInfoEntries();
            MenuManager.CreateGeneralInfoSubMenus();
            MenuManager.AdjustSubMenus();

            PlayerListConfig.OnConfigChange(false);

            MelonLogger.Msg("Initialized!");
        }

        public override void OnUpdate()
        {
            EntryManager.OnUpdate();
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.F1)) MenuManager.ToggleMenu();
        }
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (buildIndex != -1) return;

            //MenuManager.OnSceneWasLoaded();
            MelonCoroutines.Start(WaitForMenu());
            //Constants.OnSceneWasLoaded();
            EntryManager.OnSceneWasLoaded();
        }
        private IEnumerator WaitForMenu()
        {
            while (Constants.quickMenu.GetComponent<BoxCollider>() == null)
                yield return null;
            Constants.OnSceneWasLoaded();
        }
    }
}
