using Sandbox;
using System.ComponentModel.DataAnnotations;

public sealed class Manager : Component
{
	[Property] public GameObject ClonePrefab { get; set; }

	private TimeSince _timeSinceClone;


	protected override void OnEnabled()
	{
		base.OnEnabled();

		for(int i = 0; i < 5; i++)
		{
			SpawnClone( new Vector3( 0f, -100f + 30f * i, 200f ) );
		}

		_timeSinceClone = 0f;

		Scene.PhysicsWorld.SubSteps = 32;
	}

	protected override void OnUpdate()
	{
		if( _timeSinceClone > 1f)
		{
			//SpawnClone( new Vector3( 0f, 0f, 175f ) );
			_timeSinceClone = 0f;
		}
	}

	public void SpawnClone(Vector3 pos)
	{
		var cloneObj = SceneUtility.Instantiate( ClonePrefab, pos );
		cloneObj.Components.Get<Clone>().Manager = this;
	}

	public void CloneDied(Clone clone)
	{
		SpawnClone( new Vector3( 0f, Game.Random.Float(-50f, 50f), 175f + Game.Random.Float( -50f, 50f ) ) );
	}
}
