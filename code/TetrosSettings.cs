using Sandbox;

namespace TetrosEffect;

public class TetrosSettings
{
	public static TetrosSettings Instance
	{
		get
		{
			if ( _instance == null )
			{
				var file = "/settings/tetros.json";
				if ( !FileSystem.Data.FileExists( file ) )
				{
					_instance = new TetrosSettings();
					_instance.Init();
					_instance.Save();
					return _instance;
				}
				_instance = FileSystem.Data.ReadJson( file, new TetrosSettings() );
				_instance.Init();
			}

			return _instance;
		}
	}
	static TetrosSettings _instance;

	public float MusicVolume { get; set; } = 0.5f;
	public float SfxVolume { get; set; } = 0.5f;
	public int Theme { get; set; } = 0;
	public bool ShowGhost { get; set; } = true;
	public int NextBlocksShown { get; set; } = 4;
	public bool AllowHold { get; set; } = true;
	public bool BoardNudging { get; set; } = true;
	public bool ShowParticles { get; set; } = true;

	void Init()
	{
		if ( Theme == 0 )
		{
			Theme = TetrosTheme.FindByName( "theme_piano" ).ResourceId;
		}
	}

	public void Save()
	{
		var file = "/settings/tetros.json";
		if ( FileSystem.Data.DirectoryExists( "/settings" ) == false )
			FileSystem.Data.CreateDirectory( "/settings" );
		FileSystem.Data.WriteJson( file, this );
	}
}
