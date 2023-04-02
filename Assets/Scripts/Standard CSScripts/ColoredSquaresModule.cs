using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColoredSquares;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Colored Squares
/// Created by TheAuthorOfOZ, implemented by Timwi
/// </summary>
public class ColoredSquaresModule : ColoredSquaresModuleBase
{
    public override string Name { get { return "Colored Squares"; } }

    // Contains the (seeded) rules
    private object[][] _table;

    private HashSet<int> _allowedPresses;
    private HashSet<int> _expectedPresses;
    private object _lastStage;
    private SquareColor _firstStageColor;   // for Souvenir
    private static readonly SquareColor[] _colorsInUse = new[] { SquareColor.Red, SquareColor.Blue, SquareColor.Green, SquareColor.Yellow, SquareColor.Magenta };

    static T[] newArray<T>(params T[] array) { return array; }

    void Start()
    {
        var rnd = RuleSeedable.GetRNG();
        Log("Using rule seed: {0}", rnd.Seed);
        if (rnd.Seed == 1)
        {
            // false = Column; true = Row
            _table = newArray(
                new object[] { SquareColor.Blue, false, SquareColor.Red, SquareColor.Yellow, true, SquareColor.Green, SquareColor.Magenta },
                new object[] { true, SquareColor.Green, SquareColor.Blue, SquareColor.Magenta, SquareColor.Red, false, SquareColor.Yellow },
                new object[] { SquareColor.Yellow, SquareColor.Magenta, SquareColor.Green, true, SquareColor.Blue, SquareColor.Red, false },
                new object[] { SquareColor.Blue, SquareColor.Green, SquareColor.Yellow, false, SquareColor.Red, true, SquareColor.Magenta },
                new object[] { SquareColor.Yellow, true, SquareColor.Blue, SquareColor.Magenta, false, SquareColor.Red, SquareColor.Green },
                new object[] { SquareColor.Magenta, SquareColor.Red, SquareColor.Yellow, SquareColor.Green, false, SquareColor.Blue, true },
                new object[] { SquareColor.Green, true, false, SquareColor.Blue, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Red },
                new object[] { SquareColor.Magenta, SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Yellow, false, true },
                new object[] { false, SquareColor.Yellow, SquareColor.Red, SquareColor.Green, true, SquareColor.Magenta, SquareColor.Blue },
                new object[] { SquareColor.Green, false, true, SquareColor.Red, SquareColor.Magenta, SquareColor.Blue, SquareColor.Yellow },
                new object[] { SquareColor.Red, SquareColor.Yellow, true, false, SquareColor.Green, SquareColor.Magenta, SquareColor.Blue },
                new object[] { false, SquareColor.Blue, SquareColor.Magenta, SquareColor.Red, SquareColor.Yellow, true, SquareColor.Green },
                new object[] { true, SquareColor.Magenta, false, SquareColor.Yellow, SquareColor.Blue, SquareColor.Green, SquareColor.Red },
                new object[] { SquareColor.Red, SquareColor.Blue, SquareColor.Magenta, true, SquareColor.Green, SquareColor.Yellow, false },
                new object[] { false, true, false, true, false, true, false }
            );
        }
        else
        {
            var candidates = new object[] { SquareColor.Blue, false, SquareColor.Red, SquareColor.Yellow, true, SquareColor.Green, SquareColor.Magenta };
            _table = new object[15][];
            for (int i = 0; i < 14; i++)
                _table[i] = rnd.ShuffleFisherYates(candidates).ToArray();
            _table[14] = new object[] { false, true, false, true, false, true, false };
        }

        SetInitialState();
    }

