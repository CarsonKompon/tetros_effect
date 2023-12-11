using System;
using Sandbox;

namespace TetrosEffect;

[Category( "Tetros Effect" )]
public sealed class TetrosBlock : Component
{
	public TetrosBoardManager Board;

	Vector3 targetPosition;

	[Property]
	public Vector2 Position
	{
		get => _position;
		set
		{
			_position = value;
			targetPosition = Board.GetPosition( (int)value.x, (int)value.y );
		}
	}
	private Vector2 _position;

	protected override void OnStart()
	{
		targetPosition = Transform.Position;
	}

	protected override void OnUpdate()
	{
		var lerp = 1f - MathF.Pow( 0.5f, Time.Delta * 10 );
		Transform.Position = Vector3.Lerp( Transform.Position, targetPosition, lerp );
	}
}