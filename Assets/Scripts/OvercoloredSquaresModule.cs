using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ColoredSquares;
using System.Linq;

public class OvercoloredSquaresModule : ColoredSquaresModuleBase {

    SquareColor initialSquareColor;
    SquareColor[] lastRememberedSequence = new SquareColor[16];
    List<int> nextIdxesToPress;
    List<List<int>> curGroupIdxesToPress;
    List<List<int>>[] allGroupedIdxesToPress = new List<List<int>>[16];
    int curPressIdxGroup;
	public override string Name { get { return "Overcolored Squares"; } }

    readonly static SquareColor[] possibleSquareColors = new[] { SquareColor.Blue, SquareColor.Red, SquareColor.Green, SquareColor.Yellow, SquareColor.Magenta };

    // Use this for initialization
    void Start () {
        SetInitialState();
	}

	private void SetInitialState()
    {
        initialSquareColor = possibleSquareColors.PickRandom();
        if (nextIdxesToPress == null)
            nextIdxesToPress = new List<int>();
        nextIdxesToPress.Clear();
        //lastRememberedSequence = Enumerable.Repeat(initialSquareColor, 16).ToArray();
        for (var x = 0; x < lastRememberedSequence.Length; x++)
        {
            lastRememberedSequence[x] = initialSquareColor;
        }
        for (var x = 0; x < 16; x++)
        {
            int y = x;
            if (allGroupedIdxesToPress[x] == null)
                allGroupedIdxesToPress[x] = new List<List<int>>();
            allGroupedIdxesToPress[x].Clear();
            allGroupedIdxesToPress[x].Add(new List<int>() { y });
            var expectedIdxes = Enumerable.Range(0, 16).Where(a => a != y).ToList();
            while (expectedIdxes.Any())
            {
                var nextGroup = new List<int>();
                var amountToAdd = Random.Range(1, 6);
                for (var p = 0; p < amountToAdd && expectedIdxes.Any(); p++)
                {
                    var pickedItem = expectedIdxes.PickRandom();
                    expectedIdxes.Remove(pickedItem);
                    nextGroup.Add(pickedItem);
                }
                allGroupedIdxesToPress[x].Add(nextGroup);
            }
            if (allGroupedIdxesToPress[x].Last().Count == 1)
            {
                allGroupedIdxesToPress[x][allGroupedIdxesToPress[x].Count - 2].AddRange(allGroupedIdxesToPress[x].Last());
                allGroupedIdxesToPress[x].RemoveAt(allGroupedIdxesToPress[x].Count - 1);
            }
            LogDebug("Sequence of other presses for initially pressing square #{0}: [{1}]", x + 1, allGroupedIdxesToPress[x].Select(a => a.Select(b => b + 1).Join(", ")).Join("]; ["));
        }
        _colors = lastRememberedSequence.ToArray();
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.All, true, false);
        Log("Press any square to start disarming the module.");
        if (curGroupIdxesToPress != null)
            curGroupIdxesToPress.Clear();
        else
            curGroupIdxesToPress = new List<List<int>>();
    }

    void UpdateLastRememberedGrid()
    {
        var lastIdxesPressed = curGroupIdxesToPress.First().ToList();
        for (var p = 1; p <= curPressIdxGroup - 1; p++)
            lastIdxesPressed.AddRange(curGroupIdxesToPress[p]);
        for (var x = 0; x < _colors.Length; x++)
        {
            var lastSquareColor = lastRememberedSequence[x];
            _colors[x] = nextIdxesToPress.Contains(x) ? lastSquareColor :
                lastSquareColor == SquareColor.White ? SquareColor.White :
                possibleSquareColors.Where(a => a != lastSquareColor).PickRandom();
            if (lastIdxesPressed.Contains(x))
                SetButtonColor(x, SquareColor.White);
            else
                SetButtonBlack(x);
        }
        Log("{1} square{2} left to press, the grid is now showing the following colors in reading order: {0}", _colors.Select(a => a.ToString().First()).JoinString(" "), 16 - lastIdxesPressed.Count, 16 - lastIdxesPressed.Count == 1 ? "" : "s");
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.NonwhiteOnly, true, false);
    }

    protected override void ButtonPressed(int index)
    {
        if (!curGroupIdxesToPress.Any())
        {
            curPressIdxGroup = 1;
            PlaySound(index);
            curGroupIdxesToPress.AddRange(allGroupedIdxesToPress[index]);
            Log("Pressing square #{0} in reading order generated the sequence of buttons to press: [{1}]", index + 1, curGroupIdxesToPress.Skip(1).Select(a => a.Select(b => b + 1).Join(", ")).Join("]; ["));
            lastRememberedSequence[index] = SquareColor.White;
            Log("The first remembered color grid was all {0}.", initialSquareColor.ToString());
            nextIdxesToPress.AddRange(curGroupIdxesToPress[curPressIdxGroup]);
            UpdateLastRememberedGrid();
        }
        else if (nextIdxesToPress.Contains(index))
        {
            PlaySound(index);
            nextIdxesToPress.Remove(index);
            if (!nextIdxesToPress.Any())
            {
                //SetAllButtonsBlack();
                lastRememberedSequence = _colors.ToArray();
                foreach (int idxInGroup in curGroupIdxesToPress[curPressIdxGroup])
                    lastRememberedSequence[idxInGroup] = SquareColor.White;
                curPressIdxGroup++;
                if (curPressIdxGroup >= curGroupIdxesToPress.Count)
                {
                    Log("All of the squares in order were correctly pressed.");
                    ModulePassed();
                }
                else
                {
                    nextIdxesToPress.AddRange(curGroupIdxesToPress[curPressIdxGroup]);
                    UpdateLastRememberedGrid();
                }
            }
            else
            {
                SetButtonColor(index, SquareColor.White);
            }
        }
        else
        {
            Log("Pressing square #{0} in reading order was not correct. There were {1} nonwhite squares left when this occured. Starting over...", index + 1, 16 - curGroupIdxesToPress.Take(curPressIdxGroup).Select(a => a.Count).Sum() - nextIdxesToPress.Count);
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
            }
            while (IsCoroutineActive)
                yield return true;
        }
    }
}
