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
    //Character presets
    public float Speed = 300.0f;
    public float JumpVelocity = -400.0f;
    public float dashVelocity = 800f;
    public int dashLength = 100; // in ms
    public int endLagLength = 50; //in ms
    public float decelerateRate = 100f;
    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("sprite");
    }
    public override void _Process(double delta)
    {
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
        if (Input.IsActionJustPressed("dash"))
        {
            state = playerState.dashing;
            Task.Delay(dashLength).ContinueWith(t => resetDash());
            // If player isn't moving
            if (direction == Vector2.Zero)
            {
                //dash left
                if (facingLeft)
                {
                    velocity -= new Vector2(1f,0f) * dashVelocity;
                }
                //dash right
                else
                {
                    velocity += new Vector2(1f, 0f) * dashVelocity;
                }
            }
            else
            // dash in whatever direction facing
            {
                velocity = (direction.Normalized() * dashVelocity);
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
        }
        else
        {
            sprite.Scale = new Vector2(-1f, 1f);
        }
        #endregion
    }
    private void resetDash()
    {
        state = playerState.dashEndLag;
        velocity = Vector2.Zero;
        Task.Delay(endLagLength).ContinueWith(t => killEndLag());
    }
    private void killEndLag()
    {
        state = playerState.idle;
    }
    private void ManageState()
    {
        // Handle player state
        if (state == playerState.dashing || state == playerState.dashEndLag)
        {
            return;
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
}