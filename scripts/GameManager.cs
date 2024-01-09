using Godot;
using System;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using System.Threading.Tasks;
using Godot.Collections;


/**
 * Script attached to the Game Manager scene.
 * It controls all things SmartFox
 * and the local player UI.
 */
public partial class GameManager : Control
{
    [ExportCategory("SmartFoxServer Shooter - GameManager")]

    [Export] public NetworkSync networkSync;
    public enum NetworkSync
    {
        Simple,
        Complex
    }
 
    [ExportCategory("-- Player Models --")]
    [Export] public PackedScene[] PlayerScene;
    [Export] public Godot.Color[] colorarray;

    [ExportCategory("-- Prefab Items --")]
    [Export] public PackedScene HealthBox;
    [Export] public PackedScene AmmoBox;


    [ExportCategory("-- Player Interface --")]
    [Export] public TextureRect[] healthStars;
    [Export] public TextureRect[] loadedBullets;
    [Export] public TextureRect[] unloadedBullets;
    [Export] public TextureRect[] kills;
    [Export] public TextureRect[] crosshairs;
    [Export] public Label playerInfo;
    [Export] public Control statsPanel;

    private int playerHealth;

    private SmartFox sfs;
    private GlobalManager global;

    public int clientServerLag;
    public double lastServerTime = 0;
    public double lastLocalTime = 0;
    private double timeAtFrameStart;

    public CharacterTransform lastState;


    private Dictionary<int, string> recipients = new Dictionary<int, string>();
    private Dictionary<int, string> items = new Dictionary<int, string>();

    private Node3D remotePlayerNode = new Node3D();
    public Camera3D playerCamera;
    public int waitToRespawn = 4;
    public bool playerRespawn;
 
    private stats_panel stats;

    //----------------------------------------------------------
    // Callback Methods
    //----------------------------------------------------------
    #region
    public override void _Ready()
    {

        global = (GlobalManager)GetNode("/root/globalmanager");
        sfs = global.GetSfsClient();
  
        // Add event listeners
        AddSmartFoxListeners();

        // Spawn Player Character
        SendSpawnRequest();

        sfs.EnableLagMonitor(true, 1, 10);
        stats = new stats_panel();

        

    }


    public override void _Process(double delta)
    {
        // Process the SmartFox events queue
        if (sfs != null)
            sfs.ProcessEvents();

        timeAtFrameStart = Time.GetTicksMsec(); 
    }


    #endregion

