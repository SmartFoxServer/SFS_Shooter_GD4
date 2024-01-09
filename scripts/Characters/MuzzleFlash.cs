using Godot;
/**
 * ------------------------------------------------------
 * This method displays the muzzle flash
 * ------------------------------------------------------
 */
public partial class MuzzleFlash : Node3D {
    [Export] public NodePath ParticlesPath { get;set; }
    [Export] public float LifeTime { get;set; } = 0.3f;

    public async override void _Ready() {
        this.GetNode<GpuParticles3D>(this.ParticlesPath).Emitting = true;

        await ToSignal(this.GetTree().CreateTimer(this.LifeTime), "timeout");
        this.QueueFree();
    }
}
