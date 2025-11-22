using Godot;
using System.Collections;
using Steamworks;

public partial class SteamNetworkingTest : Node {
	private Vector2 m_ScrollPos = Vector2.Zero;
	private CSteamID m_RemoteSteamId;

	protected Callback<P2PSessionRequest_t> m_P2PSessionRequest;
	protected Callback<P2PSessionConnectFail_t> m_P2PSessionConnectFail;
	protected Callback<SocketStatusCallback_t> m_SocketStatusCallback;

	private PanelContainer m_MainPanel;
	private VBoxContainer m_MainVBox;
	private ScrollContainer m_ScrollContainer;
	private VBoxContainer m_ScrollContent;
	private Label m_SteamIdLabel;
	private Label m_StatusLabel;

	public override void _EnterTree() {
		// You'd typically get this from a Lobby. Hardcoding it so that we don't need to integrate the whole lobby system with the networking.
		m_RemoteSteamId = new CSteamID(0);

		m_P2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
		m_P2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionConnectFail);
		m_SocketStatusCallback = Callback<SocketStatusCallback_t>.Create(OnSocketStatusCallback);

		InitializeUI();
	}

	public override void _ExitTree() {
		// Just in case we have it open when we close/assemblies get reloaded.
		if (m_RemoteSteamId.IsValid()) {
			SteamNetworking.CloseP2PSessionWithUser(m_RemoteSteamId);
		}
	}

	enum MsgType : uint {
		Ping,
		Ack,
	}

	private void InitializeUI() {
		// Create main panel
		m_MainPanel = new PanelContainer();
		m_MainPanel.AnchorLeft = 0;
		m_MainPanel.AnchorTop = 0;
		m_MainPanel.AnchorRight = 1;
		m_MainPanel.AnchorBottom = 1;
		m_MainPanel.OffsetLeft = 10;
		m_MainPanel.OffsetTop = 10;
		m_MainPanel.OffsetRight = -10;
		m_MainPanel.OffsetBottom = -10;
		AddChild(m_MainPanel);

		// Create main vertical box container
		m_MainVBox = new VBoxContainer();
		m_MainPanel.AddChild(m_MainVBox);

		// Add header label
		var headerLabel = new Label { Text = "Variables:" };
		m_MainVBox.AddChild(headerLabel);

		// Add SteamID label
		m_SteamIdLabel = new Label { Text = "m_RemoteSteamId: " + m_RemoteSteamId };
		m_MainVBox.AddChild(m_SteamIdLabel);

		// Add input container for SteamID
		var inputContainer = new HBoxContainer();
		m_MainVBox.AddChild(inputContainer);

		var inputLabel = new Label { Text = "Enter SteamID:" };
		inputLabel.CustomMinimumSize = new Vector2(100, 0);
		inputContainer.AddChild(inputLabel);

		var steamIdInput = new LineEdit();
		steamIdInput.PlaceholderText = "Enter 64-bit SteamID";
		steamIdInput.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		steamIdInput.TextSubmitted += (text) => {
			if (ulong.TryParse(text, out ulong steamId)) {
				m_RemoteSteamId = new CSteamID(steamId);
				m_SteamIdLabel.Text = "m_RemoteSteamId: " + m_RemoteSteamId;
				UpdateUIButtons();
				GD.Print("SteamID set to: " + steamId);
			}
			else {
				GD.PrintErr("Invalid SteamID format. Please enter a valid 64-bit number.");
			}
		};
		inputContainer.AddChild(steamIdInput);

		// Add separator
		m_MainVBox.AddChild(new HSeparator());

		// Create scroll container
		m_ScrollContainer = new ScrollContainer();
		m_ScrollContainer.CustomMinimumSize = new Vector2(GetViewport().GetVisibleRect().Size.X - 215, GetViewport().GetVisibleRect().Size.Y - 100);
		m_MainVBox.AddChild(m_ScrollContainer);

		// Create content container for scroll
		m_ScrollContent = new VBoxContainer();
		m_ScrollContent.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		m_ScrollContainer.AddChild(m_ScrollContent);

		// Add initial status label
		m_StatusLabel = new Label { Text = "Please fill m_RemoteSteamId with a valid 64bit SteamId to use SteamNetworkingTest.\nAlternatively it will be filled automatically when a session request is received." };
		m_StatusLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		m_ScrollContent.AddChild(m_StatusLabel);

		// Add buttons container
		UpdateUIButtons();
	}

	private void UpdateUIButtons() {
		// Clear existing buttons
		foreach (Node child in m_ScrollContent.GetChildren()) {
			if (child != m_StatusLabel) {
				child.QueueFree();
			}
		}

		if (!m_RemoteSteamId.IsValid()) {
			m_StatusLabel.Visible = true;
			return;
		}

		m_StatusLabel.Visible = false;

		// Session-less connection functions
		var sendButton = new Button { Text = "SendP2PPacket(m_RemoteSteamId, bytes, (uint)bytes.Length, EP2PSend.k_EP2PSendReliable)" };
		sendButton.Pressed += OnSendP2PPacket;
		m_ScrollContent.AddChild(sendButton);

		// IsP2PPacketAvailable and ReadP2PPacket
		var readButton = new Button { Text = "ReadP2PPacket(bytes, MsgSize, out newMsgSize, out SteamIdRemote)" };
		readButton.Pressed += OnReadP2PPacket;
		m_ScrollContent.AddChild(readButton);

		var closeSessionButton = new Button { Text = "CloseP2PSessionWithUser(m_RemoteSteamId)" };
		closeSessionButton.Pressed += OnCloseP2PSession;
		m_ScrollContent.AddChild(closeSessionButton);

		var closeChannelButton = new Button { Text = "CloseP2PChannelWithUser(m_RemoteSteamId, 0)" };
		closeChannelButton.Pressed += OnCloseP2PChannel;
		m_ScrollContent.AddChild(closeChannelButton);

		var getStateButton = new Button { Text = "GetP2PSessionState(m_RemoteSteamId, out ConnectionState)" };
		getStateButton.Pressed += OnGetP2PSessionState;
		m_ScrollContent.AddChild(getStateButton);

		var allowRelayButton = new Button { Text = "AllowP2PPacketRelay(true)" };
		allowRelayButton.Pressed += OnAllowP2PPacketRelay;
		m_ScrollContent.AddChild(allowRelayButton);
	}

	private void OnSendP2PPacket() {
		byte[] bytes = new byte[4];
		using (System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes))
		using (System.IO.BinaryWriter b = new System.IO.BinaryWriter(ms)) {
			b.Write((uint)MsgType.Ping);
		}
		bool ret = SteamNetworking.SendP2PPacket(m_RemoteSteamId, bytes, (uint)bytes.Length, EP2PSend.k_EP2PSendReliable);
		GD.Print("SteamNetworking.SendP2PPacket(" + m_RemoteSteamId + ", " + bytes + ", " + (uint)bytes.Length + ", " + EP2PSend.k_EP2PSendReliable + ") : " + ret);
	}

	private void OnReadP2PPacket() {
		uint MsgSize;
		bool ret = SteamNetworking.IsP2PPacketAvailable(out MsgSize);
		GD.Print("IsP2PPacketAvailable(out MsgSize) : " + ret + " -- " + MsgSize);

		if (ret) {
			byte[] bytes = new byte[MsgSize];
			uint newMsgSize;
			CSteamID SteamIdRemote;
			ret = SteamNetworking.ReadP2PPacket(bytes, MsgSize, out newMsgSize, out SteamIdRemote);

			using (System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes))
			using (System.IO.BinaryReader b = new System.IO.BinaryReader(ms)) {
				MsgType msgtype = (MsgType)b.ReadUInt32();
				// switch statement here depending on the msgtype
				GD.Print("SteamNetworking.ReadP2PPacket(bytes, " + MsgSize + ", out newMsgSize, out SteamIdRemote) - " + ret + " -- " + newMsgSize + " -- " + SteamIdRemote + " -- " + msgtype);
			}
		}
	}

	private void OnCloseP2PSession() {
		bool ret = SteamNetworking.CloseP2PSessionWithUser(m_RemoteSteamId);
		GD.Print("SteamNetworking.CloseP2PSessionWithUser(" + m_RemoteSteamId + ") : " + ret);
	}

	private void OnCloseP2PChannel() {
		bool ret = SteamNetworking.CloseP2PChannelWithUser(m_RemoteSteamId, 0);
		GD.Print("SteamNetworking.CloseP2PChannelWithUser(" + m_RemoteSteamId + ", " + 0 + ") : " + ret);
	}

	private void OnGetP2PSessionState() {
		P2PSessionState_t ConnectionState;
		bool ret = SteamNetworking.GetP2PSessionState(m_RemoteSteamId, out ConnectionState);
		GD.Print("GetP2PSessionState(m_RemoteSteamId, out ConnectionState) : " + ret + " -- " + ConnectionState);
	}

	private void OnAllowP2PPacketRelay() {
		bool ret = SteamNetworking.AllowP2PPacketRelay(true);
		GD.Print("SteamNetworking.AllowP2PPacketRelay(" + true + ") : " + ret);
	}

	void OnP2PSessionRequest(P2PSessionRequest_t pCallback) {
		GD.Print("[" + P2PSessionRequest_t.k_iCallback + " - P2PSessionRequest] - " + pCallback.m_steamIDRemote);

		bool ret = SteamNetworking.AcceptP2PSessionWithUser(pCallback.m_steamIDRemote);
		GD.Print("SteamNetworking.AcceptP2PSessionWithUser(" + pCallback.m_steamIDRemote + ") - " + ret);

		m_RemoteSteamId = pCallback.m_steamIDRemote;
		m_SteamIdLabel.Text = "m_RemoteSteamId: " + m_RemoteSteamId;
		UpdateUIButtons();
	}

	void OnP2PSessionConnectFail(P2PSessionConnectFail_t pCallback) {
		GD.Print("[" + P2PSessionConnectFail_t.k_iCallback + " - P2PSessionConnectFail] - " + pCallback.m_steamIDRemote + " -- " + pCallback.m_eP2PSessionError);
	}

	void OnSocketStatusCallback(SocketStatusCallback_t pCallback) {
		GD.Print("[" + SocketStatusCallback_t.k_iCallback + " - SocketStatusCallback] - " + pCallback.m_hSocket + " -- " + pCallback.m_hListenSocket + " -- " + pCallback.m_steamIDRemote + " -- " + pCallback.m_eSNetSocketState);
	}
}
