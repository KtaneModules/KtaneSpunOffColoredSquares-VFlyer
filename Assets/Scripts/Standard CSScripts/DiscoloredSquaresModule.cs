using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColoredSquares;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Discolored Squares
/// Created by Timwi and EpicToast
/// </summary>
public class DiscoloredSquaresModule : ColoredSquaresModuleBase
{
    public override string Name { get { return "Discolored Squares"; } }

    private static readonly SquareColor[] _usefulColors = new[] { SquareColor.Blue, SquareColor.Green, SquareColor.Magenta, SquareColor.Red, SquareColor.Yellow };

    // Contains the (seeded) rules
    private Instruction[] _instructions;
    private int[][] _ordersByStage;
    private SquareColor[] _rememberedColors;
    private SquareColor _neutralColor;
    private int[] _rememberedPositions;
    private int _stage; // 0 = pre-stage 1; 1..4 = stage 1..4
    private List<int> _expectedPresses;
    private int _subprogress;

    void Start()
    {
        var rnd = RuleSeedable.GetRNG();
        Log("Using rule seed: {0}", rnd.Seed);

        var skip = rnd.Next(0, 6);
        for (var i = 0; i < skip; i++)
            rnd.NextDouble();
        _instructions = rnd.ShuffleFisherYates((Instruction[]) Enum.GetValues(typeof(Instruction)));

        var numbers = Enumerable.Range(0, 16).ToArray();
        _ordersByStage = new int[5][];
        for (var stage = 0; stage < 4; stage++)
        {
            rnd.ShuffleFisherYates(numbers);
            _ordersByStage[stage] = numbers.ToArray();
        }

        SetInitialState();
    }

    private void SetInitialState()
    {
        SetAllButtonsBlack();

        // Decide which color is the “neutral” color (the remaining four are the “live” ones)
        var colors = _usefulColors.ToArray().Shuffle();
        _rememberedColors = colors.Subarray(0, 4);  // this array will be reordered as the player presses them
        _rememberedPositions = Enumerable.Range(0, 16).ToArray().Shuffle().Subarray(0, 4);  // will be re-populated as the player presses them
        _neutralColor = colors[4];
        _stage = 0;
        _subprogress = 0;

        for (int i = 0; i < 16; i++)
            _colors[i] = _neutralColor;
        for (int i = 0; i < 4; i++)
            _colors[_rememberedPositions[i]] = _rememberedColors[i];

        Log("Initial colors are: {0}", _rememberedColors.Select((c, cIx) => string.Format("{0} at {1}", c, coord(_rememberedPositions[cIx]))).Join(", "));
        StartSquareColorsCoroutine(_colors, delay: true);
    }

    private static string coord(int ix) { return ((char) ('A' + (ix % 4))) + "" + (ix / 4 + 1); }

    protected override void ButtonPressed(int index)
    {
        if (_stage == 0)
        {
            // Preliminary stage in which the player presses the four “live” colors in any order of their choice
            if (_colors[index] == SquareColor.White)    // ignore re-presses
                return;
            if (_colors[index] == _neutralColor)
            {
                Log("During the preliminary stage, you pressed a square that wasn’t one of the singular colors. Strike.");
                Strike();
                SetInitialState();
                return;
            }
            PlaySound(index);
            _rememberedColors[_subprogress] = _colors[index];
            _rememberedPositions[_subprogress] = index;
            _subprogress++;

            // If all colors have been pressed, initialize stage 1
            if (_subprogress == 4)
            {
                Log("You pressed them in this order: {0}", Enumerable.Range(0, 4).Select(ix => string.Format("{0} ({1})", coord(_rememberedPositions[ix]), _rememberedColors[ix])).Join(", "));
                SetAllButtonsBlack();
                SetStage(1);
            }
            else
            {
                _colors[index] = SquareColor.White;
                SetButtonColor(index, SquareColor.White);
            }
            return;
        }

        if (index != _expectedPresses[_subprogress])
        {
            Log("Expected {0}, but you pressed {1}. Strike. Module resets.", coord(_expectedPresses[_subprogress]), coord(index));
            Strike();
            SetInitialState();
            return;
        }

        PlaySound(index);
        _subprogress++;
        _colors[index] = SquareColor.White;
        SetButtonColor(index, SquareColor.White);
        Log("{0} was correct.", coord(index));
        if (_subprogress == _expectedPresses.Count)
            SetStage(_stage + 1);
    }

