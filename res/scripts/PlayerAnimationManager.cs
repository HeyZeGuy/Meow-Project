using Godot;
using System;

public partial class PlayerAnimationManager : AnimatedSprite2D
{

	public void TriggerAnimation(float directionX, Player.PlayerState playerState)
	{
		if (playerState == Player.PlayerState.NORMAL)
		{

			if ( !GetParent<Player>().IsOnFloor() ){
				Play("jump");
			}
			else {
				if ( directionX == 0) {
					Play("idle");
				}
				else if ( Mathf.Abs(directionX) == 1 * Player.SlowWalkMultiplire ){ 
					// Slow walk speed.
					Play("run_slow");
				} else {
					// Normal walk speed.
					Play("run");
				}
				FlipH = directionX < 0;
			}
			
		}
	}
}
