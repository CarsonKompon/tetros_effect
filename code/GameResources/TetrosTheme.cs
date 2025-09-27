using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace TetrosEffect;

[AssetType( Name = "Tetros Theme", Extension = "tetros", Category = "Tetros" )]
public partial class TetrosTheme : GameResource
{
	public string Name { get; set; } = "Tetros Theme";

	public Model BlockModel { get; set; }
	public Model BoardModel { get; set; }

	public string MusicSlow { get; set; } = "tetros_music_slow";
	public string MusicFast { get; set; } = "tetros_music_fast";
	public string SongName { get; set; } = "";
	public string MoveLeftSound { get; set; } = "tetros_move";
	public string MoveRightSound { get; set; } = "tetros_move";
	public string MoveDownSound { get; set; } = "tetros_move";
	public string RotateLeftSound { get; set; } = "tetros_rotate";
	public string RotateRightSound { get; set; } = "tetros_rotate";
	public string PlaceSound { get; set; } = "tetros_place";
	public string LineSound { get; set; } = "tetros_line";
	public string TetrosSound { get; set; } = "tetros_tetros";
	public string HoldSound { get; set; } = "tetros_hold";

	public static List<TetrosTheme> All => ResourceLibrary.GetAll<TetrosTheme>().ToList();
	public static TetrosTheme Find( int id ) => All.Where( x => x.ResourceId == id ).FirstOrDefault();
	public static TetrosTheme FindByName( string name ) => All.Where( x => x.ResourceName == name ).FirstOrDefault();

	protected override Bitmap CreateAssetTypeIcon( int width, int height )
	{
		return CreateSimpleAssetTypeIcon( "theme", width, height, "#fdea60", "black" );
	}
}
