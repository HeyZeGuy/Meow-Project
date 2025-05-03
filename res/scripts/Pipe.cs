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
	[Export]
	public float IsBeingFlungLaunchPeriod = .1f; // Wait this time before the player can control & 'IsBeingFlung' can be disabled.
	public float XAxisExtraStrength = -0f; // Might not be necessery.
	
	public List<Node2D> IgnoreBodies = [];

	[Signal]
	public delegate void FlingEventHandler(Vector2 flingVelocity, float launchPeriod, Vector2 targetPos);

	// Teleports player to the target pipe.
	async public void _on_action_area_body_entered(Node2D body)
	{
		if (body is Player && !IgnoreBodies.Contains(body)){
			Player player = body as Player;

			// Only balls can enter pipes
			// if ( player.playerState == Player.PlayerState.BALL )
			// {
				Target.IgnoreBodies.Add(body);
				
				Vector2 flingVelocity = new Vector2(0, -Target.ExitVelocity - (Target.XAxisExtraStrength * Mathf.Sin(Target.Rotation) )).Rotated(Target.Rotation);
				Vector2 targetPos = Target.Position + new Vector2(0, -Target.ExitSpawnDistance).Rotated(Target.Rotation);

				// Connect only the current player's handler to the signal to avoid both players teleporting
				Fling += player._on_fling;
				EmitSignal(SignalName.Fling, flingVelocity,IsBeingFlungLaunchPeriod, targetPos);
				Fling -= player._on_fling;
			
				await ToSignal(GetTree().CreateTimer(.25), SceneTreeTimer.SignalName.Timeout);

				Target.IgnoreBodies.Remove(body);
			// }
			
		}
	}

}
