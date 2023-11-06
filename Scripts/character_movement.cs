using Godot;
using System;
using System.Diagnostics;

public partial class character_movement : CharacterBody2D
{
    //Constants
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    //Presets
    private Vector2 velocity;
    private Vector2 direction;
    private Sprite2D sprite;
    private bool facingLeft;
    //Character presets
    public float Speed = 300.0f;
    public float JumpVelocity = -400.0f;
    public float dashVelocity = 800f;
    public float decelerateRate = 20f;
    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("CharacterSprite");
    }
    public override void _Process(double delta)
    {
        PlayerInput();
        ManageSprite();
    }
    public override void _PhysicsProcess(double delta)
    {
        #region gravity
        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y += gravity * (float)delta;
        #endregion

        #region move_player
        if (direction != Vector2.Zero)
        {
            velocity.X = direction.X * Speed;
        }
        else
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
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }
        #endregion

        #region dash
        if (Input.IsActionJustPressed("dash"))
        {
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
            {
                GD.Print(direction.Normalized() * dashVelocity);
                velocity += (direction.Normalized() * dashVelocity);
                GD.Print(velocity);
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
}
