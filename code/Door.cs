using Sandbox;
using Sandbox.Utility;
using System.Collections.Generic;
using System.Linq;

public sealed class Door : Component, Component.ITriggerListener
{
	public Manager Manager { get; private set; }

	private GameObject _doorLeft;
	private GameObject _doorRight;

	private Vector3 _doorLeftStartPos;
	private Vector3 _doorRightStartPos;
	private Rotation _doorLeftStartRot;
	private Rotation _doorRightStartRot;

	private Vector3 _doorLeftTargetPos;
	private Vector3 _doorRightTargetPos;
	private Rotation _doorLeftTargetRot;
	private Rotation _doorRightTargetRot;

	public bool IsOpen { get; private set; }

	private bool _isCloneLeaving;
	private Clone _cloneLeaving;

	public BoxCollider BoxCollider { get; private set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Manager = Scene.GetAllComponents<Manager>().FirstOrDefault();
		BoxCollider = GameObject.Components.Get<BoxCollider>();

		_doorLeft = GameObject.Children.ToList().FirstOrDefault().Children.Where( x => x.Name == "door_left" ).FirstOrDefault();
		_doorRight = GameObject.Children.ToList().FirstOrDefault().Children.Where( x => x.Name == "door_right" ).FirstOrDefault();
		_doorLeftStartPos = _doorLeftTargetPos = _doorLeft.Transform.LocalPosition;
		_doorRightStartPos = _doorRightTargetPos = _doorRight.Transform.LocalPosition;
		_doorLeftStartRot = _doorLeftTargetRot = Rotation.From(0f, 0f, 180f);
		_doorRightStartRot = _doorRightTargetRot = Rotation.From( 0f, 0f, 0f );
	}

	protected override void OnUpdate()
	{
		float lerpSpeed = 15f * Time.Delta;

		_doorLeft.Transform.LocalPosition = Vector3.Lerp( _doorLeft.Transform.LocalPosition, _doorLeftTargetPos, lerpSpeed );
		_doorRight.Transform.LocalPosition = Vector3.Lerp( _doorRight.Transform.LocalPosition, _doorRightTargetPos, lerpSpeed );

		_doorLeft.Transform.LocalRotation = Rotation.Lerp( _doorLeft.Transform.LocalRotation, _doorLeftTargetRot, lerpSpeed );
		_doorRight.Transform.LocalRotation = Rotation.Lerp( _doorRight.Transform.LocalRotation, _doorRightTargetRot, lerpSpeed );

		if ( _isCloneLeaving )
		{
			if ( !_cloneLeaving.IsValid() )
			{
				_isCloneLeaving = false;
				SetOpen( Manager.ShouldDoorBeOpen() );
			}
		}
	}

	public void SetOpen(bool open)
	{
		if ( IsOpen == open || _isCloneLeaving )
			return;

		_doorLeftTargetPos = _doorLeftStartPos + (open ? Vector3.Left * 20f : Vector3.Zero);
		_doorLeftTargetRot = open ? Rotation.From(0f, 80f, 180f) : _doorLeftStartRot;

		_doorRightTargetPos = _doorRightStartPos + (open ? Vector3.Right * 20f : Vector3.Zero);
		_doorRightTargetRot = open ? Rotation.From( 0f, -80f, 0f) : _doorRightStartRot;

		IsOpen = open;

		RefreshTouching();
	}

	void RefreshTouching()
	{
		if ( !IsOpen || _isCloneLeaving )
			return;

		if(BoxCollider.Touching.Count() > 0 )
		{
			var cloneCollider = BoxCollider.Touching.Where( x => x.Tags.Has( "clone" )).ToList().FirstOrDefault();
			if(cloneCollider != null)
			{
				_cloneLeaving = cloneCollider.Components.Get<Clone>();
				Manager.CloneEnteredDoor( _cloneLeaving, this );
				_isCloneLeaving = true;
			}
		}
	}

	public void OnTriggerEnter( Collider collider )
	{
		if ( collider.GameObject.Tags.Has( "clone" ) )
		{
			RefreshTouching();
		}
	}

	public void OnTriggerExit( Collider collider )
	{

	}
}
