using Sandbox;
using System;

namespace TetrosEffect;

[Category( "Tetros Effect" )]
public sealed class TetrosGameCamera : Component
{
	[Property] float LerpSpeed { get; set; } = 5f;

	Vector3 startingPosition;
	Bloom bloom;

	protected override void OnStart()
	{
		startingPosition = WorldPosition;
		bloom = Components.Get<Bloom>();
	}

	protected override void OnUpdate()
	{
		var lerp = 1f - MathF.Pow( 0.5f, Time.Delta * LerpSpeed );
		WorldPosition = Vector3.Lerp( WorldPosition, startingPosition, lerp );
		bloom.Strength = MathX.Lerp( bloom.Strength, 0.15f, lerp );
	}

	public void Bloom( float strength )
	{
		bloom.Strength += strength;
	}
}
