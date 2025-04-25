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
		BALL
	};
	public PlayerState playerState = PlayerState.NORMAL;
	
	[Export]
	public float Speed = 250f;
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
	
	public const double SlowWalkRange = 0.325;
	public const float SlowWalkMultiplier = 0.5f;

	private PlayerAnimationManager playerAnimatedSprite;


	public override void _Ready(){
		playerAnimatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D") as PlayerAnimationManager; // Importing custom script.
	}

	public override void _Process(double delta)
	{
		if (BallAbility){
			if (Input.IsActionJustPressed("ball_up")){
				playerState = PlayerState.BALL;
			} 
			else if (Input.IsActionJustReleased("ball_up")){
				if ( IsOnFloor() ){
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
		BasicMovement(delta);

		if (playerState == PlayerState.BALL){
			BallMovementOverride(delta);
		}

		MoveAndSlide();
	}

	private void BasicMovement(double delta)
	{
		Vector2 velocity = Velocity;
		
		velocity.X = HandleXMovement();
		velocity.Y = HandleYMovement(delta);

		Velocity = velocity;
	
	}

	private float HandleYMovement(double delta)
	{
		float velocityY = Velocity.Y;
		// Handle Gravity, when falling add more gravity
		if ( !IsOnFloor() ){
			velocityY += GetGravity().Y * (float)delta;
			if (velocityY > 0){
				velocityY += GetGravity().Y * (float)delta * AdditionalFallGravity;
			}
		} 

		//Handle Jumps and jump stopping
		if ( Input.IsActionJustPressed("move_jump") && IsOnFloor() ){
			velocityY = JumpVelocity;
		}
		if ( Input.IsActionJustReleased("move_jump") && !IsOnFloor() && velocityY < 0 ){
			velocityY = JumpVelocity * MidAirStopMultiplier;
		}
		return velocityY;
	}

	private void BallMovementOverride(double delta)
	{
		Vector2 velocity = Velocity;

		if ( Input.IsActionJustPressed("move_jump") && IsOnFloor() ){
			velocity.Y = JumpVelocity / 2;
		}

		Velocity = velocity;
	}


	// Returns direction in x axis, -1/0/1.
	private float GetWalkDirectionX()
	{
		float directionX = Input.GetAxis("move_left", "move_right");
		bool walkSlowly = false;
		
		if (directionX == 0) {
			return 0;
		} 

		if ( directionX > -SlowWalkRange && SlowWalkRange > directionX){ 
			// Slow walk speed.
			walkSlowly = true; 
		} 

		directionX /= Mathf.Abs(directionX); // Any value from 0 to 1 will be pinned to 1, Mathf.Sign also works.
		
		// Apply slow walk speed.
		if (walkSlowly){ 
			directionX *= SlowWalkMultiplier; 
		}

		return directionX;
	}

	private float HandleXMovement(){
		float velocityX;
		
		float walkDirectionX = GetWalkDirectionX();
		
		// When on the ground, move at regular speed, when stopping add floor friction to stop
		if (IsOnFloor()){
			if (walkDirectionX != 0) {
				velocityX = walkDirectionX * Speed;
			}
			else{
				velocityX = Mathf.MoveToward(Velocity.X, 0, FloorFriction);
			}
			
		}
		// When in the air, accelerate, terminal velocity is the movement speed, when stopping, stop gracefully
		else {
			if (walkDirectionX != 0) {
				velocityX = Mathf.MoveToward(Velocity.X, Speed * walkDirectionX, InAirAcceleration);
			}
			else {
				velocityX = Mathf.MoveToward(Velocity.X, 0, InAirAcceleration * MidAirStopMultiplier);
			}
		}

		playerAnimatedSprite.TriggerAnimation(walkDirectionX, playerState);
		return velocityX;
	}
}
