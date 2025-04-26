using Godot;
using System;
using System.Collections.Generic;

public partial class Pipe : StaticBody2D
{
	[Export]
	public Pipe Target;
	[Export]
	public float ExitVelocity = 500.0f;
	[Export]
	public float ExitSpawnDistance = 25f;
	public float XAxisExtraStrength = -150f;
	
	public List<Node2D> IgnoreBodies = [];

	// Teleports player to the target pipe.
	async public void _on_action_area_body_entered(Node2D body)
	{
		if (body is Player && !IgnoreBodies.Contains(body)){
			Player Player = (Player)body;
			
			Target.IgnoreBodies.Add(body);
			
			Player.IsBeingFlung = true;
			Player.IsBeingFlungGracePeriod = .25f;
			Player.Velocity = new Vector2(0, -ExitVelocity - (XAxisExtraStrength * Mathf.Sin(Target.Rotation) )).Rotated(Target.Rotation);
			Player.Position = Target.Position + new Vector2(0, -ExitSpawnDistance).Rotated(Target.Rotation);

			await ToSignal(GetTree().CreateTimer(.25), SceneTreeTimer.SignalName.Timeout);

			Target.IgnoreBodies.Remove(body);
		}
	}

}
