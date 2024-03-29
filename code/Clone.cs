using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime;

public sealed class Clone : Component, Component.ICollisionListener, Component.ITriggerListener
{
	public Manager Manager { get; set; }

	public Vector3 Velocity { get; set; }
	public Vector3 WishVelocity { get; private set; }

	[Property] public GameObject Body { get; set; }
	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	[Property] public GameObject BloodParticles { get; set; }
	//[Property] public GameObject BloodParticlesCone { get; set; }

	public float Height { get; set; } = 0.5f;


	public Rigidbody Rigidbody { get; private set; }
	public BoxCollider BoxCollider { get; private set; }

	private float _targetYaw;

	public bool IsGrounded { get; private set; }

	public TimeSince TimeSinceSpawn { get; set; }

	public bool IsConfused { get; private set; }

	private List<Button> _touchingButtons = new();

	public bool IsSpawning { get; private set; }
	private float _spawnTimer;
	private float _spawnAngleStart;
	public bool IsEnteringDoor { get; private set; }
	public Door DoorEntering { get; private set; }
	private float _enterDoorTimer;

	public SkinnedModelRenderer Renderer { get; private set; }

	private TimeSince _timeSinceGrounded;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Rigidbody = GameObject.Components.Get<Rigidbody>();
		BoxCollider = GameObject.Components.Get<BoxCollider>();
		Renderer = Body.Components.Get<SkinnedModelRenderer>();

		_targetYaw = -90f;
		Body.Transform.LocalRotation = Rotation.FromYaw( _targetYaw );

		TimeSinceSpawn = 0f;

		AnimationHelper.HeadWeight = 100f;
		AnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Walk;
		AnimationHelper.Height = Height;

