using Sandbox;

namespace TetrosEffect;

public class DestroyAfter : Component
{
	[Property] public float DestroyTime { get; set; } = 5f;

	protected override void OnUpdate()
	{
		DestroyTime -= Time.Delta;
		if ( DestroyTime <= 0f )
		{
			GameObject.Destroy();
		}
	}
}