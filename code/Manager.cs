using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class Manager : Component
{
	[Property] public GameObject ClonePrefab { get; set; }

	private TimeSince _timeSinceClone;

	public List<Button> Buttons = new List<Button>();
	public Door Door { get; private set; }

	public List<Clone> Clones = new List<Clone>();

	protected override void OnEnabled()
	{
		base.OnEnabled();

		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToList();
		foreach( var spawnPoint in spawnPoints )
		{
			SpawnClone( spawnPoint.Transform.Position );
		}

		Buttons = Scene.GetAllComponents<Button>().ToList();
		Door = Scene.GetAllComponents<Door>().FirstOrDefault();

		//for(int i = 0; i < 5; i++)
		//{
		//	SpawnClone( new Vector3( 0f, -100f + 30f * i, 200f ) );
		//}

		_timeSinceClone = 0f;

		Scene.PhysicsWorld.SubSteps = 4;
	}

	protected override void OnUpdate()
	{
		if( _timeSinceClone > 1f)
		{
			//SpawnClone( new Vector3( 0f, 0f, 175f ) );
			_timeSinceClone = 0f;
		}

		//foreach( var clone in Clones ) 
		//{
		//	//Log.Info( $"{clone.Transform.Position}" );
		//	Gizmo.Draw.Line( clone.Transform.Position, Vector3.Zero );
		//	Gizmo.Draw.Text( $"Clone", new Transform( clone.Transform.Position ));
		//}
	}

	public void SpawnClone(Vector3 pos)
	{
		var cloneObj = SceneUtility.Instantiate( ClonePrefab, pos );
		var clone = cloneObj.Components.Get<Clone>();
		clone.Manager = this;
		Clones.Add( clone );
	}

	public void CloneDied(Clone clone)
	{
		RemoveClone( clone );
		SpawnClone( new Vector3( 0f, Game.Random.Float(-20f, 20f), 175f + Game.Random.Float( -20f, 20f ) ) );
	}

	public void RemoveClone(Clone clone)
	{
		Clones.Remove( clone );
	}

	public void ButtonPressed(Button button)
	{
		RefreshButtons();
	}

	public void ButtonReleased( Button button )
	{
		RefreshButtons();
	}

	void RefreshButtons()
	{
		Door.SetOpen( ShouldDoorBeOpen() );
	}

	public bool ShouldDoorBeOpen()
	{
		bool allButtonsPressed = true;
		foreach ( var button in Buttons )
		{
			if ( !button.IsPressed )
			{
				allButtonsPressed = false;
				break;
			}
		}

		return allButtonsPressed;
	}

	public void CloneEnteredDoor(Clone clone, Door door)
	{
		clone.EnterDoor( door );
	}
}
