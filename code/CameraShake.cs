using Sandbox;
using System;

public sealed class CameraShake : Component
{
	private Vector3 _position;
	private float _shakeAmount;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		_position = Transform.Position;
	}

	public void Shake(float amount)
	{
		_shakeAmount = MathF.Max( amount, _shakeAmount );
	}

	protected override void OnUpdate()
	{
		if(_shakeAmount > 0f)
		{
			_shakeAmount = Utils.DynamicEaseTo( _shakeAmount, 0f, 0.25f, Time.Delta);
			if ( _shakeAmount < 0.01f )
				_shakeAmount = 0f;
		}

		Transform.Position = _position + Rotation.Random.Forward * _shakeAmount;
	}
}
