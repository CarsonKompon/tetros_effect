using System;
using System.Linq;
using System.Collections.Generic;
using Sandbox;
using TetrosEffect;
using System.Diagnostics.CodeAnalysis;

[Category( "Tetros Effect" )]
public sealed class TetrosBoardManager : Component
{
	[Property] public TetrosGameManager GameManager { get; set; }
	[Property] public TetrosGameCamera Camera { get; set; }

	[Property] public int Width { get; set; } = 10;
	[Property] public int Height { get; set; } = 20;
	[Property] public float GridSize { get; set; } = 64f;
	[Property] public int QueueLength { get; set; } = 10;

	// Public Game Variables
	public long Score { get; set; } = 0;
	public long HighScore { get; private set; } = 0;
	public int Level { get; set; } = 1;
	public int LinesCleared { get; set; } = 10;
	public int Combo { get; set; } = -1;
	public List<PieceType> Queue { get; set; } = new List<PieceType>();
	public TetrosPiece CurrentPiece { get; set; } = null;
	public PieceType Held { get; set; } = PieceType.Empty;
	public bool IsPlaying { get; set; } = false;

	// Private Game Variables
	private List<TetrosBlock> Blocks = new List<TetrosBlock>();
	private List<PieceType> GrabBag { get; set; } = new List<PieceType>();
	private int LinesClearedThisLevel { get; set; } = 0;
	private bool JustHeld { get; set; } = false;

	// Timers
	private TimeSince LastUpdate = 0f;
	private TimeSince LeftTimer = 0f;
	private TimeSince RightTimer = 0f;

	protected override void OnStart()
	{
		StartGame();
	}

	protected override void OnUpdate()
	{
		if ( !IsPlaying && !IsProxy ) return;

		var interval = GetWaitTime();
		var fastDrop = Input.Down( "SoftDrop" );
		if ( fastDrop ) interval = MathF.Min( 0.04f, interval / 4f );
		if ( LastUpdate > interval )
		{
			if ( !CurrentPiece.IsValid() )
			{
				// Get a new Piece
				var piece = GetPieceFromQueue();
				SpawnCurrentPiece( piece );
			}
			else
			{
				// Move the Piece
				CurrentPiece.Position += new Vector2( 0, 1 );
				if ( CheckCurrentPieceCollision() )
				{
					CurrentPiece.Position -= new Vector2( 0, 1 );
					PlaceCurrentPiece();
				}
				else if ( fastDrop )
				{
					var sound = Sound.Play( "tetros_move" );
					sound.Pitch = 1.5f;
					Score++;
				}
			}
			LastUpdate = 0f;
			if ( CheckCurrentPieceCollision( Vector2.Down ) )
			{
				LastUpdate = -GetWaitTime() / 4f;
			}
		}

		// Check for Input
		if ( Input.Pressed( "HardDrop" ) )
		{
			HardDrop();
		}
		else if ( Input.Pressed( "Hold" ) )
		{
			Hold();
		}
		else
		{
			if ( Input.Pressed( "MoveLeft" ) )
			{
				LeftTimer = 0f;
				Move( -1 );
			}
			else if ( Input.Down( "MoveLeft" ) && LeftTimer > 0.2f )
			{
				LeftTimer = 0.1f;
				Move( -1 );
			}

			if ( Input.Pressed( "MoveRight" ) )
			{
				RightTimer = 0f;
				Move( 1 );
			}
			else if ( Input.Down( "MoveRight" ) && RightTimer > 0.2f )
			{
				RightTimer = 0.1f;
				Move( 1 );
			}

			if ( Input.Pressed( "RotateLeft" ) )
			{
				Rotate( -1 );
			}
			else if ( Input.Pressed( "RotateRight" ) )
			{
				Rotate( 1 );
			}
		}
	}

	public void ResetGame()
	{
		Score = 0;
		Combo = -1;
		Level = 1;
		LinesCleared = 0;
		LinesClearedThisLevel = 0;

		if ( CurrentPiece.IsValid() )
		{
			CurrentPiece.GameObject.Destroy();
		}

		Queue.Clear();
		for ( int i = 0; i < QueueLength; i++ )
		{
			Queue.Add( GetRandomPiece() );
		}
	}

	public void StartGame()
	{
		ResetGame();

		IsPlaying = true;
		HighScore = (long)Sandbox.Services.Stats.GetLocalPlayerStats( Game.Menu.Package.FullIdent ).Get( "tetros_highscore" ).Value;
	}

	public void EndGame()
	{
		Sandbox.Services.Stats.Increment( "tetros_lines", LinesCleared );
		Sandbox.Services.Stats.Increment( "tetros_games", 1 );
		Sandbox.Services.Stats.SetValue( "tetros_highscore", Score );

		ResetGame();

		IsPlaying = false;
	}

