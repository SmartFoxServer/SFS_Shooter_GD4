using Godot;

using Sfs2X;
using Sfs2X.Core;

using Sfs2X.Util;


public partial class GlobalManager : Node
{
    public SmartFox sfs;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        sfs = new SmartFox();

    }

    public override void _Process(double delta)
    {
        // Process the SmartFox events queue
        if (sfs != null)
            sfs.ProcessEvents();
    }
    public SmartFox CreateSfsClient()
    {
      //  if (sfs == null) sfs = new SmartFox();
        sfs = new SmartFox();
        sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
        return sfs;
    }

    public SmartFox GetSfsClient()
    {
        return sfs;
    }
    private void OnConnectionLost(BaseEvent evt)
    {

        // Remove SFS listeners
 
        sfs.RemoveEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
        sfs = null;

        // Show error message
        string reason = (string)evt.Params["reason"];
        //
        GD.Print("Connection to SmartFoxServer lost; the reason is: " + reason);

        if (reason != ClientDisconnectionReason.MANUAL)
        {
            // Show error message
            string connLostMsg = "An unexpected disconnection occurred; ";

            if (reason == ClientDisconnectionReason.IDLE)
                connLostMsg += "It looks like you have been idle for too much time.";
            else if (reason == ClientDisconnectionReason.KICK)
                connLostMsg += "You have been kicked by an administrator or moderator.";
            else if (reason == ClientDisconnectionReason.BAN)
                connLostMsg += "You have been banned by an administrator or moderator.";
            else
                connLostMsg += "The reason of the disconnection is unknown.";

            //  errorText.Text = connLostMsg;
        }

        GetTree().ChangeSceneToFile("login.tscn");

    }


}
