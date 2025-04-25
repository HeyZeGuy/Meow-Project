using Godot;
using System;
using System.Collections.Generic;

public partial class Pipe : StaticBody2D
{
	[Export]
	public Pipe Target;

	public List<Node2D> IgnoreBodies = [];

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void _on_action_area_body_entered(Node2D body)
	{
		if (body is CharacterBody2D && !IgnoreBodies.Contains(body)){
			Target.IgnoreBodies.Add(body);
			body.Position = Target.Position;
		}
	}

	public void _on_action_area_body_exited(Node2D body)
	{
		IgnoreBodies.Remove(body);
	}
}
