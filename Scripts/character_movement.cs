using Godot;
using System;

public partial class character_movement : CharacterBody2D
{
	//Constants
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	//Presets
    private Vector2 velocity;
    private Vector2 direction;
	private Vector2 facing;
    public override void _PhysicsProcess(double delta)
	{
        PlayerInput();
        // Add the gravity.
        if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0f, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void PlayerInput()
	{
        direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JumpVelocity;
    }
}
