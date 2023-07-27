using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ColoredSquares;
using System.Linq;


public class UndercoloredSquaresScript : ColoredSquaresModuleBase {
    enum AllPhases
    {
        Phase1,
        Phase2,
        Phase3
    }

    private static SquareColor[] possibleSquareColors = new[] { SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Magenta, SquareColor.Yellow, };

    AllPhases currentPhase;
    List<int> idxesToPress;
    List<SquareColor> relevantSquares;
    int curPressIdx;
    bool interactable;
    public override string Name { get { return "Undercolored Squares"; } }

    protected override void ButtonPressed(int index)
    {
        if (!interactable) return;
        switch (currentPhase)
        {
            case AllPhases.Phase1:
                if (index == idxesToPress[curPressIdx])
                {
                    PlaySound(index);
                    SetAllButtonsBlack();
                    curPressIdx++;
                    if (curPressIdx >= idxesToPress.Count)
                    {
                        currentPhase = AllPhases.Phase2;
                        GeneratePhase2();
                    }
                    else
                        UpdatePhase1();
                }
                else
                {
                    Strike();
                    GeneratePhase1();
                }
                break;
            case AllPhases.Phase2:
                {
                    if (index == curPressIdx)
                    {
                        StopAllCoroutines();
                        PlaySound(index);
                        interactable = false;
                        StartCoroutine(SolveAnim());
                    }
                    else
                    {
                        currentPhase = AllPhases.Phase1;
                        Strike();
                        GeneratePhase1();
                    }
                }
                break;
        }
    }

    // Use this for initialization
    void Start () {
        idxesToPress = new List<int>();
        relevantSquares = new List<SquareColor>();
        GeneratePhase1();
	}
    void UpdatePhase1()
    {
        var allRelevantColoredIdxes = idxesToPress.Take(curPressIdx + 1);
        for (var p = 0; p < allRelevantColoredIdxes.Count(); p++)
        {
            var relevantIdx = allRelevantColoredIdxes.ElementAt(p);
            _colors[relevantIdx] = relevantSquares[p];
        }
        var miscIdxes = Enumerable.Range(0, 16).Except(allRelevantColoredIdxes);
        foreach (var idx in miscIdxes)
            _colors[idx] = SquareColor.Black;
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.NonblackOnly, true);
    }
    void GeneratePhase1()
    {
        curPressIdx = 0;
        var attemptCount = 1;
        var selectedAmountToPick = 7 + Enumerable.Range(0, 8).Count(a => Random.value < 0.5f);
        idxesToPress.Clear();
        var shuffledIdxesAll = Enumerable.Range(0, 16).ToArray().Shuffle();
        idxesToPress.AddRange(shuffledIdxesAll.Take(selectedAmountToPick));
    retryUntilExactlyOneUnique:
        relevantSquares.Clear();
        //var possibleSquareColors = new[] { SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Magenta, SquareColor.Yellow, };
        for (var x = 0; x < selectedAmountToPick; x++)
            relevantSquares.Add(possibleSquareColors.PickRandom());
        var maxCounts = possibleSquareColors.Max(c => relevantSquares.Count(d => d == c));
        if (possibleSquareColors.Where(a => relevantSquares.Count(b => b == a) >= maxCounts).Count() > 1)
        {
            attemptCount++;
            goto retryUntilExactlyOneUnique;
        }
        interactable = true;
        UpdatePhase1();
    }
    IEnumerator SolveAnim()
    {
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.All);
        while (IsCoroutineActive)
            yield return null;
        yield return new WaitForSeconds(0.5f);
        StartSquareColorsCoroutine(Enumerable.Repeat(SquareColor.Black, 16).ToArray(), SquaresToRecolor.All);
        while (IsCoroutineActive)
            yield return null;
        ModulePassed();
    }
    IEnumerator AnimatePhase2()
    {
        while (IsCoroutineActive)
            yield return null;
        var allGrids = new IEnumerable<SquareColor>[16];
        for (var x = 0; x < allGrids.Length; x++)
            allGrids[x] = new[] { SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Magenta, SquareColor.Yellow, }.Where(a => a != _colors[x]).ToArray().Shuffle();
        var idxesAll = new int[16];
        while (currentPhase == AllPhases.Phase2)
        {
            for (var x = 0; x < idxesAll.Length; x++)
                idxesAll[x] = (idxesAll[x] + 1) % allGrids[x].Count();
            for (var p = 0; p < idxesAll.Length; p++)
                SetButtonColor(p, allGrids[p].ElementAt(idxesAll[p]));
            yield return new WaitForSeconds(0.25f);
        }
        yield break;
    }
    void GeneratePhase2()
    {
        //var possibleSquareColors = new[] { SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Magenta, SquareColor.Yellow, };
        var countsAll = Enumerable.Repeat(3, 5).ToArray();
        //countsAll[Random.Range(0, 5)]++;
        var refMaxIdx = Enumerable.Range(0, 5).Single(a => relevantSquares.Count(b => b == possibleSquareColors[a]) >= possibleSquareColors.Max(c => relevantSquares.Count(d => d == c)));
        var shuffledIdxesAll = Enumerable.Range(0, 16).ToArray().Shuffle();
        foreach (var idx in shuffledIdxesAll)
        {
            var allowedIdxes = Enumerable.Range(0, 5).Where(a => countsAll[a] > 0);
            var pickedIdx = allowedIdxes.Any() ? allowedIdxes.PickRandom() : -1;
            _colors[idx] = SquareColor.Black;
            if (pickedIdx != -1)
            {
                _colors[idx] = possibleSquareColors[pickedIdx];
                countsAll[pickedIdx]--;
            }
        }
        var newRefIdx = (refMaxIdx + 1) % 5;
        var squIdxFromRef = Enumerable.Range(0, 16).Where(a => _colors[a] == possibleSquareColors[newRefIdx]);

        var colIdxes = squIdxFromRef.Select(a => a % 4);
        var rowIdxes = squIdxFromRef.Select(a => a / 4);

        curPressIdx =
            (colIdxes.Distinct().Count() == 3 ? Enumerable.Range(0, 4).Single(a => !colIdxes.Contains(a)) :
            colIdxes.Distinct().Count() == 1 ? colIdxes.Distinct().Single() :
            Enumerable.Range(0, 4).Single(a => colIdxes.Count(b => b == a) == 1))
            + 4 *
            (rowIdxes.Distinct().Count() == 3 ? Enumerable.Range(0, 4).Single(a => !rowIdxes.Contains(a)) :
            rowIdxes.Distinct().Count() == 1 ? rowIdxes.Distinct().Single() :
            Enumerable.Range(0, 4).Single(a => rowIdxes.Count(b => b == a) == 1));

        StartSquareColorsCoroutine(Enumerable.Repeat(SquareColor.Black, 16).ToArray(), SquaresToRecolor.All, true, true);
        StartCoroutine(AnimatePhase2());
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            if (!interactable)
                yield return true;
            else
            switch (currentPhase)
            {
                case AllPhases.Phase1:
                    {
                        while (IsCoroutineActive)
                            yield return true;
                        Buttons[idxesToPress[curPressIdx]].OnInteract();
                    }
                    break;
                case AllPhases.Phase2:
                    {
                        while (IsCoroutineActive)
                            yield return true;
                        Buttons[curPressIdx].OnInteract();
                    }
                    break;
            }    
        }
    }

}
