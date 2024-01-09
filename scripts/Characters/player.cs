using Godot;
using System;
using System.Threading.Tasks;
using static GameManager;

/**
  * ------------------------------------------------------
  * This player class is to control both local player character
  * and remote player characters. it is attached to the player
  * character in each player scene.
  * 
  * It includes:
  *     Player Movement
  *     Transform sync with the server
  *     Shooting Methods
  *     Animation playing
  *     and some UI.
  * ------------------------------------------------------
  */
public partial class player : CharacterBody3D
{
    [ExportCategory("-- Player Settings --")]
    [Export] public CollisionShape3D playerCollider;
    [Export] float speed = 1.5f;
    [Export] float jumpVelocity = 7f;
    float gravity = 30f;
    public AnimationPlayer animationPlayer;
    public MeshInstance3D playerMesh;

    [ExportCategory("-- Weapon Settings --")]
    [Export] public Node3D Pistol;
    [Export] NodePath BulletRayCastPath;
    [Export] NodePath ShootRayCastPath;
    [Export] public NodePath MuzzleFlashMarkerPath;
    [Export] public PackedScene BulletTrailPrefab;
    [Export] public PackedScene MuzzleFlashPrefab;

    [ExportCategory("-- Camera Settings --")]
    [Export] public Node3D camMount;
    [Export] public SpringArm3D springarm;
    [Export] public Camera3D camera3D;
    [Export(PropertyHint.Range, "0.1,1.0")] float camSensitivity = 0.3f;
    [Export(PropertyHint.Range, "-90,0,1")] float minCamPitch = -20f;
    [Export(PropertyHint.Range, "0,90,1")] float maxCamPitch = 20f;

    [ExportCategory("-- Audio Settings --")]
    [Export] public AudioStream BackgroundMusic;
    [Export] public AudioStream Gunshot;
    [Export] public AudioStream Reload;
    [Export] public AudioStream Wounded;
    public AudioStreamPlayer BackgroundMusicPlayer;
    private AudioStreamPlayer3D audioPlayer1;
    private AudioStreamPlayer3D audioPlayer2;
    private AudioStreamPlayer3D audioPlayer3;

    private bool CrosshairsVisible = true;
    private Control crosshairs1;
    private Control crosshairs2;

    private Marker3D muzzleFlashMarker;
    private Node3D CameraMount;
    private Node3D headNode;

    public Vector3 direction;
    public Vector3 velocity;




    public bool sprint;
    public bool jump;
    public bool crouch;
    public bool reloading = false;
    public bool dead;
    public bool wounded;
    public bool aim;
    public bool aiming = false;
    public bool wasaiming = false;
    public bool shoot;
    public bool music = true;
    public bool help;
    public bool spawning;
    public bool respawn;
    public bool animating;
    public bool deathAnimation;
    public bool woundAnimation;
    public bool reloadAnimation;



    private Vector2 lastDirection;
    private Vector2 relativeDirection;
    private bool animate;

    private float stepHeight = 0.33f;
    private float stepMargin = 0.01f;

    private float cylinderRadius = 0.5f;
    private CollisionShape3D separator;
    private RayCast3D rayCastStep;
    private RayCast3D rayCastBullet;
    private RayCast3D rayCastShoot;
    private float rayShapeLocalHeight;
    private int state = 0;
    private int crosshair = 0;

    private SkeletonIK3D skeletonIK1;
    public bool isPlayer;
    public bool isMoving = true;
    public bool isAiming;
    public bool onAir;
    public int userid;

    public Control gamemanager;

    public double duration = 0.1f;

    private Transform3D _targetTransform;
    private double _elapsedTime = 0.0f;
    public static readonly Double sendingPeriod = 0.03f;
    private Double timeLastSending = 0.0f;
    public CharacterTransform currentState;
    public Transform3D newPosition;
    public Vector3 newRotation;
    public Vector3 aimRotation;

    private readonly double period = 0.1f;
    private double lastRequestTime = double.MaxValue;
    private double interpolationBackTime = 50;
    public Quaternion spineRotation;
    public CharacterTransform[] bufferedStates = new CharacterTransform[30];
    private CharacterTransform rhs;
    private CharacterTransform lhs;
    public int statesCount = 0;
    private double timeAtFrameStart;
    private GameManager gm;
    private const float SPEED = 1000f;
    private string currentAnimation;

