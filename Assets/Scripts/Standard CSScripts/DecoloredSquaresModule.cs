using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColoredSquares;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Decolored Squares
/// Created by Timwi
/// </summary>
public class DecoloredSquaresModule : ColoredSquaresModuleBase
{
    public override string Name { get { return "Decolored Squares"; } }

    // SEEDED RULES

    // 0 = y++, then x++
    // 1 = y++, then x--    (traditional Chinese reading order)
    // 2 = y--, then x++
    // 3 = y--, then x--
    // 4 = x++, then y++    (standard reading order)
    // 5 = x--, then y++
    // 6 = x++, then y--
    // 7 = x--, then y--    (reverse reading order)
    private int _direction;
    private int _squareForFlowchartStartColumn;
    private int _squareForFlowchartStartRow;
    private int[] _flowchartStartColumnFromColor;   // index is synced with index in _usefulColors; value is column. [5] is the unassigned colum.
    private int[] _flowchartStartRowFromColor;        // index is synced with index in _usefulColors; value is row. [5] is the unassigned row.
    private HashSet<SquareColor>[] _flowchart;
    private int[] _pointsAtY;
    private int[] _pointsAtN;
    private readonly List<int> _updateColors = new List<int>();

    private static readonly SquareColor[] _usefulColors = { SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Yellow, SquareColor.Magenta };
    private static readonly int[] _flowChartDeltaX = new[] { 0, 1, 0, -1 };
    private static readonly int[] _flowChartDeltaY = new[] { -1, 0, 1, 0 };
    private static readonly string[] _directionNames =
    {
        "top-left, down, columns left to right",
        "top-right, down, columns right to left",
        "bottom-left, up, columns left to right",
        "bottom-right, up, columns right to left",
        "top-left, right, rows top to bottom",
        "top-right, left, rows top to bottom",
        "bottom-left, right, rows bottom to top",
        "bottom-right, left, rows bottom to top"
    };

    private int _flowchartPosition;
    private int _modulePosition;

    // for Souvenir
    private string _color1;
    private string _color2;

    void Start()
    {
        var rnd = RuleSeedable.GetRNG();
        Log("Using rule seed: {0}", rnd.Seed);

        var skip = rnd.Next(0, 50);
        for (var i = 0; i < skip; i++)
            rnd.NextDouble();

        _direction = rnd.Next(0, 8);

        _squareForFlowchartStartColumn = rnd.Next(0, 16);
        _squareForFlowchartStartRow = rnd.Next(0, 15);
        if (_squareForFlowchartStartRow >= _squareForFlowchartStartColumn)
            _squareForFlowchartStartRow++;

        var subsets = new List<HashSet<SquareColor>>();
        var colorList = new List<SquareColor>();
        for (var i = 0; i < 32; i++)
        {
            colorList.Clear();
            for (var j = 0; j < 5; j++)
                if ((i & (1 << j)) != 0)
                    colorList.Add(_usefulColors[j]);
            rnd.ShuffleFisherYates(colorList); // this has no impact on the module, but the manual does this
            subsets.Add(new HashSet<SquareColor>(colorList));
        }
        rnd.ShuffleFisherYates(subsets);

        var pointedAt = new bool[36];
        _pointsAtY = new int[36];
        _pointsAtN = new int[36];
        var missingCells = new List<int>();
        do
        {
            missingCells.Clear();

            _flowchartStartColumnFromColor = rnd.ShuffleFisherYates(Enumerable.Range(0, 6).ToArray());
            for (var i = 0; i < 6; i++)
                missingCells.Add(_flowchartStartColumnFromColor[5] + 6 * i);

            _flowchartStartRowFromColor = rnd.ShuffleFisherYates(_flowchartStartColumnFromColor.ToArray());
            for (var i = 0; i < 6; i++)
                if (!missingCells.Contains(i + 6 * _flowchartStartRowFromColor[5]))
                    missingCells.Add(i + 6 * _flowchartStartRowFromColor[5]);

            for (var ix = 0; ix < 36; ix++)
                pointedAt[ix] = false;

            rnd.ShuffleFisherYates(missingCells);
            missingCells.RemoveRange(4, missingCells.Count - 4);

            var pointsAtSquare = new Func<int, int, int?>((px, dir) =>
            {
                var x = px % 6;
                var y = (px / 6) | 0;
                do
                {
                    x += _flowChartDeltaX[dir];
                    y += _flowChartDeltaY[dir];
                    if (x < 0 || x >= 6 || y < 0 || y >= 6)
                        return null;
                }
                while (missingCells.Contains(x + 6 * y));
                return x + 6 * y;
            });

            _flowchart = new HashSet<SquareColor>[36];
            for (var ix = 0; ix < 36; ix++)
            {
                if (missingCells.Contains(ix))
                    continue;

                var dirs = new List<int?>();
                for (var dir = 0; dir < 4; dir++)
                    if (pointsAtSquare(ix, dir) != null)
                        dirs.Add(dir);
                rnd.ShuffleFisherYates(dirs);
                dirs.RemoveRange(2, dirs.Count - 2);    // 0 = no, 1 = yes

                var sbstIx = ix - missingCells.Count(cel => cel < ix);
                _flowchart[ix] = subsets[sbstIx];
                if (subsets[sbstIx].Count == 0)
                    dirs[1] = null;
                else if (subsets[sbstIx].Count == 5)
                    dirs[0] = null;

                for (var yn = 0; yn < 2; yn++)
                {
                    if (dirs[yn] == null)
                        continue;
                    var target = pointsAtSquare(ix, dirs[yn].Value);
                    (yn == 0 ? _pointsAtN : _pointsAtY)[ix] = target.Value;
                    pointedAt[target.Value] = true;
                }
            }
        }
        while (pointedAt.Where((p, pIx) => !p && (!missingCells.Contains(pIx))).Any());

        SetInitialState();
    }

