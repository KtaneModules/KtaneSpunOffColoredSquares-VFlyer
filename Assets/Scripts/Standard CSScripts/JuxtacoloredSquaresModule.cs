using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColoredSquares;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Juxtacolored Squares
/// Created by Asew, Goofy and Timwi
/// </summary>
public class JuxtacoloredSquaresModule : ColoredSquaresModuleBase
{
    public override string Name { get { return "Juxtacolored Squares"; } }

    struct AdjacentColors
    {
        public SquareColor[] LeftRight;
        public SquareColor[] UpDown;
    }

    // Contains the (seeded) rules
    private AdjacentColors[] _table;

    private HashSet<int> _allowedPresses;
    private HashSet<int> _expectedPresses;

    // This order must match the order in the rule-seeded code in the manual
    private static readonly SquareColor[] _allColors = new[] {
        SquareColor.Azure, SquareColor.Black, SquareColor.DarkBlue, SquareColor.Brown,
        SquareColor.Chestnut, SquareColor.Cyan, SquareColor.Forest, SquareColor.Green,
        SquareColor.Gray, SquareColor.Jade, SquareColor.Magenta, SquareColor.Mauve,
        SquareColor.Orange, SquareColor.Purple, SquareColor.Red, SquareColor.Yellow };

    void Start()
    {
        // Start of rule-seed code
        var rnd = RuleSeedable.GetRNG();
        Log("Using rule seed: {0}", rnd.Seed);

        var colors = _allColors.ToArray();
        _table = new AdjacentColors[16];
        for (var row = 0; row < 16; row++)
        {
            rnd.ShuffleFisherYates(colors);
            var theseColors = colors.Where(c => c != _allColors[row]).ToArray();
            _table[row].LeftRight = theseColors.Take(3).ToArray();
            _table[row].UpDown = theseColors.Skip(3).Take(3).ToArray();
            LogDebug("Row {0}: LR {1} // UD {2}", _allColors[row], _table[row].LeftRight.JoinString(", "), _table[row].UpDown.JoinString(", "));
        }
        // End of rule-seed code

        SetInitialState();
    }

    private void SetInitialState()
    {
        tryAgain:

        // Decide on a random arrangement of the 16 colors
        _colors = _allColors.ToArray().Shuffle();

        // Determine which colors need to be pressed
        _expectedPresses = new HashSet<int>();
        for (var i = 0; i < 16; i++)
        {
            var color = Array.IndexOf(_allColors, _colors[i]);
            if (
                i % 4 != 0 && _table[color].LeftRight.Contains(_colors[i - 1]) ||
                i % 4 != 3 && _table[color].LeftRight.Contains(_colors[i + 1]) ||
                i / 4 != 0 && _table[color].UpDown.Contains(_colors[i - 4]) ||
                i / 4 != 3 && _table[color].UpDown.Contains(_colors[i + 4])
            )
                _expectedPresses.Add(i);
        }

        // Make sure at least one needs to be pressed
        if (_expectedPresses.Count == 0)
            goto tryAgain;
        _allowedPresses = new HashSet<int>(_expectedPresses);
        Log("Colors on module: {0}", _colors.JoinString(", "));
        Log("Expected key presses: {0}", _expectedPresses.Select(i => string.Format("{0}{1}", (char) ('A' + (i % 4)), i / 4 + 1)).JoinString(", "));

        StartSquareColorsCoroutine(_colors, delay: true);
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
                _expectedPresses = null;
                _allowedPresses = null;
                ModulePassed();
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
