using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;

public sealed class Manager : Component
{
	[Property] public string LevelName { get; set; }
	[Property] public SceneFile PrevScene { get; set; }
	[Property] public SceneFile NextScene { get; set; }
	[Property] public GameObject ClonePrefab { get; set; }
	[Property] public GameObject HudPrefab { get; set; }
	[Property] public float DeathHeight { get; set; } = -100f;

	public List<Clone> Clones = new List<Clone>();
	public List<Button> Buttons = new List<Button>();
	public Door Door { get; private set; }

	public bool IsCloneLeavingLevel { get; private set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		SceneUtility.Instantiate( HudPrefab );

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

		Scene.PhysicsWorld.SubSteps = 4;
	}

	protected override void OnUpdate()
	{

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
		IsCloneLeavingLevel = true;
	}

	public void FinishLevel()
	{
		LoadNextLevel();
	}

	public void LoadPrevLevel()
	{
		if(PrevScene == null)
		{
			Log.Info( "No previous level!" );
			return;
		}

		GameManager.ActiveScene.Clear();
		GameManager.ActiveScene.Load( PrevScene );
	}

	public void LoadNextLevel()
	{
		if ( NextScene == null )
		{
			Log.Info( "No next level!" );
			return;
		}

		GameManager.ActiveScene.Clear();
		GameManager.ActiveScene.Load( NextScene );
	}
}
