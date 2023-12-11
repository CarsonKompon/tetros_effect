using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace TetrosEffect;

public enum PieceType { Empty, I, O, T, S, Z, J, L };

public class TetrosPiece : Component
{
    [Property] public TetrosBoardManager Board { get; set; }
    [Property] public PieceType Type { get; set; } = PieceType.Empty;
    [Property] public GameObject Container { get; set; }
    [Property] public Vector2 Position { get; set; } = new Vector2( 5, -2 );
    [Property] public int PieceRotation { get; set; } = 0;

    [Property] public List<float> CustomPieceRotations { get; set; } = new List<float>();

    protected override void OnUpdate()
    {
        // Rotate
        var lerp = 1f - MathF.Pow( 0.5f, Time.Delta * 25 );
        var customAngle = PieceRotation * 90f;
        Log.Info( $"rot: {PieceRotation}: {customAngle}" );
        if ( CustomPieceRotations.Count > PieceRotation && CustomPieceRotations[PieceRotation] != -1 )
        {
            customAngle = CustomPieceRotations[PieceRotation];
        }
        Container.Transform.Rotation = Rotation.Lerp( Container.Transform.Rotation, Rotation.From( new Angles( customAngle, 0, 0 ) ), lerp );

        // Lerp to position
        lerp = 1f - MathF.Pow( 0.5f, Time.Delta * 15 );
        var targetPosition = Board.GetPosition( (int)Position.x, (int)Position.y ) + new Vector3( Board.GridSize / 2f, 0, -Board.GridSize / 2f );
        Transform.Position = Vector3.Lerp( Transform.Position, targetPosition, lerp );
    }
}

public class TetrosShape
{
    public int[] Blocks { get; set; }

    public int[] GetGrid()
    {
        var grid = new int[16];
        for ( int i = 0; i < 16; i++ )
        {
            var x = i % 4;
            var y = i / 4;
            if ( Blocks.Contains( i ) )
            {
                grid[i] = 1;
            }
            else
            {
                grid[i] = 0;
            }
        }
        return grid;
    }
}

public static class TetrosShapes
{
    public static readonly TetrosShape[] I = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {4,5,6,7}},
        new TetrosShape {Blocks = new int[4] {2,6,10,14}},
        new TetrosShape {Blocks = new int[4] {4,5,6,7}},
        new TetrosShape {Blocks = new int[4] {2,6,10,14}}
    };

    public static readonly TetrosShape[] O = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {5,6,9,10}},
        new TetrosShape {Blocks = new int[4] {5,6,9,10}},
        new TetrosShape {Blocks = new int[4] {5,6,9,10}},
        new TetrosShape {Blocks = new int[4] {5,6,9,10}}
    };

    public static readonly TetrosShape[] T = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {4,5,6,9}},
        new TetrosShape {Blocks = new int[4] {1,4,5,9}},
        new TetrosShape {Blocks = new int[4] {1,4,5,6}},
        new TetrosShape {Blocks = new int[4] {1,5,6,9}}
    };

    public static readonly TetrosShape[] S = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {5,6,8,9}},
        new TetrosShape {Blocks = new int[4] {0,4,5,9}},
        new TetrosShape {Blocks = new int[4] {5,6,8,9}},
        new TetrosShape {Blocks = new int[4] {0,4,5,9}}
    };

    public static readonly TetrosShape[] Z = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {4,5,9,10}},
        new TetrosShape {Blocks = new int[4] {2,5,6,9}},
        new TetrosShape {Blocks = new int[4] {4,5,9,10}},
        new TetrosShape {Blocks = new int[4] {2,5,6,9}},
    };

    public static readonly TetrosShape[] J = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {4,5,6,10}},
        new TetrosShape {Blocks = new int[4] {1,5,9,8}},
        new TetrosShape {Blocks = new int[4] {0,4,5,6}},
        new TetrosShape {Blocks = new int[4] {1,2,5,9}}
    };

    public static readonly TetrosShape[] L = new TetrosShape[4] {
        new TetrosShape {Blocks = new int[4] {4,5,6,8}},
        new TetrosShape {Blocks = new int[4] {0,1,5,9}},
        new TetrosShape {Blocks = new int[4] {2,4,5,6}},
        new TetrosShape {Blocks = new int[4] {1,5,9,10}}
    };

    public static TetrosShape GetShape( PieceType blockType, int rotation )
    {
        if ( rotation < 0 ) rotation += 4;
        switch ( blockType )
        {
            case PieceType.I:
                return I[rotation];
            case PieceType.O:
                return O[rotation];
            case PieceType.T:
                return T[rotation];
            case PieceType.S:
                return S[rotation];
            case PieceType.Z:
                return Z[rotation];
            case PieceType.J:
                return J[rotation];
            case PieceType.L:
                return L[rotation];
            default:
                return I[rotation];
        }
    }
}