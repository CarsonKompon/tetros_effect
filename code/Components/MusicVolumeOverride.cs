using Sandbox;

namespace TetrosEffect;

public class MusicVolumeOverride : Component
{
    [Property] public float Volume { get; set; } = 1f;

    BaseSoundComponent sound;

    protected override void OnStart()
    {
        sound = Components.Get<BaseSoundComponent>();
    }

    protected override void OnUpdate()
    {
        sound.Volume = Volume * TetrosSettings.Instance.MusicVolume;
    }
}