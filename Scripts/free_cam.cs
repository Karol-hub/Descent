using Godot;
using System;

public partial class free_cam : Node2D
{
    private Node2D self;
    private float speedy;
    private Vector2 direction;
    private Vector2 zoom;
    private float multiplier = 5f;
    private Camera2D cam;
    private float zoomStep = 0.2f;

    public override void _Ready()
    {
        self = GetNode<Node2D>(".");
        cam = GetNode<Camera2D>("./Camera2D");
        zoom = cam.Zoom;
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
        if (Input.IsActionJustPressed("zoom_out"))
        {
            zoom += new Vector2(zoomStep,zoomStep);
        }
        if (Input.IsActionJustPressed("zoom_in"))
        {
            zoom -= new Vector2(zoomStep, zoomStep);
        }
        cam.Zoom = new Vector2(1 / zoom.X, 1 / zoom.Y);
        direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        self.Position += direction * speedy * (1/cam.Zoom.X) *300f;
    }
}