    /**
    * ------------------------------------------------------
    * Godot Ready and Process cycles
    * ------------------------------------------------------
    */
    #region
    public override void _Ready()
    {
        gamemanager = GetNode<Control>("../../Game");
        gm = (GameManager)gamemanager;

        animationPlayer = ((AnimationPlayer)GetNode("Visuals/SFSCowboy/AnimationPlayer"));
        audioPlayer1 = new AudioStreamPlayer3D();
        audioPlayer2 = new AudioStreamPlayer3D();
        audioPlayer3 = new AudioStreamPlayer3D();

        BackgroundMusicPlayer = new AudioStreamPlayer();
        AddChild(audioPlayer1);
        AddChild(audioPlayer2);
        AddChild(audioPlayer3);
        AddChild(BackgroundMusicPlayer);
        audioPlayer1.Stream = Gunshot;
        audioPlayer2.Stream = Wounded;
        audioPlayer3.Stream = Reload;

        /**
         * Please Note:
         * SkeletonIK3D error states that it has been Deprecated.
         * However, it has still been included in 4.3 and there has been no replacement method suggested.
         */

        skeletonIK1 = GetNode<SkeletonIK3D>("Visuals/SFSCowboy/Armature/Skeleton3D/SkeletonIK3D1");
        playerMesh = GetNode<MeshInstance3D>("Visuals/SFSCowboy/Armature/Skeleton3D/SFSCowboy");

        this.headNode = camera3D;
        this.rayCastShoot = this.GetNode<RayCast3D>(this.ShootRayCastPath);
        this.rayCastBullet = this.GetNode<RayCast3D>(this.BulletRayCastPath);
        this.muzzleFlashMarker = this.GetNode<Marker3D>(this.MuzzleFlashMarkerPath);
        Node3D healthbar = this.GetNode<Node3D>("HealthBar");

        Input.MouseMode = Input.MouseModeEnum.Captured;
        if (!isPlayer)
        {
            Label3D Name = this.GetNode<Label3D>("HealthBar/Panel/Name");
            Name.Text = (String)this.Name;
        }

        if (isPlayer)
        {
            foreach (var node in GetChildren())
            {
                if (node is not CollisionShape3D col) continue;

                if (col.Shape is not CapsuleShape3D collider)
                {
                    GD.PrintErr("StairCharacter's collider must use a CapsuleShape3D!");
                    break;
                }
                separator = new CollisionShape3D();
                separator.RotationDegrees = new Vector3(90, 0, 0);
                var shape = new SeparationRayShape3D();
                shape.Length = stepHeight;
                separator.Shape = shape;
                rayCastStep = new RayCast3D();
                rayCastStep.TargetPosition = Vector3.Down * stepHeight;
                rayCastStep.CollisionMask = CollisionMask;
                rayCastStep.ExcludeParent = true;
                rayCastStep.Enabled = false;
                rayShapeLocalHeight = col.Position.Y - collider.Height * 0.5f + stepHeight;
                cylinderRadius = collider.Radius;
                AddChild(separator);
                AddChild(rayCastStep);
                separator.TranslateObjectLocal(rayShapeLocalHeight * Vector3.Down);

                break;
            }
            BackgroundMusicPlayer.Stream = BackgroundMusic;
            BackgroundMusicPlayer.Play();
            springarm.Position = new Vector3(-0.6f, 1.4f, 0.0f);
            springarm.SpringLength = 0.5f;
            gm.SendAnimationState("idle");

            animationPlayer.Play("idle");
            healthbar.Visible = false;
            var viewport = headNode.GetViewport().GetVisibleRect();
            gm.crosshairs[0].Position = (viewport.GetCenter() - gm.crosshairs[0].Size / 2);
            gm.crosshairs[1].Position = (viewport.GetCenter() - gm.crosshairs[1].Size / 2);
            gm.crosshairs[2].Position = (viewport.GetCenter() - gm.crosshairs[2].Size / 2);
            gm.crosshairs[3].Position = (viewport.GetCenter() - gm.crosshairs[3].Size / 2);
            gm.crosshairs[0].Visible = true;
        }
    }