    private void SetInitialState()
    {
        // Must have three colors that occur exactly twice each and two that occur exactly 5 times each
        var colors = _usefulColors.ToList().Shuffle();
        for (int i = 0; i < 16; i++)
            _colors[i] = i < 6 ? colors[i / 2] : colors[(i - 6) / 5 + 3];
        _colors.Shuffle();
        StartSquareColorsCoroutine(_colors, delay: true);

        // Determine starting position in the flowchart.
        var col = _flowchartStartColumnFromColor[Array.IndexOf(_usefulColors, _colors[_squareForFlowchartStartColumn])];
        var row = _flowchartStartRowFromColor[Array.IndexOf(_usefulColors, _colors[_squareForFlowchartStartRow])];
        _flowchartPosition = col + 6 * row;
        _modulePosition = new[] { 0, 3, 12, 15 }[_direction % 4];
        _updateColors.Clear();

        _color1 = _colors[_squareForFlowchartStartColumn].ToString();
        _color2 = _colors[_squareForFlowchartStartRow].ToString();

        Log("{0}={1}, {2}={3} ⇒ Starting position in the flowchart: {4}", convertCoord(_squareForFlowchartStartColumn, 4), _color1, convertCoord(_squareForFlowchartStartRow, 4), _color2, convertCoord(_flowchartPosition, 6));
        Log("Order of processing on the module: {0}", _directionNames[_direction]);
        ProcessCurrentSquare();
    }

    private void ProcessCurrentSquare()
    {
        while (true)
        {
            if (_flowchart[_flowchartPosition].Contains(_colors[_modulePosition]))
            {
                _flowchartPosition = _pointsAtY[_flowchartPosition];
                Log("{0} color is {1}, which is in the flowchart cell, so I expect you to press it. Flowchart position now {2}.", convertCoord(_modulePosition, 4), _colors[_modulePosition], convertCoord(_flowchartPosition, 6));
                return;
            }
            else
            {
                var next = NextSquare(_modulePosition);
                if (next == null)
                {
                    var possibleLastColors = _flowchart[_flowchartPosition].ToArray();
                    if (possibleLastColors.Length == 0)
                    {
                        Log("You ran into the rare case where the last square has you on the empty flowchart cell. Module solves prematurely.");
                        ModulePassed();
                        return;
                    }
                    _colors[_modulePosition] = possibleLastColors[Rnd.Range(0, possibleLastColors.Length)];
                    continue;
                }
                _flowchartPosition = _pointsAtN[_flowchartPosition];
                Log("{0} color is {1}, which is NOT in the flowchart cell. Moving on to {2}. Flowchart position now {3}.", convertCoord(_modulePosition, 4), _colors[_modulePosition], convertCoord(next.Value, 4), convertCoord(_flowchartPosition, 6));
                _updateColors.Add(_modulePosition);
                _modulePosition = next.Value;
            }
        }
    }

    private int? NextSquare(int sq)
    {
        var x = sq % 4;
        var y = sq / 4;
        switch (_direction)
        {
            case 0: y++; if (y > 3) { y = 0; x++; } break;
            case 1: y++; if (y > 3) { y = 0; x--; } break;
            case 2: y--; if (y < 0) { y = 3; x++; } break;
            case 3: y--; if (y < 0) { y = 3; x--; } break;
            case 4: x++; if (x > 3) { x = 0; y++; } break;
            case 5: x--; if (x < 0) { x = 3; y++; } break;
            case 6: x++; if (x > 3) { x = 0; y--; } break;
            case 7: x--; if (x < 0) { x = 3; y--; } break;
        }
        return x == 4 || y == 4 || x < 0 || y < 0 ? (int?) null : (x + 4 * y);
    }

    string convertCoord(int sq, int w)
    {
        return (char) ('A' + (sq % w)) + "" + (char) ('1' + (sq / w));
    }

    protected override void ButtonPressed(int index)
    {
        if (index != _modulePosition)
        {
            Log("{0} was pressed when {1} was expected. Strike and reset.", convertCoord(index, 4), convertCoord(_modulePosition, 4));
            Strike();
            SetInitialState();
        }
        else if (NextSquare(_modulePosition) == null)
        {
            PlaySound(index);
            ModulePassed();
        }
        else
        {
            PlaySound(index);
            Log("{0} pressed correctly.", convertCoord(index, 4));

            // We know this isn’t null because of the above if
            _modulePosition = NextSquare(_modulePosition).Value;

            // Make everything white _including_ the square that was pressed
            var indexes = _updateColors.Select(i => (int?) i).ToList();
            indexes.Add(index);
            foreach (var ix in indexes)
                _colors[ix.Value] = SquareColor.White;
            indexes.Add(null);

            var indexes2 = new List<int?>();
            for (int? i = _modulePosition; i != null; i = NextSquare(i.Value))
            {
                SetButtonBlack(i.Value);
                _colors[i.Value] = _usefulColors[Rnd.Range(0, _usefulColors.Length)];
                indexes2.Add(i.Value);
            }

            _updateColors.Clear();
            ProcessCurrentSquare();

            if (!_isSolved)
            {
                indexes2.Shuffle();
                indexes.AddRange(indexes2);
                StartSquareColorsCoroutine(_colors, indexes.ToArray());
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (IsCoroutineActive)
            yield return true;

        while (!_isSolved)
        {
            Buttons[_modulePosition].OnInteract();
            yield return new WaitForSeconds(.1f);

            while (IsCoroutineActive)
                yield return true;
        }
    }
}
