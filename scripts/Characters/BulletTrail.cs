using Godot;

/**
 * ------------------------------------------------------
 * This class is attached to the Bullet Trail node
 * ------------------------------------------------------
 */
public partial class BulletTrail : Node3D {
    private MeshInstance3D _trailMesh;
    private float _trailMeshHeight;

    public float MaxDistance { get;set; } = 0;

    [Export] public NodePath TrailMeshPath { get;set; }
    [Export] public float BulletTrailLifeTime { get;set; } = 1f;
    [Export] public float BulletTrailSpeed { get;set; } = 200f;

    public async  override void _Ready() {
        base._Ready();

        this._trailMesh = this.GetNode<MeshInstance3D>(this.TrailMeshPath);
        this._trailMeshHeight = ((CapsuleMesh)this._trailMesh.Mesh).Height;

        if (this.MaxDistance == 0) {
            await ToSignal(this.GetTree().CreateTimer(this.BulletTrailLifeTime), "timeout");
            this.QueueFree();
        }
    }

    public override void _Process(double delta) {
        this._trailMesh.Position += Vector3.Forward * BulletTrailSpeed * (float) delta;

        if (
            this.MaxDistance > 0 &&
            this.GlobalPosition.DistanceTo(this._trailMesh.GlobalPosition) >= (
                this.MaxDistance - (this._trailMeshHeight * 2)
            )
        ) {
            this.QueueFree();
        }
    }
}
