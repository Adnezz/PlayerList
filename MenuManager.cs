using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;
using PlayerList.Config;
using PlayerList.Entries;
using PlayerList.Utilities;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using VRCSDK2;
using VRChatUtilityKit.Components;
using VRChatUtilityKit.Ui;
using VRChatUtilityKit.Utilities;
using VRC.UI.Elements;


namespace PlayerList
{
    public class MenuManager
    {
        //public static List<SubMenu> playerListMenus = new List<SubMenu>();
        public static SubMenu sortMenu;
        public static ToggleButton menuToggleButton;

        public static bool shouldStayHidden;

        public static Label fontSizeLabel;

        public static GameObject playerList;
        public static RectTransform playerListRect;

        public static GameObject menuButton;
        public static TabButton tabButton;

        private static readonly Dictionary<EntrySortManager.SortType, SingleButton> sortTypeButtonTable = new Dictionary<EntrySortManager.SortType, SingleButton>();
        private static Image currentHighlightedSortType;
        private static PropertyInfo entryWrapperValue;
        struct menuStates
        {

            public bool dashboard;
            public bool plsettings;
            public bool sort;
            public bool userLocal;
            public bool userRemote;

            public menuStates(bool a, bool b, bool c, bool d, bool e)
            {
                dashboard = a;
                plsettings = b;
                sort = c;
                userLocal = d;
                userRemote = e;
            }
        }
        private static menuStates curMenuState;// = {false, false, false};

        public static SingleButton SingleButtonWr(string title, Action act, string tooltip, string name, bool resize = false)
        {
            //Sprite BlankSprite = Sprite.Create(new Rect(0, 0, 64, 64), new Vector2(2, 2), 100);
            //new SingleButton(new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.Alphabetical)), BlankSprite, "Alphabetical", "AlphabeticalSortButton", "Set sort type to alphabetical"));
            //new SingleButton(sortMenu.gameObject, new Vector3(4, 0), "Avatar Perf", new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.AvatarPerf)), "Set sort type to avatar perf", "AvatarPerfSortButton"));
            return new SingleButton(act, Constants.blankSprite, title, name, tooltip);
        }

        public static ToggleButton ToggleButtonWr(string onText, string offText, Action<bool> act, string offTooltip, string onTooltip, string name, bool defState = true, bool resize = false)
        {
            //var blank = Sprite.Create(new Rect(0, 0, 64, 64), new Vector2(2, 2), 100);
            //ToggleButton(playerListMenus[0].Path, new Vector3(5, -1), "Enabled on Start", "Disabled", new Action<bool>((state) => PlayerListConfig.enabledOnStart.Value = state), "Toggle if the list is toggled hidden on start", "Toggle if the list is toggled hidden on start", "EnabledOnStartToggle", PlayerListConfig.enabledOnStart.Value, true);
            ToggleButton newButt = new ToggleButton(null, Constants.checkSprite, null, onText, name, onTooltip, offTooltip);
            newButt.sprite = newButt.rectTransform.Find("Icon_On").GetComponent<Image>().activeSprite;
            newButt.ToggleComponent.isOn = defState;
            newButt.OnClick = act;
            return newButt;
        }

