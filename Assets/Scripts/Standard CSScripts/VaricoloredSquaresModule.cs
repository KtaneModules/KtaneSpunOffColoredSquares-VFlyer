using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ColoredSquares;
using UnityEngine;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Varicolored Squares
/// Created and implemented by ZekNikZ upon the foundation of Colored Squares by Timwi
/// </summary>
public class VaricoloredSquaresModule : ColoredSquaresModuleBase
{
    public override string Name { get { return "Varicolored Squares"; } }

    private readonly SquareColor[] _colorCandidates = new[] { SquareColor.Blue, SquareColor.Red, SquareColor.Yellow, SquareColor.Green, SquareColor.Magenta };

    // Contains the (seeded) rules
    private Dictionary<SquareColor, SquareColor[]> _table;
    private int _ruleOneDirection;
    private int _backupDirection;

    private HashSet<int> _allowedPresses;
    private HashSet<int> _updateIndices;
    private SquareColor _lastPressedColor;
    private SquareColor _nextColor;
    private int _startingPosition;
    private SquareColor _firstStageColor; // for Souvenir
    private int _lastPress;
    private HashSet<int> _lastArea = new HashSet<int>();
    private int _pressesWithoutChange = 0;
    private Coroutine _activeCoroutine;
    private int _animating = 0;

    void Start()
    {
        GenerateRules();
        SetInitialState();
    }

    private void SetInitialState()
    {
        for (int i = 0; i < 5; i++)
        {
            _colors[3 * i] = _colorCandidates[i];
            _colors[(3 * i) + 1] = _colorCandidates[i];
            _colors[(3 * i) + 2] = _colorCandidates[i];
        }

        _firstStageColor = _colorCandidates[Rnd.Range(0, _colorCandidates.Length)];
        _colors[15] = _firstStageColor;
        _colors.Shuffle();

        _allowedPresses = new HashSet<int>();
        _lastPressedColor = _firstStageColor;
        _startingPosition = -1;
        _lastPress = -1;

        for (int i = 0; i < 16; i++)
            if (_colors[i] == _firstStageColor)
                _allowedPresses.Add(i);

        Log("Initial state: {0}", _colors.Select(c => c.ToString()[0]).JoinString(" "));
        StartSquareColorsCoroutine(_colors, delay: true);
        Log("First color to press is {0}.", _firstStageColor);
    }

    private IEnumerator SetSquareColors(bool delay, bool solve = false)
    {
        _animating++;
        if (delay)
            yield return new WaitForSeconds(Rnd.Range(1.5f, 2f));

        var sequence = _updateIndices != null ? _updateIndices.ToList().Shuffle() : Enumerable.Range(0, 16).ToList().Shuffle();
        for (int i = 0; i < sequence.Count; i++)
        {
            SetButtonColor(sequence[i], _colors[sequence[i]]);
            yield return new WaitForSeconds(.03f);
        }

        if (solve)
        {
            ModulePassed();
            _activeCoroutine = null;
        }
        else
            _activeCoroutine = StartCoroutine(BlinkLastSquare());

        _updateIndices = null;
        _animating--;
    }

