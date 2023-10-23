using Godot;
using System;

public partial class character_movement : CharacterBody2D
{
	//Constants
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	//Presets
    private Vector2 velocity;
    private Vector2 direction;
	private bool facingLeft;
    private Sprite2D sprite = GetNode("CharacterSprite");
    //Character presets
    public float Speed = 300.0f;
    public float JumpVelocity = -400.0f;
    public float dashVelocity = 400f;
    public override void _Process(double delta)
    {
        PlayerInput();
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
			velocity.X = Mathf.MoveToward(Velocity.X, 0f, Speed);
		}
        #endregion

        Velocity = velocity;
		MoveAndSlide();
	}

	private void PlayerInput()
	{
        #region Moving
        //gets direction of player input
        direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (direction.X == -1f)
        {
            facingLeft = true;
        }
        else if (direction.X == 1f) 
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
    }
}
