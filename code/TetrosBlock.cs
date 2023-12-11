using System;
using Sandbox;

namespace TetrosEffect;

[Category( "Tetros Effect" )]
public sealed class TetrosBlock : Component
{
	public TetrosBoardManager Board;

	[Property]
	public Vector2 Position { get; set; }

	protected override void OnUpdate()
	{
		var lerp = 1f - MathF.Pow( 0.5f, Time.Delta * 10 );
		Transform.LocalPosition = Vector3.Lerp( Transform.LocalPosition, Board.GetLocalPosition( (int)Position.x, (int)Position.y ), lerp );
	}
}