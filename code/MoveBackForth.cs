using Sandbox;

public enum MoveDirection { Horizontal, Vertical }

public sealed class MoveBackForth : Component
{
	[Property] MoveDirection Direction { get; set; }
	[Property] float Min { get; set; }
	[Property] float Max { get; set; }
	[Property] float Speed { get; set; } = 100f;
	[Property] bool Increasing { get; set; }
	[Property] bool ShouldPlaySfx { get; set; } = true;

	protected override void OnEnabled()
	{
		base.OnEnabled();

	}

	protected override void OnUpdate()
	{
		if ( Direction == MoveDirection.Horizontal )
		{
			Transform.Position += Vector3.Right * Speed * Time.Delta * (Increasing ? 1f : -1f);
			if(Transform.Position.y > Max)
			{
				Transform.Position = Transform.Position.WithY( Max );
				Increasing = !Increasing;
				PlaySfx();
			}
			else if ( Transform.Position.y < Min )
			{
				Transform.Position = Transform.Position.WithY( Min );
				Increasing = !Increasing;
				PlaySfx();
			}
		}
		else
		{
			Transform.Position += Vector3.Up * Speed * Time.Delta * (Increasing ? 1f : -1f);
			if ( Transform.Position.z > Max )
			{
				Transform.Position = Transform.Position.WithZ( Max );
				Increasing = !Increasing;
				PlaySfx();
			}
			else if ( Transform.Position.z < Min )
			{
				Transform.Position = Transform.Position.WithZ( Min );
				Increasing = !Increasing;
				PlaySfx();
			}
		}
	}

	void PlaySfx()
	{
		if ( !ShouldPlaySfx )
			return;

		Sound.Play( "spike_hit", Transform.Position );
	}
}
