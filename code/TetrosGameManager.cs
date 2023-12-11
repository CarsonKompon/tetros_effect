using System.Collections.Generic;
using Sandbox;


namespace TetrosEffect;

[Category( "Tetros Effect" )]
public sealed class TetrosGameManager : Component
{
	[Property] public List<TetrosBoardManager> Boards { get; set; }

	[Property] public GameObject BlockPrefab { get; set; }
	[Property] public GameObject PieceJPrefab { get; set; }
	[Property] public GameObject PieceLPrefab { get; set; }
	[Property] public GameObject PieceOPrefab { get; set; }
	[Property] public GameObject PieceSPrefab { get; set; }
	[Property] public GameObject PieceTPrefab { get; set; }
	[Property] public GameObject PieceZPrefab { get; set; }
	[Property] public GameObject PieceIPrefab { get; set; }

	[Property] public GameObject ParticleBurstPrefab { get; set; }

}
