using Godot;
using System;

public partial class start : TextureButton
{
    public Node simultaneousScene;
    private bool pressed = false;
    private void _pressed()
    {
        if (!pressed)
        {
            pressed = true;
            simultaneousScene = ResourceLoader.Load<PackedScene>("res://Scenes/generation_test.tscn").Instantiate();
            GetTree().Root.AddChild(simultaneousScene);
            simultaneousScene.GetChild<Node2D>(1).GetChild<Camera2D>(0).MakeCurrent();
            GetNode("/root/main_menue").QueueFree();
        }
    }
}