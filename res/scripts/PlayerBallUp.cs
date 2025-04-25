using Godot;
using System;

public partial class PlayerBallUp : Player
{
	[Export]
	public float Speed = 350.0f;
	[Export]
	public float JumpVelocity = -500.0f;

	private AnimatedSprite2D animatedSprite;

	public override void _Ready(){
		animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("move_jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		float walkDirectionX = GetWalkDirectionX();

		if (walkDirectionX != 0)
		{
			velocity.X = walkDirectionX * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		Velocity = velocity;

		HandleBallUpMech();
		MoveAndSlide();
	}



	// Returns direction in x axis, -1/0/1.
	private float GetWalkDirectionX()
	{
		float directionX = Input.GetAxis("move_left", "move_right");
		bool walkSlowly = false;
		
		if (directionX == 0) {
			animatedSprite.Play("idle");
			return 0;
		} 

		if ( directionX < -.325 || .325 < directionX){ 
			// Normal walk speed.
			animatedSprite.Play("run");
		} else {
			// Slow walk speed.
			walkSlowly = true; 
			animatedSprite.Play("run_slow");
		}
		animatedSprite.FlipH = directionX < 0;

		directionX /= Mathf.Abs(directionX); // Any value from 0 to 1 will be pinned to 1, Mathf.Sign also works.
		
		// Apply slow walk speed.
		if (walkSlowly){ 
			directionX *= SlowWalkMultiplire; 
		}

		return directionX;
	}

	private void HandleBallUpMech(){
		if( Input.IsActionJustPressed("ball_up") ) {
			animatedSprite.Play("ball_up");
			GD.Print("Ball");
		} 
		else if ( Input.IsActionJustReleased("ball_up") ) {
			if ( Velocity.Length() > 10 ){
				animatedSprite.Play("idle");
			} else {
				animatedSprite.Play("run");
			}
			GD.Print("No Ball");
		}
	}
}
