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
	// X axis speed related vars
	[Export] public float Speed = 250f;
	[Export] public float FloorFriction = 250f;
	[Export] public float InAirAcceleration = 40f;
	// Air movement related vars
	[Export] public float JumpVelocity = -400f;
	[Export] public float AdditionalFallGravity = 0.5f;
	[Export] public float MidAirSpeedMultiplier = .75f;
	[Export] public float JumpStopMultiplier = 0.25f;
	// Ability checks
	[Export] public bool BallAbility = true;
	[Export] public bool WallJumpAbility = true;
	[Export] public bool WallSlideAbility = true;
	// Ability Related vars
	[Export] public float BallBounceMultiplier = 0.5f;
	[Export] public float WallSlideSpeed = 100f;
	[Export] public Vector2 WallJumpMultiplier = new Vector2(1.5f, 0.6f); // The X value will be multiplied by Speed and the Y value by the JumpVelocity to determine the wall jump vector
	// Pipe fling related vars
	private bool IsBeingFlung = false;
	private float IsBeingFlungLaunchPeriod = 0f; // Wait this time before the player can control & 'IsBeingFlung' can be disabled - set this from the launching scene.
	private float LastFlingDir;
	// Slow walks
	private const double SlowWalkRange = 0.325;
	public const float SlowWalkMultiplier = 0.5f;

	private PlayerAnimationManager playerAnimatedSprite;

	public override void _Ready()
	{
		playerAnimatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D") as PlayerAnimationManager; // Importing custom script.
	}

	public override void _PhysicsProcess(double delta)
	{
		IsBeingFlungLaunchPeriod -= (float)delta; // Counting down timer.

		BaseMovement(delta);
		if (playerState == PlayerState.BALL) BallMovementOverride(delta);
		if (playerState == PlayerState.SLIDE) SlideMovementOverride();

		// After calculating physics for current frame, Use them to re-evaluate state for next frame. 
		playerState = EvaluatePlayerState();

		// Built-In function to move the player
		MoveAndSlide();
	}

	private void BaseMovement(double delta)
	{
		Vector2 velocity = Velocity;
		// Move regularly unless just flung
		if(IsBeingFlungLaunchPeriod <= 0)
		{
			velocity.X = HandleXMovement();
			velocity.Y = HandleYMovement(delta);
		}

		// Apply all previous changes (after override)
		Velocity = velocity;
	}

	private float HandleXMovement(){
		float velocityX;
		
		float walkDirectionX = GetWalkDirectionX();
		
		if (IsOnFloor()){
			stopFling();

			if (walkDirectionX != 0)
			{
				// When on the ground, move at regular speed, account for slow walk
				velocityX = walkDirectionX * Speed;
			}
			else
			{
				// decelerate to halt using floor friction 
				velocityX = Mathf.MoveToward(Velocity.X, 0, FloorFriction);
			}
				// disable flungness when hitting ground			
		}
		else {
			if (IsBeingFlung)
			{
				// When resisting a fling, stop flinging.
				velocityX = Velocity.X;
				if(walkDirectionX != LastFlingDir && walkDirectionX != 0)
				{
					stopFling();
					velocityX = Mathf.MoveToward(Velocity.X, Speed * walkDirectionX, InAirAcceleration);
				}
			}
			else
			{
				// When in the air, accelerate slowly to reach speed, If not walking, stop gracefully
				velocityX = Mathf.MoveToward(Velocity.X, Speed * walkDirectionX, InAirAcceleration);
			}

		}

		playerAnimatedSprite.TriggerAnimation(walkDirectionX, playerState);
		return velocityX;
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
			velocityY = JumpVelocity * JumpStopMultiplier;
		}
		return velocityY;
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


	// Override Base Movement when playerState==SLIDE.
	private void SlideMovementOverride()
	{	
		Vector2 velocity = Velocity;
		stopFling();

		float wallNormal = Math.Sign(GetWallNormal().X);
			
		velocity.Y = WallSlideSpeed;
			
		// Wall jump
		if(Input.IsActionJustPressed("move_jump"))
		{
			Vector2 WallJump = Vector2.Zero;
			WallJump.X = Speed * WallJumpMultiplier.X;
			WallJump.Y = JumpVelocity * WallJumpMultiplier.Y;
			if(wallNormal < 0) WallJump.X = -WallJump.X; // Flip Jump in case of opposite wall
			_on_fling(WallJump, 0.1f);
			return;
		}

		Velocity = velocity;
	}

	// Override Base Movement when playerState==BALL.
	private void BallMovementOverride(double delta)
	{	
			Vector2 velocity = Velocity;

			if ( Input.IsActionJustPressed("move_jump") && IsOnFloor() )
			{
				velocity.Y = JumpVelocity * BallBounceMultiplier;
			}
			// Little bounce when exiting BALL state.
			if (Input.IsActionJustReleased("ball_up") && IsOnFloor())
			{
				velocity.Y = JumpVelocity * BallBounceMultiplier;
				Velocity = velocity;
			}

			Velocity = velocity;
	}

	// Use the physics calculated this frame to re-evaluate state for next frame.*
	private PlayerState EvaluatePlayerState()
	{
		Vector2 velocity = Velocity;

		// Handling BALL state.
		if (BallAbility){
			if (Input.IsActionPressed("ball_up"))
			{
				return PlayerState.BALL;
			} 
			else if (Input.IsActionJustReleased("ball_up"))
			{
				return PlayerState.NORMAL;
			}
		}

		// Handling SLIDE state.
		if (WallSlideAbility && IsOnWall() && !IsOnFloor()){

			float dir = Math.Sign(velocity.X);
			float wallNormal = Math.Sign(GetWallNormal().X);
			bool isWallSliding = dir != wallNormal && dir != 0; // If player is moving towards a wall.

			// Balls can't slide off walls
			if(playerState != PlayerState.BALL)
			{	
				// Enable slide
				if(isWallSliding)
				{	
					// Wall jump resets state
					if(Input.IsActionJustPressed("move_jump"))
					{
						return PlayerState.NORMAL;
					} 
					return PlayerState.SLIDE;
				}
				// Disable slide
				else
				{
					return PlayerState.NORMAL;
				}
			}
		}
	
		return PlayerState.NORMAL;
	}


	// Handling piping mechanic
	public void _on_fling(Vector2 flingVelocity, float launchPeriod, Vector2 targetPos = new Vector2())
	{
		if(!targetPos.Equals(new Vector2())) Position = targetPos;
		LastFlingDir = Math.Sign(flingVelocity.X);
		Velocity = flingVelocity;
		IsBeingFlung = true;
		IsBeingFlungLaunchPeriod = launchPeriod;
	}

	public void stopFling()
	{
		IsBeingFlung = false;
	}
}