        public static void Init()
        {
            entryWrapperValue = PlayerListConfig.currentBaseSort.GetType().GetProperty("Value");
            PlayerListConfig.OnConfigChanged += OnConfigChanged;
            curMenuState = new menuStates( false, false, false, false, false );
        }
        public static void OnUiManagerInit()
        {
            if (tabButton != null)
                return;
            var test1 = menuButton.GetComponent<Image>().sprite;
            tabButton = new TabButton(test1, "Player List", "PLTab", "Player List", "Player List Mod Settings");
            tabButton.SubMenu.ToggleScrollbar(true);
            CreateMainSubMenu();
            //tabButton.gameObject.SetActive(PlayerListConfig.useTabMenu.Value);
        }
        public static void OnConfigChanged()
        {
            switch (PlayerListConfig.menuButtonPosition.Value)
            {
                case MenuButtonPositionEnum.TopLeft:
                    menuButton.transform.localPosition = Converters.ConvertToUnityUnits(new Vector3(2, -1));
                    menuButton.GetComponent<RectTransform>().pivot = new Vector2(1, 0);
                    break;
                case MenuButtonPositionEnum.BottomLeft:
                    menuButton.transform.localPosition = Converters.ConvertToUnityUnits(new Vector3(2, -1));
                    menuButton.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
                    break;
                case MenuButtonPositionEnum.BottomRight:
                    menuButton.transform.localPosition = Converters.ConvertToUnityUnits(new Vector3(4, -1));
                    menuButton.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
                    break;
                default:
                    menuButton.transform.localPosition = Converters.ConvertToUnityUnits(new Vector3(4, -1));
                    menuButton.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
                    break;
            }
            menuButton.SetActive(!PlayerListConfig.useTabMenu.Value);
            //if (PlayerListConfig.useTabMenu.Value && (tabButton != null && !tabButton.gameObject.activeSelf))
                //tabButton.OpenTabMenu();
            tabButton?.gameObject.SetActive(PlayerListConfig.useTabMenu.Value);
        }

