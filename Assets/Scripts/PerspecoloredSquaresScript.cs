using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ColoredSquares;

public class PerspecoloredSquaresScript : ColoredSquaresModuleBase {
    public override string Name {get { return "Perspecticolored Squares"; } }
    public TextMesh debugRotationTxt;
    [SerializeField]
    private Vector3[] possibleViewAngles;
    private Vector3[] selectedViewAngles;
    SquareColor[][] allMergedSquareColors;
    List<int> expectedIdxPresses, idxPressesMade;
    int idxViewAngleShow;
    static string[] pieceNames = new[] { "Knight", "Bishop", "Rook" };

    //Coroutine activeRotationHandler;
    string QuickCoord(int idx)
    {
        return string.Format("{0}{1}", "ABCD"[idx % 4], "1234"[idx / 4]);
    }
    // Use this for initialization
    void Start () {
        idxPressesMade = new List<int>();
        ResetModule();
	}
	IEnumerator HandleAutoRotateWhileActive()
    {
        var storedRememberedStateRot = idxViewAngleShow;
        //int?[] possibleIdxes = ((IEnumerable<int?>)(Enumerable.Range(0, 16).Where(a => _colors[a] != altSquareColors[a]))).ToArray().Shuffle();
        while (IsCoroutineActive)
            yield return null;
        while (!_isSolved)
        {
            if (storedRememberedStateRot != idxViewAngleShow)
            {
                storedRememberedStateRot = idxViewAngleShow;
                StartSquareColorsCoroutine(allMergedSquareColors[storedRememberedStateRot], SquaresToRecolor.NonwhiteOnly);
                yield return null;
                while (IsCoroutineActive)
                    yield return null;
            }
            else
                yield return null;
        }
    }
    void ResetModule()
    {
        selectedViewAngles = Enumerable.Range(0, possibleViewAngles.Length).ToArray().Shuffle().Take(4).Select(a => possibleViewAngles[a]).ToArray();
        LogDebug("New rotation angles (global): {0}", selectedViewAngles.Select(a => a.ToString()).Join("; "));
        idxPressesMade.Clear();
        CalculateExpected();
    }
    void LogBoard(SquareColor[] squareColors)
    {
        if (squareColors.Length != 16) return;
        for (var x = 0; x < 4; x++)
            Log(squareColors.Skip(4 * x).Take(4).Select(a => a.ToString().First()).Join());
    }
    void CalculateExpected()
    {
        var possibleColors = new[] { SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Yellow, SquareColor.Magenta };
        var initialSquareColorsAll = new SquareColor[16];
        var attemptCount = 0;
    retryGen:
        attemptCount++;
        for (var x = 0; x < 16; x++)
            initialSquareColorsAll[x] = possibleColors.PickRandom();
        var idxAllSets = new IEnumerable<int>[5];
        var relevantLoggings = new List<string>();
        idxAllSets[0] = Enumerable.Range(0, 16).ToArray();
        expectedIdxPresses = Enumerable.Range(0, 16).ToArray().Shuffle().Take(3).ToList();
        var allRememberedMoves = new List<int[]> { expectedIdxPresses.ToArray() };
        for (var x = 0; x < 4; x++)
        {
            var curPosIterModify = allRememberedMoves[x].ToArray();
            var curPosInitIter = allRememberedMoves[x];
            var finalIdxAllSetsCur = idxAllSets[x].ToArray();
            var idxMoveOrder = Enumerable.Range(0, 6).Select(a => a / 2).ToArray().Shuffle();
            // 0: Knight, 1: Bishop, 2: Rook
            for (int i = 0; i < idxMoveOrder.Length; i++)
            {
                int v = idxMoveOrder[i];
                var allowedMovesCur = new List<int>();
                var isFirstMoveInCurIteration = idxMoveOrder.IndexOf(a => a == v) == i;
                var curPos = curPosIterModify[v];
                switch (v)
                {
                    case 0: // Knight
                        var allowedMovesKnight = Enumerable.Range(0, 16).Where(a => Mathf.Abs(a / 4 - curPos / 4) == 2 && Mathf.Abs(a % 4 - curPos % 4) == 1 || Mathf.Abs(a % 4 - curPos % 4) == 2 && Mathf.Abs(a / 4 - curPos / 4) == 1);
                        if (allowedMovesKnight.Any())
                            allowedMovesCur.AddRange(allowedMovesKnight);
                        break;
                    case 1: // Bishop
                        var allowedMovesBishop = Enumerable.Range(0, 16).Where(a => Mathf.Abs(a / 4 - curPos / 4) ==  Mathf.Abs(a % 4 - curPos % 4));
                        if (allowedMovesBishop.Any())
                            allowedMovesCur.AddRange(allowedMovesBishop);
                        break;
                    case 2: // Rook
                        var allowedMovesRook = Enumerable.Range(0, 16).Where(a => Mathf.Abs(a / 4 - curPos / 4) == 0 || Mathf.Abs(a % 4 - curPos % 4) == 0);
                        if (allowedMovesRook.Any())
                            allowedMovesCur.AddRange(allowedMovesRook);
                        break;
                }
                allowedMovesCur.RemoveAll(a => curPosIterModify.Contains(a)); // Remove current positions from the list of all possible move.
                if (!isFirstMoveInCurIteration) // Remove the "walk back" coordinate if this is not the first move in the current iteration.
                    allowedMovesCur.Remove(curPosInitIter[v]);
                if (x == 0)
                    allowedMovesCur.RemoveAll(a => initialSquareColorsAll[a] == initialSquareColorsAll[curPos]);
                if (!allowedMovesCur.Any())
                    goto retryGen;
                var pickedMove = allowedMovesCur.PickRandom();
                var temp = finalIdxAllSetsCur[pickedMove];
                finalIdxAllSetsCur[pickedMove] = finalIdxAllSetsCur[curPos];
                finalIdxAllSetsCur[curPos] = temp;
                curPosIterModify[v] = pickedMove;
                relevantLoggings.Add(string.Format("{0} moved to {1}, swapping with {2}.", pieceNames[v] , QuickCoord(pickedMove), QuickCoord(curPos)) );
            }
            allRememberedMoves.Add(curPosIterModify);
            if (idxAllSets.Take(x).Any(a => a.Select(b => initialSquareColorsAll[b]).SequenceEqual(finalIdxAllSetsCur.Select(b => initialSquareColorsAll[b]))))
                goto retryGen;
            idxAllSets[x + 1] = finalIdxAllSetsCur;
            relevantLoggings.Add(string.Format("Board state after {0} iteration(s):", x + 1));
            relevantLoggings.AddRange(Enumerable.Range(0, 4).Select(a => finalIdxAllSetsCur.Skip(4 * a).Take(4).Select(b => initialSquareColorsAll[b].ToString().First()).Join()));
        }
        Log("{0} attempt(s) taken to generate a valid state.", attemptCount);
        Log("Placing a knight, bishop, and rook at {0} respectively.", expectedIdxPresses.Select(a => QuickCoord(a)).Join(", "));
        Log("Initial Board:");
        LogBoard(initialSquareColorsAll);
        foreach (var relevantLog in relevantLoggings)
            Log(relevantLog);
        allMergedSquareColors = new SquareColor[5][];
        for (var x = 0; x < idxAllSets.Length; x++)
            allMergedSquareColors[x] = idxAllSets[x].Select(a => initialSquareColorsAll[a]).ToArray();
        allMergedSquareColors = allMergedSquareColors.Reverse().ToArray();

        StopAllCoroutines();
        
        StartSquareColorsCoroutine(allMergedSquareColors[idxViewAngleShow], SquaresToRecolor.NonwhiteOnly, true);
        StartCoroutine(HandleAutoRotateWhileActive());
    }
    protected override void ButtonPressed(int index)
    {
        if (expectedIdxPresses.Contains(index))
        {
            PlaySound(index);
            if (!idxPressesMade.Contains(index))
            {
                Log("Correctly pressed {0}.", QuickCoord(index));
                idxPressesMade.Add(index);
            }
            if (idxPressesMade.Intersect(expectedIdxPresses).Count() == expectedIdxPresses.Count())
            {
                Log("All relevant squares have been pressed correctly.");
                ModulePassed();
            }
            else
            {
                SetButtonColor(index, SquareColor.White);
                for (var x = 0; x < allMergedSquareColors.Length; x++)
                    allMergedSquareColors[x][index] = SquareColor.White;
            }
        }
        else
        {
            Log("Pressing square {0} was not correct. Starting over...", QuickCoord(index));
            Strike();
            ResetModule();
        }
    }
    bool CheckEularAnglesWithinMarginOfError(Vector3 firstVector, Vector3 secondVector, float errorMargin = 1f)
    {
        var absDiffX = Mathf.Abs(firstVector.x - secondVector.x);
        var absDiffY = Mathf.Abs(firstVector.y - secondVector.y);
        var absDiffZ = Mathf.Abs(firstVector.z - secondVector.z);

        return absDiffX * absDiffX + absDiffY * absDiffY + absDiffZ * absDiffZ <= errorMargin * errorMargin;
    }

    // Update is called once per frame
    void Update () {
        var firstAngle = Vector3.Angle(transform.up, Camera.main.transform.up);
        var secondAngle = Vector3.Angle(transform.up, Camera.main.transform.right);
        var combinedAngleView = new Vector3(firstAngle, 0, secondAngle);
        if (debugRotationTxt != null)
        {
            debugRotationTxt.text = string.Format("{0}\n{1}", idxViewAngleShow, combinedAngleView.ToString());
            debugRotationTxt.color = selectedViewAngles.Any(a => CheckEularAnglesWithinMarginOfError(a, combinedAngleView, 15f)) ? Color.green : Color.white;
        }
        if (!_isSolved)
        {
            idxViewAngleShow = Enumerable.Range(1, selectedViewAngles.Length).FirstOrDefault(a => CheckEularAnglesWithinMarginOfError(selectedViewAngles[a - 1], combinedAngleView, 15f));
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            var remainingIdxesToPress = expectedIdxPresses.Except(idxPressesMade);

            foreach (var idx in remainingIdxesToPress)
            {
                Buttons[idx].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
