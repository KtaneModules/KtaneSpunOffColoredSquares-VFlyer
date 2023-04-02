using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColoredSquares;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Uncolored Squares
/// Created by Timwi
/// </summary>
public class UncoloredSquaresModule : ColoredSquaresModuleBase
{
    public override string Name { get { return "Uncolored Squares"; } }

#pragma warning disable 414
    private SquareColor _firstStageColor1;   // for Souvenir
    private SquareColor _firstStageColor2;   // for Souvenir
#pragma warning restore 414

    private readonly HashSet<int> _squaresPressedThisStage = new HashSet<int>();
    private List<List<int>> _permissiblePatterns;
    private bool[][][,] _table;

    private static readonly bool[][,] _alwaysShapes = newArray(b("##"), b("#|#"));
    private static readonly bool[][,] _sometimesShapes = newArray(
        b("#|##"), b("###| #"), b(" ##|##"), b(" #|##| #"), b("###"), b("#|#|#"), b(" #|##|#"), b("##|#|#"), b("  #|###"), b("#|##|#"), b(" #|##"), b(" #| #|##"),
        b("##|##"), b("###|  #"), b("##| #"), b(" #|###"), b("##| ##"), b("#|###"), b("#|#|##"), b("##|#"), b("##| #| #"), b("###|#"), b("#|##| #"));

    private static bool[,] b(string v)
    {
        var rows = v.Split('|');
        var w = rows.Max(row => row.Length);
        var h = rows.Length;
        var arr = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (x < rows[y].Length)
                    arr[x, y] = rows[y][x] == '#';
        return arr;
    }

    static T[] newArray<T>(params T[] array) { return array; }

    void Start()
    {
        var rnd = RuleSeedable.GetRNG();
        Log("Using rule seed: {0}", rnd.Seed);

        var shapes = new List<bool[,]>();
        var extraShapes = rnd.ShuffleFisherYates(_sometimesShapes.ToArray());
        for (var i = 0; i < 18; i++)
            shapes.Add(extraShapes[i]);

        // Sneaky! Put the two “alwaysShapes” in the right place to recreate original rules under Seed #1
        shapes.Insert(8, _alwaysShapes[0]);
        shapes.Insert(8, _alwaysShapes[1]);
        rnd.ShuffleFisherYates(shapes);

        _table = newArray(
            new bool[][,] { null, shapes[0], shapes[1], shapes[2], shapes[3] },
            new bool[][,] { shapes[4], null, shapes[5], shapes[6], shapes[7] },
            new bool[][,] { shapes[8], shapes[9], null, shapes[10], shapes[11] },
            new bool[][,] { shapes[12], shapes[13], shapes[14], null, shapes[15] },
            new bool[][,] { shapes[16], shapes[17], shapes[18], shapes[19], null });

        SetStage(isStart: true);
    }

