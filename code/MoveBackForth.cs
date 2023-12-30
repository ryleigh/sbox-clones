using Sandbox;

public sealed class MoveBackForth : Component
{
	private TimeSince _timeSinceChange;
	private bool _left;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		_timeSinceChange = 0f;
	}

	protected override void OnUpdate()
	{
		if(_timeSinceChange > 3f)
		{
			_timeSinceChange = 0f;
			_left = !_left;
		}

		Transform.Position += (_left ? Vector3.Left : Vector3.Right) * 50f * Time.Delta;
	}
}
