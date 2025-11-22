using Godot;
using System.Collections;
using Steamworks;

public partial class SteamTest : Node {
    public enum EGUIState {
        SteamApps,
        SteamClient,
        SteamFriends,
        SteamHTMLSurface,
        SteamHTTP,
        SteamInput,
        SteamInventory,
        SteamMatchmaking,
        SteamMatchmakingServers,
        SteamMusic,
        SteamMusicRemote,
        SteamNetworking,
        SteamParentalSettings,
        SteamParties,
        SteamRemoteStorage,
        SteamScreenshots,
        SteamTimeline,
        SteamUGC,
        SteamUser,
        SteamUserStatsTest,
        SteamUtils,
        SteamVideo,

        MAX_STATES
    }

    public EGUIState CurrentState { get; private set; }

    private bool m_bInitialized = false;

    private static SteamTest m_SteamTest = null;

    // private SteamAppsTest AppsTest;
    // private SteamClientTest ClientTest;
    // private SteamFriendsTest FriendsTest;
    // private SteamHTMLSurfaceTest HTMLSurfaceTest;
    // private SteamHTTPTest HTTPTest;
    // private SteamInputTest InputTest;
    // private SteamInventoryTest InventoryTest;
    // private SteamMatchmakingServersTest MatchmakingServersTest;
    // private SteamMatchmakingTest MatchmakingTest;
    // private SteamMusicRemoteTest MusicRemoteTest;
    // private SteamMusicTest MusicTest;
    private SteamNetworkingTest NetworkingTest;
    // private SteamParentalSettingsTest ParentalSettingsTest;
    // private SteamPartiesTest PartiesTest;
    // private SteamRemoteStorageTest RemoteStorageTest;
    // private SteamScreenshotsTest ScreenshotsTest;
    // private SteamTimelineTest TimelineTest;
    // private SteamUGCTest UGCTest;
    // private SteamUserStatsTest UserStatsTest;
    // private SteamUserTest UserTest;
    // private SteamUtilsTest UtilsTest;
    // private SteamVideoTest VideoTest;

    SteamAPIWarningMessageHook_t SteamAPIWarningMessageHook;

