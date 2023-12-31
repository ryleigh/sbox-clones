using Sandbox;
using Sandbox.Utility;
using System.Collections.Generic;
using System.Linq;

public sealed class Door : Component
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

	private List<Clone> _clonesTouching = new();

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Manager = Scene.GetAllComponents<Manager>().FirstOrDefault();

		_doorLeft = this.FindChild( "door_left" );
		_doorRight = this.FindChild( "door_right" );
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
	}

	public void StartTouching( Clone clone )
	{
		if ( _clonesTouching.Contains( clone ) )
			return;

		_clonesTouching.Add( clone );
		RefreshTouching();
	}

	public void StopTouching( Clone clone )
	{
		if ( !_clonesTouching.Contains( clone ) )
			return;

		_clonesTouching.Remove( clone );
		RefreshTouching();
	}

	public void SetOpen(bool open)
	{
		_doorLeftTargetPos = _doorLeftStartPos + (open ? Vector3.Left * 20f : Vector3.Zero);
		_doorLeftTargetRot = open ? Rotation.From(0f, 80f, 180f) : _doorLeftStartRot;

		_doorRightTargetPos = _doorRightStartPos + (open ? Vector3.Right * 20f : Vector3.Zero);
		_doorRightTargetRot = open ? Rotation.From( 0f, -80f, 0f) : _doorRightStartRot;

		IsOpen = open;

		RefreshTouching();
	}
	void RefreshTouching()
	{
		//Log.Info( $"RefreshTouching: {_clonesTouching.Count()}" );

		if ( !IsOpen )
			return;

		if(_clonesTouching.Count > 0 )
		{
			Manager.CloneEnteredDoor( _clonesTouching.FirstOrDefault(), this );
		}
	}
}
