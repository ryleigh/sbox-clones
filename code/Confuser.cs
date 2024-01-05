using Sandbox;
using System.Linq;

public sealed class Confuser : Component
{
	private ParticleSphereEmitter _particleEmitter;
	public bool IsActive { get; private set; }
	public TimeSince TimeSinceConsumed { get; private set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		_particleEmitter = GameObject.Children.Where( x => x.Name == "smoke" ).FirstOrDefault().Components.Get<ParticleSphereEmitter>();
		IsActive = true;
	}

	protected override void OnUpdate()
	{
		if ( !_particleEmitter.Enabled && TimeSinceConsumed > 1.75f )
			_particleEmitter.Enabled = true;

		if (!IsActive && TimeSinceConsumed > 2f )
			IsActive = true;
	}

	public void Consume(Clone clone)
	{
		if ( !IsActive )
			return;

		clone.SetConfused( true );

		_particleEmitter.Enabled = false;
		IsActive = false;
		TimeSinceConsumed = 0f;
	}
}
