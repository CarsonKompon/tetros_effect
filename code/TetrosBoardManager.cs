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
	[Property] public GameObject Board { get; set; }
	[Property] public GameObject BlocksContainer { get; set; }
	[Property] public TetrosTheme Theme { get; set; }

	[Property] public int Width { get; set; } = 10;
	[Property] public int Height { get; set; } = 20;
	[Property] public float GridSize { get; set; } = 64f;
	[Property] public int QueueShown { get; set; } = 4;
	[Property] public int QueueLength { get; set; } = 10;

	// Public Game Variables
	public long Score { get; set; } = 0;
	public long HighScore { get; private set; } = 0;
	public int Level { get; set; } = 1;
	public int LinesCleared { get; set; } = 10;
	public int LinesClearedThisLevel { get; set; } = 0;
	public int Combo { get; set; } = -1;
	public float GameTime { get; set; } = 0f;
	public List<PieceType> Queue { get; set; } = new List<PieceType>();
	public TetrosPiece CurrentPiece { get; set; } = null;
	public TetrosPiece HeldPiece { get; set; } = null;
	public List<TetrosPiece> QueuePieces { get; set; } = new List<TetrosPiece>();
	public TetrosPiece GhostPiece { get; set; } = null;
	public bool IsPlaying { get; set; } = false;

	// Private Game Variables
	private List<TetrosBlock> Blocks = new List<TetrosBlock>();
	private List<PieceType> GrabBag { get; set; } = new List<PieceType>();
	private bool JustHeld { get; set; } = false;
	private SoundHandle SlowMusic { get; set; } = new SoundHandle();
	private SoundHandle FastMusic { get; set; } = new SoundHandle();
	private TimeUntil MusicTimer = 0f;

	// Timers
	public TimeSince LastUpdate = 0f;
	private TimeSince LeftTimer = 0f;
	private TimeSince RightTimer = 0f;

	Vector3 AnchorPosition;

	protected override void OnStart()
	{
		AnchorPosition = Board.Transform.Position;

		StartGame();
	}

	protected override void OnUpdate()
	{
		if ( !IsPlaying ) return;

		GameTime += Time.Delta;

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
				CurrentPiece.Move( new Vector2( 0, 1 ) );
				if ( CheckCurrentPieceCollision() )
				{
					CurrentPiece.Move( new Vector2( 0, -1 ) );
					PlaceCurrentPiece();
				}
				else if ( fastDrop )
				{
					PlaySound( Theme.MoveDownSound );
					Score++;
				}
			}
			LastUpdate = 0f;
			if ( CheckCurrentPieceCollision( Vector2.Down ) )
			{
				LastUpdate = -GetWaitTime() / 4f;
			}

			UpdateGhost();
		}

		// Check for Input
		if ( Input.Pressed( "HardDrop" ) )
		{
			HardDrop();
		}
		else if ( Input.Pressed( "Hold" ) && TetrosSettings.Instance.AllowHold )
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
				LeftTimer = 0.15f;
				Move( -1 );
			}

			if ( Input.Pressed( "MoveRight" ) )
			{
				RightTimer = 0f;
				Move( 1 );
			}
			else if ( Input.Down( "MoveRight" ) && RightTimer > 0.2f )
			{
				RightTimer = 0.15f;
				Move( 1 );
			}

			if ( Input.Pressed( "RotateLeft" ) )
			{
				Rotate( -1 );
			}
			else if ( Input.Pressed( "RotateRight" ) || Input.Pressed( "AlternateRotate" ) )
			{
				Rotate( 1 );
			}
		}

		// Lerp to anchor position
		var lerpAm = 1f - MathF.Pow( 0.5f, Time.Delta * 10 );
		Board.Transform.Position = Vector3.Lerp( Board.Transform.Position, AnchorPosition, lerpAm );

		// Move the ghost piece
		if ( GhostPiece.IsValid() )
		{
			GhostPiece.Transform.Position = GetPosition( (int)CurrentPiece.Position.x + (int)LastGhostOffset.x, (int)CurrentPiece.Position.y + (int)LastGhostOffset.y ) + new Vector3( GridSize / 2f, 0, -GridSize / 2f ); ;
		}

		// Loop the music
		if ( !SlowMusic.IsPlaying )
		{
			FastMusic.Stop( true );
			SlowMusic = PlaySound( Theme.MusicSlow );
			FastMusic = PlaySound( Theme.MusicFast );
		}

		// Set music volume
		var volume = TetrosSettings.Instance.MusicVolume;
		var slowMusic = SlowMusic;
		slowMusic.Volume = (MusicTimer > 0 ? 0f : 1f) * volume;
		var fastMusic = FastMusic;
		fastMusic.Volume = (MusicTimer > 0 ? 1f : 0f) * volume;
	}

	void UpdateNextPieces()
	{
		foreach ( var piece in QueuePieces )
		{
			piece.GameObject.Destroy();
		}
		QueuePieces.Clear();

		int am = Math.Min( QueueLength, QueueShown );
		for ( int i = 0; i < am; i++ )
		{
			var piece = SpawnPiece( Queue[i], false );
			piece.Transform.Position = GetPosition( 13, 1 + i * 3 );
			if ( i > 0 )
			{
				foreach ( var block in piece.Container.Children )
				{
					var model = block.Components.Get<ModelRenderer>();
					model.Tint = model.Tint.WithAlpha( 1f - (i / (float)am) );
				}
			}
			QueuePieces.Add( piece );
		}
	}

	public void ResetGame()
	{
		Score = 0;
		Combo = -1;
		Level = 1;
		LinesCleared = 0;
		LinesClearedThisLevel = 0;
		GameTime = 0f;

		if ( CurrentPiece.IsValid() )
		{
			CurrentPiece.GameObject.Destroy();
			CurrentPiece = null;
		}

		if ( HeldPiece.IsValid() )
		{
			HeldPiece.GameObject.Destroy();
			HeldPiece = null;
		}

		if ( GhostPiece.IsValid() )
		{
			GhostPiece.GameObject.Destroy();
			GhostPiece = null;
		}

		if ( QueuePieces.Count > 0 )
		{
			foreach ( var piece in QueuePieces )
			{
				piece.GameObject.Destroy();
			}
			QueuePieces.Clear();
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
		Board.Components.Get<ModelRenderer>().Model = Model.Load( Theme.BoardModel );

		IsPlaying = true;
		RequestHighscore();

		var allResults = GameManager.UIObject.Components.GetAll<TetrosResultsScreen>();
		for ( int i = 0; i < allResults.Count(); i++ )
		{
			allResults.ElementAt( i ).Destroy();
		}
	}

	public void EndGame()
	{
		if ( !IsPlaying ) return;
		Sandbox.Services.Stats.Increment( "tetros_lines", LinesCleared );
		Sandbox.Services.Stats.Increment( "tetros_games", 1 );
		Sandbox.Services.Stats.SetValue( "tetros_highscore", Score );
		IsPlaying = false;

		CurrentPiece?.GameObject.Destroy();
		GhostPiece?.GameObject.Destroy();

		foreach ( var block in Blocks )
		{
			SpawnParticleBurst( block.Position, block.Components.Get<ModelRenderer>().Tint );
			block.GameObject.Destroy();
		}
		Blocks.Clear();

		var results = GameManager.UIObject.Components.Create<TetrosResultsScreen>();
		results.Board = this;
	}

	TetrosPiece SpawnPiece( PieceType pieceType, bool inBoard )
	{
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
			pieceObj.SetParent( BlocksContainer );
			pieceObj.Enabled = true;
			var piece = pieceObj.Components.GetInChildrenOrSelf<TetrosPiece>();
			foreach ( var child in piece.Container.Children )
			{
				var model = child.Components.Get<ModelRenderer>();
				model.Model = Model.Load( Theme.BlockModel );
				model.Tint = piece.Color;
			}
			return piece;
		}

		return null;
	}

	void SpawnCurrentPiece( PieceType pieceType )
	{
		if ( CurrentPiece.IsValid() )
		{
			CurrentPiece.GameObject.Destroy();
			CurrentPiece = null;
		}

		CurrentPiece = SpawnPiece( pieceType, true );

		if ( CurrentPiece is not null )
		{
			CurrentPiece.Board = this;
		}
	}

	GameObject SpawnBlock( Vector2 position, Color color )
	{
		var blockObject = SceneUtility.Instantiate( GameManager.BlockPrefab, GetPosition( (int)position.x, (int)position.y ) );
		blockObject.SetParent( BlocksContainer );
		var blockScript = blockObject.Components.Get<TetrosBlock>();
		blockScript.Board = this;
		blockScript.Position = position;
		var blockRenderer = blockObject.Components.Get<ModelRenderer>();
		if ( blockRenderer != null )
		{
			blockRenderer.Model = Model.Load( Theme.BlockModel );
			blockRenderer.Tint = color;
		}
		Blocks.Add( blockScript );
		return blockObject;
	}

	public void Move( int dir )
	{
		if ( !CurrentPiece.IsValid() ) return;
		CurrentPiece.Move( new Vector2( dir, 0 ) );
		if ( CheckCurrentPieceCollision() )
		{
			CurrentPiece.Move( new Vector2( -dir, 0 ) );
		}
		if ( dir < 0 )
			PlaySound( Theme.MoveLeftSound );
		else
			PlaySound( Theme.MoveRightSound );

		UpdateGhost();
	}

	public void Rotate( int dir = 1 )
	{
		if ( CurrentPiece is null ) return;
		int prevRot = CurrentPiece.PieceRotation;
		CurrentPiece.PieceRotation += dir;
		while ( CurrentPiece.PieceRotation < 0 ) CurrentPiece.PieceRotation += 4;
		CurrentPiece.PieceRotation %= 4;

		// Check if colliding after we rotate
		if ( CheckCurrentPieceCollision() )
		{
			// If we are colliding, try nudging the piece to the left or right
			if ( CurrentPiece.Position.x < 6 )
			{
				CurrentPiece.Move( new Vector2( 1, 0 ) );
				if ( CheckCurrentPieceCollision() )
				{
					// See if we can move one more time
					CurrentPiece.Move( new Vector2( 1, 0 ) );
					if ( CheckCurrentPieceCollision() )
					{
						// We can't rotate, so move back
						CurrentPiece.Move( new Vector2( -2, 0 ) );
						CurrentPiece.PieceRotation = prevRot;
					}
				}
			}
			else if ( CurrentPiece.Position.x > 6 )
			{
				CurrentPiece.Move( new Vector2( -1, 0 ) );
				if ( CheckCurrentPieceCollision() )
				{
					// We can't rotate, so move back
					CurrentPiece.Move( new Vector2( 1, 0 ) );
					CurrentPiece.PieceRotation = prevRot;
				}
			}
			else
			{
				// We can't rotate, so move back
				CurrentPiece.PieceRotation = prevRot;
			}
		}

		LastUpdate /= 2f;

		if ( dir < 0 )
			PlaySound( Theme.RotateLeftSound );
		else
			PlaySound( Theme.RotateRightSound );

		UpdateGhost();
	}

	public void HardDrop()
	{
		if ( CurrentPiece is null ) return;
		while ( !CheckCurrentPieceCollision() )
		{
			CurrentPiece.Move( new Vector2( 0, 1 ) );
			Score += 2;
		}
		Score -= 2;
		CurrentPiece.Move( new Vector2( 0, -1 ) );
		PlaceCurrentPiece();
		LastUpdate = GetWaitTime() / 4f * 3f;

		NudgeBoard( new Vector2( 0, -0.33f ) );

		UpdateGhost();
	}

	public void Hold()
	{
		if ( JustHeld ) return;
		if ( CurrentPiece is null ) return;

		if ( !HeldPiece.IsValid() )
		{
			HeldPiece = SpawnPiece( CurrentPiece.Type, false );
			CurrentPiece.GameObject.Destroy();
		}
		else
		{
			var heldType = HeldPiece.Type;
			HeldPiece.GameObject.Destroy();
			HeldPiece = SpawnPiece( CurrentPiece.Type, false );
			SpawnCurrentPiece( heldType );
		}
		HeldPiece.Transform.Position = GetPosition( -4, 1 );
		JustHeld = true;
		PlaySound( Theme.HoldSound );

		UpdateGhost();
	}

	Vector2 LastGhostOffset = Vector2.Zero;
	void UpdateGhost()
	{
		if ( !CurrentPiece.IsValid() )
		{
			if ( GhostPiece.IsValid() )
			{
				GhostPiece.GameObject.Destroy();
				GhostPiece = null;
			}
			return;
		}

		if ( GhostPiece.IsValid() && GhostPiece.Type != CurrentPiece.Type )
		{
			GhostPiece.GameObject.Destroy();
			GhostPiece = null;
		}

		if ( !GhostPiece.IsValid() && TetrosSettings.Instance.ShowGhost )
		{
			GhostPiece = SpawnPiece( CurrentPiece.Type, false );
			foreach ( var block in GhostPiece.Container.Children )
			{
				var model = block.Components.Get<ModelRenderer>();
				model.Tint = model.Tint.WithAlpha( 0.2f );
			}
		}

		LastGhostOffset = Vector2.Zero;
		while ( !CheckCurrentPieceCollision( LastGhostOffset ) )
		{
			LastGhostOffset += new Vector2( 0, 1 );
		}
		LastGhostOffset -= new Vector2( 0, 1 );
		GhostPiece?.SetRotation( CurrentPiece.PieceRotation, true );
	}

	bool CheckCurrentPieceCollision( Vector2 offset = default )
	{
		if ( !CurrentPiece.IsValid() ) return true;
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
				int x = (int)pos.x + (i % 4) - 1;
				int y = (int)pos.y + (i / 4) - 1;
				if ( y < 0 )
				{
					EndGame();
					return;
				}

				SpawnBlock( new Vector2( x, y ), CurrentPiece.Color );
			}
		}
		JustHeld = false;
		PlaySound( Theme.PlaceSound );
		CurrentPiece.GameObject.Destroy();
		CurrentPiece = null;

		NudgeBoard( new Vector2( 0, -0.25f ) );
		Camera.Bloom( 0.05f );

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
				var lineBlocks = Blocks.FindAll( b => (int)b.Position.y == y );
				for ( int i = 0; i < lineBlocks.Count(); i++ )
				{
					var block = lineBlocks.ElementAt( i );
					SpawnParticleBurst( block.Position, block.Components.Get<ModelRenderer>().Tint );
					block.GameObject.Destroy();
					Blocks.Remove( block );
				}

				var blocksAbove = Blocks.FindAll( b => (int)b.Position.y < y );
				foreach ( var block in blocksAbove )
				{
					block.Position += new Vector2( 0, 1 );
				}

				Camera.Bloom( 0.2f );

				lines++;
			}
		}

		if ( lines > 0 )
		{
			var sound = PlaySound( lines == 4 ? Theme.TetrosSound : Theme.LineSound );
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
					break;
			}

			if ( Combo > 0 )
			{
				Score += 50 * Combo * Level;
				MusicTimer = 30f;
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

	public int GetLinesNeeded()
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
		if ( GrabBag.Count <= 0 )
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

		UpdateNextPieces();
		return piece;
	}

	public void SpawnParticleBurst( Vector2 position, Color color )
	{
		if ( !TetrosSettings.Instance.ShowParticles ) return;
		var particle = SceneUtility.Instantiate( GameManager.ParticleBurstPrefab, GetPosition( (int)position.x, (int)position.y ) );
		particle.SetParent( GameObject );
		particle.Enabled = true;
		var particleScript = particle.Components.Get<ParticleEffect>();
		particleScript.Tint = color;
	}

	public void NudgeBoard( Vector2 direction )
	{
		if ( !TetrosSettings.Instance.BoardNudging ) return;
		Board.Transform.Position += new Vector3( direction.x * GridSize, 0, direction.y * GridSize );
	}

	public Vector3 GetPosition( int x, int y )
	{
		return Transform.Position + new Vector3( x * GridSize, 0, -y * GridSize );
	}

	public Vector3 GetLocalPosition( int x, int y )
	{
		return new Vector3( x * GridSize, 0, -y * GridSize );
	}

	public bool HasBlock( int x, int y )
	{
		return Blocks.Exists( b => b.Position == new Vector2( x, y ) );
	}

	SoundHandle PlaySound( string soundName )
	{
		if ( string.IsNullOrEmpty( soundName ) ) return new SoundHandle();
		var sound = Sound.Play( soundName, Camera.Transform.Position + Camera.Transform.Rotation.Forward * 25f + Camera.Transform.Rotation.Left * 5f );
		sound.Volume = TetrosSettings.Instance.SfxVolume;
		return sound;
	}

	public void StopMusic()
	{
		SlowMusic.Stop( true );
		FastMusic.Stop( true );
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

	async void RequestHighscore()
	{
		var leaderboard = Sandbox.Services.Leaderboards.Get( "tetros_highscore" );
		await leaderboard.Refresh();
		foreach ( var entry in leaderboard.Entries )
		{
			if ( entry.SteamId == Game.SteamId )
			{
				HighScore = (long)entry.Value;
			}
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