    private void SetInitialState()
    {
        tryAgain:
        var counts = new Dictionary<SquareColor, int>();
        foreach (var c in _colorsInUse)
            counts[c] = 1;
        var indexes = Enumerable.Range(0, 16).ToList().Shuffle().Take(_colorsInUse.Length).ToArray();
        for (var i = 0; i < _colorsInUse.Length; i++)
            _colors[indexes[i]] = _colorsInUse[i];
        for (int i = 0; i < 16; i++)
            if (!indexes.Contains(i))
            {
                _colors[i] = _colorsInUse.PickRandom();
                counts[_colors[i]]++;
            }
        var minCount = _colorsInUse.Min(c => counts[c]);
        if (counts.Count(kvp => kvp.Value == minCount) > 1)
            goto tryAgain;

        var minCountColor = _colorsInUse.First(c => counts[c] == minCount);

        _firstStageColor = minCountColor;
        _lastStage = minCountColor;
        _expectedPresses = new HashSet<int>();
        _allowedPresses = new HashSet<int>();
        for (int i = 0; i < 16; i++)
            if (_colors[i] == minCountColor)
            {
                _allowedPresses.Add(i);
                _expectedPresses.Add(i);
            }
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.NonwhiteOnly, delay: true);
        Log("First stage color is {0}. Count: {1}.", _firstStageColor, minCount);
        LogDebug("Colors: {0}", _colors.JoinString(", "));
    }

    protected override void ButtonPressed(int index)
    {
        if (_isSolved)
            return;

        if (!_allowedPresses.Contains(index))
        {
            Log(@"Button #{0} ({1}) was incorrect at this time.", index, _colors[index]);
            Strike();
            SetInitialState();
        }
        else
        {
            PlaySound(index);
            _expectedPresses.Remove(index);
            _colors[index] = SquareColor.White;
            SetButtonColor(index, SquareColor.White);
            if (_expectedPresses.Count == 0)
            {
                var whiteCount = _colors.Count(c => c == SquareColor.White);
                if (whiteCount == 16)
                {
                    _expectedPresses = null;
                    _allowedPresses = null;
                    ModulePassed();
                }
                else
                {
                    _allowedPresses.Clear();

                    var nonWhite = Enumerable.Range(0, 16).Where(i => _colors[i] != SquareColor.White).ToArray();
                    foreach (var i in nonWhite)
                    {
                        SetButtonBlack(i);
                        _colors[i] = _colorsInUse.PickRandom();
                    }

                    // Move to next stage.
                    var columnIx = _lastStage.Equals(true) ? 5 : _lastStage.Equals(false) ? 6 : Array.IndexOf(_colorsInUse, (SquareColor) _lastStage);
                    var nextStage = _table[whiteCount - 1][columnIx];
                    Log("{0} lit: next stage is {1}.", whiteCount, nextStage.Equals(true) ? "Row" : nextStage.Equals(false) ? "Column" : ((SquareColor) nextStage).ToString());
                    if (nextStage.Equals(true))
                    {
                        // Row
                        var firstRow = Enumerable.Range(0, 4).First(row => Enumerable.Range(0, 4).Any(col => _colors[4 * row + col] != SquareColor.White));
                        for (int col = 0; col < 4; col++)
                            if (_colors[4 * firstRow + col] != SquareColor.White)
                            {
                                _allowedPresses.Add(4 * firstRow + col);
                                _expectedPresses.Add(4 * firstRow + col);
                            }
                    }
                    else if (nextStage.Equals(false))
                    {
                        // Column
                        var firstCol = Enumerable.Range(0, 4).First(col => Enumerable.Range(0, 4).Any(row => _colors[4 * row + col] != SquareColor.White));
                        for (int row = 0; row < 4; row++)
                            if (_colors[4 * row + firstCol] != SquareColor.White)
                            {
                                _allowedPresses.Add(4 * row + firstCol);
                                _expectedPresses.Add(4 * row + firstCol);
                            }
                    }
                    else
                    {
                        // A specific color
                        // Make sure at least one square has that color
                        var color = (SquareColor) nextStage;
                        _colors[nonWhite[Rnd.Range(0, nonWhite.Length)]] = color;
                        for (int i = 0; i < 16; i++)
                            if (_colors[i] == color)
                            {
                                _allowedPresses.Add(i);
                                _expectedPresses.Add(i);
                            }
                    }
                    _lastStage = nextStage;
                    StartSquareColorsCoroutine(_colors, SquaresToRecolor.NonwhiteOnly, delay: true);
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            Buttons[_expectedPresses.First()].OnInteract();
            yield return new WaitForSeconds(.1f);

            while (IsCoroutineActive)
                yield return true;
        }
    }
}
