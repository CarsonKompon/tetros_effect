using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using Sandbox;

namespace Rhythm4K;

[GameResource( "Tetros Theme", "tetros", "Describes a Tetros Effect Theme", Icon = "theme" )]
public partial class TetrosTheme : GameResource
{
    [ResourceType( "vmdl" )]
    public string BlockModel { get; set; }
    [ResourceType( "vmdl" )]
    public string BoardModel { get; set; }

    public string MoveLeftSound { get; set; } = "tetros_move";
    public string MoveRightSound { get; set; } = "tetros_move";
    public string MoveDownSound { get; set; } = "tetros_move";
    public string RotateLeftSound { get; set; } = "tetros_rotate";
    public string RotateRightSound { get; set; } = "tetros_rotate";
    public string PlaceSound { get; set; } = "tetros_place";
    public string LineSound { get; set; } = "tetros_line";
    public string TetrosSound { get; set; } = "tetros_tetros";
    public string HoldSound { get; set; } = "tetros_hold";

}