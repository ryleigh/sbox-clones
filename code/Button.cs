using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class Button : Component
{
	private GameObject _buttonModel;
	private List<Clone> _clonesPressing = new();
	private float _modelZPosUnpressed;
	private float _modelTargetZPos;
	public bool IsPressed { get; private set; }
	public Manager Manager { get; private set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		_buttonModel = this.FindChild( "model" );
		_modelZPosUnpressed = _modelTargetZPos = _buttonModel.Transform.LocalPosition.z;
		Manager = Scene.GetAllComponents<Manager>().FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		_buttonModel.Transform.LocalPosition = _buttonModel.Transform.LocalPosition.WithZ( Utils.DynamicEaseTo( _buttonModel.Transform.LocalPosition.z, _modelTargetZPos, 0.55f, Time.Delta) );
	}

	public void StartPressing(Clone clone)
	{
		if ( _clonesPressing.Contains( clone ) )
			return;

		_clonesPressing.Add( clone );
		RefreshPressing();
		
		Manager.ButtonPressed( this );
	}

	public void StopPressing( Clone clone )
	{
		if ( !_clonesPressing.Contains( clone ) )
			return;

		_clonesPressing.Remove( clone );
		RefreshPressing();
		
		Manager.ButtonReleased( this );
	}

	void RefreshPressing()
	{
		IsPressed = _clonesPressing.Count > 0;
		_modelTargetZPos = _modelZPosUnpressed + (IsPressed ? -1.5f : 0f);
		//_buttonModel.Transform.LocalPosition = _buttonModel.Transform.LocalPosition.WithZ( _modelZPosUnpressed + (IsPressed ? -1.5f : 0f) );
	}
}