    private void SetStage(int stage)
    {
        _stage = stage;
        _subprogress = 0;
        for (int i = 0; i < 16; i++)
            if (_colors[i] != SquareColor.White)
                SetButtonBlack(i);

        if (stage == 5)
        {
            ModulePassed();
            return;
        }
        Log("On to stage {0}.", _stage);

        // Put 2–3 of the active color in that many random squares
        var availableSquares = Enumerable.Range(0, 16).Where(ix => stage == 1 || _colors[ix] != SquareColor.White).ToList().Shuffle();
        var take = Math.Min(stage == 1 ? 3 : Rnd.Range(2, 4), availableSquares.Count);
        for (int i = 0; i < take; i++)
            _colors[availableSquares[i]] = _rememberedColors[stage - 1];

        // Fill the rest of the grid with the other colors
        var otherColors = _usefulColors.Where(c => c != _rememberedColors[stage - 1]).ToArray();
        for (int i = take; i < availableSquares.Count; i++)
            _colors[availableSquares[i]] = otherColors.PickRandom();

        var relevantSquares = availableSquares.Take(take).OrderBy(sq => _ordersByStage[stage - 1][sq]).ToArray();
        Log("Stage {0}: {1} squares in the correct order are {2}.", stage, _rememberedColors[stage - 1], relevantSquares.Select(sq => coord(sq)).Join(", "));

        // Process the active squares in the correct order for this stage to compute the intended solution
        _expectedPresses = new List<int>();
        foreach (var activeSquare in relevantSquares)
        {
            if (_expectedPresses.Contains(activeSquare))    // square already became white in this stage
            {
                Log("— {0} has already become white. Skip it.", coord(activeSquare));
                continue;
            }
            var solutionSquare = activeSquare;
            do
                solutionSquare = process(solutionSquare, _instructions[_rememberedPositions[stage - 1]]);
            while (_colors[solutionSquare] == SquareColor.White || _expectedPresses.Contains(solutionSquare));
            Log("— {0} / {1} translates to {2}", coord(activeSquare), _instructions[_rememberedPositions[stage - 1]], coord(solutionSquare));
            _expectedPresses.Add(solutionSquare);
        }

        StartSquareColorsCoroutine(_colors, delay: true);
    }

    private int process(int sq, Instruction instruction)
    {
        int x = sq % 4, y = sq / 4;
        int x2 = x, y2 = y;
        switch (instruction)
        {
            case Instruction.MoveUpLeft: x += 3; y += 3; break;
            case Instruction.MoveUp: y += 3; break;
            case Instruction.MoveUpRight: x++; y += 3; break;
            case Instruction.MoveRight: x++; break;
            case Instruction.MoveDownRight: x++; y++; break;
            case Instruction.MoveDown: y++; break;
            case Instruction.MoveDownLeft: x += 3; y++; break;
            case Instruction.MoveLeft: x += 3; break;
            case Instruction.MirrorHorizontally: x = 3 - x; break;
            case Instruction.MirrorVertically: y = 3 - y; break;
            case Instruction.MirrorDiagonallyA1D4: x = y2; y = x2; break;
            case Instruction.MirrorDiagonallyA4D1: x = 3 - y2; y = 3 - x2; break;
            case Instruction.Rotate90CW: y = x2; x = 3 - y2; break;
            case Instruction.Rotate90CCW: y = 3 - x2; x = y2; break;
            case Instruction.Rotate180: x = 3 - x; y = 3 - y; break;
            default: break;
        }
        return (x % 4) + 4 * (y % 4);
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (IsCoroutineActive)
            yield return true;

        // Press the four “live” colors in the preliminary stage
        for (var i = 0; i < 16 && _stage == 0; i++)
            if (_colors[i] != SquareColor.White && _colors[i] != _neutralColor)
            {
                Buttons[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }

        while (IsCoroutineActive)
            yield return true;

        while (!_isSolved)
        {
            Buttons[_expectedPresses[_subprogress]].OnInteract();
            yield return new WaitForSeconds(.1f);

            while (IsCoroutineActive)
                yield return true;
        }
    }
}
