using Sandbox;

public enum MoveState { Up, Left, Down, Right }

public sealed class MoveSequence : Component
{
	[Property] MoveState MoveState { get; set; }
	[Property] float UpLimit { get; set; }
	[Property] float LeftLimit { get; set; }
	[Property] float DownLimit { get; set; }
	[Property] float RightLimit { get; set; }
	[Property] float Speed { get; set; } = 100f;

	protected override void OnEnabled()
	{
		base.OnEnabled();

	}

	protected override void OnUpdate()
	{
		if ( MoveState == MoveState.Up)
		{
			Transform.Position += Vector3.Up * Speed * Time.Delta;
			if ( Transform.Position.z > UpLimit )
			{
				Transform.Position = Transform.Position.WithZ( UpLimit );
				MoveState = MoveState.Left;
			}
		}
		else if ( MoveState == MoveState.Left )
		{
			Transform.Position += Vector3.Left * Speed * Time.Delta;
			if ( Transform.Position.y > LeftLimit)
			{
				Transform.Position = Transform.Position.WithY( LeftLimit);
				MoveState = MoveState.Down;
			}
		}
		else if ( MoveState == MoveState.Down )
		{
			Transform.Position += Vector3.Down * Speed * Time.Delta;
			if ( Transform.Position.z < DownLimit)
			{
				Transform.Position = Transform.Position.WithZ( DownLimit );
				MoveState = MoveState.Right;
			}
		}
		else if ( MoveState == MoveState.Right )
		{
			Transform.Position += Vector3.Right * Speed * Time.Delta;
			if ( Transform.Position.y < RightLimit)
			{
				Transform.Position = Transform.Position.WithY( RightLimit );
				MoveState = MoveState.Up;
			}
		}
	}
}
