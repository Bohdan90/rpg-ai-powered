using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPG.Core
{
    // Contract v0.1: one flat 13x9 board, with solid cells only.
    public sealed class Battlefield
    {
        public const int Width = 13;
        public const int Height = 9;
        public static readonly Battlefield ControlMap = new Battlefield();
        public static readonly Battlefield BaseMap = new Battlefield(new[] {
            new GridPosition(6, 3), new GridPosition(6, 4), new GridPosition(6, 5)
        });
        private readonly bool[,] solids = new bool[Width, Height];
        public ReadOnlyCollection<GridPosition> SolidCells { get; }

        public Battlefield(IEnumerable<GridPosition> solidCells = null)
        {
            var cells = (solidCells ?? Enumerable.Empty<GridPosition>()).Distinct()
                .OrderBy(p => p.Y).ThenBy(p => p.X).ToList();
            foreach (var cell in cells)
            {
                if (!Contains(cell)) throw new ArgumentOutOfRangeException(nameof(solidCells));
                solids[cell.X, cell.Y] = true;
            }
            SolidCells = cells.AsReadOnly();
        }
        public bool Contains(GridPosition cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        public bool IsSolid(GridPosition cell) => Contains(cell) && solids[cell.X, cell.Y];
        public bool IsWalkable(GridPosition cell) => Contains(cell) && !IsSolid(cell);
    }
}
