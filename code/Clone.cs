using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;
using System;
using System.Drawing;
using System.Numerics;
using System.Runtime;

public sealed class Clone : Component, Component.ICollisionListener, Component.ITriggerListener
{
	public Manager Manager { get; set; }

	public Vector3 Gravity { get; set; } = new Vector3( 0, 0, 1000 );

	public Vector3 Velocity { get; set; }
	public Vector3 WishVelocity { get; private set; }

	[Property] public GameObject Body { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }

	public float Height { get; set; } = 0.5f;

	public Angles EyeAngles;
	public bool IsRunning;

	public Rigidbody Rigidbody { get; private set; }
	public BoxCollider BoxCollider { get; private set; }

	private float _targetYaw;

	public bool IsGrounded { get; private set; }
	public float StepHeight { get; set; } = 10.0f;
	public float GroundAngle { get; set; } = 45.0f;
	public BBox BoundingBox => BBox.FromPositionAndSize( BoxCollider.Center, BoxCollider.Scale );
	[Property] public TagSet IgnoreTags { get; set; } = new();

	public TimeSince TimeSinceSpawn { get; set; }

	public bool IsConfused { get; private set; }

	public bool IsEnteringDoor { get; private set; }
	public Door DoorEntering { get; private set; }
	private float _enterDoorTimer;

	public SkinnedModelRenderer Renderer { get; private set; }

	private float _lastZPos;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Rigidbody = GameObject.Components.Get<Rigidbody>();
		BoxCollider = GameObject.Components.Get<BoxCollider>();
		Renderer = Body.Components.Get<SkinnedModelRenderer>();

		_targetYaw = Game.Random.Int( 0, 1 ) == 0 ? -90f : 90f;

		TimeSinceSpawn = 0f;