	void SpawnCurrentPiece( PieceType pieceType )
	{
		if ( CurrentPiece.IsValid() )
		{
			CurrentPiece.GameObject.Destroy();
		}

		GameObject prefab = null;
		switch ( pieceType )
		{
			case PieceType.I: prefab = GameManager.PieceIPrefab; break;
			case PieceType.O: prefab = GameManager.PieceOPrefab; break;
			case PieceType.T: prefab = GameManager.PieceTPrefab; break;
			case PieceType.S: prefab = GameManager.PieceSPrefab; break;
			case PieceType.Z: prefab = GameManager.PieceZPrefab; break;
			case PieceType.J: prefab = GameManager.PieceJPrefab; break;
			case PieceType.L: prefab = GameManager.PieceLPrefab; break;
		}

		if ( prefab is not null )
		{
			var pieceObj = SceneUtility.Instantiate( prefab, GetPosition( 5, -2 ) );
			pieceObj.Enabled = true;
			CurrentPiece = pieceObj.Components.GetInChildrenOrSelf<TetrosPiece>();
			CurrentPiece.Board = this;
		}
	}

	GameObject SpawnBlock( Vector2 position, Color color )
	{
		var blockObject = SceneUtility.Instantiate( GameManager.BlockPrefab, GetPosition( (int)position.x, (int)position.y ) );
		blockObject.SetParent( GameObject );
		var blockScript = blockObject.Components.Get<TetrosBlock>();
		blockScript.Board = this;
		blockScript.Position = position;
		var blockRenderer = blockObject.Components.Get<ModelRenderer>();
		if ( blockRenderer != null ) blockRenderer.Tint = color;
		Blocks.Add( blockScript );
		return blockObject;
	}

	public void Move( int dir )
	{
		if ( CurrentPiece is null ) return;
		CurrentPiece.Position += new Vector2( dir, 0 );
		if ( CheckCurrentPieceCollision() )
		{
			CurrentPiece.Position -= new Vector2( dir, 0 );
		}
		Sound.Play( "tetros_move" );
	}

	public void Rotate( int dir = 1 )
	{
		if ( CurrentPiece is null ) return;
		int prevRot = CurrentPiece.PieceRotation;
		CurrentPiece.PieceRotation += dir;
		while ( CurrentPiece.PieceRotation < 0 ) CurrentPiece.PieceRotation += 4;
		CurrentPiece.PieceRotation %= 4;
		if ( CheckCurrentPieceCollision() )
		{
			CurrentPiece.PieceRotation = prevRot;
		}
		LastUpdate /= 2f;
		Sound.Play( "tetros_rotate" );
	}

	public void HardDrop()
	{
		if ( CurrentPiece is null ) return;
		while ( !CheckCurrentPieceCollision() )
		{
			CurrentPiece.Position += new Vector2( 0, 1 );
			Score += 2;
		}
		Score -= 2;
		CurrentPiece.Position -= new Vector2( 0, 1 );
		PlaceCurrentPiece();
		LastUpdate = GetWaitTime() / 4f * 3f;
	}

	public void Hold()
	{
		if ( JustHeld ) return;
		if ( CurrentPiece is null ) return;

		if ( Held == PieceType.Empty )
		{
			Held = CurrentPiece.Type;
			CurrentPiece.GameObject.Destroy();
		}
		else
		{
			var temp = Held;
			Held = CurrentPiece.Type;
			SpawnCurrentPiece( temp );
		}
		JustHeld = true;
		Sound.Play( "tetros_hold" );
	}

	bool CheckCurrentPieceCollision( Vector2 offset = default )
	{
		var piece = TetrosShapes.GetShape( CurrentPiece.Type, CurrentPiece.PieceRotation );
		var grid = piece.GetGrid();
		var pos = CurrentPiece.Position + offset;
		for ( int i = 0; i < 16; i++ )
		{
			if ( grid[i] == 1 )
			{
				var x = (int)pos.x + (i % 4) - 1;
				var y = (int)pos.y + (i / 4) - 1;
				if ( x < 0 || x >= Width || y >= Height ) return true;
				if ( HasBlock( x, y ) ) return true;
			}
		}

		return false;
	}

	void PlaceCurrentPiece()
	{
		var piece = TetrosShapes.GetShape( CurrentPiece.Type, CurrentPiece.PieceRotation );
		var grid = piece.GetGrid();
		var pos = CurrentPiece.Position;
		for ( int i = 0; i < 16; i++ )
		{
			if ( grid[i] == 1 )
			{
				var x = (int)pos.x + (i % 4) - 1;
				var y = (int)pos.y + (i / 4) - 1;
				if ( y < 0 )
				{
					EndGame();
					return;
				}

				SpawnBlock( new Vector2( x, y ), Color.Cyan );
			}
		}
		JustHeld = false;
		Sound.Play( "tetros_place" );
		CurrentPiece.GameObject.Destroy();

		CheckLine();
	}

