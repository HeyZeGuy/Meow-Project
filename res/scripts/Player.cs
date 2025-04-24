using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 350.0f;
	[Export]
	public float JumpVelocity = -500.0f;
	[Export]
	public float SlowWalkMultiplire = 0.5f;

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
		MoveAndSlide();
	}



	// Returns direction in x axis, -1/0/1.
	private float GetWalkDirectionX()
	{
		float directionX = Input.GetAxis("move_left", "move_right");
		bool walkSlowly = false;
		
		if (directionX != 0) { 
			if ( directionX < -.325 || .325 < directionX){ 
				// Normal walk speed.
			} else {
				// Slow walk speed.
				walkSlowly = true; 
			}

			directionX /= Mathf.Abs(directionX); // Any value from 0 to 1 will be pinned to 1.
			
			// Apply slow walk speed.
			if (walkSlowly){ 
				directionX *= SlowWalkMultiplire; 
			}
		}

		return directionX;
	}
}
