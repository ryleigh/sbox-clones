using Sandbox;

public sealed class WoodenBox : Component
{
	private bool _hasPlayedSfx;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if(!_hasPlayedSfx && Transform.Position.z < 155f)
		{
			var clangSfx = Sound.Play( "wood_slam", Transform.Position );
			clangSfx.Volume = 0.5f;
			_hasPlayedSfx = true;
		}
	}
}
