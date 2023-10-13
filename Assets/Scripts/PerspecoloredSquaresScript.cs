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
    SquareColor[][] currentMergedSquareColors;
    List<int> expectedIdxPresses, idxPressesMade;
    int idxViewAngleShow;
    static string[] pieceNames = new[] { "Knight", "Bishop", "Rook" };
    const int iterationCount = 3;
    enum PieceType
    {
        Unknown,
        Knight,
        Bishop,
        Rook
    }
    bool interactable = true;
    IEnumerator activeRotationHandler;
    string QuickCoord(int idx)
    {
        return string.Format("{0}{1}", "ABCD"[idx % 4], idx / 4 + 1);
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
                _colors = currentMergedSquareColors[storedRememberedStateRot];
                storedRememberedStateRot = idxViewAngleShow;
                StartSquareColorsCoroutine(currentMergedSquareColors[storedRememberedStateRot], SquaresToRecolor.NonwhiteOnly);
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
        selectedViewAngles = Enumerable.Range(0, possibleViewAngles.Length).ToArray().Shuffle().Take(iterationCount).Select(a => possibleViewAngles[a]).ToArray();
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
    bool IsSwapValid(SquareColor[] firstBoard, SquareColor[] secondBoard, int firstIdx, int secondIdx)
    {
        return firstIdx != secondIdx &&
            firstBoard[firstIdx] == secondBoard[secondIdx] &&
            firstBoard[secondIdx] == secondBoard[firstIdx] &&
            firstBoard[firstIdx] != secondBoard[firstIdx] &&
            firstBoard[secondIdx] != secondBoard[secondIdx];
        /* TLDR:
         * - Check if the 1st idx is not the 2nd idx
         * - Check if color of the 1st idx on the 1st board is the color of the 2nd idx of the 2nd board
         * - Check if color of the 2nd idx on the 1st board is the color of the 1st idx of the 2nd board
         * - Check if color of the 1st idx on the 1st board is not the color of the 1st idx of the 2nd board
         * - Check if color of the 2nd idx on the 1st board is not the color of the 2nd idx of the 2nd board
         */
    }
    bool IsMoveValid(int firstIdx, int secondIdx, PieceType piece)
    {
        var firstIdxR = firstIdx / 4;
        var firstIdxC = firstIdx % 4;
        var secondIdxR = secondIdx / 4;
        var secondIdxC = secondIdx % 4;

        switch(piece)
        {
            case PieceType.Knight:
                return (Mathf.Abs(firstIdxR - secondIdxR) == 2 && Mathf.Abs(firstIdxC - secondIdxC) == 1) ||
                    (Mathf.Abs(firstIdxR - secondIdxR) == 1 && Mathf.Abs(firstIdxC - secondIdxC) == 2);
            case PieceType.Bishop:
                return Mathf.Abs(firstIdxR - secondIdxR) == Mathf.Abs(firstIdxC - secondIdxC);
            case PieceType.Rook:
                return firstIdxR == secondIdxR || firstIdxC == secondIdxC;
        }

        return false;
    }

    void CalculateExpected()
    {
        var possibleColors = new[] { SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Yellow, SquareColor.Magenta };
        var initialSquareColorsAll = new SquareColor[16];
        var attemptCount = 0;
    retryGen:
        attemptCount++;
        // Generation Algorithm
        for (var x = 0; x < 16; x++)
            initialSquareColorsAll[x] = possibleColors.PickRandom();
        var idxAllSets = new IEnumerable<int>[iterationCount + 1];
        var relevantLoggings = new List<string>();
        idxAllSets[0] = Enumerable.Range(0, 16).ToArray();
        expectedIdxPresses = Enumerable.Range(0, 16).ToArray().Shuffle().Take(3).ToList();
        var allRememberedMoves = new List<int[]> { expectedIdxPresses.ToArray() };
        for (var x = 0; x < iterationCount; x++)
        {
            var curPosIterModify = allRememberedMoves[x].ToArray();
            var finalIdxAllSetsCur = idxAllSets[x].ToArray();
            var idxMoveOrder = Enumerable.Range(0, 3).ToArray().Shuffle();
            // 0: Knight, 1: Bishop, 2: Rook
            for (int i = 0; i < idxMoveOrder.Length; i++)
            {
                int v = idxMoveOrder[i];
                var allowedMovesCur = new List<int>();
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
                        var allowedMovesRook = Enumerable.Range(0, 16).Where(a => a / 4 == curPos / 4 || a % 4 == curPos % 4);
                        if (allowedMovesRook.Any())
                            allowedMovesCur.AddRange(allowedMovesRook);
                        break;
                }
                allowedMovesCur.RemoveAll(a => curPosIterModify.Contains(a)); // Remove current positions from the list of all possible move.
                allowedMovesCur.RemoveAll(a => allRememberedMoves.Any(b => b.Contains(a))); // Remove all possible moves that would result in an overlap from another visited square.
                allowedMovesCur.RemoveAll(a => initialSquareColorsAll[idxAllSets[x].ElementAt(a)] == initialSquareColorsAll[idxAllSets[x].ElementAt(curPos)]); // Remove all possible moves that would swap the same color as its destination square.
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
        allMergedSquareColors = new SquareColor[iterationCount + 1][];
        for (var x = 0; x < idxAllSets.Length; x++)
            allMergedSquareColors[x] = idxAllSets[x].Select(a => initialSquareColorsAll[a]).ToArray();
        allMergedSquareColors = allMergedSquareColors.Reverse().ToArray();
        // Solver Algorithm
        var allPossibleCombos = new List<IEnumerable<int>>();
        for (var x = 0; x < 16; x++)
            for (var y = x + 1; y < 16; y++)
                for (var z = y + 1; z < 16; z++)
                    allPossibleCombos.Add(new[] { x, y, z });
        // Generates all initial positions in ascending order.
        var pieceOptions = new[] {
            new[] { 0, 1, 2 },
            new[] { 0, 2, 1 },
            new[] { 1, 0, 2 },
            new[] { 1, 2, 0 },
            new[] { 2, 0, 1 },
            new[] { 2, 1, 0 },
        };
        var nestedCombinations = new Dictionary<int[], List<int[]>>();
        // Key is the two idx states between the results. Value is all possible combinations that can be made within those.
        for (var x = 0; x < allMergedSquareColors.Length; x++)
        {
            var firstIterBoard = allMergedSquareColors[x];
            for (var y = x; y < allMergedSquareColors.Length; y++)
            {
                if (y == x) continue;
                var secondIterBoard = allMergedSquareColors[y];
                var differingStates = Enumerable.Range(0, 16).Select(a => firstIterBoard[a] != secondIterBoard[a]);
                // Because one rule states that a move must swap two different colors,
                // we can conclude that if not exactly 6 tiles are affected,
                // it is impossible to have 3 pieces swap more or less than 6 tiles with each piece using 1 move each.
                if (differingStates.Count(a => a) == 6)
                {
                    var allowedOptions = allPossibleCombos.Where(b => b.All(a => differingStates.ElementAt(a))).ToArray();
                    var nxtCombo = new List<int[]>();
                    for (var z = 0; z < allowedOptions.Length; z++)
                        for (var n = z + 1; n < allowedOptions.Length; n++)
                        {
                            var unionizedItems = allowedOptions.ElementAt(z).Union(allowedOptions.ElementAt(n)).Distinct();
                            // Make sure that the allowed options do not overlap.
                            if (unionizedItems.Count() == 6) 
                            {
                                var comboValid = false;
                                for (int i = 0; i < pieceOptions.Length && !comboValid; i++)
                                {
                                    var aCombo = pieceOptions[i].Select(c => allowedOptions[z].ElementAt(c)).ToArray();
                                    for (int d = 0; d < pieceOptions.Length && !comboValid; d++)
                                    {
                                        var _2Combo = pieceOptions[d].Select(e => allowedOptions[n].ElementAt(e)).ToArray();
                                        var allValid = true;
                                        // Basically check if the movement follows that of the respective piece.
                                        allValid &= IsMoveValid(aCombo[0], _2Combo[0], PieceType.Knight) && IsMoveValid(aCombo[1], _2Combo[1], PieceType.Bishop) && IsMoveValid(aCombo[2], _2Combo[2], PieceType.Rook);
                                        // And check if the tiles were actually swapped there.
                                        allValid &= IsSwapValid(firstIterBoard, secondIterBoard, aCombo[0], _2Combo[0]) && IsSwapValid(firstIterBoard, secondIterBoard, aCombo[1], _2Combo[1]) && IsSwapValid(firstIterBoard, secondIterBoard, aCombo[2], _2Combo[2]);
                                        comboValid |= allValid;
                                    }
                                }
                                if (comboValid)
                                    nxtCombo.Add(new[] { allPossibleCombos.IndexOf(allowedOptions[z]), allPossibleCombos.IndexOf(allowedOptions[n]) });
                            }
                        }
                    if (nxtCombo.Any())
                        nestedCombinations.Add(new[] { x, y }, nxtCombo);
                }
            }
        }
        for (var x = 0; x < nestedCombinations.Count; x++)
            LogDebug("{0}: {1}", nestedCombinations.Keys.ElementAt(x).Join("<->"), nestedCombinations[nestedCombinations.Keys.ElementAt(x)].Select(a => string.Format("[{0}]", a.Select(b => allPossibleCombos[b].Join(",")).Join("<->"))).Join(";"));
        // Using the results, determine the sequence of boards that would cause the solution to be valid.
        var allKeys = nestedCombinations.Keys.ToList();
        
        var solutionCombos = new List<KeyValuePair<int, List<int>>>();
        var curCombos = Enumerable.Range(0, allPossibleCombos.Count).Select(a => new KeyValuePair<int, List<int>>(a, new List<int> { 0 }));
        // KeyValuePair has the idx for its combination, and 
        while (curCombos.Any())
        {
            var nextCombos = new List<KeyValuePair<int, List<int>>>();
            foreach (var aCombo in curCombos)
            {
                var curSequ = aCombo.Value;
                var possibleOptions = Enumerable.Range(1, 3).Where(a => !curSequ.Contains(a));
                if (possibleOptions.Any())
                {
                    foreach (var anOption in possibleOptions)
                    {
                        var newCombo = curSequ.Concat(new[] { anOption }).ToList();
                        var last2IdxesSorted = newCombo.TakeLast(2).OrderBy(a => a).ToArray();
                        var idxKey = allKeys.IndexOf(a => a.SequenceEqual(last2IdxesSorted));
                        if (idxKey != -1)
                            foreach(var exchCombo in nestedCombinations[allKeys[idxKey]])
                                if (exchCombo.Contains(aCombo.Key))
                                    nextCombos.Add(new KeyValuePair<int, List<int>>(exchCombo.Single(a => a != aCombo.Key), newCombo));
                    }
                }
                else
                    solutionCombos.Add(aCombo);

            }

            curCombos = nextCombos;
        }
        LogDebug("Reached solver state after {0} attempt(s):", attemptCount);
        foreach (var solCombo in solutionCombos)
            LogDebug("{0}, {1}", allPossibleCombos[solCombo.Key].Select(a => QuickCoord(a)).Join(), solCombo.Value.Join(" -> "));
        if (solutionCombos.Count > 1)
            goto retryGen;
        // Final result.
        Log("{0} attempt(s) taken to generate a valid state.", attemptCount);
        Log("Placing a knight, bishop, and rook at {0} respectively.", expectedIdxPresses.Select(a => QuickCoord(a)).Join(", "));
        Log("Initial Board:");
        LogBoard(initialSquareColorsAll);
        foreach (var relevantLog in relevantLoggings)
            Log(relevantLog);
        currentMergedSquareColors = allMergedSquareColors.Select(a => a.ToArray()).ToArray();
        if (activeRotationHandler != null)
            StopCoroutine(activeRotationHandler);
        _colors = currentMergedSquareColors[idxViewAngleShow];
        StartSquareColorsCoroutine(currentMergedSquareColors[idxViewAngleShow], SquaresToRecolor.NonwhiteOnly, true);
        activeRotationHandler = HandleAutoRotateWhileActive();
        StartCoroutine(activeRotationHandler);
    }
    void HandleSoftReset()
    {
        idxPressesMade.Clear();
        StopCoroutine(activeRotationHandler);
        currentMergedSquareColors = allMergedSquareColors.Select(a => a.ToArray()).ToArray();
        _colors = currentMergedSquareColors[idxViewAngleShow];
        StartSquareColorsCoroutine(currentMergedSquareColors[idxViewAngleShow], SquaresToRecolor.NonwhiteOnly, true);
        activeRotationHandler = HandleAutoRotateWhileActive();
        StartCoroutine(activeRotationHandler);
    }
    protected override void ButtonPressed(int index)
    {
        if (!interactable) return;
        if (expectedIdxPresses.Contains(index))
        {
            PlaySound(index);
            if (!idxPressesMade.Contains(index))
            {
                Log("Correctly pressed {0}.", QuickCoord(index));
                idxPressesMade.Add(index);
            }
            else
            {
                Log("Deselected {0}.", QuickCoord(index));
                idxPressesMade.Remove(index);
            }
            if (idxPressesMade.Intersect(expectedIdxPresses).Count() == expectedIdxPresses.Count())
            {
                Log("All relevant squares have been pressed correctly.");
                _isSolved = true;
                interactable = false;
                StartCoroutine(AnimateSolve());
            }
            else
            {
                if (idxPressesMade.Contains(index))
                    SetButtonColor(index, SquareColor.White);
                else
                    SetButtonColor(index, allMergedSquareColors[idxViewAngleShow][index]);
                for (var x = 0; x < currentMergedSquareColors.Length; x++)
                    currentMergedSquareColors[x][index] = idxPressesMade.Contains(index) ? SquareColor.White : allMergedSquareColors[x][index];
            }
        }
        else
        {
            Log("Pressing square {0} was not correct. Clearing all correct inputs.", QuickCoord(index));
            Strike();
            HandleSoftReset();
            //ResetModule();
        }
    }
    IEnumerator AnimateSolve()
    {
        for (var x = 0; x < 16; x++)
            SetButtonColor(x, expectedIdxPresses.Contains(x) ? SquareColor.White : currentMergedSquareColors[idxViewAngleShow][x]);
        StartSquareColorsCoroutine(Enumerable.Repeat(SquareColor.Black, 16).ToArray(), SquaresToRecolor.All);
        while (IsCoroutineActive)
            yield return null;
        ModulePassed();
        yield break;
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