    /**
      * ------------------------------------------------------
      * SmartFoxServer event listeners
      * ------------------------------------------------------
      * 
      * See the Smartfox Documentation to understand each method
      * 
      */
    #region
    private void AddSmartFoxListeners()
    {
        sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
        sfs.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserLeaveRoom);
        sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserJoinRoom);
        sfs.AddEventListener(SFSEvent.PING_PONG, OnPingPong);
    }

    /**
	 * Remove all SmartFoxServer-related event listeners added by the scene.
	 */
    private void RemoveSmartFoxListeners()
    {
        sfs.RemoveEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
        sfs.RemoveEventListener(SFSEvent.USER_EXIT_ROOM, OnUserLeaveRoom);
        sfs.RemoveEventListener(SFSEvent.USER_ENTER_ROOM, OnUserJoinRoom);
        sfs.RemoveEventListener(SFSEvent.PING_PONG, OnPingPong);
    }

    public void OnPingPong(BaseEvent evt)
    {
        clientServerLag = (int)evt.Params["lagValue"] / 2;
    }
    private void OnUserJoinRoom(BaseEvent evt)
    {
        User user = (User)evt.Params["user"];
        Room room = (Room)evt.Params["room"];
        string info = ("              " + user.Name + " has joined the game\n  ");
        if (playerInfo != null)
            playerInfo.Text = info;
        ClearText();
    }

    private void OnUserLeaveRoom(BaseEvent evt)
    {
        User user = (User)evt.Params["user"];
        Room room = (Room)evt.Params["room"];
        string info = ("              " + user.Name + " has left the game\n  ");
        if (playerInfo != null)
            playerInfo.Text = info;
        DestroyPlayer(user.Id);
        if (user.Id != sfs.MySelf.Id)
            ClearText();
    }

    public async void ClearText()
    {
        await Task.Delay(3000);
        string info = ("             ");
        if (playerInfo != null )
            playerInfo.Text = info;
    }

    public async void AddChild()
    {
        await Task.Delay(1000);
        string info = ("             ");
        if (playerInfo != null)
            playerInfo.Text = info;
    }
    /**
    * On Leave button click, go back to Login scene.
    */
    public void OnExitGame()
    {
        sfs.EnableLagMonitor(false, 1, 10);

        stats.exitStats();

        // Leave current game room
        sfs.Send(new LeaveRoomRequest());

        // Return to lobby scene
        RemoveSmartFoxListeners();

        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("scenes/lobby.tscn");

    }


    #endregion

    /**
   * ------------------------------------------------------
   * Network Send Methods
   * ------------------------------------------------------
   * 
   * This section contains the creation of SFS Objects that 
   * are transmitted to the Server Scripts
   * 
   */

    #region Network Send Methods

    public void SendSpawnRequest()
    {
        Random random1 = new Random();
        int colors1 = random1.Next(0, colorarray.Length);
        Random random2 = new Random();
        int prefab1 = random2.Next(0, PlayerScene.Length);
        Room room = sfs.LastJoinedRoom;
        ISFSObject data = new SFSObject();
        data.PutInt("prefab", prefab1);
        data.PutInt("color", colors1);
        ExtensionRequest request = new ExtensionRequest("spawnMe", data, room);
        sfs.Send(request);
    }
    public void SendShot(int target)
    {
        Room room = sfs.LastJoinedRoom;
        ISFSObject data = new SFSObject();
        data.PutInt("target", target);
        ExtensionRequest request = new ExtensionRequest("shot", data, room);
        sfs.Send(request);
    }

    public void SendReload()
    {
        Room room = sfs.LastJoinedRoom;
        ExtensionRequest request = new ExtensionRequest("reload", new SFSObject(), room);
        sfs.Send(request);
    }

    public void SendTransform()
    {
        Room room = sfs.LastJoinedRoom;
        ISFSObject data = new SFSObject();
        lastState.ToSFSObject(data);
        ExtensionRequest request = new ExtensionRequest("sendTransform", data, room, true); // True flag = UDP
        sfs.Send(request);
    }

    public void SendAnimationState(string message)
    {
        Room room = sfs.LastJoinedRoom;
        ISFSObject data = new SFSObject();
        data.PutUtfString("msg", message);
        ExtensionRequest request = new ExtensionRequest("sendAnim", data, room);
        sfs.Send(request);
    }
    public void TimeSyncRequest()
    {
        Room room = sfs.LastJoinedRoom;
        ExtensionRequest request = new ExtensionRequest("getTime", new SFSObject(), room);
        sfs.Send(request);
    }
 
    public void ResurrectPlayer()
    {
        playerRespawn = true;
        SendSpawnRequest(); 

   }

    public void DestroyPlayer(int userId)
    {
         if (recipients.ContainsKey(userId))
        {
            string name1 = recipients[userId].ToString();
            player remoteplayer = (player)GetNodeOrNull(name1);
            if (remoteplayer != null)
            {
                remoteplayer.RemoveFromGroup("remotePlayer");
                remoteplayer.QueueFree();
                recipients.Remove(userId);
            }
        }
    }

    public async void DestroyEnemy(int userId)
    {
        await Task.Delay((waitToRespawn) * 1000);

        if (recipients.ContainsKey(userId))
        {
            string name1 = recipients[userId].ToString();
            player remoteplayer = (player)GetNodeOrNull(name1);
            if (remoteplayer != null)
            {
                remoteplayer.RemoveFromGroup("remotePlayer");
                remoteplayer.QueueFree();
                recipients.Remove(userId);
            }
        }
    }

    #endregion
    /**
   * ------------------------------------------------------
   *    Network Receive Methods
   * ------------------------------------------------------
   * 
   * This section contains Methods that are executed when 
   * they receive of SFS Objects from the Server Scripts
   * 
   */

    #region Network Receive Methods

    private void OnExtensionResponse(BaseEvent evt)
    {
        string cmd = (string)evt.Params["cmd"];
        ISFSObject sfsobject = (SFSObject)evt.Params["params"];
        switch (cmd)
        {
            case "spawnPlayer":
                {
                    HandleInstantiatePlayer(sfsobject);
                }
                break;
            case "transform":
                {
                    HandleTransform(sfsobject);
                }
                break;
            case "notransform":
                {
                    HandleNoTransform(sfsobject);
                }
                break;
            case "killed":
                {
                    HandleKill(sfsobject);
                }
                break;

            case "health":
                {
                    HandleHealthChange(sfsobject);
                }
                break;
            case "anim":
                {
                    HandleAnimation(sfsobject);
                }
                break;
            case "score":
                {
                    HandleScoreChange(sfsobject);
                }
                break;
            case "ammo":
                {
                    HandleAmmoCountChange(sfsobject);
                }
                break;
            case "spawnItem":
                {
                    HandleItem(sfsobject);
                }
                break;
            case "removeItem":
                {
                    HandleRemoveItem(sfsobject);
                }
                break;
            case "enemyShotFired":
                {
                    HandleShotFired(sfsobject);
                }
                break;
            case "time":
                {
                    HandleServerTime(sfsobject);
                }
                break;
            case "reloaded":
                {
                    HandleReload(sfsobject);
                }
                break;
        }
    }

    private void HandleInstantiatePlayer(ISFSObject sfsobject)
    {
        ISFSObject playerData = sfsobject.GetSFSObject("player");
        int userId = playerData.GetInt("id");
        int score = playerData.GetInt("score");
        int prefab = playerData.GetInt("prefab");
        int colors = playerData.GetInt("color");
        CharacterTransform chtransform = CharacterTransform.FromSFSObject(playerData);
        RandomNumberGenerator _rng = new RandomNumberGenerator();

        User user = sfs.UserManager.GetUserById(userId);
        string name = user.Name;
        if (userId == sfs.MySelf.Id && !playerRespawn)
        {
            player localplayer = (player)PlayerScene[prefab].Instantiate();
            localplayer.isPlayer = true;
            localplayer.Visible = false;
            Vector3 value = chtransform.Position;
            float x = value.X + _rng.RandfRange(-2.0f, 2.0f);
            float y = value.Y;
            float z = value.Z + _rng.RandfRange(-2.0f, 2.0f);
            localplayer.Position = new Vector3(x, y, z);
            localplayer.Rotation = chtransform.AngleRotationFPS;
            localplayer.Name = "LocalPlayer";
            localplayer.userid = userId;
            recipients[userId] = localplayer.Name;
            playerHealth = healthStars.Length;
            AddChild(localplayer);
            PlayerVisible(localplayer, colors);

        }
        else if (userId == sfs.MySelf.Id && playerRespawn)
        {
            player localplayer = (player)GetNode("LocalPlayer");
            Vector3 value = chtransform.Position;
            float x = value.X + _rng.RandfRange(-2.0f, 2.0f);
            float y = value.Y;
            float z = value.Z + _rng.RandfRange(-2.0f, 2.0f);
            localplayer.GlobalPosition = new Vector3(x, y, z);
            for (int i = 0; i < healthStars.Length; i++)
            {
                healthStars[i].Visible = true;
            }
            playerHealth = healthStars.Length;
            localplayer.reSpawnPlayer();
            playerRespawn = false;
        }

        else
        {
            player remoteplayer = (player)PlayerScene[prefab].Instantiate();
            remoteplayer.Visible = false;
            remoteplayer.isPlayer = false;
            remoteplayer.Position = chtransform.Position;
            remoteplayer.Rotation = chtransform.AngleRotationFPS;
            remoteplayer.Name = user.Name;
            remoteplayer.userid = userId;
            recipients[userId] = remoteplayer.Name;
            remoteplayer.AddToGroup("remotePlayer", true);
            AddChild(remoteplayer);
            RemotePlayerVisible (remoteplayer, colors);
            

        }
    }

    public async void PlayerVisible(player localPlayer, int coloring)
    {
        await Task.Delay(100);
        StandardMaterial3D material = new StandardMaterial3D();
        material.AlbedoColor = colorarray[coloring];
        localPlayer.playerMesh.SetSurfaceOverrideMaterial(0, material);
        await Task.Delay(200);
        localPlayer.Visible = true;

    }
    public async void RemotePlayerVisible(player remotePlayer, int coloring)
    {
        await Task.Delay(100);
        StandardMaterial3D material = new StandardMaterial3D();
        material.AlbedoColor = colorarray[coloring];
        remotePlayer.playerMesh.SetSurfaceOverrideMaterial(0, material);
        await Task.Delay(500);
        remotePlayer.Visible = true;


    }

    private void HandleTransform(ISFSObject sfsobject)
    {
        int userId = sfsobject.GetInt("id");
        CharacterTransform chtransform = CharacterTransform.FromSFSObject(sfsobject);
        if (userId != sfs.MySelf.Id)
        {
            if (recipients.ContainsKey(userId))
            {
                string name1 = recipients[userId].ToString();
                player remoteplayer = (player)GetNode(name1);
                remoteplayer.newPosition.Origin = chtransform.Position;
                remoteplayer.newRotation = chtransform.AngleRotation;
                remoteplayer.aimRotation = chtransform.SpineRotation;

                if (networkSync == NetworkSync.Complex)
                {
                    for (int i = remoteplayer.bufferedStates.Length - 1; i >= 1; i--)
                    {
                        remoteplayer.bufferedStates[i] = remoteplayer.bufferedStates[i - 1];
                    }
                    remoteplayer.bufferedStates[0] = chtransform;
                    remoteplayer.statesCount = Mathf.Min(remoteplayer.statesCount + 1, remoteplayer.bufferedStates.Length);
                }               
            }
        }
    }

    private void HandleNoTransform(ISFSObject sfsobject)
    {
        int userId = sfsobject.GetInt("id");
        CharacterTransform chtransform = CharacterTransform.FromSFSObject(sfsobject);
        if (userId == sfs.MySelf.Id)
        {
            player localplayer = (player)GetNode("LocalPlayer");
            chtransform.ResetTransform(localplayer.Transform);
        }
    }

    private void HandleAnimation(ISFSObject sfsobject)
    {

        int userId = sfsobject.GetInt("id");
        string msg = sfsobject.GetUtfString("msg");

        if (userId != sfs.MySelf.Id)
        {
            if (recipients.ContainsKey(userId))
            {
                string name1 = recipients[userId].ToString();
                player remoteplayer = (player)GetNode(name1);
                remoteplayer.AnimationSync(msg);
            }
        }
    }

    private void HandleShotFired(ISFSObject sfsobject)
    {
        int userId = sfsobject.GetInt("id");
        string name1 = recipients[userId].ToString();
        if (userId == sfs.MySelf.Id)
        {
            player localplayer = (player)GetNode("LocalPlayer");
            localplayer.Shooting();
        }
        else
        {
            player remoteplayer = (player)GetNode(name1);
            remoteplayer.Shooting();
        }
    }

    private void HandleReload(ISFSObject sfsobject)
    {
        int userId = sfsobject.GetInt("id");
        string name1 = recipients[userId].ToString();
        if (userId == sfs.MySelf.Id)
        {
            player localplayer = (player)GetNode("LocalPlayer");
            localplayer.AnimationSync("reload");

        }
        else
        {
            if (recipients.ContainsKey(userId))
            {

                player remoteplayer = (player)GetNode(name1);
                remoteplayer.AnimationSync("reload");
            }
        }
    }
    private void HandleKill(ISFSObject sfsobject)
    {
        int userId = sfsobject.GetInt("id");
        int killerId = sfsobject.GetInt("killerId");
        string name1 = recipients[userId].ToString();
        User killed = sfs.UserManager.GetUserById(userId);
        string Killed = killed.Name;
        User killer = sfs.UserManager.GetUserById(killerId);
        string Killer = killer.Name;

        if (userId == sfs.MySelf.Id)
        {
            player localplayer = (player)GetNode("LocalPlayer");
  
                localplayer.AnimationSync("die");

                for (int i = 0; i < healthStars.Length; i++)
                {
                    healthStars[i].Visible = false;
                }

        }
        else
        {
            if (recipients.ContainsKey(userId))
            {
                 
                player remoteplayer = (player)GetNode(name1);
                remoteplayer.AnimationSync("die");
                Node3D remoteHeathBar = GetNodeOrNull<Node3D>(name1 + "/HealthBar");
                if (remoteHeathBar != null)
                {
                    remoteHeathBar.Call("HandleDie");
                }
                DestroyEnemy(userId);
            }
        }
        string info = ("              " + Killer + " has shot " + Killed + " dead\n  ");
        playerInfo.Text = info;
        ClearText();

    }

    private void HandleHealthChange(ISFSObject sfsobject)
    {
        int userId = sfsobject.GetInt("id");
        int health = sfsobject.GetInt("health");
        string name1 = recipients[userId].ToString();
        if (userId == sfs.MySelf.Id)
        {
 
                player localplayer = (player)GetNode("LocalPlayer");
                localplayer.AnimationSync("wounded");

                for (int i = 0; i < healthStars.Length; i++)
                {
                    healthStars[i].Visible = false;
                }
                for (int i = 0; i < health; i++)
                {
                    healthStars[i].Visible = true;
                }
   
        }
        else
        {
            if (recipients.ContainsKey(userId))
            {
                player remoteplayer = (player)GetNode(name1);
                remoteplayer.AnimationSync("wounded");
                Node3D remoteHeathBar = GetNodeOrNull<Node3D>(name1 + "/HealthBar");
                if (remoteHeathBar != null)
                {
                    remoteHeathBar.Call("HandleHealth", health);
                }
            }
        }

        }

    private void HandleAmmoCountChange(ISFSObject sfsobject)
    {
        int userId = sfsobject.GetInt("id");
        if (userId != sfs.MySelf.Id) return;
        int loadedAmmo = sfsobject.GetInt("ammo");
        int maxAmmo = sfsobject.GetInt("maxAmmo");
        int ammo = sfsobject.GetInt("unloadedAmmo");
       for (int i = 0; i < loadedBullets.Length; i++)
        {
            loadedBullets[i].Visible = false;
        }
        for (int i = 0; i < loadedAmmo; i++)
        {
            loadedBullets[i].Visible = true;
        }
        for (int i = 0; i < unloadedBullets.Length; i++)
        {
            unloadedBullets[i].Visible = false;
        }
        for (int i = 0; i < ammo; i++)
        {
            unloadedBullets[i].Visible = true;
        }
    }

    private void HandleScoreChange(ISFSObject sfsobject)
    {
        int userId = sfsobject.GetInt("id");
        int c = sfsobject.GetInt("score");
        if (userId != sfs.MySelf.Id) return;

        if (c <= kills.Length)
        {
            for (int i = 0; i < kills.Length; i++)
            {
                kills[i].Visible = false;
            }
            for (int i = 0; i < c; i++)
            {
                kills[i].Visible = true;
            }
        }
    }

    private void HandleItem(ISFSObject sfsobject)
    {
        ISFSObject item = sfsobject.GetSFSObject("item");
        int id = item.GetInt("id");
        string itemType = item.GetUtfString("type");
        GD.Print(itemType);
        CharacterTransform chtransform = CharacterTransform.FromSFSObject(item);

        Node3D itemObj;
        if (itemType == "Ammo")
        {
            itemObj = (Node3D)AmmoBox.Instantiate();
            itemObj.Name = "Ammo" + id.ToString();
         }
        else
        {
            itemObj = (Node3D)HealthBox.Instantiate();
            itemObj.Name = "HealthPack" + id.ToString();
        }

        itemObj.Position = chtransform.Position;
        AddChild(itemObj);
        items[id] = itemType;

        string info = ("              Health and Ammo spawned in Buildings\n  ");
        playerInfo.Text = info;
        ClearText();
    }

    private void HandleRemoveItem(ISFSObject sfsobject)
    {
        ISFSObject item = sfsobject.GetSFSObject("item");
        int id = item.GetInt("id");
        string type = item.GetUtfString("type");
        if (items.ContainsKey(id))
        {
            Node3D itemNode = GetNode<Node3D>(type + id);
            itemNode.QueueFree();
            items.Remove(id);
        }
    }

    private void HandleServerTime(ISFSObject sfsobject)
    {
        long time = sfsobject.GetLong("t");
        double timePassed = clientServerLag / 2.0f;
        lastServerTime = Convert.ToDouble(time) + timePassed;
        lastLocalTime = timeAtFrameStart;
    }
    #endregion
}