        public static void ToggleMenu()
        {
            if (!sortMenu.gameObject.active && !tabButton.SubMenu.gameObject.active && !Constants.qmDashboard.gameObject.active) return;
            menuToggleButton.ToggleComponent.Set(shouldStayHidden);
            shouldStayHidden = !shouldStayHidden;
            if (sortMenu.gameObject.active || tabButton.SubMenu.gameObject.active || Constants.qmDashboard.gameObject.active) playerList.SetActive(!playerList.activeSelf);
        }
        public static void LoadAssetBundle()
        {
            // Stolen from UIExpansionKit (https://github.com/knah/VRCMods/blob/master/UIExpansionKit) #Imnotaskidiswear
            MelonLogger.Msg("Loading List UI...");
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlayerList.playerlistmod.assetbundle"))
            {
                using (var memoryStream = new MemoryStream((int)stream.Length))
                {
                    stream.CopyTo(memoryStream);
                    AssetBundle assetBundle = AssetBundle.LoadFromMemory_Internal(memoryStream.ToArray(), 0);
                    assetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    playerList = UnityEngine.Object.Instantiate(assetBundle.LoadAsset_Internal("Assets/Prefabs/PlayerListMod.prefab", Il2CppType.Of<GameObject>()).Cast<GameObject>(), Constants.quickMenu.transform);
                    menuButton = UnityEngine.Object.Instantiate(assetBundle.LoadAsset_Internal("Assets/Prefabs/PlayerListMenuButton.prefab", Il2CppType.Of<GameObject>()).Cast<GameObject>(), Constants.quickMenu.transform);
                }
            }
            menuButton.SetLayerRecursive(12);
            menuButton.transform.localPosition = Converters.ConvertToUnityUnits(new Vector3(4, -1));
            menuButton.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
            menuButton.SetActive(!PlayerListConfig.useTabMenu.Value);

            UiTooltip tooltip = menuButton.AddComponent<UiTooltip>();
            tooltip.field_Public_String_0 = "Open PlayerList menu";
            tooltip.field_Public_String_1 = "Open PlayerList menu";

            playerList.SetLayerRecursive(12);
            playerList.AddComponent<VRC_UiShape>();
            playerListRect = playerList.GetComponent<RectTransform>();
            playerListRect.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            playerListRect.anchoredPosition = PlayerListConfig.playerListPosition.Value;
            playerListRect.localPosition = playerListRect.localPosition.SetZ(25); // Do this or else it looks off for whatever reason
            playerList.SetActive(false);

            shouldStayHidden = !PlayerListConfig.enabledOnStart.Value;

            Constants.playerListLayout = playerList.transform.Find("PlayerList Viewport/PlayerList").GetComponent<VerticalLayoutGroup>();
            Constants.generalInfoLayout = playerList.transform.Find("GeneralInfo Viewport/GeneralInfo").GetComponent<VerticalLayoutGroup>();
        }
        public static void CreateMainSubMenu()
        {
            MelonLogger.Msg("Initializing Menu...");
            var Buttons = new List<IButtonGroupElement>();

            //playerListMenus.Add(new SubMenu("UserInterface/QuickMenu", "PlayerListMenuPage1");

            //Buttons.Add(SingleButtonWr("Back", new Action(() => UiManager.OpenSubMenu("UserInterface/QuickMenu/ShortcutMenu")), "Press to go back", "BackButton");
            Buttons.Add(SingleButtonWr($"Save Settings", new Action(PlayerListConfig.SaveEntries), $"Saves all settings if you have made changes, this is also done automatically when you close the menu", "SaveEntriesButton"));
            menuToggleButton = ToggleButtonWr("Enabled", "Disabled", new Action<bool>((state) => ToggleMenu()), "Toggle the menu. Can also be toggled using Left Ctrl + F1", "Toggle the menu. Can also be toggled using Left Ctrl + F1", "ToggleMenuToggle", !shouldStayHidden);
            Buttons.Add(menuToggleButton);
            Buttons.Add(ToggleButtonWr("Enabled on Start", "Disabled", new Action<bool>((state) => PlayerListConfig.enabledOnStart.Value = state), "Toggle if the list is toggled hidden on start", "Toggle if the list is toggled hidden on start", "EnabledOnStartToggle", PlayerListConfig.enabledOnStart.Value, true));
            Buttons.Add(ToggleButtonWr("Enabled Only in Config", "Disabled", new Action<bool>((state) => PlayerListConfig.onlyEnabledInConfig.Value = state), "Toggle if the list is toggled off outside of this menu", "Toggle if the list is toggled off outside of this menu", "OnlyEnabledInConfigToggle", PlayerListConfig.onlyEnabledInConfig.Value, true));

            //Buttons.Add(ToggleButtonWr("Tab Button", "Regular Button", new Action<bool>((state) => PlayerListConfig.useTabMenu.Value = !PlayerListConfig.useTabMenu.Value), "Toggle if the config menu button should be a regular one or a tab button", "Toggle if the config menu button should be a regular one or a tab button", "TabButtonToggle", PlayerListConfig.useTabMenu.Value, true));
            Buttons.Add(ToggleButtonWr("Condense Text", "Regular Text", new Action<bool>((state) => PlayerListConfig.condensedText.Value = !PlayerListConfig.condensedText.Value), "Toggle if text should be condensed", "Toggle if text should be condensed", "CondensedTextToggle", PlayerListConfig.condensedText.Value, true));
            Buttons.Add(ToggleButtonWr("Numbered List", "Tick List", new Action<bool>((state) => PlayerListConfig.numberedList.Value = !PlayerListConfig.numberedList.Value), "Toggle if the list should be numbered or ticked", "Toggle if the list should be numbered or ticked", "NumberedTickToggle", PlayerListConfig.numberedList.Value, true));
            tabButton.SubMenu.AddButtonGroup(new ButtonGroup("MainButts", " ", Buttons));


            var Buttons2 = new List<IButtonGroupElement>();

            

            /*Buttons2.Add(SingleButtonWr("1", new Action(() => PlayerListConfig.menuButtonPosition.Value = MenuButtonPositionEnum.TopRight), "Move PlayerList menu button to the top right", "1PlayerListMenuButton"));
            Buttons2.Add(SingleButtonWr("2", new Action(() => PlayerListConfig.menuButtonPosition.Value = MenuButtonPositionEnum.TopLeft), "Move PlayerList menu button to the top left", "2PlayerListMenuButton"));
            Buttons2.Add(SingleButtonWr("3", new Action(() => PlayerListConfig.menuButtonPosition.Value = MenuButtonPositionEnum.BottomLeft), "Move PlayerList menu button to the bottom left", "3PlayerListMenuButton"));
            Buttons2.Add(SingleButtonWr("4", new Action(() => PlayerListConfig.menuButtonPosition.Value = MenuButtonPositionEnum.BottomRight), "Move PlayerList menu button to the bottom right", "4PlayerListMenuButton"));*/

            
            Buttons2.Add(SingleButtonWr("Snap Grid\nSize -", new Action(() => PlayerListConfig.snapToGridSize.Value -= 10), "Decrease the size of the snap to grid by 10", "DecreaseSnapGridSize", true));
            ListPositionManager.snapToGridSizeLabel = new Label($"{PlayerListConfig.snapToGridSize.Value}", "Snap Grid Size", "SnapToGridSizeLabel");
            Buttons2.Add(ListPositionManager.snapToGridSizeLabel);
            Buttons2.Add(SingleButtonWr("Snap Grid\nSize +", new Action(() => PlayerListConfig.snapToGridSize.Value += 10), "Increase the size of the snap to grid by 10", "IncreaseSnapGridSize", true));
            Buttons2.Add(SingleButtonWr("Reset Snap\nGrid Size", new Action(() => PlayerListConfig.snapToGridSize.Value = 420), "Set snap to grid to the default value (420)", "DefaultSnapGridSize", true));
            
            

            
            Buttons2.Add(SingleButtonWr("Font\nSize -", new Action(() => PlayerListConfig.fontSize.Value--), "Decrease font size of the list by 1", "DecreaseFontSizeButton", true));
            fontSizeLabel = new Label("", "Font Size", "FontSizeLabel");
            EntryManager.SetFontSize(PlayerListConfig.fontSize.Value);
            Buttons2.Add(fontSizeLabel);
            Buttons2.Add(SingleButtonWr("Font\nSize +", new Action(() => PlayerListConfig.fontSize.Value++), "Increase font size of the list by 1", "IncreaseFontSizeButton", true));
            Buttons2.Add(SingleButtonWr("Reset\nFont", new Action(() => PlayerListConfig.fontSize.Value = 35), "Set font size to the default value (35)", "DefaultFontSizeButton", true));
            
            
            
            Buttons2.Add(SingleButtonWr("Edit PlayerList Position", new Action(ListPositionManager.MovePlayerList), "Click to edit the position of the PlayerList", "EditPlayerListPosButton", true));

            //Buttons2.Add(SingleButtonWr("Move to Right of QuickMenu", new Action(ListPositionManager.MovePlayerListToEndOfMenu), "Move PlayerList to right side of menu, this can also serve as a reset position button", "LockPlayerListToRightButton", true));
            tabButton.SubMenu.AddButtonGroup(new ButtonGroup("SizeButts", "Size & Position", Buttons2));



            var Buttons3 = new List<IButtonGroupElement>();

            //playerListMenus.Add(new SubMenu("UserInterface/QuickMenu", "PlayerListMenuPage3"));
            Buttons3.Add(SingleButtonWr("Base Sort Type", new Action(() => { EntrySortManager.currentComparisonProperty = EntrySortManager.baseComparisonProperty; tabButton.SubMenu.OpenSubMenu(sortMenu); }), "Set base sort which will run when the upper sort and highest sort creates ambiguous entries", "BaseSortTypeButton", true));
            Buttons3.Add(SingleButtonWr("Upper Sort Type", new Action(() => { EntrySortManager.currentComparisonProperty = EntrySortManager.upperComparisonProperty; tabButton.SubMenu.OpenSubMenu(sortMenu); }), "Set upper sort which will run on top of the base sort type and below the highest", "UpperSortTypeButton", true));
            Buttons3.Add(SingleButtonWr("Highest Sort Type", new Action(() => { EntrySortManager.currentComparisonProperty = EntrySortManager.highestComparisonProperty; tabButton.SubMenu.OpenSubMenu(sortMenu); }), "Set highest sort which will run on top of the base and upper sort type", "UpperSortTypeButton", true));
            Buttons3.Add(ToggleButtonWr("Reverse Base", "Disabled", new Action<bool>((state) => PlayerListConfig.reverseBaseSort.Value = state), "Toggle reverse order of base sort", "Toggle reverse order of base sort", "BaseReverseToggle", PlayerListConfig.reverseBaseSort.Value, true));
            Buttons3.Add(ToggleButtonWr("Reverse Upper", "Disabled", new Action<bool>((state) => PlayerListConfig.reverseUpperSort.Value = state), "Toggle reverse order of upper sort", "Toggle reverse order of upper sort", "UpperReverseToggle", PlayerListConfig.reverseUpperSort.Value, true));
            Buttons3.Add(ToggleButtonWr("Reverse Highest", "Disabled", new Action<bool>((state) => PlayerListConfig.reverseHighestSort.Value = state), "Toggle reverse order of upper sort", "Toggle reverse order of upper sort", "HighestReverseToggle", PlayerListConfig.reverseHighestSort.Value, true));
            Buttons3.Add(ToggleButtonWr("Show Self On Top", "Disabled", new Action<bool>((state) => PlayerListConfig.showSelfAtTop.Value = state), "Show the local player on top of the list always", "Show the local player on top of the list always", "ShowSelfOnTopToggle", PlayerListConfig.showSelfAtTop.Value, true));
            Buttons3.Add(ToggleButtonWr("Freeze sort when visible", "Disabled", new Action<bool>((state) => PlayerListConfig.freezeSortWhenVisible.Value = state), "Freeze sorting while the list is visible", "Freeze sorting while the list is visible", "FreezeSortWhenVisibleToggle", PlayerListConfig.freezeSortWhenVisible.Value, true));

            tabButton.SubMenu.AddButtonGroup(new ButtonGroup("SortButts", "Sorting", Buttons3));

            //menuButton.GetComponent<Button>().onClick.AddListener(new Action(() => { playerListMenus[0].OpenSubMenu(); playerList.SetActive(!shouldStayHidden); }));


            //playerListMenus.Add(new SubMenu("UserInterface/QuickMenu", "PlayerListMenuPage4"));

            var Buttons4 = new List<IButtonGroupElement>();

            Buttons4.Add(ToggleButtonWr("Ping", "Disabled", new Action<bool>((state) => PlayerListConfig.pingToggle.Value = state), "Toggle player ping", "Toggle player ping", "PingToggle", PlayerListConfig.pingToggle.Value, true));
            Buttons4.Add(ToggleButtonWr("Fps", "Disabled", new Action<bool>((state) => PlayerListConfig.fpsToggle.Value = state), "Toggle player fps", "Toggle player fps", "FpsToggle", PlayerListConfig.fpsToggle.Value, true));
            Buttons4.Add(ToggleButtonWr("Platform", "Disabled", new Action<bool>((state) => PlayerListConfig.platformToggle.Value = state), "Toggle player Platform", "Toggle player Platform", "PlatformToggle", PlayerListConfig.platformToggle.Value, true));
            Buttons4.Add(ToggleButtonWr("Avatar Performance", "Disabled", new Action<bool>((state) => PlayerListConfig.perfToggle.Value = state), "Toggle avatar performance", "Toggle avatar performance", "PerfToggle", PlayerListConfig.perfToggle.Value, true));
            Buttons4.Add(ToggleButtonWr("Distance", "Disabled", new Action<bool>((state) => PlayerListConfig.distanceToggle.Value = state), "Toggle distance to player", "Toggle distance to player", "DistanceToPlayerToggle", PlayerListConfig.distanceToggle.Value, true));
            Buttons4.Add(ToggleButtonWr("Photon ID", "Disabled", new Action<bool>((state) => PlayerListConfig.photonIdToggle.Value = state), "Toggles the photon ID. Not generally useful", "Toggles the photon ID. Not generally useful", "PhotonIDToggle", PlayerListConfig.photonIdToggle.Value, true));
            Buttons4.Add(ToggleButtonWr("Owned Object Count", "Disabled", new Action<bool>((state) => PlayerListConfig.ownedObjectsToggle.Value = state), "Toggles the how many objects a player owns. Not accurate right after world join and in worlds alone", "Toggles the how many objects a player owns. Not accurate right after world join and in worlds alone", "OwnedObjectsToggle", PlayerListConfig.ownedObjectsToggle.Value, true));
            Buttons4.Add(ToggleButtonWr("DisplayName", "Disabled", new Action<bool>((state) => PlayerListConfig.displayNameToggle.Value = state), "Why...?", "Why...?", "DisplayNameToggle", PlayerListConfig.displayNameToggle.Value, true));
            Buttons4.Add(ToggleButtonWr("Party Check", "Disabled", new Action<bool>((state) => PlayerListConfig.jeffToggle.Value = state), "PARTY!", "PARTY!", "PartyToggle", PlayerListConfig.jeffToggle.Value, true));

            Buttons4.Add(SingleButtonWr("TF", new Action(() => PlayerListConfig.displayNameColorMode.Value = PlayerEntry.DisplayNameColorMode.TrustAndFriends), "Set displayname coloring to show friends and trust rank", "TrustAndFriendsButton"));
            Buttons4.Add(SingleButtonWr("T", new Action(() => PlayerListConfig.displayNameColorMode.Value = PlayerEntry.DisplayNameColorMode.TrustOnly), "Set displayname coloring to show trust rank only", "TrustOnlyButton"));
            Buttons4.Add(SingleButtonWr("F", new Action(() => PlayerListConfig.displayNameColorMode.Value = PlayerEntry.DisplayNameColorMode.FriendsOnly), "Set displayname coloring to show friends only", "FriendsOnlyButton"));
            Buttons4.Add(SingleButtonWr("N", new Action(() => PlayerListConfig.displayNameColorMode.Value = PlayerEntry.DisplayNameColorMode.None), "Set displayname coloring to none", "NoneButton"));
            tabButton.SubMenu.AddButtonGroup(new ButtonGroup("CatButts", "User Info", Buttons4));
        }
        public static void AddMenuListeners()
        {

            UiManager.OnUIPageToggled += new Action<UIPage, bool>((page, state) =>
            {
                //MelonLogger.Msg("Page: " + page.name + " State: " + state.ToString());

                if (page == Constants.qmDashboard)
                {
                    curMenuState.dashboard = state;
                }
                else if (page == tabButton.SubMenu.uiPage)
                {
                    curMenuState.plsettings = state;
                }
                else if (page == sortMenu.uiPage)
                {
                    curMenuState.sort = state;
                }
                else if (page == Constants.selectedUserLocal)
                {
                    curMenuState.userLocal = state;
                }
                else if (page == Constants.selectedUserRemote)
                {
                    curMenuState.userRemote = state;
                }

                if (curMenuState.dashboard || curMenuState.plsettings || curMenuState.sort || curMenuState.userLocal || curMenuState.userRemote)
                {
                    playerList.SetActive(
                        (curMenuState.dashboard && (!shouldStayHidden && !PlayerListConfig.onlyEnabledInConfig.Value)) ||
                        (curMenuState.plsettings && !shouldStayHidden) ||
                        (curMenuState.sort) ||
                        (curMenuState.userLocal && !shouldStayHidden) ||
                        (curMenuState.userRemote && !shouldStayHidden)
                        );
                }
                else
                {
                    playerList.SetActive(false);
                }



            });
            UiManager.OnQuickMenuOpened += new Action(() => {
                EntryManager.CleanUpHungAOI();
                curMenuState.dashboard = true;
                curMenuState.plsettings = false;
                curMenuState.sort = false;
                curMenuState.userLocal = false;
                curMenuState.userRemote = false;
                playerList.SetActive(!shouldStayHidden && !PlayerListConfig.onlyEnabledInConfig.Value);
            });
            UiManager.OnQuickMenuClosed += new Action(PlayerListConfig.SaveEntries);
            
        }