    private void SetStage(bool isStart)
    {
        if (isStart)
        {
            for (int i = 0; i < 16; i++)
            {
                _colors[i] = SquareColor.White;
                SetButtonBlack(i);
            }
        }
        else
        {
            var sq = 0;
            for (int i = 0; i < 16; i++)
            {
                switch (_colors[i])
                {
                    case SquareColor.Black:
                        break;

                    case SquareColor.Red:
                    case SquareColor.Green:
                    case SquareColor.Blue:
                    case SquareColor.Yellow:
                    case SquareColor.Magenta:
                        SetButtonBlack(i);
                        sq++;
                        break;

                    case SquareColor.White:
                        _colors[i] = SquareColor.Black;
                        break;
                }
            }
            if (sq <= 3)
            {
                ModulePassed();
                return;
            }
        }

        // Check all color combinations and find all valid pattern placements
        var order = new[] { SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Yellow, SquareColor.Magenta };
        var validCombinations = Enumerable.Range(0, 5).SelectMany(first => Enumerable.Range(0, 5).Select(second =>
        {
            if (first == second)
                return null;
            var pattern = _table[second][first];
            var w = pattern.GetLength(0);
            var h = pattern.GetLength(1);
            var placements = new List<List<int>>();
            for (int i = 0; i < 16; i++)
            {
                if (i % 4 + w > 4 || i / 4 + h > 4)
                    continue;
                var placement = new List<int>();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (pattern[x, y])
                        {
                            var ix = i % 4 + x + 4 * (i / 4 + y);
                            if (_colors[ix] == SquareColor.Black)
                                goto nope;
                            placement.Add(ix);
                        }
                placements.Add(placement);
                nope:;
            }
            return placements.Count == 0 ? null : new { First = order[first], Second = order[second], Placements = placements };
        })).Where(inf => inf != null).ToArray();

        if (validCombinations.Length == 0)
        {
            ModulePassed();
            return;
        }

        // Fill the still-lit squares with “codes” (numbers 0–5 that we will later map to actual colors)
        // in such a way that there’s a two-way tie for fewest number of occurrences
        int[] colorCodes = new int[16];
        tryAgain:
        var counts = new int[5];
        for (int i = 0; i < 16; i++)
            if (_colors[i] != SquareColor.Black)
            {
                var col = Rnd.Range(0, 5);
                colorCodes[i] = col;
                counts[col]++;
            }
            else
                colorCodes[i] = -1;
        var minCount = counts.Where(c => c > 0).Min();
        var minCountCodes = Enumerable.Range(0, 5).Where(code => counts[code] == minCount).OrderBy(c => Array.IndexOf(colorCodes, c)).ToArray();
        if (minCountCodes.Length != 2)
            goto tryAgain;

        // Pick a color combination at random
        var combination = validCombinations[Rnd.Range(0, validCombinations.Length)];

        // Create the map from color code to actual color in such a way that the chosen colors are in the correct place
        var allColors = new List<SquareColor> { SquareColor.Blue, SquareColor.Green, SquareColor.Magenta, SquareColor.Red, SquareColor.Yellow };
        allColors.Remove(combination.First);
        allColors.Remove(combination.Second);
        if (minCountCodes[0] > minCountCodes[1])
        {
            allColors.Insert(minCountCodes[1], combination.Second);
            allColors.Insert(minCountCodes[0], combination.First);
        }
        else
        {
            allColors.Insert(minCountCodes[0], combination.First);
            allColors.Insert(minCountCodes[1], combination.Second);
        }

        // Assign the colors
        for (int i = 0; i < 16; i++)
            if (_colors[i] != SquareColor.Black)
                _colors[i] = allColors[colorCodes[i]];

        if (isStart)
        {
            _firstStageColor1 = combination.First;
            _firstStageColor2 = combination.Second;
        }

        Log("{0} stage color pair is {1}/{2}", isStart ? "First" : "Next", combination.First, combination.Second);
        _permissiblePatterns = combination.Placements;
        _squaresPressedThisStage.Clear();
        StartSquareColorsCoroutine(_colors, delay: true);
    }

    protected override void ButtonPressed(int index)
    {
        if (!_permissiblePatterns.Any(p => p.Contains(index)))
        {
            Log("Button {0}{1} was incorrect at this time. Resetting module.", "ABCD"[index % 4], "1234"[index / 4]);
            Strike();
            SetStage(isStart: true);
        }
        else
        {
            PlaySound(index);
            _permissiblePatterns.RemoveAll(lst => !lst.Contains(index));
            _squaresPressedThisStage.Add(index);
            _colors[index] = SquareColor.White;
            SetButtonColor(index, SquareColor.White);

            if (_permissiblePatterns.Count == 1 && _squaresPressedThisStage.Count == _permissiblePatterns[0].Count)
                SetStage(isStart: false);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            var pattern = _permissiblePatterns[0];
            foreach (var index in pattern)
            {
                Buttons[index].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            while (IsCoroutineActive)
                yield return true;
        }
    }
}