		_lastZPos = Transform.Position.z;
		//SetConfused( Game.Random.Int( 0, 1 ) == 0 );
	}

	protected override void OnUpdate()
	{
		//var cc = GameObject.Components.Get<CharacterController>();
		//if ( cc is null ) return;

		//float rotateDifference = 0;

		//Transform.Scale = Utils.Map( TimeSinceSpawn, 0f, 0.25f, 0f, 1f, EasingType.ExpoOut );

		if ( IsEnteringDoor )
		{
			_enterDoorTimer += Time.Delta;
			float ENTER_TIME = 1f;
			if ( _enterDoorTimer > ENTER_TIME )
			{
				GameObject.Destroy();
			}
			else
			{
				float progress = Utils.Map( _enterDoorTimer, 0f, ENTER_TIME, 0f, 1f );
				float heightAdjust = Utils.Map( progress, 0f, 1f, 2f, 14f );
				Transform.Position = Vector3.Lerp( Transform.Position, DoorEntering.Transform.Position.WithX( 0f ) + Vector3.Up * heightAdjust, progress );
				Transform.Scale = Vector3.One * Utils.Map( progress, 0f, 1f, 1f, 0.1f );
				Renderer.Tint = Renderer.Tint.WithAlpha( Utils.Map( progress, 0f, 1f, 1f, 0f, EasingType.SineIn ) );
				Body.Transform.LocalRotation = Rotation.Lerp( Body.Transform.LocalRotation, Rotation.FromYaw( 0f ), 10f * Time.Delta);
			}

			return;
		}

		if ( Body is not null )
		{
			//float vel = cc.Velocity.Length;
			if ( Velocity.Length > 0.5f)
			{
				_targetYaw = Velocity.y < 0f ? -90f : 90f;
			}

			Body.Transform.LocalRotation = Rotation.Lerp( Body.Transform.LocalRotation, Rotation.FromYaw( _targetYaw ), Time.Delta * 25f );

			// ROTATE TOWARD CAMERA INSTEAD OF AWAY
			float yaw = Body.Transform.Rotation.Yaw();
			if ( yaw < 0f && yaw > -90f )
			{
				yaw = -90f + (-90f - yaw);
				Body.Transform.LocalRotation = Rotation.FromYaw( yaw );
			}
			else if(yaw > 0f && yaw < 90f)
			{
				yaw = 90f + (90f - yaw);
				Body.Transform.LocalRotation = Rotation.FromYaw( yaw );
			}
		}

		//Log.Info( $"Velocity.z: {Rigidbody.PhysicsBody.Velocity.z} _lastZPos: {_lastZPos} z: {Transform.Position.z}" );
		//if ( Input.Pressed( "Jump" ) && MathF.Abs( Rigidbody.PhysicsBody.Velocity.z ) < 0.1f ) // todo: you can jump at apex of jump, need proper IsGrounded check
		if ( Input.Pressed( "Jump" ) ) // todo: you can jump at apex of jump, need proper IsGrounded check
		{
			if( MathF.Abs( Rigidbody.PhysicsBody.Velocity.z ) < 1f )
			{
				Rigidbody.PhysicsBody.Velocity += Vector3.Up * 300f;

				OnJump();
			}
		}

		//Log.Info( $"zVel: {Rigidbody.PhysicsBody.Velocity.z}" );

		if ( AnimationHelper is not null )
		{
			//Vector3 lookDir = Input.Down( "Forward" ) ? Vector3.Up : (Input.Down( "Backward" ) ? Vector3.Down : Body.Transform.Rotation.Forward);

			AnimationHelper.HeadWeight = 100f;
			AnimationHelper.WithVelocity( Velocity * 100f);
			//AnimationHelper.WithWishVelocity( WishVelocity * 10f );
			//AnimationHelper.IsGrounded = cc.IsOnGround;
			//AnimationHelper.IsGrounded = MathF.Abs(Rigidbody.PhysicsBody.Velocity.z) < 1f;
			//AnimationHelper.FootShuffle = rotateDifference;
			//AnimationHelper.WithLook( lookDir );
			AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Walk;
			AnimationHelper.Height = Height;
			//AnimationHelper.DuckLevel = Input.Down( "Backward" ) ? 0.8f : 0f;
		}

		//cc.Height = Height * 100f;

		if(Transform.Position.z < -100f)
		{
			Manager.CloneDied( this );
			GameObject.Destroy();
		}

		_lastZPos = Transform.Position.z;
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
		if ( IsEnteringDoor || !Rigidbody.Enabled )
			return;

		BuildWishVelocity(28f);

		float dt = Time.Delta;

		Rigidbody.AngularDamping = 99999999f;
		Rigidbody.AngularVelocity = Vector3.Zero;
		//Rigidbody.LinearDamping = 4f;
		//Rigidbody.PhysicsBody.LinearDrag = 99999999f;
		//Rigidbody.ClearForces();

		//Transform.LocalRotation = Rotation.Identity;
		//Body.Transform.LocalRotation = Rotation.From( 0f, Body.Transform.LocalRotation.Yaw(), 0f );

		//Log.Info( $"PhysicsBody Vel: {rigidBody.PhysicsBody.Velocity}, Velocity: {Velocity}" );

		//var collider = GameObject.Components.Get<BoxCollider>();
		//collider.KeyframeBody.

		//Log.Info( $"{rigidBody.PhysicsBody.SpeculativeContactEnabled}" );
		//rigidBody.PhysicsBody.Move( Transform, dt );

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

		Velocity = Velocity.WithAcceleration( WishVelocity, 1.5f * Time.Delta );

		var damping = (1f - dt * 8.5f);
		Velocity = new Vector3( Velocity.x * damping, Velocity.y * damping, Velocity.z );
		Rigidbody.PhysicsBody.Velocity = new Vector3( Rigidbody.PhysicsBody.Velocity.x * damping, Rigidbody.PhysicsBody.Velocity.y * damping, Rigidbody.PhysicsBody.Velocity.z );

		//Velocity *= (1f - dt * 8f);
		//ApplyFriction( 2f );

		//rigidBody.Velocity = (Velocity * 50f).WithZ( rigidBody.Velocity.z );

		//Transform.Position += Velocity;
		Rigidbody.PhysicsBody.ApplyImpulse( Velocity * 1600f );
		//Rigidbody.PhysicsBody.ApplyForce( Velocity * 1000f );
		//Transform.Position = Transform.Position.WithX( 0f );
		//Log.Info( $"WishVelocity: {WishVelocity} Velocity: {Velocity}" );

		CategorizePosition();
	}

	void CategorizePosition()
	{
		var startPos = Transform.Position + Vector3.Down * 1f;
		var endPos = startPos + Vector3.Down * 2f;
		var wasOnGround = IsGrounded;

		//Gizmo.Draw.Line( startPos, endPos );

		// We're flying upwards too fast, never land on ground
		if ( !IsGrounded && Velocity.z > 50.0f )
		{
			IsGrounded = false;
			return;
		}

		// trace down one step height if we're already on the ground "step down". If not, search for floor right below us
		// because if we do StepHeight we'll snap that many units to the ground
		endPos.z -= wasOnGround ? StepHeight : 0.1f;

		SceneTraceResult tr = Scene.Trace.Ray( startPos, endPos ).Size( BoundingBox ).WithoutTags(IgnoreTags).Run();
		//SceneTraceResult tr = Scene.Trace.Ray( startPos, endPos ).WithoutTags( IgnoreTags ).Run();

		//if(tr.Hit)
		//	Log.Info( $"{tr.Body.GetGameObject().Name}" );
		//else
		//	Log.Info( $"none" );

		// we didn't hit - or the ground is too steep to be ground
		if ( !tr.Hit || Vector3.GetAngle( Vector3.Up, tr.Normal ) > GroundAngle )
		{
			IsGrounded = false;
			return;
		}

		// we are on ground
		IsGrounded = true;

		// move to this ground position, if we moved, and hit
		if ( wasOnGround && !tr.StartedSolid && tr.Fraction > 0.0f && tr.Fraction < 1.0f )
		{
			Transform.Position = tr.EndPosition + tr.Normal * 0.01f;
		}

		//Log.Info( $"{IsGrounded}" );
	}

	//PhysicsTraceBuilder BuildTrace( Vector3 from, Vector3 to ) => BuildTrace( Scene.PhysicsWorld.Trace.Ray( from, to ) );

	//PhysicsTraceBuilder BuildTrace( PhysicsTraceBuilder source ) => source.Size( BoundingBox ).WithoutTags( IgnoreTags );

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

	public void SetConfused(bool confused)
	{
		IsConfused = confused;

		Renderer.Tint = IsConfused ? new Color(0.3f, 0.1f, 0.9f) : Color.White;
	}

	public void BuildWishVelocity(float speed)
	{
		WishVelocity = 0;

		float confusedFactor = IsConfused ? -1f : 1f;

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
			WishVelocity += Vector3.Left * confusedFactor;
			EyeAngles = new Angles( WishVelocity );
		}

		if ( Input.Down( "Right" ) )
		{
			WishVelocity += Vector3.Right * confusedFactor;
			EyeAngles = new Angles( WishVelocity );
		}

		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		WishVelocity *= speed;
		//if ( Input.Down( "Run" ) ) WishVelocity *= 320.0f;
		//else WishVelocity *= 110.0f;
	}



	public void OnCollisionStart( Collision collision )
	{
		
	}

	public void OnCollisionUpdate( Collision collision )
	{
		//Log.Info( $"{collision.Other.Body.GetGameObject().Name}" );
	}

	public void OnCollisionStop( CollisionStop collision )
	{

	}

	public void OnTriggerEnter(Collider collider)
	{
		//Log.Info( $"Enter: {collider.GameObject.Name}, {collider.GameObject.Tags}" );

		if ( collider.GameObject.Tags.Has( "button" ) )
		{
			collider.GameObject.Components.Get<Button>().StartPressing( this );
		}

		if ( collider.GameObject.Tags.Has( "door" ) )
		{
			collider.GameObject.Components.Get<Door>().StartTouching( this );
		}

		//Log.Info( $"{collider.GameObject.Name}, {collider.GameObject.Tags}" );

		//if (collider.GameObject.Tags.Has("block"))
		//{
		//	var boxCollider = collider as BoxCollider;
		//	Log.Info( $"{boxCollider.Scale.z}" );
		//	if (boxCollider.Center.z < Transform.Position.z)
		//	{
		//		Transform.Position = Transform.Position.WithZ( boxCollider.Center.z + boxCollider.Scale.z * collider.Transform.Scale.z * 0.5f );
		//	}

		//	//other.GameObject.Components.Get<BoxCollider>().
		//}
	}

	public void OnTriggerExit( Collider collider )
	{
		//Log.Info( $"Exit: {collider.GameObject.Name}, {collider.GameObject.Tags}" );

		if ( collider.GameObject.Tags.Has( "button" ) )
		{
			collider.GameObject.Components.Get<Button>().StopPressing( this );
		}

		if ( collider.GameObject.Tags.Has( "door" ) )
		{
			collider.GameObject.Components.Get<Door>().StopTouching( this );
		}
	}

	public void EnterDoor(Door door)
	{
		if ( IsEnteringDoor )
			return;

		IsEnteringDoor = true;
		DoorEntering = door;
		//Components.Get<BoxCollider>().Enabled = false;
		Rigidbody.Enabled = false;
		_enterDoorTimer = 0f;
		AnimationHelper.WithVelocity( Vector2.Right * 40f * 100f );
		AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		Manager.RemoveClone( this );
	}
}
