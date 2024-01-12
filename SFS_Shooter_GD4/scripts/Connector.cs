using Godot;

using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Logging;
using Sfs2X.Requests;
using Sfs2X.Util;

using System.ComponentModel;

/**
 * Script attached to the Controller object in the scene.
 */
public partial class Connector : Control
{
    //----------------------------------------------------------
    // Editor public properties
    //----------------------------------------------------------

    [ExportCategory("SFS2X Connection Settings")]

    [Export, Description("IP address or domain name of the SmartFoxServer instance; if encryption is enabled, a domain name must be entered")]
    public string host = "127.0.0.1";
    [Export, Description("TCP listening port of the SmartFoxServer instance, used for TCP socket connection in all builds except HTM L5")]
    public int tcpPort = 9933;
    [Export, Description("UDP listening port of the SmartFoxServer instance, used for UDP communication")]
    public int UdpPort = 9933;
    [Export, Description("HTTP listening port of the SmartFoxServer instance, used for WebSocket (WS) connections in HTML5 build")]
    public int httpPort = 8080;
    [Export, Description("Name of the SmartFoxServer Zone to join")]
    public string zone = "BasicExamples";
    [Export, Description("Display SmartFoxServer client debug messages")]
    public bool debug = false;
    [Export, Description("Client-side SmartFoxServer logging level")]
    public LogLevel logLevel = LogLevel.INFO;

    //----------------------------------------------------------
    // UI elements
    //----------------------------------------------------------

    [ExportCategory("UI Settings")]

    [Export]
    public Control loginPanel;
    [Export]
    public LineEdit errorText;
    [Export]
    public LineEdit nameInput;

    [ExportCategory("Next Scene")]
    [Export]
    public string SceneName;

    private SmartFox sfs;
    private GlobalManager global;

    //----------------------------------------------------------
    // Callback Methods
    //----------------------------------------------------------
    #region
    public override void _Ready()
    {
        global = (GlobalManager)GetNode("/root/globalmanager");
    }
    public override void _Process(double delta)
    {
        // Process the SmartFox events queue
        if (sfs != null)
            sfs.ProcessEvents();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (sfs != null && sfs.IsConnected)
                sfs.Disconnect();

            GD.Print("Application Quit");
        }
    }
    #endregion

    //----------------------------------------------------------
    // UI event listeners
    //----------------------------------------------------------
    #region
    /**
	 * On Login button click, connect to SmartFoxServer.
	 */
    public void OnLoginButtonClick()
    {
        Connect();
    }

    #endregion

    //----------------------------------------------------------
    // Helper methods
    //----------------------------------------------------------
    #region
    private void Connect()
    {
        GD.Print("Attempting connection to SFS2X...");

        // Clear any previour error message
        errorText.Text = "";


        // Set connection parameters
        ConfigData cfg = new ConfigData();
        cfg.Host = host;
        cfg.Port = tcpPort;
        cfg.UdpHost = host;
        cfg.UdpPort = UdpPort;
        cfg.Zone = zone;
        cfg.Debug = debug;

  
        // Initialize SmartFox client
  
         sfs = global.CreateSfsClient();
    

        // Configure internal SFS2X logger
        sfs.Logger.EnableConsoleTrace = debug;
        sfs.Logger.LoggingLevel = logLevel;

        // Add event listeners
        AddSmartFoxListeners();


        // Connect to SmartFoxServer

        sfs.Connect(cfg);
    }

    private void AddSmartFoxListeners()
    {
        // Add event listeners
        sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
        sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
        sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
        sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
        sfs.AddEventListener(SFSEvent.UDP_INIT, OnUdpInit);
    }

    private void RemoveSmartFoxListeners()
    {
        // NOTE
        // If this scene is stopped before a connection is established, the SmartFox client instance
        // could still be null, causing an error when trying to remove its listeners

        if (sfs != null)
        {
            sfs.RemoveEventListener(SFSEvent.CONNECTION, OnConnection);
            sfs.RemoveEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
            sfs.RemoveEventListener(SFSEvent.LOGIN, OnLogin);
            sfs.RemoveEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
            sfs.RemoveEventListener(SFSEvent.UDP_INIT, OnUdpInit);

        }
    }

    private void Login()
    {
        GD.Print("Performing login...");

        // Login
        sfs.Send(new LoginRequest(nameInput.Text));
    }
    #endregion

    //----------------------------------------------------------
    // SmartFoxServer event listeners
    //----------------------------------------------------------
    #region
    private void OnConnection(BaseEvent evt)
    {
        // Check if the conenction was established or not
        if ((bool)evt.Params["success"])
        {
            GD.Print("Connection established successfully");
            GD.Print("SFS2X API version: " + sfs.Version);
            GD.Print("Connection mode is: " + sfs.ConnectionMode);

 
            Login();
       

        }
        else
        {
            // Show error message
            errorText.Text = "Connection failed; is the server running at all?";
         
        }
    }

    private void OnConnectionLost(BaseEvent evt)
    {
        // Remove SFS listeners
        RemoveSmartFoxListeners();

        // Show error message
        string reason = (string)evt.Params["reason"];

        if (reason != ClientDisconnectionReason.MANUAL)
            errorText.Text = "Connection lost; reason is: " + reason;

    }

 

    private void OnLogin(BaseEvent evt)
    {

        // Initialize UDP communication
        sfs.InitUDP();

    }


    private void OnLoginError(BaseEvent evt)
    {
        GD.Print("Login failed");

        // Disconnect
        // NOTE: this causes a CONNECTION_LOST event with reason "manual", which in turn removes all SFS listeners
        sfs.Disconnect();

        // Show error message
        errorText.Text = "Login failed due to the following error:\n" + (string)evt.Params["errorMessage"];
    }
    private void OnUdpInit(BaseEvent evt)
    {
        if ((bool)evt.Params["success"])
        {
            GD.Print("Login successful");

            // Update view
            RemoveSmartFoxListeners();
            GetTree().ChangeSceneToFile("scenes/" + SceneName + ".tscn");
        }
        else
        {
            // Disconnect
            // NOTE: this causes a CONNECTION_LOST event with reason "manual", which in turn removes all SFS listeners
            sfs.Disconnect();

            // Show error message
            errorText.Text = "UDP initialization failed due to the following error:\n" + (string)evt.Params["errorMessage"];

        }
    }

    #endregion
}