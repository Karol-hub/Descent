using Godot;
using System;
using System.ComponentModel.Design;
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
        dashEndLag,
        noResistance
    }
    private playerState state;
    private float frameDelta;
    private float time;
    //Character presets
    public float decelerateRate = 100f;
    public float noResDecelerateRate = 1f;
    //moving
    public float speed = 5000.0f;
    public float jumpVelocity = -400.0f;
    //bounce
    public float bounceVel = 50f;
    public float bounceDir = 80f; //in degrees
    public float bounceNoResLength = 0.1f; //in s
    //dash
    public float dashVelocity = 800f;
    public float dashLength = 0.1f; // in s
    public int endLagLength = 50; //in ms
    
    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("sprite");
        climbCollider = GetNode<Area2D>("climbBox");
    }
    public override void _Process(double delta)
    {
        frameDelta = (float)delta;
        direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        ManageState();
        ManageSprite();
        Movement();
        Velocity = velocity;
        GD.Print("acc Vel:"+Velocity);
        GD.Print(state);
        MoveAndSlide();
    }
    private void Movement()
    {
        #region gravity
        // Add the gravity.
        if (state == playerState.airBorn || state == playerState.noResistance)
            velocity.Y += gravity * frameDelta;
        #endregion

        #region speed_control
        if (state != playerState.dashing && state != playerState.noResistance)
        {
            velocity.X = (float)Mathf.MoveToward(velocity.X, 0f, decelerateRate) * frameDelta;
        }
        else if (state == playerState.noResistance)
        {
            velocity.X = (float)Mathf.MoveToward(velocity.X, 0f, noResDecelerateRate) * frameDelta;
        }
        #endregion

        #region move_player
        if (direction != Vector2.Zero && (state != playerState.dashing) && (state != playerState.noResistance))
        {
            velocity.X = direction.X * speed * frameDelta;
        }
        else if (state == playerState.noResistance)
        {
            velocity.X += direction.X * speed * frameDelta;
        }
        #endregion

        #region jumping
        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor() && (state != playerState.dashing))
        {
            velocity.Y = jumpVelocity;
        }
        #endregion

        #region dash
        if (Input.IsActionJustPressed("dash")) //init dash
        {
            time = 0f;
            state = playerState.dashing;
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
                state = playerState.noResistance;
                velocity.X *= -1f;
                velocity += BounceVel(bounceVel,bounceDir);
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

        #region which_dir_facing
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
            time += frameDelta;
            if (time >= dashLength)
            {
                EndDash();
            }
            return;
        }
        else if (state == playerState.noResistance)
        {
            if (IsOnFloor())
            {
                state = playerState.idle;
            }

            time += frameDelta;
            if (time >= bounceNoResLength)
            {
                state = playerState.idle;
            }
            return;
        }
        else if (!IsOnFloor())
        {
            state = playerState.airBorn;
            return;
        }
        else if (direction == Vector2.Zero && IsOnFloor())
        {
            state = playerState.idle;
            return;
        }
        else if (direction != Vector2.Zero && IsOnFloor())
        {
            state = playerState.walking;
            return;
        }

    }
    private void EndDash()
    {
        state = playerState.dashEndLag;
        velocity = Vector2.Zero;
        Task.Delay(endLagLength).ContinueWith(t => ToIdle());
    }
    private void ToIdle()
    {
        state = playerState.idle;
    }
    private Vector2 BounceVel(float mag, float dir)
    {
        if (facingLeft)
        {
            return new Vector2(mag * MathF.Sin(dir), -mag * MathF.Cos(dir));
        }
        else
        {
            return new Vector2(-mag * MathF.Sin(dir), -mag * MathF.Cos(dir));
        }
    }
}