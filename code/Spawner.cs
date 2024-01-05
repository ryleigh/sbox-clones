using Sandbox;
using System.Linq;

public sealed class Spawner : Component
{
	private GameObject _ring;
	private GameObject _bgLight;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		_ring = GameObject.Children.Where( x => x.Name == "ring" ).FirstOrDefault();
		_bgLight = GameObject.Children.Where( x => x.Name == "bg_light" ).FirstOrDefault();
	}

	protected override void OnUpdate()
	{
		_ring.Transform.LocalRotation = Rotation.FromRoll( Time.Now * 30f );
		_bgLight.Transform.LocalRotation = Rotation.FromRoll( -Time.Now * 25f );
	}
}
