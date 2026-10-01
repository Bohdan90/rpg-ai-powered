using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPG.Core
{
    // Flat fixture geometry. Default dimensions preserve the original control board.
    public sealed class Battlefield
    {
        // Legacy control-fixture defaults. Use Columns/Rows for a battlefield instance.
        public const int Width = 13;
        public const int Height = 9;
        public static readonly Battlefield ControlMap = new Battlefield();
        public static readonly Battlefield BaseMap = new Battlefield(new[] {
            new GridPosition(6, 3), new GridPosition(6, 4), new GridPosition(6, 5)
        });
        public int Columns { get; }
        public int Rows { get; }
        public bool EastRetreatUsesPerimeter { get; }
        private readonly bool[,] solids;
        public ReadOnlyCollection<GridPosition> SolidCells { get; }

        public Battlefield(IEnumerable<GridPosition> solidCells = null) : this(Width, Height, solidCells) { }

        public Battlefield(int columns, int rows, IEnumerable<GridPosition> solidCells = null, bool eastRetreatUsesPerimeter = false)
        {
            if (columns < 2) throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows < 2) throw new ArgumentOutOfRangeException(nameof(rows));
            Columns = columns; Rows = rows; EastRetreatUsesPerimeter = eastRetreatUsesPerimeter;
            solids = new bool[columns, rows];
            var cells = (solidCells ?? Enumerable.Empty<GridPosition>()).Distinct()
                .OrderBy(p => p.Y).ThenBy(p => p.X).ToList();
            foreach (var cell in cells)
            {
                if (!Contains(cell)) throw new ArgumentOutOfRangeException(nameof(solidCells));
                solids[cell.X, cell.Y] = true;
            }
            SolidCells = cells.AsReadOnly();
        }
        public bool Contains(GridPosition cell) => cell.X >= 0 && cell.X < Columns && cell.Y >= 0 && cell.Y < Rows;
        public bool IsRetreatZone(Side side, GridPosition cell) => IsWalkable(cell)
            && (side == Side.West ? cell.X == 0 : side == Side.East && (cell.X == Columns - 1
                || (EastRetreatUsesPerimeter && (cell.X == 0 || cell.Y == 0 || cell.Y == Rows - 1))));
        public bool IsRetreatZone(UnitState unit, GridPosition cell)
        {
            if(!unit.OwnRetreatEdge.HasValue)return IsRetreatZone(unit.Side,cell);
            if(!IsWalkable(cell))return false;
            switch(unit.OwnRetreatEdge.Value) {
                case RetreatEdge.Unavailable:return false;
                case RetreatEdge.West:return cell.X==0;
                case RetreatEdge.East:return cell.X==Columns-1;
                case RetreatEdge.North:return cell.Y==Rows-1;
                default:return cell.Y==0;
            }
        }
        public bool IsSolid(GridPosition cell) => Contains(cell) && solids[cell.X, cell.Y];
        public bool IsWalkable(GridPosition cell) => Contains(cell) && !IsSolid(cell);
    }
}
