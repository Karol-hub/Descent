using Godot;
using System;

public partial class free_cam : Node2D
{
    private Node2D self;
    private float speedy;
    private Vector2 direction;
    private float multiplier = 5f;
    private Camera2D cam;

    public override void _Ready()
    {
        self = GetNode<Node2D>(".");
    }
    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("dash"))
        {
            speedy = multiplier * (float)delta;
        }
        else
        {
            speedy = (float)delta;
        }
        direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        self.Position += direction * speedy *300f;
    }
}
