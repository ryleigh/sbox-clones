using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;
using System.Drawing;
using System.Runtime;

public sealed class Clone : Component
{
	public Manager Manager { get; set; }

	public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 1000 );

	public Vector3 Velocity { get; set; }
	public Vector3 WishVelocity { get; private set; }

	[Property] public GameObject Body { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }

	public float Height { get; set; } = 0.6f;

	public Angles EyeAngles;
	public bool IsRunning;

	protected override void OnEnabled()
	{
		base.OnEnabled();

	}

	protected override void OnUpdate()
	{
		//var cc = GameObject.Components.Get<CharacterController>();
		//if ( cc is null ) return;

		float rotateDifference = 0;

		if ( Body is not null )
		{
			//float vel = cc.Velocity.Length;
			if ( Velocity.Length > 1f)
			{
				Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, Rotation.FromYaw( Velocity.y < 0f ? -90f : 90f ), Time.Delta * 15f );
			}
		}

		if ( AnimationHelper is not null )
		{
			//Vector3 lookDir = Input.Down( "Forward" ) ? Vector3.Up : (Input.Down( "Backward" ) ? Vector3.Down : Body.Transform.Rotation.Forward);

			AnimationHelper.WithVelocity( Velocity * 100f);
			AnimationHelper.WithWishVelocity( WishVelocity * 10f );
			//AnimationHelper.IsGrounded = cc.IsOnGround;
			AnimationHelper.IsGrounded = true;
			AnimationHelper.FootShuffle = rotateDifference;
			//AnimationHelper.WithLook( lookDir );
			AnimationHelper.MoveStyle = IsRunning ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
			AnimationHelper.Height = Height;
			//AnimationHelper.DuckLevel = Input.Down( "Backward" ) ? 0.8f : 0f;
		}

		//cc.Height = Height * 100f;

		if(Transform.Position.z < -100f)
		{
			Manager.CloneDied( this );
			GameObject.Destroy();
		}
	}

	public void OnJump()
	{
		AnimationHelper?.TriggerJump();
	}

	//public static void PreStep() // Lock the Y axis
	//{
	//	if ( Game.IsClient ) return;

	//	foreach ( var entity in AxisLockedEntities.Where( entity => entity.PhysicsBody.IsValid() )
	//				 .Where( entity => !entity.PhysicsBody.Sleeping ) )
	//	{
	//		entity.Position = entity.Position.WithY( 0f );
	//		entity.Rotation = Rotation.FromAxis( Vector3.Right, entity.Rotation.Roll() )
	//			.RotateAroundAxis( Vector3.Up, -90f );
	//		entity.AngularVelocity = entity.AngularVelocity.WithRoll( 0 );
	//		entity.PhysicsBody.AngularDrag = 10f;
	//	}
	//}

	protected override void OnFixedUpdate()
	{
		BuildWishVelocity(20f);

		float dt = Time.Delta;

		var rigidBody = GameObject.Components.Get<Rigidbody>();
		rigidBody.AngularDamping = 99999999f;
		rigidBody.AngularVelocity = Vector3.Zero;
		//rigidBody.ClearForces();

		Transform.LocalRotation = Rotation.Identity;
		Body.Transform.LocalRotation = Rotation.From( 0f, Body.Transform.LocalRotation.Yaw(), 0f );

		//var cc = GameObject.Components.Get<CharacterController>();
		//if ( cc == null )
		//	return;

		//if ( cc.IsOnGround && Input.Pressed( "Jump" ) )
		//{
		//	cc.Punch( Vector3.Up * 300f );
		//	cc.IsOnGround = false;

		//	OnJump();
		//}

		//if ( cc.IsOnGround )
		//{
		//	cc.Velocity = cc.Velocity.WithZ( 0 );
		//	cc.Accelerate( WishVelocity );
		//	cc.ApplyFriction( 4.0f );
		//}
		//else
		//{
		//	cc.Velocity -= Gravity * Time.Delta * 0.5f;
		//	cc.Accelerate( WishVelocity );
		//	//cc.Accelerate( WishVelocity.ClampLength( 50 ) );
		//	cc.ApplyFriction( 0.1f );
		//}

		//cc.Move();

		//if ( !cc.IsOnGround )
		//{
		//	cc.Velocity -= Gravity * Time.Delta * 0.5f;
		//}
		//else
		//{
		//	cc.Velocity = cc.Velocity.WithZ( 0 );
		//}

		Accelerate( WishVelocity, 1.5f );
		Velocity *= (1f - dt * 8f);
		//ApplyFriction( 2f );

		//rigidBody.Velocity = (Velocity * 50f).WithZ( rigidBody.Velocity.z );

		Transform.Position += Velocity;
		Transform.Position = Transform.Position.WithX( 0f );
		//Log.Info( $"WishVelocity: {WishVelocity} Velocity: {Velocity}" );
	}

	public void Accelerate( Vector3 vector, float speed )
	{
		Velocity = Velocity.WithAcceleration( vector, speed * Time.Delta );
	}

	//public void ApplyFriction( float frictionAmount, float stopSpeed = 140.0f )
	//{
	//	var speed = Velocity.Length;
	//	if ( speed < 0.01f ) return;

	//	// Bleed off some speed, but if we have less than the bleed
	//	//  threshold, bleed the threshold amount.
	//	float control = (speed < stopSpeed) ? stopSpeed : speed;

	//	// Add the amount to the drop amount.
	//	var drop = control * Time.Delta * frictionAmount;

	//	// scale the velocity
	//	float newspeed = speed - drop;
	//	if ( newspeed < 0 ) newspeed = 0;
	//	if ( newspeed == speed ) return;

	//	newspeed /= speed;
	//	Velocity *= newspeed;
	//}

	public void BuildWishVelocity(float speed)
	{
		WishVelocity = 0;

		if ( Input.Down( "Forward" ) )
		{
			WishVelocity += Vector3.Zero;
		}

		if ( Input.Down( "Backward" ) )
		{
			WishVelocity += Vector3.Zero;
		}

		if ( Input.Down( "Left" ) )
		{
			WishVelocity += Vector3.Left;
			EyeAngles = new Angles( WishVelocity );
		}

		if ( Input.Down( "Right" ) )
		{
			WishVelocity += Vector3.Right;
			EyeAngles = new Angles( WishVelocity );
		}

		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		WishVelocity *= speed;
		//if ( Input.Down( "Run" ) ) WishVelocity *= 320.0f;
		//else WishVelocity *= 110.0f;
	}
}
