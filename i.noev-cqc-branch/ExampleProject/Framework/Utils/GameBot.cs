using System;
using System.Collections.Generic;
using System.Linq;
using Aquality.Selenium.Core.Logging;
using ExampleProject.Framework.Pages;

namespace ExampleProject.Framework.Utils
{
    internal class GameBot
    {
        private const int BoardSize = 10;

        private readonly MainPage _page;
        private readonly CellStatus[,] _board = new CellStatus[BoardSize, BoardSize];
        private readonly List<Coordinate> _huntQueue = new();
        private readonly Queue<Coordinate> _targetQueue = new();
        private readonly List<Coordinate> _currentShipHits = new();
        private readonly List<int> _remainingShips = new() { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        private readonly Random _random = new();

        public GameBot(MainPage page)
        {
            _page = page;
            InitBoard();
            InitHuntQueue();
        }

        private void InitBoard()
        {
            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    _board[x, y] = CellStatus.Unknown;
                }
            }
        }

        private void InitHuntQueue()
        {
            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    if ((x + y) % 2 == 0)
                    {
                        _huntQueue.Add(new Coordinate(x, y));
                    }
                }
            }
            Shuffle(_huntQueue);
        }

        private static void Shuffle(IList<Coordinate> list)
        {
            var rng = new Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public void Play(int randomPlacements, int maxIterations)
        {
            _page.ClickRandomiseSeveralTimes(randomPlacements);
            _page.ClickRandomOpponent();
            _page.ClickPlayButton();
            _page.WaitForOpponentConnectionMessages();

            int iterations = 0;

            while (!_page.IsGameOver() && iterations < maxIterations)
            {
                iterations++;
                if (_page.IsMyTurn())
                {
                    MakeMove();
                    _page.WaitForTurnTransition();
                }
                else
                {
                    _page.WaitForMyTurnOrGameOver();
                }
            }

            if (iterations >= maxIterations)
            {
                Logger.Instance.Error("Exceeded maximum iterations, infinite loop protection triggered.");
            }
        }

        private void MakeMove()
        {
            var target = GetNextTarget();

            _page.FireAt(target.X, target.Y);
            UpdateStatusAfterFire(target);
        }

        private Coordinate GetNextTarget()
        {
            RebuildTargetQueue();

            while (_targetQueue.Count > 0)
            {
                var target = _targetQueue.Dequeue();
                if (IsCellGoodForShot(target))
                {
                    return target;
                }
            }

            Coordinate huntTarget = FindBestHuntCell();
            while (!IsCellGoodForShot(huntTarget))
            {
                huntTarget = FindBestHuntCell();
            }

            return huntTarget;
        }

        private bool IsCellGoodForShot(Coordinate coordinate)
        {
            if (_board[coordinate.X, coordinate.Y] != CellStatus.Unknown)
            {
                return false;
            }

            var realStatus = _page.GetRivalCellStatus(coordinate.X, coordinate.Y);
            if (realStatus == CellStatus.Unknown)
            {
                return true;
            }

            _board[coordinate.X, coordinate.Y] = realStatus;
            return false;
        }

        private void UpdateStatusAfterFire(Coordinate target)
        {
            int x = target.X;
            int y = target.Y;

            if (_page.IsCellHit(x, y))
            {
                if (_page.IsCellKilled(x, y))
                {
                    Logger.Instance.Info("Ship killed!");
                    _board[x, y] = CellStatus.Killed;
                    _currentShipHits.Add(target);
                    MarkShipSurroundingsAsKilled(_currentShipHits);
                    RemoveKilledShipFromRemaining(_currentShipHits.Count);
                    _currentShipHits.Clear();
                    _targetQueue.Clear();
                }
                else
                {
                    Logger.Instance.Info("Ship hit!");
                    _board[x, y] = CellStatus.Hit;
                    _currentShipHits.Add(target);
                }
            }
            else if (_page.IsCellMiss(x, y))
            {
                Logger.Instance.Info("Miss.");
                _board[x, y] = CellStatus.Miss;
            }
            else
            {
                Logger.Instance.Warn($"Cell {x},{y} status unknown after firing.");
                _board[x, y] = CellStatus.Miss;
            }
        }

        private void RebuildTargetQueue()
        {
            _targetQueue.Clear();

            var hits = _currentShipHits
                .Where(hit => _board[hit.X, hit.Y] == CellStatus.Hit)
                .Distinct()
                .ToList();

            if (hits.Count == 0)
            {
                return;
            }

            if (hits.Count == 1)
            {
                var hit = hits[0];
                EnqueueTargetsByScore(new List<Coordinate>
                {
                    new(hit.X - 1, hit.Y),
                    new(hit.X + 1, hit.Y),
                    new(hit.X, hit.Y - 1),
                    new(hit.X, hit.Y + 1)
                });
                return;
            }

            bool isHorizontal = hits.Select(hit => hit.Y).Distinct().Count() == 1;
            bool isVertical = hits.Select(hit => hit.X).Distinct().Count() == 1;

            if (isHorizontal)
            {
                int y = hits[0].Y;
                EnqueueTargetsByScore(new List<Coordinate>
                {
                    new(hits.Min(hit => hit.X) - 1, y),
                    new(hits.Max(hit => hit.X) + 1, y)
                });
            }
            else if (isVertical)
            {
                int x = hits[0].X;
                EnqueueTargetsByScore(new List<Coordinate>
                {
                    new(x, hits.Min(hit => hit.Y) - 1),
                    new(x, hits.Max(hit => hit.Y) + 1)
                });
            }
        }

        private void EnqueueTargetsByScore(List<Coordinate> candidates)
        {
            var sortedCandidates = candidates
                .Where(cell => IsInsideBoard(cell.X, cell.Y) && _board[cell.X, cell.Y] == CellStatus.Unknown)
                .OrderByDescending(cell => CalculateProbabilityScore(cell.X, cell.Y))
                .ThenBy(_ => _random.Next())
                .ToList();

            foreach (var cell in sortedCandidates)
            {
                _targetQueue.Enqueue(cell);
            }
        }

        private static bool IsInsideBoard(int x, int y)
        {
            return x >= 0 && x < BoardSize && y >= 0 && y < BoardSize;
        }

        private void MarkShipSurroundingsAsKilled(List<Coordinate> shipCells)
        {
            foreach (var c in shipCells)
            {
                _board[c.X, c.Y] = CellStatus.Killed;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = c.X + dx;
                        int ny = c.Y + dy;
                        if (nx >= 0 && nx < BoardSize && ny >= 0 && ny < BoardSize &&
                            _board[nx, ny] == CellStatus.Unknown)
                        {
                            _board[nx, ny] = CellStatus.Miss;
                        }
                    }
                }
            }
        }

        private Coordinate FindUnknownCell()
        {
            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    if (_board[x, y] == CellStatus.Unknown)
                    {
                        return new Coordinate(x, y);
                    }
                }
            }
            throw new NoAvailableCellsException("No unknown cells available on the board.");
        }

        private Coordinate FindBestHuntCell()
        {
            int bestScore = -1;
            var bestCells = new List<Coordinate>();

            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    if (_board[x, y] != CellStatus.Unknown)
                    {
                        continue;
                    }

                    int score = CalculateProbabilityScore(x, y);
                    if ((x + y) % 2 == 0)
                    {
                        score++;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCells.Clear();
                        bestCells.Add(new Coordinate(x, y));
                    }
                    else if (score == bestScore)
                    {
                        bestCells.Add(new Coordinate(x, y));
                    }
                }
            }

            if (bestCells.Count == 0)
            {
                return FindUnknownCell();
            }

            return bestCells[_random.Next(bestCells.Count)];
        }

        private int CalculateProbabilityScore(int targetX, int targetY)
        {
            int score = 0;

            foreach (int shipLength in _remainingShips)
            {
                score += CountPlacementsThroughCell(targetX, targetY, shipLength, horizontal: true);
                score += CountPlacementsThroughCell(targetX, targetY, shipLength, horizontal: false);
            }

            return score;
        }

        private int CountPlacementsThroughCell(int targetX, int targetY, int shipLength, bool horizontal)
        {
            int count = 0;

            for (int offset = 0; offset < shipLength; offset++)
            {
                int startX = horizontal ? targetX - offset : targetX;
                int startY = horizontal ? targetY : targetY - offset;

                if (CanPlaceShip(startX, startY, shipLength, horizontal))
                {
                    count++;
                }
            }

            return count;
        }

        private bool CanPlaceShip(int startX, int startY, int shipLength, bool horizontal)
        {
            for (int i = 0; i < shipLength; i++)
            {
                int x = horizontal ? startX + i : startX;
                int y = horizontal ? startY : startY + i;

                if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize || _board[x, y] != CellStatus.Unknown)
                {
                    return false;
                }
            }

            return true;
        }

        private void RemoveKilledShipFromRemaining(int shipLength)
        {
            if (!_remainingShips.Remove(shipLength))
            {
                Logger.Instance.Warn($"Killed ship length {shipLength} was not found in remaining ships list.");
            }
        }
    }
}