    static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText) {
        GD.PrintErr(pchDebugText.ToString());
    }

    public override void _EnterTree() {
        // Only one instance of Steamworks at a time!
        if (m_SteamTest != null) {
            QueueFree();
            return;
        }
        m_SteamTest = this;

        // We want our Steam Instance to persist across scenes.
        if (IsNodeReady()) {
            GetTree().Root.AddChild(this);
            GetTree().Root.MoveChild(this, 0);
        }

        if (!Packsize.Test()) {
            throw new System.Exception("Packsize is wrong! You are likely using a Linux/OSX build on Windows or vice versa.");
        }

        if (!DllCheck.Test()) {
            throw new System.Exception("DllCheck returned false.");
        }

        try {
            m_bInitialized = SteamAPI.Init();
        }
        catch (System.DllNotFoundException e) { // We catch this exception here, as it will be the first occurrence of it.
            GD.PrintErr("[Steamworks] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e);

            GetTree().Quit();
            return;
        }

        if (!m_bInitialized) {
            GD.PrintErr("SteamAPI_Init() failed");
            return;
        }

        // Set up our callback to receive warning messages from Steam.
        // You must launch with "-debug_steamapi" in the launch args to receive warnings.
        SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
        SteamClient.SetWarningMessageHook(SteamAPIWarningMessageHook);

        // Register our Steam Callbacks
        // AppsTest = new SteamAppsTest();
        // AddChild(AppsTest);
        // ClientTest = new SteamClientTest();
        // AddChild(ClientTest);
        // FriendsTest = new SteamFriendsTest();
        // AddChild(FriendsTest);
        // HTMLSurfaceTest = new SteamHTMLSurfaceTest();
        // AddChild(HTMLSurfaceTest);
        // HTTPTest = new SteamHTTPTest();
        // AddChild(HTTPTest);
        // InputTest = new SteamInputTest();
        // AddChild(InputTest);
        // InventoryTest = new SteamInventoryTest();
        // AddChild(InventoryTest);
        // MatchmakingServersTest = new SteamMatchmakingServersTest();
        // AddChild(MatchmakingServersTest);
        // MatchmakingTest = new SteamMatchmakingTest();
        // AddChild(MatchmakingTest);
        // MusicRemoteTest = new SteamMusicRemoteTest();
        // AddChild(MusicRemoteTest);
        // MusicTest = new SteamMusicTest();
        // AddChild(MusicTest);
        NetworkingTest = new SteamNetworkingTest();
        AddChild(NetworkingTest);
        //     ParentalSettingsTest = new SteamParentalSettingsTest();
        //     AddChild(ParentalSettingsTest);
        //     PartiesTest = new SteamPartiesTest();
        //     AddChild(PartiesTest);
        //     TimelineTest = new SteamTimelineTest();
        //     AddChild(TimelineTest);
        //     RemoteStorageTest = new SteamRemoteStorageTest();
        //     AddChild(RemoteStorageTest);
        //     UGCTest = new SteamUGCTest();
        //     AddChild(UGCTest);
        //     UserStatsTest = new SteamUserStatsTest();
        //     AddChild(UserStatsTest);
        //     UserTest = new SteamUserTest();
        //     AddChild(UserTest);
        //     UtilsTest = new SteamUtilsTest();
        //     AddChild(UtilsTest);
        //     VideoTest = new SteamVideoTest();
        //     AddChild(VideoTest);
        //     ScreenshotsTest = new SteamScreenshotsTest();
        //     AddChild(ScreenshotsTest);
    }

    public override void _ExitTree() {
        if (!m_bInitialized) {
            return;
        }

        SteamAPI.Shutdown();
    }

    public override void _Process(double delta) {
        if (!m_bInitialized) {
            return;
        }

        SteamAPI.RunCallbacks();

        if (Input.IsActionJustPressed("ui_cancel")) {
            GetTree().Quit();
        }
        else if (Input.IsActionJustPressed("ui_accept") || Input.IsActionJustPressed("ui_right")) {
            ++CurrentState;

            if (CurrentState == EGUIState.MAX_STATES) {
                CurrentState = (EGUIState)0;
            }
        }
        else if (Input.IsActionJustPressed("ui_left")) {
            --CurrentState;

            if (CurrentState == (EGUIState)(-1)) {
                CurrentState = EGUIState.MAX_STATES - 1;
            }
        }
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
            if (keyEvent.Keycode == Key.Escape) {
                GetTree().Quit();
                GetTree().Root.SetInputAsHandled();
            }
        }
    }

    public override void _Ready() {
        // Set up UI for displaying debug info
        if (!m_bInitialized) {
            GD.Print("Steamworks is not Initialized");
            return;
        }
    }

    // public void RenderOnGUI() {
    //     if (!m_bInitialized) {
    //         GD.Print("Steamworks is not Initialized");
    //         return;
    //     }

    //     GD.Print("[" + ((int)CurrentState + 1) + " / " + (int)EGUIState.MAX_STATES + "] " + CurrentState.ToString());

    //     switch (CurrentState) {
    //         case EGUIState.SteamApps:
    //             AppsTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamClient:
    //             ClientTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamFriends:
    //             FriendsTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamHTMLSurface:
    //             HTMLSurfaceTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamHTTP:
    //             HTTPTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamInput:
    //             InputTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamInventory:
    //             InventoryTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamMatchmaking:
    //             MatchmakingTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamMatchmakingServers:
    //             MatchmakingServersTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamMusic:
    //             MusicTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamMusicRemote:
    //             MusicRemoteTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamNetworking:
    //             NetworkingTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamParentalSettings:
    //             ParentalSettingsTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamParties:
    //             PartiesTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamRemoteStorage:
    //             RemoteStorageTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamScreenshots:
    //             ScreenshotsTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamTimeline:
    //             TimelineTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamUGC:
    //             UGCTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamUser:
    //             UserTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamUserStatsTest:
    //             UserStatsTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamUtils:
    //             UtilsTest?.RenderOnGUI();
    //             break;
    //         case EGUIState.SteamVideo:
    //             VideoTest?.RenderOnGUI();
    //             break;
    //     }
    // }

    public static void PrintArray(string name, IList arr) {
        System.Text.StringBuilder strBuilder = new System.Text.StringBuilder(name + '\n');

        for (int i = 0; i < arr.Count; ++i) {
            strBuilder.AppendLine(arr[i].ToString());
        }

        GD.Print(strBuilder.ToString());
    }
}
