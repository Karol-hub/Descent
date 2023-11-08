using Godot;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class character_movement : CharacterBody2D
{
    //Constants
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    //Presets
    private Vector2 velocity;
    private Vector2 direction;
    private Sprite2D sprite;
    private Area2D climbCollider;
    private bool facingLeft;
    private enum playerState
    {
        idle,
        walking,
        airBorn,
        dashing,
        dashEndLag
    }
    private playerState state;
    private float frameDelta;
    private float dashTime;
    //Character presets
    public float Speed = 300.0f;
    public float JumpVelocity = -400.0f;
    public float dashVelocity = 800f;
    public float bounceVel = -400f;
    public float bounceDir = 1.1f;
    public int dashLength = 100; // in ms
    public int endLagLength = 50; //in ms
    public float decelerateRate = 100f;
    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("sprite");
        climbCollider = GetNode<Area2D>("climbBox");
    }
    public override void _Process(double delta)
    {
        frameDelta = (float)delta;
        PlayerInput();
        ManageState();
        ManageSprite();
    }
    public override void _PhysicsProcess(double delta)
    {
        #region gravity
        // Add the gravity.
        if (state == playerState.airBorn)
            velocity.Y += gravity * (float)delta;
        #endregion

        #region move_player
        if (direction != Vector2.Zero && (state != playerState.dashing))
        {
            velocity.X = direction.X * Speed;
        }
        else if (state != playerState.dashing)
        {
            velocity.X = (float)Mathf.MoveToward(velocity.X, 0f, decelerateRate);
        }
        #endregion

        Velocity = velocity;
        //GD.Print("acc Vel:"+Velocity);
        MoveAndSlide();
    }
    private void PlayerInput()
    {
        direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        #region Moving
        //gets direction of player input
        if (direction.X < 0f)
        {
            facingLeft = true;
        }
        else if (direction.X > 0f)
        {
            facingLeft = false;
        }
        #endregion

        #region jumping
        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor() && (state != playerState.dashing))
        {
            velocity.Y = JumpVelocity;
        }
        #endregion

        #region dash
        if (Input.IsActionJustPressed("dash")) //init dash
        {
            dashTime = 0f;
            state = playerState.dashing;
            Task.Delay(dashLength).ContinueWith(t => ResetDash());
            // If player isn't moving
            if (direction == Vector2.Zero)
            {
                if (facingLeft) //dash left
                {
                    velocity -= new Vector2(1f,0f) * dashVelocity;
                }
                else //dash right
                {
                    velocity += new Vector2(1f, 0f) * dashVelocity;
                }
            }
            else // dash in whatever direction facing
            {
                velocity = (direction.Normalized() * dashVelocity);
            }
        }
        if (state == playerState.dashing) //when in dash
        {
            if (climbCollider.HasOverlappingBodies() && Input.IsActionPressed("jump")) //check for wallbounce
            {
                KillEndLag();
                velocity += BounceVel(bounceVel,bounceDir);
                GD.Print("xComponent:" + BounceVel(bounceVel, bounceDir).X);
                GD.Print("DashVel"+velocity);
            }
        }
        #endregion
    }
    private void ManageSprite()
    {
        #region sprite dir
        if (facingLeft)
        {
            sprite.Scale = new Vector2(1f, 1f);
            climbCollider.Scale = new Vector2(1f, 1f);
        }
        else
        {
            sprite.Scale = new Vector2(-1f, 1f);
            climbCollider.Scale = new Vector2(-1f, 1f);
        }
        #endregion
    }
    private void ManageState()
    {
        // Handle player state
        if (state == playerState.dashEndLag)
        {
            return;
        }
        else if (state == playerState.dashing)
        {
            dashTime += frameDelta;
            if (dashTime >= dashLength)
            {
                ResetDash();
            }
        }
        else if (!IsOnFloor())
        {
            state = playerState.airBorn;
        }
        else if (direction == Vector2.Zero && IsOnFloor())
        {
            state = playerState.idle;
        }
        else if (direction != Vector2.Zero && IsOnFloor())
        {
            state = playerState.walking;
        }
    }
    private void ResetDash()
    {
        state = playerState.dashEndLag;
        velocity = Vector2.Zero;
        Task.Delay(endLagLength).ContinueWith(t => KillEndLag());
    }
    private void KillEndLag()
    {
        state = playerState.idle;
    }
    private Vector2 BounceVel(float mag, float dir)
    {
        /// <summary>
        /// Essentially rotates vector by dir
        /// </summary>
        /// <param name="mag">magnitude of vector</param>
        /// <param name="dir">rotation to normal in radians to y axis</param>
        /// <returns>vector</returns>
        if (facingLeft)
        {
            return new Vector2(-mag * MathF.Sin(dir), mag * MathF.Cos(dir));
        }
        else
        {
            return new Vector2(mag * MathF.Sin(dir), mag * MathF.Cos(dir));
        }
        
    }
}