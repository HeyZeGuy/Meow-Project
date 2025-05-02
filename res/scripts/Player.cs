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
	[Export] public float MidAirStopMultiplier = 0.25f;
	// Ability checks
	[Export] public bool BallAbility = true;
	[Export] public bool WallJumpAbility = true;
	[Export] public bool WallSlideAbility = true;
	// Ability Related vars
	[Export] public float BallBounceMultiplier = 0.5f;
	[Export] public float WallSlideSpeed = 100f;
	[Export] public Vector2 WallJumpMultiplier = new Vector2(2, 0.75f);
	// Pipe fling related vars
	private bool IsBeingFlung = false;
	private float IsBeingFlungLaunchPeriod = 0f; // Wait this time before the player can control & 'IsBeingFlung' can be disabled - set this from the launching scene.
	private Vector2 FlingVelocity;
	// Slow walks
	private const double SlowWalkRange = 0.325;
	public const float SlowWalkMultiplier = 0.5f;

	private PlayerAnimationManager playerAnimatedSprite;

	// Handling piping mechanic
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
		// Activate and disenagage balling mechanic
		if (BallAbility){
			if (Input.IsActionPressed("ball_up"))
			{
				playerState = PlayerState.BALL;
			} 
			else if (Input.IsActionJustReleased("ball_up"))
			{
				if ( IsOnFloor() )
				{
					Vector2 velocity = Velocity;
					velocity.Y = JumpVelocity * BallBounceMultiplier;
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
		// Built-In function to move the player
		MoveAndSlide();
	}

	private void BasicMovement(double delta)
	{
		Vector2 velocity = Velocity;
		// Move regularly unless just flung
		if(IsBeingFlungLaunchPeriod <=0)
		{
		velocity.X = HandleXMovement();
		velocity.Y = HandleYMovement(delta);
		}

		// Handle Wall jump and slide movement overrides
		if(IsOnWall())
		{
			IsBeingFlung = false;

			float dir = Math.Sign(velocity.X);
			if(dir == 0) dir = Math.Sign(GetWallNormal().X);
			
			// Balls can't slide off walls
			if(playerState != PlayerState.BALL)
			{
				if(dir !=Math.Sign(GetWallNormal().X) && WallSlideAbility)
				{
					playerState = PlayerState.SLIDE;
					velocity.Y = WallSlideSpeed;
				}
				else
				{
					playerState = PlayerState.NORMAL;
				}
				if(Input.IsActionJustPressed("move_jump"))
				{
					velocity.X = Speed * WallJumpMultiplier.X;
					velocity.Y = JumpVelocity * WallJumpMultiplier.Y;
					if(Math.Sign(GetWallNormal().X) < 0) velocity.X = -velocity.X; // Flip Jump in case of opposite wall
				}
			}


		}
		// Apply all previous changes (after override)
		Velocity = velocity;
	
	}

	private float HandleXMovement(){
		float velocityX;
		
		float walkDirectionX = GetWalkDirectionX();
		
		if (IsOnFloor()){
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
			if ( IsBeingFlungLaunchPeriod <= 0 )
			{
				// Wait until in the air before disabling flungness for no reason
				IsBeingFlung = false;
			}
		}
		else {
			if (IsBeingFlung)
			{
				// Allow for faster launches without forcing player to regular speed
				float dir = 0;
				if(walkDirectionX == 0) dir = Math.Sign(FlingVelocity.X);
				else dir = Math.Sign(walkDirectionX);
				velocityX = Mathf.MoveToward(Velocity.X, Math.Abs(FlingVelocity.X) * dir + Speed * walkDirectionX, InAirAcceleration * MidAirStopMultiplier);
			}
			else
			{
				// When in the air, accelerate slowly to reach speed
				velocityX = Mathf.MoveToward(Velocity.X, Speed * walkDirectionX * MidAirSpeedMultiplier, InAirAcceleration);
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
			velocityY = JumpVelocity * MidAirStopMultiplier;
		}
		return velocityY;
	}

	private void BallMovementOverride(double delta)
	{	
			Vector2 velocity = Velocity;

			if ( Input.IsActionJustPressed("move_jump") && IsOnFloor() )
			{
				velocity.Y = JumpVelocity * BallBounceMultiplier;
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
}
