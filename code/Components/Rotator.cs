using Sandbox;

namespace TetrosEffect;

public sealed class Rotator : Component
{
	[Property] public Rotation Speed { get; set; }



	protected override void OnUpdate()
	{
		Transform.LocalRotation *= Speed * Time.Delta;
	}
}