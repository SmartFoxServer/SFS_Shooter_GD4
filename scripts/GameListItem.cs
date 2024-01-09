using Godot;
using System;

using Sfs2X.Entities;


public partial class GameListItem : Control
{
    [Export] public TextureButton playButton;
    [Export] public TextureButton watchButton;
    [Export] public LineEdit nameText;
    [Export] public LineEdit detailsText;

    [Export] public int roomId;
    // Called when the node enters the scene tree for the first time.
    public void Init(Room room)
    {
        nameText.Text = room.Name;
        roomId = room.Id;
        SetState(room);
    }

    /**
 * Update prefab instance based on the corresponding Room state.
 */
    public void SetState(Room room)
    {
        int playerSlots = room.MaxUsers - room.UserCount;
        int spectatorSlots = room.MaxSpectators - room.SpectatorCount;

        // Set player count and spectator count in game list item
        detailsText.Text = String.Format("Player slots: {0}", playerSlots);

        // Enable/disable game play button
        playButton.Disabled = playerSlots < 1;

    }
}