    private IEnumerator BlinkLastSquare()
    {
        bool lit = false;

        while (_lastPress != -1)
        {
            SetButtonColor(_lastPress, lit ? SquareColor.White : _colors[_lastPress]);
            lit = !lit;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        _activeCoroutine = null;
    }

    private void SpreadColor(SquareColor oldColor, SquareColor newColor, int index)
    {
        _colors[index] = newColor;
        _updateIndices.Add(index);

        if (index - 4 >= 0 && _colors[index - 4] == oldColor) SpreadColor(oldColor, newColor, index - 4);
        if (index + 4 < 16 && _colors[index + 4] == oldColor) SpreadColor(oldColor, newColor, index + 4);
        if ((index - 1) % 4 < index % 4 && index - 1 >= 0 && _colors[index - 1] == oldColor) SpreadColor(oldColor, newColor, index - 1);
        if ((index + 1) % 4 > index % 4 && index + 1 < 16 && _colors[index + 1] == oldColor) SpreadColor(oldColor, newColor, index + 1);
    }

    private void GenerateRules()
    {
        var rnd = RuleSeedable.GetRNG();

        // Add more random spread
        for (int i = 0; i < 13; i++)
            rnd.Next();

        // Generate color pentagons
        _table = new Dictionary<SquareColor, SquareColor[]>();
        var origColors = new[] { SquareColor.Red, SquareColor.Blue, SquareColor.Green, SquareColor.Yellow, SquareColor.Magenta };
        for (int i = 0; i < 5; i++)
        {
            rnd.ShuffleFisherYates(_colorCandidates);
            _table[origColors[i]] = _colorCandidates.ToArray();
            LogDebug("Rule Generator: {0} pentagon is: {1}", origColors[i], _table[origColors[i]].Select(c => c.ToString()[0]).JoinString("-"));
        }

        // Random spread
        rnd.Next();
        rnd.Next();
        rnd.Next();

        // Generate directions
        _ruleOneDirection = (rnd.Next(2) * 2) - 1;
        _backupDirection = (rnd.Next(2) * 2) - 1;
        LogDebug("Rule Generator: rule one direction is: {0}", _ruleOneDirection == -1 ? "counter-clockwise" : "clockwise");
        LogDebug("Rule Generator: backup rule direction is: {0}", _backupDirection == -1 ? "counter-clockwise" : "clockwise");

    }

    private HashSet<int> CalculateNewAllowedPresses(int index)
    {
        var result = new HashSet<int>();

        SquareColor[] pentagon = _table[_colors[index]];

        var adjacentColors = new List<SquareColor>();
        if (index - 4 >= 0) adjacentColors.Add(_colors[index - 4]);
        if (index + 4 < 16) adjacentColors.Add(_colors[index + 4]);
        if ((index - 1) % 4 < index % 4 && index - 1 >= 0) adjacentColors.Add(_colors[index - 1]);
        if ((index + 1) % 4 > index % 4 && index + 1 < 16) adjacentColors.Add(_colors[index + 1]);

        adjacentColors = adjacentColors.Distinct().OrderBy(c => Array.IndexOf(pentagon, c)).ToList();

        int c0, c1;
        switch (adjacentColors.Count())
        {
            case 1: // press next color in circle
                c0 = Array.IndexOf(pentagon, adjacentColors[0]);
                _nextColor = pentagon[(c0 + _ruleOneDirection + 5) % 5];
                break;

            case 2:
                c0 = Array.IndexOf(pentagon, adjacentColors[0]);
                c1 = Array.IndexOf(pentagon, adjacentColors[1]);
                if (c1 - c0 == 1)
                {
                    // colors are adjacent
                    _nextColor = pentagon[(c1 + 2) % 5];
                }
                else if (c0 - c1 == -4)
                {
                    // colors are adjacent (special case)
                    _nextColor = pentagon[(c0 + 2) % 5];
                }
                else
                {
                    // colors are split
                    _nextColor = (c0 + c1) % 2 == 0 ? pentagon[(c0 + c1) / 2] : pentagon[(c1 + 1) % 5];
                }
                break;

            case 3:
                var nonPresentColors = _colorCandidates.Where(c => !adjacentColors.Contains(c)).OrderBy(c => Array.IndexOf(pentagon, c)).ToList();
                c0 = Array.IndexOf(pentagon, nonPresentColors[0]);
                c1 = Array.IndexOf(pentagon, nonPresentColors[1]);
                if (c1 - c0 == 1)
                {
                    // colors are adjacent
                    _nextColor = pentagon[(c1 + 2) % 5];
                }
                else if (c0 - c1 == -4)
                {
                    // colors are adjacent (special case)
                    _nextColor = pentagon[(c0 + 2) % 5];
                }
                else
                {
                    // colors are split
                    _nextColor = (c0 + c1) % 2 == 0 ? pentagon[(c0 + c1) / 2] : pentagon[(c1 + 1) % 5];
                }
                break;

            case 4: // press the other color
                _nextColor = _colorCandidates.Where(c => !adjacentColors.Contains(c)).First();
                break;

            default:
                Log("Error with rule checking. Next color is red.");
                _nextColor = SquareColor.Red;
                break;
        }

        // Populate result (potentially find a backup color)
        while (true)
        {
            if (_nextColor != _lastPressedColor)
                for (int i = 0; i < 16; i++)
                    if (_colors[i] == _nextColor)
                        result.Add(i);

            if (result.Count != 0)
            {
                Log("Button #{0} correct. Last pressed: {1}. Flashing color: {2}. Adjacent: {3}. Next color is {4}.", index, _lastPressedColor, _colors[index], adjacentColors.Select(c => c.ToString()[0]).JoinString(), _nextColor);
                return result;
            }

            _nextColor = pentagon[(Array.IndexOf(pentagon, _nextColor) + _backupDirection + 5) % 5];
        }
    }

    protected override void ButtonPressed(int index)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        if (_lastPress != -1)
            SetButtonColor(_lastPress, _colors[_lastPress]);

        if (_lastPress == -1 && _allowedPresses.Contains(index))
        {
            PlaySound(index);
            _lastPress = index;
            _startingPosition = index;
            _allowedPresses = CalculateNewAllowedPresses(index);
            _activeCoroutine = StartCoroutine(BlinkLastSquare());
        }
        else if (!_allowedPresses.Contains(index))
        {
            Log("Button #{0} ({1}) was incorrect at this time.", index, _colors[index]);
            Strike();
            SetInitialState();
        }
        else
        {
            PlaySound(index);
            _lastPress = index;

            _updateIndices = new HashSet<int>();
            SpreadColor(_lastPressedColor, _colors[index], _startingPosition);
            if (_updateIndices.SetEquals(_lastArea))
            {
                _pressesWithoutChange++;
            }
            else
            {
                _lastArea = _updateIndices;
                _pressesWithoutChange = 0;
            }

            _lastPressedColor = _colors[index];

            if (_pressesWithoutChange >= 3)
            {
                var currentColor = _colors[index];
                while (currentColor == _colors[index])
                    _colors[index] = _colorCandidates[Rnd.Range(0, _colorCandidates.Length)];
                _pressesWithoutChange = 0;
                SetButtonBlack(index);
                Audio.PlaySoundAtTransform("colorreset", Buttons[index].transform);
            }

            if (_colors.All(c => c == _colors[0]))
            {
                _allowedPresses = null;
                _activeCoroutine = StartCoroutine(SetSquareColors(delay: false, solve: true));
            }
            else
            {
                _allowedPresses = CalculateNewAllowedPresses(index);
                _activeCoroutine = StartCoroutine(SetSquareColors(delay: false));
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (IsCoroutineActive || _animating > 0)
            yield return true;

        while (!_isSolved)
        {
            Buttons[_allowedPresses.PickRandom()].OnInteract();
            yield return new WaitForSeconds(.1f);

            while (IsCoroutineActive || _animating > 0)
                yield return true;
            yield return new WaitForSeconds(.25f);
        }
    }
}