		_timeSinceGrounded = 0f;
	}

	protected override void OnUpdate()
	{
		if(IsSpawning)
		{
			_spawnTimer += Time.Delta;
			float SPAWN_TIME = 0.3f;
			if ( _spawnTimer > SPAWN_TIME )
			{
				Transform.Scale = Vector3.One;
				Renderer.Tint = Renderer.Tint.WithAlpha( 1f );
				Body.Transform.LocalRotation = Rotation.FromYaw( -90f );
				IsSpawning = false;
			}
			else
			{
				float progress = Utils.Map( _spawnTimer, 0f, SPAWN_TIME, 0f, 1f );
				Transform.Scale = Vector3.One * Utils.Map( progress, 0f, 1f, 0.3f, 1f, EasingType.QuadOut );
				Renderer.Tint = Renderer.Tint.WithAlpha( Utils.Map( progress, 0f, 1f, 0f, 1f, EasingType.QuadOut ) );
				Body.Transform.LocalRotation = Rotation.FromYaw( Utils.Map( _spawnTimer, 0f, SPAWN_TIME, _spawnAngleStart, -90f) );
			}

			return;
		}
		else if ( IsEnteringDoor )
		{
			_enterDoorTimer += Time.Delta;
			float ENTER_TIME = 1.5f;
			if ( _enterDoorTimer > ENTER_TIME )
			{
				Manager.FinishLevel();
				return;
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

		if ( Velocity.Length > 0.5f)
			_targetYaw = Velocity.y < 0f ? -90f : 90f;

		Body.Transform.LocalRotation = Rotation.Lerp( Body.Transform.LocalRotation, Rotation.FromYaw( _targetYaw ), Time.Delta * 25f );

		// rotate facing camera instead of away
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

		//if ( Input.Pressed( "Jump" ) && IsGrounded && MathF.Abs( Rigidbody.PhysicsBody.Velocity.z ) < 0.3f && !Manager.IsCloneLeavingLevel )
		if ( Input.Pressed( "Jump" ) && _timeSinceGrounded < 0.075f && Rigidbody.PhysicsBody.Velocity.z < 0.3f && !Manager.IsCloneLeavingLevel )
		{
			Rigidbody.PhysicsBody.Velocity += Vector3.Up * 300f;
			OnJump();
		}

		AnimationHelper.WithVelocity( Velocity * 100f);

		if (Transform.Position.z < Manager.DeathHeight)
		{
			Die();
		}
	}

	public void OnJump()
	{
		AnimationHelper.TriggerJump();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsEnteringDoor || !Rigidbody.Enabled || (IsSpawning && _spawnTimer < 0.15f) )
			return;

		CheckGrounded();

		if ( IsGrounded && Rigidbody.PhysicsBody.Velocity.z < 0f )
			Rigidbody.PhysicsBody.Velocity = Rigidbody.PhysicsBody.Velocity.WithZ( 0f );

		BuildWishVelocity(28f);

		Velocity = Velocity.WithAcceleration( WishVelocity, 1.5f * Time.Delta );

		//if(!IsGrounded && Input.Down( "Jump" ) )
		//	Rigidbody.PhysicsBody.Velocity = Rigidbody.PhysicsBody.Velocity.WithZ( Rigidbody.PhysicsBody.Velocity.z + 200f * Time.Delta );

		var damping = (1f - Time.Delta * 8.5f);
		Velocity = new Vector3( Velocity.x * damping, Velocity.y * damping, Velocity.z );
		Rigidbody.PhysicsBody.Velocity = new Vector3( Rigidbody.PhysicsBody.Velocity.x * damping, Rigidbody.PhysicsBody.Velocity.y * damping, Rigidbody.PhysicsBody.Velocity.z );
		Rigidbody.PhysicsBody.ApplyImpulse( Velocity * 1600f );
	}

	void CheckGrounded()
	{
		// We're flying upwards too fast, never land on ground
		if ( !IsGrounded && Velocity.z > 50.0f )
			return;

		var startPos = Transform.Position;
		var endPos = Transform.Position + Vector3.Down * 1f;

		var tr = Scene.Trace.Ray( startPos, endPos ).Size( (BoxCollider.Scale * 0.99f).WithZ( 0.1f ) ).WithoutTags("trigger");
		tr = tr.IgnoreGameObject( GameObject );
		var r = tr.Run();

		//if(r.Hit)
		//	Log.Info( $"{r.Body.GetGameObject().Name}" );

		IsGrounded = r.Hit;

		if ( IsGrounded )
		{
			if(_timeSinceGrounded > 0.4f)
			{
				var thudSfx = Sound.Play( "thud", Transform.Position );
				thudSfx.Volume = Utils.Map( _timeSinceGrounded, 0.4f, 0f, 1f, 0.75f );
			}

			_timeSinceGrounded = 0f;
		}
	}

	public void SetConfused(bool confused)
	{
		IsConfused = confused;
		Renderer.Tint = IsConfused ? new Color(0.3f, 0.1f, 0.9f) : Color.White;

		Sound.Play( "curse", Transform.Position );
	}

	public void BuildWishVelocity(float speed)
	{
		WishVelocity = Vector3.Zero;

		float confusedFactor = IsConfused ? -1f : 1f;

		if( !Manager.IsCloneLeavingLevel )
		{
			if ( Input.Down( "Left" ) )
				WishVelocity += Vector3.Left * confusedFactor;

			if ( Input.Down( "Right" ) )
				WishVelocity += Vector3.Right * confusedFactor;
		}
		
		if ( !WishVelocity.IsNearZeroLength ) 
			WishVelocity = WishVelocity.Normal;

		WishVelocity *= speed;
	}

	public void EnterDoor( Door door )
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

		Sound.Play( "footsteps", Transform.Position );
	}

	public void OnTriggerEnter( Collider collider )
	{
		//Log.Info( $"Enter: {collider.GameObject.Name}, {collider.GameObject.Tags}" );

		if ( collider.GameObject.Tags.Has( "button" ) )
		{
			var button = collider.GameObject.Components.Get<Button>();
			_touchingButtons.Add( button );
			button.StartPressing( this );
		}
		else if ( collider.GameObject.Tags.Has( "confuser" ) )
		{
			if(!IsConfused)
			{
				var confuser = collider.GameObject.Components.Get<Confuser>();
				confuser.Consume( this );
			}
		}
		else if ( collider.GameObject.Tags.Has( "spike_block" ) )
		{
			TouchSpikeBlock( collider.GameObject, Transform.Position + (collider.Transform.Position - Transform.Position) * 0.5f );
		}
	}

	public void OnTriggerExit( Collider collider )
	{
		if ( collider.GameObject.Tags.Has( "button" ) )
		{
			var button = collider.GameObject.Components.Get<Button>();
			if ( _touchingButtons.Contains( button ) )
				_touchingButtons.Remove( button );

			button.StopPressing( this );
		}
	}

	public void Die()
	{
		foreach ( var button in _touchingButtons )
			button.StopPressing( this );

		BloodParticles.Clone( Transform.Position );

		Sound.Play( "squish", Transform.Position );

		Manager.CloneDied( this );
		GameObject.Destroy();
	}

	void TouchSpikeBlock(GameObject spikeBlockObj, Vector3 hitPos)
	{
		Die();

		//var bloodCone = BloodParticlesCone.Clone( hitPos );
		//bloodCone.Transform.Rotation = Rotation.LookAt( (hitPos - spikeBlockObj.Transform.Position).Normal );
	}

	public void OnCollisionStart( Collision collision )
	{
		if ( collision.Other.GameObject.Tags.Has( "spike_block" ) )
		{
			TouchSpikeBlock(collision.Other.GameObject, collision.Contact.Point);
		}
	}

	public void OnCollisionUpdate( Collision collision )
	{

	}

	public void OnCollisionStop( CollisionStop collision )
	{

	}

	public void StartSpawning()
	{
		_spawnTimer = 0f;
		IsSpawning = true;
		_spawnAngleStart = Game.Random.Float( -300f, -600f);
	}
}
