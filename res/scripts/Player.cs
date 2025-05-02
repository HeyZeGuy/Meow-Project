using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = Godot.Vector2;

public partial class Player : CharacterBody2D
{
	public enum PlayerState
	{
		NORMAL,
		BALL,
		SLIDE
	};
	public PlayerState playerState = PlayerState.NORMAL;
	
	[Export]
	public float Speed = 250f;
	[Export]
	public float WallSlideSpeed = 100f;
	[Export]
	public Vector2 WallJumpForce = new Vector2(-750f, -300f);
	[Export]
	public float InAirAcceleration = 50f;
	[Export]
	public float JumpVelocity = -400f;
	[Export]
	public float MidAirStopMultiplier = 0.25f;
	[Export]
	public float AdditionalFallGravity = 0.5f;
	[Export]
	public float FloorFriction = 250f;
	[Export]
	public float BallLeaveBounce = -250f;
	[Export]
	public bool BallAbility = true;
	[Export]
	public bool WallJumpAbility = true;
	[Export]
	public bool WallSlideAbility = true;

	private bool IsBeingFlung = false;
	private float IsBeingFlungLaunchPeriod = 0f; // Wait this time before the player can control & 'IsBeingFlung' can be disabled - set this from the launching scene.
	
	private Vector2 FlingVelocity;
	private const double SlowWalkRange = 0.325;
	public const float SlowWalkMultiplier = 0.5f;

	private PlayerAnimationManager playerAnimatedSprite;

	public void _on_pipe_fling(Vector2 flingVelocity, float launchPeriod, Vector2 targetPos)
	{
		Position = targetPos;
		FlingVelocity = flingVelocity;
		Velocity = FlingVelocity;
		IsBeingFlung = true;
		IsBeingFlungLaunchPeriod = launchPeriod;
	}
	public override void _Ready()
	{
		playerAnimatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D") as PlayerAnimationManager; // Importing custom script.
	}

	public override void _Process(double delta)
	{
		if (BallAbility){
			if (Input.IsActionJustPressed("ball_up"))
			{
				playerState = PlayerState.BALL;
			} 
			else if (Input.IsActionJustReleased("ball_up"))
			{
				if ( IsOnFloor() )
				{
					Vector2 velocity = Velocity;
					velocity.Y = BallLeaveBounce;
					Velocity = velocity;
				}
				
				playerState = PlayerState.NORMAL;
			}
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		IsBeingFlungLaunchPeriod -= (float)delta;

		BasicMovement(delta);

		if (playerState == PlayerState.BALL)
		{
			BallMovementOverride(delta);
		}

		MoveAndSlide();
	}

	private void BasicMovement(double delta)
	{
		Vector2 velocity = Velocity;
		if(IsBeingFlungLaunchPeriod <=0)
		{
		velocity.X = HandleXMovement();
		velocity.Y = HandleYMovement(delta);
		}

		if(IsOnWall())
		{
			IsBeingFlung = false;

			float dir = Math.Sign(velocity.X);
			if(dir == 0) dir = Math.Sign(GetWallNormal().X);

			if(dir !=Math.Sign(GetWallNormal().X))
			{
				playerState = PlayerState.SLIDE;
				if(WallSlideAbility) velocity.Y = WallSlideSpeed;
			}
			else
			{
				playerState = PlayerState.NORMAL;
			}
			if(Input.IsActionJustPressed("move_jump"))
			{
				velocity = WallJumpForce;
				if(Math.Sign(GetWallNormal().X) > 0) velocity.X = -velocity.X ;
				GD.Print(velocity);
			}

		}
		Velocity = velocity;
	
	}

	private float HandleYMovement(double delta)
	{
		float velocityY = Velocity.Y;
		// Handle Gravity, when falling add more gravity
		if ( !IsOnFloor() )
		{
			velocityY += GetGravity().Y * (float)delta;
			if (velocityY > 0)
			{
				velocityY += GetGravity().Y * (float)delta * AdditionalFallGravity;
			}
		} 

		//Handle Jumps and jump stopping
		if ( Input.IsActionJustPressed("move_jump") && IsOnFloor() )
		{
			velocityY = JumpVelocity;
		}
		if ( Input.IsActionJustReleased("move_jump") && !IsOnFloor() && velocityY < 0 )
		{
			velocityY = JumpVelocity * MidAirStopMultiplier;
		}
		return velocityY;
	}

	private void BallMovementOverride(double delta)
	{	
			Vector2 velocity = Velocity;

			if ( Input.IsActionJustPressed("move_jump") && IsOnFloor() )
			{
				velocity.Y = JumpVelocity / 2;
			}

			Velocity = velocity;
	}


	// Returns direction in x axis, -1/0/1.
	private float GetWalkDirectionX()
	{
		float directionX = Input.GetAxis("move_left", "move_right");
		bool walkSlowly = false;
		
		if (directionX == 0)
		{
			return 0;
		} 

		if ( directionX > -SlowWalkRange && SlowWalkRange > directionX)
		{ 
			// Slow walk speed.
			walkSlowly = true; 
		} 

		directionX = Mathf.Sign(directionX); // Any value from 0 to 1 will be pinned to 1, Mathf.Sign also works.
		
		// Apply slow walk speed.
		if (walkSlowly)
		{ 
			directionX *= SlowWalkMultiplier; 
		}

		return directionX;
	}

	private float HandleXMovement(){
		float velocityX;
		
		float walkDirectionX = GetWalkDirectionX();
		
		// When on the ground, move at regular speed, when stopping add floor friction to stop
		if (IsOnFloor()){
			if (walkDirectionX != 0)
			{
				velocityX = walkDirectionX * Speed;
			}
			else
			{
				velocityX = Mathf.MoveToward(Velocity.X, 0, FloorFriction);
			}
			// Wait until in the air to disable flungness
			if ( IsBeingFlungLaunchPeriod <= 0 )
			{
				IsBeingFlung = false;
			}
		}
		// When in the air, accelerate, terminal velocity is the movement speed, when stopping, stop gracefully
		else {
			if (IsBeingFlung)
			{
				float dir = 0;
				if(walkDirectionX == 0) dir = Math.Sign(FlingVelocity.X);
				else dir = Math.Sign(walkDirectionX);
				velocityX = Mathf.MoveToward(Velocity.X, Math.Abs(FlingVelocity.X) * dir + Speed * walkDirectionX, InAirAcceleration * MidAirStopMultiplier);
			}
			else
			{
				velocityX = Mathf.MoveToward(Velocity.X, Speed * walkDirectionX, InAirAcceleration);
			}
		}

		playerAnimatedSprite.TriggerAnimation(walkDirectionX, playerState);
		return velocityX;
	}
	
}
