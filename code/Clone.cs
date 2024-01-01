using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;
using System;
using System.Diagnostics;
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
	//[Property] public TagSet IgnoreTags { get; set; } = new();

	public TimeSince TimeSinceSpawn { get; set; }

	public bool IsConfused { get; private set; }

	public bool IsEnteringDoor { get; private set; }
	public Door DoorEntering { get; private set; }
	private float _enterDoorTimer;

	public SkinnedModelRenderer Renderer { get; private set; }

	//private float _lastZPos;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Rigidbody = GameObject.Components.Get<Rigidbody>();
		BoxCollider = GameObject.Components.Get<BoxCollider>();
		Renderer = Body.Components.Get<SkinnedModelRenderer>();

		_targetYaw = Game.Random.Int( 0, 1 ) == 0 ? -90f : 90f;

		TimeSinceSpawn = 0f;

		//_lastZPos = Transform.Position.z;
		//SetConfused( Game.Random.Int( 0, 1 ) == 0 );
	}

	protected override void OnUpdate()
	{
		//Log.Info( $"{Transform.Position}" );
		Gizmo.Draw.Color = Color.Yellow;
		//Gizmo.Draw.Text( $"test{1}", new Transform( new Vector3( 0f, -100f, 320f) ), "Poppins", 18 );

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
		if ( Input.Pressed( "Jump" ) && IsGrounded)//&& MathF.Abs( Rigidbody.PhysicsBody.Velocity.z ) < 0.01f )
		{
			Rigidbody.PhysicsBody.Velocity += Vector3.Up * 300f;
			OnJump();
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

		//_lastZPos = Transform.Position.z;

		//CategorizePosition();
		////Rigidbody.Gravity = !IsGrounded;
		//if ( IsGrounded && Rigidbody.PhysicsBody.Velocity.z < 0f )
		//	Rigidbody.PhysicsBody.Velocity = Rigidbody.PhysicsBody.Velocity.WithZ( 0f );
	}

	public void OnJump()
	{
		AnimationHelper?.TriggerJump();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsEnteringDoor || !Rigidbody.Enabled )
			return;

		CategorizePosition();
		//Rigidbody.Gravity = !IsGrounded;
		if ( IsGrounded && Rigidbody.PhysicsBody.Velocity.z < 0f )
			Rigidbody.PhysicsBody.Velocity = Rigidbody.PhysicsBody.Velocity.WithZ( 0f );

		BuildWishVelocity(28f);

		Rigidbody.AngularDamping = 99999999f;
		Rigidbody.AngularVelocity = Vector3.Zero;

		Velocity = Velocity.WithAcceleration( WishVelocity, 1.5f * Time.Delta );

		var damping = (1f - Time.Delta * 8.5f);
		Velocity = new Vector3( Velocity.x * damping, Velocity.y * damping, Velocity.z );
		Rigidbody.PhysicsBody.Velocity = new Vector3( Rigidbody.PhysicsBody.Velocity.x * damping, Rigidbody.PhysicsBody.Velocity.y * damping, Rigidbody.PhysicsBody.Velocity.z );
		Rigidbody.PhysicsBody.ApplyImpulse( Velocity * 1600f );
	}

	void CategorizePosition()
	{

		var startPos = Transform.Position;

		//startPos *= 0.7f;
		var endPos = Transform.Position + Vector3.Down * 1f;
		//var endPos = Vector3.Zero;
		//var wasOnGround = IsGrounded;

		//Gizmo.Draw.IgnoreDepth = true;
		//Gizmo.Draw.Line( startPos, endPos );

		//Gizmo.Draw.Line( Transform.Position + BoxCollider.Center, Transform.Position + BoxCollider.Center + Vector3.Up * 50f );

		//var boxCenter = Transform.Position + BoxCollider.Center;
		//var mins = boxCenter - BoxCollider.Scale * 0.5f;
		//var maxs = boxCenter + BoxCollider.Scale * 0.5f;

		//Gizmo.Draw.Color = Color.Red;
		//Gizmo.Draw.Line( mins, maxs );
		//Gizmo.Draw.LineBBox( new BBox( mins, maxs ) );

		// We're flying upwards too fast, never land on ground
		if ( !IsGrounded && Velocity.z > 50.0f )
		{
			IsGrounded = false;
			return;
		}

		//Log.Info( $"my guid: {GameObject.Id}" );
		// trace down one step height if we're already on the ground "step down". If not, search for floor right below us
		// because if we do StepHeight we'll snap that many units to the ground
		//endPos.z -= wasOnGround ? StepHeight : 0.1f;

		//var tr = Scene.Trace.Ray( startPos, endPos ).Size( BoundingBox ).WithoutTags(IgnoreTags);
		//var tr = Scene.Trace.Ray( startPos, endPos ).Size( BoxCollider.Scale * 0.5f);


		// does ignore self
		var tr = Scene.Trace.Ray( startPos, endPos ).Size( (BoxCollider.Scale * 0.99f).WithZ(0.1f) ).WithoutTags("trigger");
		tr = tr.IgnoreGameObject( GameObject );
		var r = tr.Run();

		//// DOESN'T ignore self
		//var tr2 = Scene.Trace.Ray( startPos, endPos ).Radius( 1f );
		//tr2.IgnoreGameObject( GameObject );
		//var r2 = tr2.Run();

		//// DOESN'T ignore self
		//var tr3 = Scene.Trace.Ray( startPos, endPos ).Size( BoxCollider.Scale * 0.5f );
		//tr3.IgnoreGameObject( GameObject );
		//var r3 = tr3.Run();

		//if ( r.Hit )
		//{
		//	Gizmo.Draw.Color = Color.Blue;
		//	Gizmo.Draw.SolidSphere( r.HitPosition, 3f );
		//	Log.Info( $"{r.Body.GetGameObject().Name}" );
		//}
		//else
		//{
		//	Log.Info( $"none" );
		//}

		Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"Test", new Transform( Transform.Position ) );

		// we didn't hit - or the ground is too steep to be ground
		//if ( !r.Hit || Vector3.GetAngle( Vector3.Up, r.Normal ) > GroundAngle )
		//if ( !r.Hit)
		//{
		//	IsGrounded = false;
		//	//Gizmo.Draw.Text( $"NOT on ground", new Transform( Transform.Position ) );
		//	return;
		//}

		// we are on ground
		IsGrounded = r.Hit;

		//// move to this ground position, if we moved, and hit
		//if ( wasOnGround && !r.StartedSolid && r.Fraction > 0.0f && r.Fraction < 1.0f )
		//{
		//	Transform.Position = r.EndPosition + r.Normal * 0.01f;
		//}

		//Gizmo.Draw.Text( $"{Rigidbody.PhysicsBody.Velocity.z}", new Transform( Transform.Position ) );

		//Log.Info( $"{IsGrounded}" );
	}

	//PhysicsTraceBuilder BuildTrace( Vector3 from, Vector3 to ) => BuildTrace( Scene.PhysicsWorld.Trace.Ray( from, to ) );

	//PhysicsTraceBuilder BuildTrace( PhysicsTraceBuilder source ) => source.Size( BoundingBox ).WithoutTags( IgnoreTags );

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
		Rigidbody.Enabled = false;
		_enterDoorTimer = 0f;
		AnimationHelper.WithVelocity( Vector2.Right * 40f * 100f );
		AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		Manager.RemoveClone( this );
	}
}
