using Godot;
using System;

public partial class room_maker : Area2D
{
	private Room_generator parent;
	public override void _Ready()
	{
		parent = GetNode<Room_generator>("..");
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	public void DoSomething()
	{

	}
}
