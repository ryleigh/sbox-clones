using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;

public class SavedProgressData
{
	public int NumLevelsCompleted { get; set; }
}

public class CloneDeathData
{
	public int NumCloneDeaths { get; set; }
}

public sealed class Manager : Component
{
	[Property] public string LevelName { get; set; }
	[Property] public int LevelNum { get; set; }
	[Property] public SceneFile PrevScene { get; set; }
	[Property] public SceneFile NextScene { get; set; }
	[Property] public GameObject ClonePrefab { get; set; }
	[Property] public GameObject HudPrefab { get; set; }
	[Property] public float DeathHeight { get; set; } = -100f;

	public List<Clone> Clones = new List<Clone>();
	public List<Button> Buttons = new List<Button>();
	public Door Door { get; private set; }
	public Spawner Spawner { get; private set; }
	private int _numClonesToSpawn;
	private TimeSince _timeSinceSpawnClone;

	public bool IsCloneLeavingLevel { get; private set; }

	public MusicPlayer MusicPlayer { get; private set; }

	public int NumLevelsCompleted { get; private set; }
	public int NumCloneDeaths { get; private set; }

	public CameraShake CameraShake { get; private set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		HudPrefab.Clone();

		var spawnPoints = Scene.GetAllComponents<CloneSpawnPoint>().ToList();
		foreach( var spawnPoint in spawnPoints )
		{
			SpawnClone( spawnPoint.Transform.Position );
		}

		Buttons = Scene.GetAllComponents<Button>().ToList();
		Door = Scene.GetAllComponents<Door>().FirstOrDefault();
		Spawner = Scene.GetAllComponents<Spawner>().FirstOrDefault();
		CameraShake = Scene.GetAllComponents<CameraShake>().FirstOrDefault();

		_timeSinceSpawnClone = 0f;

		//for(int i = 0; i < 5; i++)
		//{
		//	SpawnClone( new Vector3( 0f, -100f + 30f * i, 200f ) );
		//}

		var levelData = FileSystem.Data.ReadJson<SavedProgressData>( "levels_completed.json" );
		NumLevelsCompleted = levelData?.NumLevelsCompleted ?? 0;

		var cloneDeathData = FileSystem.Data.ReadJson<CloneDeathData>( "num_clone_deaths.json" );
		NumCloneDeaths = cloneDeathData?.NumCloneDeaths ?? 0;

		Scene.PhysicsWorld.SubSteps = 4;

		StartMusic();
	}

	protected override void OnUpdate()
	{
		if ( IsCloneLeavingLevel )
			return;

		if(_numClonesToSpawn > 0 && _timeSinceSpawnClone > 0.2f && Spawner != null )
		{
			var clone = SpawnClone( Spawner.Transform.Position.WithX( 0f ) + Vector3.Down * 10f ) ;
			clone.StartSpawning();
			clone.Renderer.Tint = clone.Renderer.Tint.WithAlpha( 0f );

			_timeSinceSpawnClone = 0f;
			_numClonesToSpawn--;
		}

		//Log.Info( $"{MusicPlayer.OnFinished} {MusicPlayer.PlaybackTime} / {MusicPlayer.PlaybackTime}" );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		//Scene.PhysicsWorld.Gravity = Vector3.Down * (Input.Down( "Jump" ) ? 700f : 850f);
	}

	public Clone SpawnClone(Vector3 pos)
	{
		var cloneObj = ClonePrefab.Clone( pos );
		var clone = cloneObj.Components.Get<Clone>();
		clone.Manager = this;
		Clones.Add( clone );
		return clone;
	}

	public void CloneDied(Clone clone)
	{
		RemoveClone( clone );

		CameraShake?.Shake( Game.Random.Float(2.5f, 4f) );

		_numClonesToSpawn++;

		NumCloneDeaths++;
		//Log.Info( $"NumCloneDeaths: {NumCloneDeaths}" );
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
		if((LevelNum + 1) > NumLevelsCompleted)
		{
			var newSaveData = new SavedProgressData() { NumLevelsCompleted = LevelNum + 1 };
			FileSystem.Data.WriteJson<SavedProgressData>( "levels_completed.json", newSaveData );

			//Log.Info( $"saving # levels completed: {LevelNum + 1}" );
		}

		LoadNextLevel();
	}

	public void LoadPrevLevel()
	{
		if(PrevScene == null)
		{
			Log.Info( "No previous level!" );
			return;
		}

		var newCloneDeathData = new CloneDeathData() { NumCloneDeaths = NumCloneDeaths };
		FileSystem.Data.WriteJson<CloneDeathData>( "num_clone_deaths.json", newCloneDeathData );

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

		var newCloneDeathData = new CloneDeathData() { NumCloneDeaths = NumCloneDeaths };
		FileSystem.Data.WriteJson<CloneDeathData>( "num_clone_deaths.json", newCloneDeathData );

		GameManager.ActiveScene.Clear();
		GameManager.ActiveScene.Load( NextScene );
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		MusicPlayer?.Stop();
		MusicPlayer?.Dispose();

		var newCloneDeathData = new CloneDeathData() { NumCloneDeaths = NumCloneDeaths };
		FileSystem.Data.WriteJson<CloneDeathData>( "num_clone_deaths.json", newCloneDeathData );
	}

	public void OnMusicFinished()
	{
		StartMusic();
	}

	void StartMusic()
	{
		MusicPlayer?.Dispose();

		MusicPlayer = MusicPlayer.Play( FileSystem.Mounted, $"music/music{Game.Random.Int(1, 3)}.mp3" );
		MusicPlayer.ListenLocal = true;
		MusicPlayer.Volume = 0.3f;
		MusicPlayer.OnFinished += OnMusicFinished;
	}
}