        public static void AttemptMenuHideShow(bool show)
        {
            if (!show)
            {
                if (!sortMenu.gameObject.active && !tabButton.SubMenu.gameObject.active && !(Constants.qmDashboard.gameObject.active && !PlayerListConfig.onlyEnabledInConfig.Value)) playerList.SetActive(false);
            }
            else playerList.SetActive(true);
        }



        public static void CreateSortPages()
        {
            sortMenu = new SubMenu("UserInterface/QuickMenu", "PlayerListSortMenu", "Sort By");

            // Shush its fine
            Sprite BlankSprite = Sprite.Create(new Rect(0, 0, 64, 64), new Vector2(2, 2), 100);
            sortTypeButtonTable.Add(EntrySortManager.SortType.None, new SingleButton(new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.None)), BlankSprite, "None", "NoneSortButton", "Set sort type to none"));
            sortTypeButtonTable.Add(EntrySortManager.SortType.Default, new SingleButton(new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.Default)), BlankSprite, "Default", "DefaultSortButton", "Set sort type to default"));
            sortTypeButtonTable.Add(EntrySortManager.SortType.Alphabetical, new SingleButton(new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.Alphabetical)), BlankSprite, "Alphabetical", "AlphabeticalSortButton", "Set sort type to alphabetical"));
            sortTypeButtonTable.Add(EntrySortManager.SortType.AvatarPerf, SingleButtonWr("Avatar Perf", new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.AvatarPerf)), "Set sort type to avatar perf", "AvatarPerfSortButton"));
            sortTypeButtonTable.Add(EntrySortManager.SortType.Distance, SingleButtonWr("Distance", new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.Distance)), "Set sort type to distance\nWARNING: This may cause noticable frame drops", "DistanceSortButton"));
            sortTypeButtonTable.Add(EntrySortManager.SortType.Friends, SingleButtonWr("Friends", new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.Friends)), "Set sort type to friends", "FriendsSortButton"));
            sortTypeButtonTable.Add(EntrySortManager.SortType.NameColor, SingleButtonWr("Name Color", new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.NameColor)), "Set sort type to displayname color", "NameColorSortButton"));
            sortTypeButtonTable.Add(EntrySortManager.SortType.Ping, SingleButtonWr("Ping", new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.Ping)), "Set sort type to ping\nWARNING: This may cause noticable frame drops", "PingSortButton"));
            sortTypeButtonTable.Add(EntrySortManager.SortType.Jeff, SingleButtonWr("Party Limits", new Action(() => entryWrapperValue.SetValue(EntrySortManager.currentComparisonProperty.GetValue(null), EntrySortManager.SortType.Jeff)), "Set sort type to Party Limits", "JeffSortButton"));

            //new SingleButton(sortMenu.gameObject, new Vector3(5, 2), "Back", new Action(() => playerListMenus[2].OpenSubMenu()), "Press to go back", "BackButton", textColor: Color.yellow);
            sortMenu.AddButtonGroup(new ButtonGroup("SortButts", "", sortTypeButtonTable.Values.ToList<IButtonGroupElement>()));
            //AddPlayerListToSubMenu(sortMenu);

            for (int i = 0; i < sortTypeButtonTable.Count; i++)
            {
                SingleButton button = sortTypeButtonTable.ElementAt(i).Value;
                button.ButtonComponent.onClick.AddListener(new Action(() =>
                {
                    foreach (SingleButton butt in sortTypeButtonTable.Values)
                    {
                            butt.sprite = Constants.blankSprite;
                    }
                    button.sprite = Constants.checkSprite;
                    
                }));
            }

            EnableDisableListener listener = sortMenu.gameObject.AddComponent<EnableDisableListener>();
            listener.OnEnableEvent += new Action(() => 
            {
                EntrySortManager.SortType currentSortType = EntrySortManager.SortType.None;
                if (EntrySortManager.currentComparisonProperty.Name.Contains("Base"))
                {
                    currentSortType = PlayerListConfig.currentBaseSort.Value;
                    sortMenu.Text = "Base Sort Type";
                }
                else if (EntrySortManager.currentComparisonProperty.Name.Contains("Upper"))
                {
                    currentSortType = PlayerListConfig.currentUpperSort.Value;
                    sortMenu.Text = "Upper Sort Type";
                }
                else if (EntrySortManager.currentComparisonProperty.Name.Contains("Highest"))
                { 
                    currentSortType = PlayerListConfig.currentHighestSort.Value;
                    sortMenu.Text = "Highest Sort Type";
                }

                //sortTypeButtonTable[currentSortType].sprite = Constants.checkSprite;
                foreach (EntrySortManager.SortType stype in sortTypeButtonTable.Keys)
                {
                    if (stype == currentSortType)
                        sortTypeButtonTable[stype].sprite = Constants.checkSprite;
                    else
                        sortTypeButtonTable[stype].sprite = Constants.blankSprite;
                }
            });
        }

        public static void CreateSubMenus()
        {
            // Initialize Movement menu
            

            

            // Initialize PlayerList Customization menu
            
        }

        public static void CreateGeneralInfoSubMenus()
        {
            // Create Toggle Button Submenus (done automatically to enable expandability)
            int totalMade = 0;
            List<IButtonGroupElement> Buttons = new List<IButtonGroupElement>();
            for (int i = 0; i < Math.Ceiling(EntryManager.generalInfoEntries.Count / 9f); i++)
            {
                //SubMenu subMenu = new SubMenu("UserInterface/QuickMenu", $"PlayerListMenuPage{i + 5}");
                
                for (; totalMade < (9 * (i + 1)) && totalMade < EntryManager.generalInfoEntries.Count; totalMade++)
                {
                    EntryBase entry = EntryManager.generalInfoEntries.ElementAt(totalMade);
                    Buttons.Add(ToggleButtonWr($"{entry.Name}", $"Disabled", new Action<bool>((state) => { entry.gameObject.SetActive(state); entry.prefEntry.Value = state; }), $"Toggle the {entry.Name} entry", $"Toggle the {entry.Name} entry", $"{entry.Name.Replace(" ", "")}EntryToggle", entry.prefEntry.Value, true));
                }
                
                //playerListMenus.Add(subMenu);
            }
            tabButton.SubMenu.AddButtonGroup(new ButtonGroup("GenButts", "Info Sidebar", Buttons));
        }
        public static void AdjustSubMenus()
        {
            /*for (int i = 0; i < playerListMenus.Count; i++)
            {
                int k = i; // dum reference stuff

                if (i > 0)
                {
                    new SingleButton(playerListMenus[i].gameObject, GameObject.Find("UserInterface/QuickMenu/EmojiMenu/PageUp"), new Vector3(4, 0), $"Page {i}", new Action(() => UiManager.OpenSubMenu($"UserInterface/QuickMenu/PlayerListMenuPage{k}")), $"Go back to page {i}", "BackPageButton");
                    new SingleButton(playerListMenus[i].gameObject, new Vector3(4, 1), $"Save", new Action(PlayerListConfig.SaveEntries), $"Saves all settings if you have made changes, this is also done automatically when you close the menu", "SaveEntriesButton");
                }
                if (i + 1 < playerListMenus.Count)
                    new SingleButton(playerListMenus[i].gameObject, GameObject.Find("UserInterface/QuickMenu/EmojiMenu/PageDown"), new Vector3(4, 2), $"Page {i + 2}", new Action(() => UiManager.OpenSubMenu($"UserInterface/QuickMenu/PlayerListMenuPage{k + 2}")), $"Go to page {i + 2}", "ForwardPageButton");

                if (i == 0) continue; // Skip main config menu

                AddPlayerListToSubMenu(playerListMenus[i]);
            }*/
        }

        /*public static void AddPlayerListToSubMenu(SubMenu menu)
        {
            EnableDisableListener subMenuListener;
            subMenuListener = menu.gameObject.GetComponent<EnableDisableListener>();
            if (subMenuListener == null)
                subMenuListener = menu.gameObject.AddComponent<EnableDisableListener>();
            subMenuListener.OnEnableEvent += new Action(() =>
            {
                playerList.SetActive(!shouldStayHidden);
                playerListRect.anchoredPosition = Converters.ConvertToUnityUnits(new Vector3(6.5f, 3.5f));
            });
            subMenuListener.OnDisableEvent += new Action(() =>
            {
                playerList.SetActive(false);
                playerListRect.anchoredPosition = PlayerListConfig.playerListPosition.Value;
                playerListRect.localPosition = playerListRect.localPosition.SetZ(25);
            });
        }*/
        public enum MenuButtonPositionEnum
        {
            TopRight,
            TopLeft,
            BottomLeft,
            BottomRight
        }
    }
}
