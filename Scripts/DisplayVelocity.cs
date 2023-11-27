using Godot;
using System;
using System.Diagnostics.SymbolStore;

public partial class DisplayVelocity : Label
{
	private character_movement script;
	private Label parent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		script = GetNode<character_movement>("../..");
        parent = GetNode<Label>(".");
        
    }
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        parent.Text = script.velocity.ToString() + "\n" + script.jumps + "\n" + script.state;
	}
}