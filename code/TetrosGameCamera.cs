using System;
using Sandbox;

namespace TetrosEffect;

[Category( "Tetros Effect" )]
public sealed class TetrosGameCamera : Component
{
	[Property] float LerpSpeed { get; set; } = 5f;

	Vector3 startingPosition;

	protected override void OnStart()
	{
		startingPosition = Transform.Position;
	}

	protected override void OnUpdate()
	{
		var lerp = 1f - MathF.Pow( 0.5f, Time.Delta * LerpSpeed );
		Transform.Position = Vector3.Lerp( Transform.Position, startingPosition, lerp );
	}
}