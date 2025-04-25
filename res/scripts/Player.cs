using Godot;
using System;
using System.Collections.Generic;

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
	public float FloorFriction = 250f;
	[Export]
	public float BallLeaveBounce = -250f;

	
	public const double SlowWalkRange = 0.325;
	public const float SlowWalkMultiplire = 0.5f;

	private PlayerAnimationManager playerAnimatedSprite;


	public override void _Ready(){
		playerAnimatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D") as PlayerAnimationManager; // Importing custom script.
	}

	public override void _Process(double delta)
	{
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
		float walkDirectionX = GetWalkDirectionX();

		if ( !IsOnFloor() ){
			velocity += GetGravity() * (float)delta;
			if (Velocity.Y > 0){
				velocity += GetGravity() * (float)delta / 2;
			}
		} 
		if ( Input.IsActionJustPressed("move_jump") && IsOnFloor() ){
			velocity.Y = JumpVelocity;
		}
		if ( Input.IsActionJustReleased("move_jump") && !IsOnFloor() && velocity.Y < 0 ){
			velocity.Y = JumpVelocity / 4;
		}
		if (walkDirectionX != 0 && IsOnFloor()) {
			velocity.X = walkDirectionX * Speed;
		}
		else if (walkDirectionX != 0 && !IsOnFloor()){
			velocity.X = Mathf.MoveToward(Velocity.X, Speed * walkDirectionX, InAirAcceleration);
		}
		else if (walkDirectionX == 0 && !IsOnFloor()){
			velocity.X = Mathf.MoveToward(Velocity.X, Speed * walkDirectionX / 4, InAirAcceleration / 4);
		}
		else if (walkDirectionX == 0 && IsOnFloor()){
			velocity.X = Mathf.MoveToward(Velocity.X, 0, FloorFriction);
		}

		Velocity = velocity;
	
		playerAnimatedSprite.TriggerAnimation(walkDirectionX, playerState);
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
			directionX *= SlowWalkMultiplire; 
		}

		return directionX;
	}
}
