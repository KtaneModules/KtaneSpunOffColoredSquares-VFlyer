using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ColoredSquares;
using System.Linq;

public class OvercoloredSquaresModule : ColoredSquaresModuleBase {

    SquareColor initialSquareColor;
    SquareColor[] lastRememberedSequence = new SquareColor[16];
    List<int> nextIdxesToPress;
    List<int>[] allExpectedIdexesToPress = new List<int>[16];
	public override string Name { get { return "Overcolored Squares"; } }

	// Use this for initialization
	void Start () {
        SetInitialState();
	}

	private void SetInitialState()
    {
        initialSquareColor = new[] { SquareColor.Blue, SquareColor.Red, SquareColor.Green, SquareColor.Yellow, SquareColor.Magenta }.PickRandom();
        if (nextIdxesToPress == null)
            nextIdxesToPress = new List<int>();
        nextIdxesToPress.Clear();
        //lastRememberedSequence = Enumerable.Repeat(initialSquareColor, 16).ToArray();
        for (var x = 0; x < lastRememberedSequence.Length; x++)
        {
            lastRememberedSequence[x] = initialSquareColor;
        }
        for (var x = 0; x < allExpectedIdexesToPress.Length; x++)
        {
            int y = x;
            if (allExpectedIdexesToPress[x] == null)
                allExpectedIdexesToPress[x] = new List<int>();
            allExpectedIdexesToPress[x].Clear();
            var expectedIdxes = Enumerable.Range(0, 16).Where(a => a != y);
            allExpectedIdexesToPress[x].AddRange(expectedIdxes);
            allExpectedIdexesToPress[x].Shuffle();
            LogDebug("Sequence of other presses for initially pressing square #{0}: {1}", x + 1, allExpectedIdexesToPress[x].Select(a => a + 1).Join(", "));
        }
        _colors = lastRememberedSequence.ToArray();
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.All, true, false);
        Log("Press any square to start disarming the module.");
    }

    void UpdateLastRememberedGrid()
    {
        for (var x = 0; x < _colors.Length; x++)
        {
            var lastSquareColor = lastRememberedSequence[x];
            _colors[x] = x == nextIdxesToPress.First() ? lastSquareColor :
                lastSquareColor == SquareColor.White ? SquareColor.White :
                new[] { SquareColor.Blue, SquareColor.Red, SquareColor.Green, SquareColor.Yellow, SquareColor.Magenta }.Where(a => a != lastSquareColor).PickRandom();
            if (nextIdxesToPress.Contains(x))
                SetButtonBlack(x);
            else
                SetButtonColor(x, SquareColor.White);
        }
        Log("{1} square{2} left to press, the grid is now showing the following colors in reading order: {0}", _colors.Select(a => a.ToString().First()).JoinString(" "), nextIdxesToPress.Count, nextIdxesToPress.Count == 1 ? "" : "s");
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.NonwhiteOnly, true, false);
    }

    protected override void ButtonPressed(int index)
    {
        
        if (!nextIdxesToPress.Any())
        {
            PlaySound(index);
            nextIdxesToPress.AddRange(allExpectedIdexesToPress[index]);
            Log("Pressing square #{0} in reading order generated the sequence of buttons to press: {1}", index + 1, nextIdxesToPress.Select(a => a + 1).Join(", "));
            lastRememberedSequence[index] = SquareColor.White;
            Log("The first remembered color grid was all {0}.", initialSquareColor.ToString());
            UpdateLastRememberedGrid();
        }
        else if (nextIdxesToPress.First() == index)
        {
            PlaySound(index);
            nextIdxesToPress.RemoveAt(0);
            if (nextIdxesToPress.Any())
            {
                //SetAllButtonsBlack();
                lastRememberedSequence = _colors.ToArray();
                lastRememberedSequence[index] = SquareColor.White;
                UpdateLastRememberedGrid();
            }
            else
            {
                Log("All of the squares in order were correctly pressed.");
                ModulePassed();
            }
        }
        else
        {
            Log("Pressing square #{0} in reading order was not correct. There were {1} nonwhite squares left when this occured. Starting over...", index + 1, nextIdxesToPress.Count);
            Strike();
            SetInitialState();
        }
        
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            if (!nextIdxesToPress.Any())
            {
                Buttons.PickRandom().OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                Buttons[nextIdxesToPress.First()].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (nextIdxesToPress.Count == 1)
                    Buttons[nextIdxesToPress.First()].OnInteract();
            }
            while (IsCoroutineActive)
                yield return true;
        }
    }
}
