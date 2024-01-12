using Godot;

using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Requests;
using Sfs2X.Entities;
using System.Collections.Generic;

using System.Linq;

/**
 * Script attached to the Controller object in the scene.
 */
public partial class LobbyManager : Control
{
  
    private static string GAME_ROOMS_GROUP_NAME = "games";
    private const string EXTENSION_ID = "Shooter";
    private const string EXTENSION_CLASS = "sfs2x.extensions.games.shooter.ShooterExtension";

    //----------------------------------------------------------
    // UI elements
    //----------------------------------------------------------

    [ExportCategory("UI Settings")]

    [Export]
    public Control loginPanel;
    [Export]
    public LineEdit loggedInAsLabel;
    [Export]
    public LineEdit errorText;
    [Export]
    public Control userProfilePopup;
    [Export]
    public LineEdit userProfileText;
    [Export]
    public Control warningPopup;
    [Export]
    public TextEdit warningText;
    [Export]
    public ScrollContainer scrollContainer;

    [Export]
    public PackedScene gameListScene;

    private GameListItem gameListItem;

    private SmartFox sfs;
    private GlobalManager global;

    private Dictionary<int, GameListItem> gameListItems;
    private Dictionary<string, Node> itemInstances = new Dictionary<string, Node>();


    //----------------------------------------------------------
    // Callback Methods
    //----------------------------------------------------------
    #region
    public override void _Ready()
    {

        global = (GlobalManager)GetNode("/root/globalmanager");
        sfs = global.GetSfsClient();
   
        loggedInAsLabel.Text = "Logged in as " + sfs.MySelf.Name;

           // Add event listeners
        AddSmartFoxListeners();

        // Populate list of available games
        PopulateGamesList();

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
	 * On Logout button click, disconnect from SmartFoxServer.
	 */
    public void OnLogoutButtonClick()
    {
        sfs.Disconnect(); 
        
    }
    /**
	 * On Start game button click, create and join a new game Room.
	 */
    public void OnStartGameButtonClick()
    {
        // Configure Room
        RoomSettings settings = new RoomSettings(sfs.MySelf.Name + "'s game");
        settings.GroupId = GAME_ROOMS_GROUP_NAME;
        settings.IsGame = true;
        settings.MaxUsers = 4;
        settings.MaxSpectators = 10;
        settings.Extension = new RoomExtension(EXTENSION_ID, EXTENSION_CLASS);

        // Request Room creation to server
        sfs.Send(new CreateRoomRequest(settings, true));
    }

    /**
	 * On Play game button click in Game List Item prefab instance, join an existing game Room as a player.
	 */
    public void OnGameItemPlayClick(int roomId)
    {
        // Join game Room as player
        sfs.Send(new Sfs2X.Requests.JoinRoomRequest(roomId));

    }

    /**
	 * On Watch game button click in Game List Item prefab instance, join an existing game Room as a spectator.
	 */
    public void OnGameItemWatchClick(int roomId)
    {
        // Join game Room as spectator
        sfs.Send(new Sfs2X.Requests.JoinRoomRequest(roomId, null, null, true));
    }

    /**
	 * On User icon click, show User Profile Panel prefab instance.
	 */
    public void OnUserIconClick()
    {
        userProfilePopup.Position = (Vector2I)((GetViewportRect().Size - userProfilePopup.Size) / 2);
        userProfileText.Text = "Username: " + sfs.MySelf.Name;
        GetTree().Paused = true;
        GetNode<Control>("User Profile Panel").Show();
 
    }
    public void OnUserIconCloseClick()
    {
        GetNode<Control>("User Profile Panel").Hide();
        GetTree().Paused = false;
    }

    public void OnWarningPanelShow()
    {
        warningPopup.Position = (Vector2I)((GetViewportRect().Size - warningPopup.Size) / 2);
        GetTree().Paused = true;
        GetNode<Control>("Warning Panel").Show();
    }
    public void OnWarningPanelHide()
    {
        GetNode<Control>("Warning Panel").Hide();
        GetTree().Paused = false;
    }
    #endregion

    //----------------------------------------------------------
    // Helper methods
    //----------------------------------------------------------
    #region
    private void AddSmartFoxListeners()
    {
   
            sfs.AddEventListener(SFSEvent.ROOM_CREATION_ERROR, OnRoomCreationError);
            sfs.AddEventListener(SFSEvent.ROOM_ADD, OnRoomAdded);
            sfs.AddEventListener(SFSEvent.ROOM_REMOVE, OnRoomRemoved);
            sfs.AddEventListener(SFSEvent.USER_COUNT_CHANGE, OnUserCountChanged);
            sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
            sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
         
    }

    /**
	 * Remove all SmartFoxServer-related event listeners added by the scene.
	 */
    private void RemoveSmartFoxListeners()
    {
        sfs.RemoveEventListener(SFSEvent.ROOM_CREATION_ERROR, OnRoomCreationError);
        sfs.RemoveEventListener(SFSEvent.ROOM_ADD, OnRoomAdded);
        sfs.RemoveEventListener(SFSEvent.ROOM_REMOVE, OnRoomRemoved);
        sfs.RemoveEventListener(SFSEvent.USER_COUNT_CHANGE, OnUserCountChanged);
        sfs.RemoveEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
        sfs.RemoveEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
     
    }

    private void PopulateGamesList()
    {
       // Initialize list
        if (gameListItems == null)
            gameListItems = new Dictionary<int, GameListItem>();

        // For the game list we use a scrollable area containing a separate prefab for each Game Room
        // The prefab contains clickable buttons to join the game
        List<Room> rooms = sfs.RoomManager.GetRoomList();

        // Display game list items
        foreach (Room room in rooms)
            AddGameListItem(room);
     
    }

    /**
 * Create Game List Item prefab instance and add to games list.
 */
    private void AddGameListItem(Room room)
    {
        // Show only game rooms
        // Also password protected Rooms are skipped, to make this example simpler
        // (protection would require an interface element to input the password)
        if (!room.IsGame || room.IsHidden || room.IsPasswordProtected)
            return;

        var instance = gameListScene.Instantiate<Control>();
        GameListItem gameListItem = instance.GetNode<GameListItem>("GameListItem");

        // Init game list item
        gameListItem.Init(room);
        gameListItems.Add(room.Id, gameListItem);


        // Connect method to play and watch buttons
        gameListItem.playButton.Pressed += () => { OnGameItemPlayClick(room.Id); };
 
        var vboxContainer = GetNode<VBoxContainer>("BackGround/Login Panel/ScrollContainer/VBoxContainer");
          vboxContainer.AddChild(instance);
          itemInstances.Add(room.Id.ToString(), instance);
    }

   
    #endregion

    //----------------------------------------------------------
    // SmartFoxServer event listeners
    //----------------------------------------------------------
    #region
    private void OnRoomCreationError(BaseEvent evt)
    {
        // Show Warning Panel prefab instance
        warningText.Text = ("Room creation failed: " + (string)evt.Params["errorMessage"]);
        OnWarningPanelShow();
    }

    private void OnRoomAdded(BaseEvent evt)
    {
        Room room = (Room)evt.Params["room"];

        // Display game list item
        AddGameListItem(room);
    }

    public void OnRoomRemoved(BaseEvent evt)
    {
        Room room = (Room)evt.Params["room"];

        // Get reference to game list item corresponding to Room
        gameListItems.TryGetValue(room.Id, out GameListItem gameListItem);
       int index = gameListItems.Keys.ToList().IndexOf(room.Id);
   
        // Remove game list item
        if (gameListItem != null)
        {

            var vboxContainer = GetNode<VBoxContainer>("BackGround/Login Panel/ScrollContainer/VBoxContainer");
            vboxContainer.RemoveChild(itemInstances[room.Id.ToString()]);
            itemInstances.Remove(room.Id.ToString());

            // Remove game list item from dictionary
            gameListItems.Remove(room.Id);
            gameListItem.QueueFree();


        }

    }

    public void OnUserCountChanged(BaseEvent evt)
    {
        Room room = (Room)evt.Params["room"];

       // Get reference to game list item corresponding to Room
        gameListItems.TryGetValue(room.Id, out GameListItem gameListItem);

        // Update game list item
       if (gameListItem != null)
           gameListItem.SetState(room);
     
    }

    private void OnRoomJoin(BaseEvent evt)
    {
        // Load game scene
        RemoveSmartFoxListeners();
        GetTree().ChangeSceneToFile("scenes/game.tscn");
    }

    private void OnRoomJoinError(BaseEvent evt)
    {
        // Show Warning Panel prefab instance
        warningText.Text = ("Room join failed: " + (string)evt.Params["errorMessage"]);
        OnWarningPanelShow();
    }


    #endregion
}