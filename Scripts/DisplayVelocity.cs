using Godot;
using System;
using System.Diagnostics.SymbolStore;

public partial class DisplayVelocity : Label
{
	private Vector2 vel;
	private character_movement script;
	private Label parent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		script = GetNode<character_movement>("/root");
        parent = GetNode<Label>("/root/Control/Velocity");
    }
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		script.velocity = vel;
		parent.Text = (vel.X+","+vel.Y);
	}
}
