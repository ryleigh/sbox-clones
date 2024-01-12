using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;

public sealed class VictoryManager : Component
{
	[Property] public SceneFile FirstScene { get; set; }

	public MusicPlayer MusicPlayer { get; private set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();

		StartMusic();
	}

	public void Restart()
	{
		GameManager.ActiveScene.Clear();
		GameManager.ActiveScene.Load( FirstScene );
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		MusicPlayer?.Stop();
		MusicPlayer?.Dispose();
	}

	public void OnMusicFinished()
	{
		StartMusic();
	}

	void StartMusic()
	{
		MusicPlayer?.Dispose();

		MusicPlayer = MusicPlayer.Play( FileSystem.Mounted, $"music/victory_music{Game.Random.Int( 1, 3 )}.mp3" );
		MusicPlayer.ListenLocal = true;
		MusicPlayer.Volume = 0.3f;
		MusicPlayer.OnFinished += OnMusicFinished;
	}
}
