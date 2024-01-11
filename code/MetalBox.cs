using Sandbox;

public sealed class MetalBox : Component, Component.ICollisionListener
{
	public Rigidbody Rigidbody { get; private set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Rigidbody = GameObject.Components.Get<Rigidbody>();
	}

	public void OnCollisionStart( Collision collision )
	{
		var vel = Rigidbody.PhysicsBody.Velocity.Length;

		if(vel > 100f)
		{
			var clangSfx = Sound.Play( "clang", Transform.Position );
			clangSfx.Volume = Utils.Map( vel, 100f, 300f, 0.5f, 0.8f);
		}
	}

	public void OnCollisionUpdate( Collision collision )
	{

	}

	public void OnCollisionStop( CollisionStop collision )
	{

	}
}