	void CheckLine()
	{
		int lines = 0;
		for ( int y = 0; y < Height; y++ )
		{
			bool line = true;
			for ( int x = 0; x < Width; x++ )
			{
				if ( !HasBlock( x, y ) )
				{
					line = false;
					break;
				}
			}

			if ( line )
			{
				var lineBlocks = Blocks.Where( b => b.Position.y == y );
				foreach ( var block in lineBlocks )
				{
					block.GameObject.Destroy();
					Blocks.Remove( block );
				}

				var blocksAbove = Blocks.Where( b => b.Position.y > y );
				foreach ( var block in blocksAbove )
				{
					block.Position -= new Vector2( 0, 1 );
				}

				lines++;
			}
		}

		if ( lines > 0 )
		{
			var sound = Sound.Play( "tetros_line" );
			sound.Pitch = 1f + (MathF.Max( 0, Combo ) * (1.0f / 12.0f));

			Combo++;
			switch ( lines )
			{
				case 1:
					Score += 100 * Level;
					break;
				case 2:
					Score += 300 * Level;
					break;
				case 3:
					Score += 500 * Level;
					break;
				case 4:
					Score += 800 * Level;
					Sound.Play( "tetros_tetris" );
					break;
			}

			if ( Combo > 0 )
			{
				Score += 50 * Combo * Level;
			}

			LinesCleared += lines;
			LinesClearedThisLevel += lines;
			if ( LinesClearedThisLevel >= GetLinesNeeded() )
			{
				Level++;
				LinesClearedThisLevel = 0;
			}
		}
		else
		{
			Combo = -1;
		}
	}

	int GetLinesNeeded()
	{
		switch ( Level )
		{
			case 1: return 10;
			case 2: return 20;
			case 3: return 30;
			case 4: return 40;
			case 5: return 50;
			case 6: return 60;
			case 7: return 70;
			case 8: return 80;
			case 9: return 90;
			case 10: return 100;
			case 11: return 120;
			case 12: return 140;
			case 13: return 160;
			case 14: return 180;
			case 15: return 200;
			case 16: return 220;
			case 17: return 240;
			case 18: return 260;
			case 19: return 280;
			case 20: return 300;
			default: return 300;
		}
	}

	PieceType GetRandomPiece()
	{
		if ( GrabBag.Count < QueueLength )
		{
			GrabBag = new List<PieceType> { PieceType.I, PieceType.O, PieceType.T, PieceType.S, PieceType.Z, PieceType.J, PieceType.L };
			GrabBag = GrabBag.OrderBy( x => Guid.NewGuid() ).ToList();
		}

		var piece = GrabBag[0];
		GrabBag.RemoveAt( 0 );

		return piece;
	}

	PieceType GetPieceFromQueue()
	{
		while ( Queue.Count < QueueLength )
		{
			Queue.Add( GetRandomPiece() );
		}

		var piece = Queue[0];
		Queue.RemoveAt( 0 );
		var newPiece = GetRandomPiece();
		Queue.Add( newPiece );
		return piece;
	}

	public Vector3 GetPosition( int x, int y )
	{
		return Transform.Position + new Vector3( x * GridSize, 0, -y * GridSize );
	}

	public bool HasBlock( int x, int y )
	{
		return Blocks.Exists( b => b.Position == new Vector2( x, y ) );
	}

	public float GetWaitTime()
	{
		switch ( Level )
		{
			case 0: return 36f / 60f;
			case 1: return 32f / 60f;
			case 2: return 29f / 60f;
			case 3: return 25f / 60f;
			case 4: return 22f / 60f;
			case 5: return 18f / 60f;
			case 6: return 15f / 60f;
			case 7: return 11f / 60f;
			case 8: return 7f / 60f;
			case 9: return 5f / 60f;
			case 10:
			case 11:
			case 12:
				return 4f / 60f;
			case 13:
			case 14:
			case 15:
				return 3f / 60f;
			case 16:
			case 17:
			case 18:
				return 2f / 60f;
			case 19: return 1f / 60f;
			case 20: return 1f / 60f;
			default: return 0.01f;
		}
	}

	protected override void DrawGizmos()
	{
		if ( Gizmo.IsSelected )
		{
			var hgs = GridSize / 2f;
			for ( int x = 0; x < Width; x++ )
			{
				for ( int y = 0; y < Height; y++ )
				{
					Gizmo.Draw.Line( new Vector3( x * GridSize - hgs, 0, -y * GridSize + hgs ), new Vector3( x * GridSize - hgs, 0, -(y + 1) * GridSize + hgs ) );
					Gizmo.Draw.Line( new Vector3( x * GridSize - hgs, 0, -y * GridSize + hgs ), new Vector3( (x + 1) * GridSize - hgs, 0, -y * GridSize + hgs ) );
				}
			}
		}
	}
}