    public override void _Process(double delta)
    {
        if (!isPlayer && gm.networkSync == NetworkSync.Complex)
        {
            timeAtFrameStart = Time.GetTicksMsec();
        }

        if (isPlayer && !dead)
        {

            if (Input.IsActionJustPressed("move_run") && IsOnFloor())
            {
                sprint = !sprint;
                if (sprint)
                    speed = 3.0f;
                else
                    speed = 1.5f;
                animate = true;
            }

            if (Input.IsActionJustPressed("aim") && IsOnFloor())
            {
                aim = !aim;
                animate = true;
                if (aim)
                {
                    StartAimFunction();
                    gm.SendAnimationState("aiming");

                }
                else
                {
                    EndAimFunction();
                    gm.SendAnimationState("notaiming");

                }
            }

            if (Input.IsActionJustPressed("move_jump") && IsOnFloor())
            {
                jump = true;
            }

            if (Input.IsActionJustPressed("reload"))
            {
                gm.SendReload();

            }

            if (Input.IsActionJustPressed("shoot") && aim)
            {
                OnShoot();
            }
        }

        if (isPlayer)
        {
            if (Input.IsActionJustPressed("shoot") && dead && !spawning && respawn)
            {
                OnResurrect();
            }

            if (Input.IsActionJustPressed("music"))
            {
                music = music ? false : true;
                if (music)
                    BackgroundMusicPlayer.Play();
                else
                    BackgroundMusicPlayer.Stop();
            }
            if (Input.IsActionJustPressed("help"))
            {
                help = help ? false : true;
                if (help)
                {
                    GetNode<TextureRect>("../UserInterface/MarginContainer/HelpPanel").Visible = true;
                }
                else
                    GetNode<TextureRect>("../UserInterface/MarginContainer/HelpPanel").Visible = false;
            }
            if (Input.IsActionJustPressed("exit"))
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                gm.OnExitGame();
            }

            if (Input.IsActionJustPressed("camminus"))
            {
                camSensitivity = camSensitivity - 0.05f;
                if (camSensitivity <= 0.0f)
                    camSensitivity = 0.01f;
            }

            if (Input.IsActionJustPressed("camplus"))
            {
                camSensitivity = camSensitivity + 0.05f;
                if (camSensitivity >= 1.0f)
                    camSensitivity = 1.0f;
            }

            if (Input.IsActionJustPressed("view"))
            {
                state = (state + 1) % 3;
                switch (state)
                {
                    case 0:
                        springarm.SpringLength = 0.5f;
                        break;
                    case 1:
                        springarm.SpringLength = 1.5f;
                        break;
                    case 2:
                        springarm.SpringLength = 2.5f;
                        break;
                }
            }

            if (Input.IsActionJustPressed("crosshair"))
            {
                gm.crosshairs[0].Visible = false;
                gm.crosshairs[1].Visible = false;
                gm.crosshairs[2].Visible = false;
                gm.crosshairs[3].Visible = false;

                state = (state + 1) % 4;
                switch (state)
                {
                    case 0:
                        gm.crosshairs[0].Visible = true;
                        break;
                    case 1:
                        gm.crosshairs[1].Visible = true;
                        break;
                    case 2:
                        gm.crosshairs[2].Visible = true;
                        break;
                    case 3:
                        gm.crosshairs[3].Visible = true;
                        break;
                }
            }
        }
    }


    public override void _PhysicsProcess(double delta)
    {

        if (isPlayer && !dead && !reloading && !wounded)

        {
            animate = true;
            velocity = Velocity;

            if (!IsOnFloor() && !jump)
            {
                velocity.Y -= gravity * (float)delta;
            }
            else if (jump)
            {
                velocity.Y = jumpVelocity;
            }
            Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
            direction = new Vector3(inputDir.X, 0, inputDir.Y).Rotated(Vector3.Up, camMount.Rotation.Y).Normalized();
            if (direction != Vector3.Zero)
            {
                velocity.X = direction.X * speed * (float)delta * 60;
                velocity.Z = direction.Z * speed * (float)delta * 60;
            }
            else
            {
                velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
                velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
            }
            Velocity = velocity;
            Animate();
            MoveAndSlide();
            HandleStairs();

            int roundedX = (int)Math.Round(inputDir.X, MidpointRounding.AwayFromZero);
            int roundedY = (int)Math.Round(inputDir.Y, MidpointRounding.AwayFromZero);

            relativeDirection = new Vector2(roundedX, roundedY);
            jump = false;

        }

        if (isPlayer)
        {

            if (timeLastSending >= sendingPeriod)
            {
                var trans = GetNode<Node3D>("Visuals");
                var aimer = GetNode<Node3D>("CameraMount/SpringArm3D/Camera3D/AimTarget/AimSphere");
                var agp = aimer.GlobalPosition;
                gm.lastState = CharacterTransform.FromTransform(this.Transform, trans.Transform, agp);
                gm.SendTransform();

                timeLastSending = 0;
            }
            timeLastSending += delta;
        }

        if (!isPlayer)
        {

            if (gm.networkSync == NetworkSync.Simple)
            {
                this.GlobalRotation = new Vector3(0, newRotation.Y, 0);

                Vector3 targetPosition = newPosition.Origin;
                float distance = GlobalTransform.Origin.DistanceTo(targetPosition);
                if (distance > 0)
                {
                    Vector3 direction = (targetPosition - GlobalTransform.Origin).Normalized();
                    GlobalTransform = new Transform3D(GlobalTransform.Basis, GlobalTransform.Origin + direction * Mathf.Min(distance, SPEED * (float)delta));
                }
            }
            else if (gm.networkSync == NetworkSync.Complex)
            {
                SyncCharacter();
            }
            var aimremote = GetNode<Node3D>("CameraMount/SpringArm3D/Camera3D/AimTarget/AimSphere");
            aimremote.GlobalPosition = new Vector3(aimRotation.X, aimRotation.Y, aimRotation.Z);
        }

    }

    #endregion

    /**
    * ---------------------------------------------------------------------
    * We have used the Godot Input Map to create Keyboard and mouse inputs
    * These inputs can be seen in the Process loop above.
    * The Camera is moun ted on a Springarm for collision detection
    * and first/third person views are included.
    * The view distance can be changed by setting the Springarm length.
    * ---------------------------------------------------------------------
    */
    #region

    public override void _Input(InputEvent @event)
    {
        if (isPlayer && !dead && !reloading && !wounded && !jump)
        {
            Vector3 camMountRot = camMount.RotationDegrees;
            Vector3 camRot = camera3D.RotationDegrees;
            if (@event is InputEventMouseMotion mouseMotion)
            {
                camMountRot.Y -= mouseMotion.Relative.X * camSensitivity;
                camRot.X -= mouseMotion.Relative.Y * camSensitivity;
            }
            camRot.X = Mathf.Clamp(camRot.X, minCamPitch, maxCamPitch);
            camMount.RotationDegrees = new Vector3(this.Rotation.X, camMountRot.Y, camMountRot.Z);
            camera3D.RotationDegrees = new Vector3(camRot.X, 0, 0);
            GetNode<Node3D>("Visuals").RotationDegrees = new Vector3(this.Rotation.X, camMountRot.Y, this.Rotation.Z);
        }

    }
    #endregion

    /**
    * ---------------------------------------------------------------------
    * The following methods are for Player Character Interaction
    * The GameManager scripted is called where necessary, 
    * to send data to the Java script on the server. 
    * ---------------------------------------------------------------------
    */
    #region
    protected void HandleStairs()
    {
        if (IsOnFloor() == false || GetLastSlideCollision() == null)
        {
            separator.Disabled = true;
            return;
        }

        var localPos = ToLocal(GetLastSlideCollision().GetPosition());
        localPos.Y = 0;
        var dir = (localPos * new Vector3(1, 0, 1)).Normalized();
        localPos += dir * stepMargin;
        localPos = localPos.LimitLength(cylinderRadius + stepMargin);
        localPos.Y = rayShapeLocalHeight;
        rayCastStep.Position = localPos;
        rayCastStep.ForceUpdateTransform();
        rayCastStep.ForceRaycastUpdate();

        var angle = rayCastStep.GetCollisionNormal().AngleTo(UpDirection);
        if (angle > FloorMaxAngle)
        {
            return;
        }

        separator.Disabled = false;
        separator.Position = localPos;
    }


    public async void StartAimFunction()
    {
        float startpos = 0.0f;
        float endpos = 1.0f;
        skeletonIK1.Start();

        while (startpos < endpos)
        {
            skeletonIK1.Interpolation = Mathf.Lerp(startpos, endpos, 0.01f);
            await ToSignal(GetTree().CreateTimer(0.001f), "timeout");
            startpos += 0.1f;
        }
        aiming = true;

    }

    public async void EndAimFunction()
    {
        float startpos = 1.0f;
        float endpos = 0.0f;
        while (startpos > endpos)
        {
            skeletonIK1.Interpolation = Mathf.Lerp(startpos, endpos, 0.01f);
            await ToSignal(GetTree().CreateTimer(0.001f), "timeout");
            startpos -= 0.1f;
        }
        await Task.Delay(100);
        skeletonIK1.Stop();
        aiming = false;
    }


    private void OnShoot()
    {
        if (!dead)
        {
            int target;
            target = 99999;
            if (this.rayCastShoot.IsColliding())
            {
                var collision = this.rayCastShoot.GetCollider();
                var collsionid = collision.GetInstanceId();
                foreach (Node3D n in GetTree().GetNodesInGroup("remotePlayer"))
                {
                    if (n.GetInstanceId() == collsionid)
                    {
                        String hitname = n.Name.ToString();
                        player remoteplayer = (player)GetNode("../" + hitname);
                        target = remoteplayer.userid;
                        break;
                    }
                }
            }
            gm.SendShot(target);

        }
    }

    private void OnResurrect()
    {
        if (!spawning && dead)
        {
            spawning = true;
            gm.ResurrectPlayer();

        }
    }

    public void reSpawnPlayer()
    {
        gm.SendAnimationState("idle");
        animationPlayer.Play("idle");
        dead = false;
        spawning = false;
        respawn = false;
    }

    public async void Shooting()
    {
        if (isPlayer)
            gm.SendAnimationState("aiming");


        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");

        audioPlayer1.Play();
        BulletTrail bulletTrail = this.BulletTrailPrefab.Instantiate<BulletTrail>();
        Vector3 lookAtPoint = this.headNode.GlobalPosition + (-this.headNode.GlobalTransform.Basis.Z * 500);

        if (this.rayCastBullet.IsColliding())
        {
            bulletTrail.MaxDistance = this.muzzleFlashMarker.GlobalPosition.DistanceTo(this.rayCastBullet.GetCollisionPoint());
            lookAtPoint = this.rayCastBullet.GetCollisionPoint();

        }
        this.muzzleFlashMarker.AddChild(bulletTrail);
        bulletTrail.LookAt(lookAtPoint, Vector3.Up);
        MuzzleFlash muzzleFlash = this.MuzzleFlashPrefab.Instantiate<MuzzleFlash>();
        this.muzzleFlashMarker.AddChild(muzzleFlash);

    }
    #endregion

    /**
   * ---------------------------------------------------------------------
   * This Method is for the Local Player Animation.
   * isPlayer = true
   * Local animations are played directly and sent to the server for propagating.
   * ---------------------------------------------------------------------
   */
    #region
    private async void Animate()
    {
        if (!IsOnFloor())
        {
            gm.SendAnimationState("jump");
            animationPlayer.Play("falling");
            return;
        }

        else if (deathAnimation == true)
        {
            dead = true;
            await Task.Delay(200);
            audioPlayer2.Play();
            this.animationPlayer.Play("death");
            this.animationPlayer.Seek(1);
            await Task.Delay(gm.waitToRespawn * 1000);
            respawn = true;
            deathAnimation = false;
            return;
        }

        else if (woundAnimation == true)
        {
            this.wounded = true;
            await Task.Delay(200);
            this.animationPlayer.Play("hit");
            this.animationPlayer.Seek(0.8);
            audioPlayer2.Play();
            await Task.Delay(400);
            this.wounded = false;
            woundAnimation = false;
            return;
        }

        else if (reloadAnimation == true)
        {
            this.reloading = true;
            if (aiming)
            {
                EndAimFunction();
                wasaiming = true;
            }
            audioPlayer3.Play(1f);
            this.animationPlayer.Play("reload");
            this.animationPlayer.Seek(1);
            await Task.Delay(2000);
            this.reloading = false;
            if (wasaiming)
            {
                StartAimFunction();
                wasaiming = false;
            }
            reloadAnimation = false;
            return;
        }

        else if ((lastDirection != relativeDirection || animate) && (IsOnFloor() && !dead))
        {
            switch (relativeDirection.ToString())

            {
                case "(0, -1)":
                case "(1, -1)":
                case "(-1, -1)":
                    {
                        if (!sprint)
                        {
                            animationPlayer.Play("walkforward");
                            gm.SendAnimationState("walkforward");
                        }
                        else
                        {
                            animationPlayer.Play("runforward");
                            gm.SendAnimationState("runforward");
                        }
                    }
                    break;
                case "(0, 1)":
                case "(1, 1)":
                case "(-1, 1)":
                    {
                        if (!sprint)
                        {
                            animationPlayer.Play("walkbackward");
                            gm.SendAnimationState("walkbackward");
                        }
                        else
                        {
                            animationPlayer.Play("runbackward");
                            gm.SendAnimationState("runbackward");
                        }
                    }
                    break;
                case "(-1, 0)":
                    {
                        if (!sprint)
                        {
                            animationPlayer.Play("walkleft");
                            gm.SendAnimationState("walkleft");
                        }
                        else
                        {
                            animationPlayer.Play("runleft");
                            gm.SendAnimationState("runleft");
                        }
                    }
                    break;
                case "(1, 0)":
                    {
                        if (!sprint)
                        {
                            animationPlayer.Play("walkright");
                            gm.SendAnimationState("walkright");
                        }
                        else
                        {
                            animationPlayer.Play("runright"); ;
                            gm.SendAnimationState("runright");
                        }
                    }
                    break;
                case "(0, 0)":
                    {
                        animationPlayer.Play("idle");
                        gm.SendAnimationState("idle");

                    }
                    break;
            }
            lastDirection = relativeDirection;
            animate = false;
        }

    }


    #endregion

    /**
   * ---------------------------------------------------------------------
   * This Method is for the Remote Player Animation.
   * isPlayer = false
   * Animations that have been received for specific remote characters 
   * are played and synced with the following methods.
   * ---------------------------------------------------------------------
   */
    #region
    public void AnimationSync(string nextanimation)
    {

        if (!isPlayer)
        {
            AnimationSyncing(nextanimation);
        }
        else
        {
            if (nextanimation == "die")
                this.deathAnimation = true;
            else if (nextanimation == "reload")
                this.reloadAnimation = true;
            else if (nextanimation == "wounded")
                this.woundAnimation = true;

        }

    }

    public async void AnimationSyncing(string value)
    {

        string currentAnimation = animationPlayer.CurrentAnimation;
  
        if (!dead)
        {
            switch (value)
            {
                case "die":
                    {
                        audioPlayer2.Play();
                        this.animationPlayer.Play("death");
                        this.animationPlayer.Seek(1);
                        dead = true;
                        await Task.Delay(gm.waitToRespawn * 1000);
                        respawn = true;
                    }
                    break;
                case "jump":
                    {
                        if (currentAnimation != "hit")
                        {
                            this.animationPlayer.Play("falling");
                        }

                    }
                    break;
                case "reload":
                    {
                        this.reloading = true;
                        if (aiming)
                        {
                            EndAimFunction();
                            wasaiming = true;
                        }
                        await Task.Delay(100);
                        this.animationPlayer.Play("reload");
                        this.animationPlayer.Seek(1);
                        audioPlayer3.Play(1f);
                        await Task.Delay(2000);

                        if (wasaiming)
                        {
                            StartAimFunction();
                            wasaiming = false;
                        }
                        await Task.Delay(100);
                        this.reloading = false;

                    }
                    break;
                case "wounded":
                    { 
                            await Task.Delay(200);
                            this.animationPlayer.Play("hit");
                            this.animationPlayer.Seek(0.8);
                            this.wounded = true;
                            audioPlayer2.Play();
                            await Task.Delay(600);
                            this.wounded = false;
                        }                  
                        break;
                   
                case "aiming":
                    {
                        if (!aiming)
                            StartAimFunction();
                    }
                    break;
                case "notaiming":
                    {
                        if (aiming)
                            EndAimFunction();
                    }
                    break;
                case "idle":
                    {
                        this.animationPlayer.Play("idle");
                    }
                    break;
                case "walkforward":
                    {
                        this.animationPlayer.Play("walkforward");
                    }
                    break;
                case "walkbackward":
                    {
                        this.animationPlayer.Play("walkbackward");
                    }
                    break;
                case "walkleft":
                    {
                        this.animationPlayer.Play("walkleft");
                    }
                    break;
                case "walkright":
                    {
                        this.animationPlayer.Play("walkright");
                    }
                    break;
                case "runforward":
                    {
                        this.animationPlayer.Play("runforward");
                    }
                    break;
                case "runbackward":
                    {
                        this.animationPlayer.Play("runbackward");
                    }
                    break;
                case "runleft":
                    {
                        this.animationPlayer.Play("runleft");
                    }
                    break;
                case "runright":
                    {
                        this.animationPlayer.Play("runright"); ;
                    }
                    break;
            }
        }
    }
    #endregion

    /**
   * ---------------------------------------------------------------------
   * The following Method is only called when Network Sync is set to Complex
   * Frames are interpolated and used depending on the Ping time from the Server.
   * ---------------------------------------------------------------------
   */
    #region

    public void SyncCharacter()
    {
        if (!isPlayer)
        {
            if (lastRequestTime > period)
            {
                lastRequestTime = 0;
                gm.TimeSyncRequest();
            }
            else
            {
                lastRequestTime += timeAtFrameStart;
            }
            if (statesCount == 0) return;

            double ping = gm.clientServerLag;
            if (ping < 40)
            {
                interpolationBackTime = 40;
            }
            else if (ping < 90)
            {
                interpolationBackTime = 90;
            }
            else if (ping < 150)
            {
                interpolationBackTime = 150;
            }
            else if (ping < 200)
            {
                interpolationBackTime = 200;
            }
            else
            {
                interpolationBackTime = 300;
            }
            Vector3 startpos = this.GlobalPosition;
            Vector3 startrot = this.GlobalRotation;

            double currentTime = NetworkTime;
            double interpolationTime = currentTime - interpolationBackTime;
            if (bufferedStates[0].TimeStamp > interpolationTime)
            {

                for (int i = 0; i < statesCount; i++)
                {
                    if (bufferedStates[i].TimeStamp <= interpolationTime || i == statesCount - 1)
                    {
                        rhs = bufferedStates[Mathf.Max(i - 1, 0)];
                        lhs = bufferedStates[i];
                        double length = rhs.TimeStamp - lhs.TimeStamp;
                        float t = 0.0F;
                        if (length > 0.0001)
                        {
                            t = (float)((interpolationTime - lhs.TimeStamp) / length);
                        }
                        else
                        {
                            t = (float)Mathf.Clamp(_elapsedTime / duration, 0.0f, 1.0f);
                        }

                        this.GlobalRotation = new Vector3(0, rhs.AngleRotation.Y, 0);

                        Vector3 targetPosition = rhs.Position;
                        float distance = GlobalTransform.Origin.DistanceTo(targetPosition);
                        if (distance > 0)
                        {
                            Vector3 direction = (targetPosition - GlobalTransform.Origin).Normalized();
                            GlobalTransform = new Transform3D(GlobalTransform.Basis, GlobalTransform.Origin + direction * Mathf.Min(distance, SPEED * t));

                        }

                        return;
                    }
                }
            }
        }

    }

    public double NetworkTime
    {
        get
        {
            return (timeAtFrameStart - gm.lastLocalTime) + gm.lastServerTime;
        }
    }

    #endregion

}
