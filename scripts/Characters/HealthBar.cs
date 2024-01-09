using Godot;
using System;

/**
 * ------------------------------------------------------
 * The Health bar is displayed for each character in the scene
 * and follows the player camera.
 * It is hidden if the character is the local Player.
 * ------------------------------------------------------
 */
public partial class HealthBar : Node3D
{
    [Export] public Sprite3D[] healthStars { get; set; }

    public Camera3D playerCamera;
    private bool dead;
    public override void _Ready()
	{
        playerCamera = GetNodeOrNull<Camera3D>("../../LocalPlayer/CameraMount/SpringArm3D/Camera3D");    
        dead = false;
    }

	public override void _Process(double delta)
	{
        if (playerCamera == null)
        {
            playerCamera = GetNodeOrNull<Camera3D>("../../LocalPlayer/CameraMount/SpringArm3D/Camera3D");
        }
       else
        {
          this.LookAt(playerCamera.GlobalTransform.Origin, Vector3.Up);
        }    
    }

    public void HandleHealth(int health)
    {
        for (int i = 0; i < this.healthStars.Length; i++)
        {
            this.healthStars[i].Visible = false;
        }

        if (!dead)
        { 
            for (int i = 0; i < health; i++)
            {
                this.healthStars[i].Visible = true;
            }
        }
    }

    public void HandleDie()
    {
        dead = true;
        for (int i = 0; i < this.healthStars.Length; i++)
        {
            this.healthStars[i].Visible = false;
        }
    }
}
