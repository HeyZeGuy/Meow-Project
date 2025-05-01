using Godot;
using System;

public partial class PlayerAnimationManager : AnimatedSprite2D
{

	public void TriggerAnimation(float directionX, Player.PlayerState playerState)
	{
		FlipH = directionX < 0;

		if (playerState == Player.PlayerState.NORMAL)
		{

			if ( !GetParent<Player>().IsOnFloor() ){
				Play("jump");
			}
			else {
				if ( directionX == 0) {
					Play("idle");
				}
				else if ( Mathf.Abs(directionX) == 1 * Player.SlowWalkMultiplier ){ 
					// Slow walk speed.
					Play("run_slow");
				} else {
					// Normal walk speed.
					Play("run");
				}
			}
	
		} 
		else if (playerState == Player.PlayerState.BALL) 
		{	
			Play("ball_up");
		}
	}
}
