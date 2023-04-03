using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ColoredSquares;
using KModkit;
using System.Linq;

public class IsocoloredSquaresModule : ColoredSquaresModuleBase {

	SquareColor[] initialSquareColors;

	public KMBombInfo bombInfo;
	public override string Name { get { return "Isocolored Squares"; } }
    int curIdxTransition, movesMadeOnModule, debugPathSolution;
    bool disableInteractions;
    List<List<int>> solutionIdxes = new List<List<int>>();
    readonly Dictionary<int, Dictionary<SquareColor, SquareColor>> TransitionValues = new Dictionary<int, Dictionary<SquareColor, SquareColor>>
    {
        { 0, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Yellow },
                { SquareColor.Red, SquareColor.Blue },
                { SquareColor.Magenta, SquareColor.Yellow },
                { SquareColor.Green, SquareColor.Red },
                { SquareColor.Blue, SquareColor.Magenta },
            }
        },
        { 1, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Green },
                { SquareColor.Red, SquareColor.Blue },
                { SquareColor.Yellow, SquareColor.Green },
                { SquareColor.Magenta, SquareColor.Yellow },
                { SquareColor.Green, SquareColor.Red },
                { SquareColor.Blue, SquareColor.Yellow },
            }
        },
        { 2, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Green },
                { SquareColor.Red, SquareColor.Green },
                { SquareColor.Yellow, SquareColor.Red },
                { SquareColor.Magenta, SquareColor.Green },
                { SquareColor.Green, SquareColor.Yellow },
                { SquareColor.Blue, SquareColor.Yellow },
            }
        },
        { 3, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Yellow },
                { SquareColor.Red, SquareColor.Green },
                { SquareColor.Yellow, SquareColor.Green },
                { SquareColor.Magenta, SquareColor.White },
                { SquareColor.Blue, SquareColor.Red },
            }
        },
        { 4, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Green },
                { SquareColor.Red, SquareColor.Magenta },
                { SquareColor.Yellow, SquareColor.Blue },
                { SquareColor.Magenta, SquareColor.Green },
                { SquareColor.Green, SquareColor.Yellow },
                { SquareColor.Blue, SquareColor.Red },
            }
        },
        { 5, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Magenta },
                { SquareColor.Red, SquareColor.Blue },
                { SquareColor.Yellow, SquareColor.Blue },
                { SquareColor.Magenta, SquareColor.Red },
                { SquareColor.Green, SquareColor.Red },
                { SquareColor.Blue, SquareColor.Magenta },
            }
        },
        { 6, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Magenta },
                { SquareColor.Red, SquareColor.Green },
                { SquareColor.Yellow, SquareColor.Blue },
                { SquareColor.Magenta, SquareColor.Yellow },
                { SquareColor.Green, SquareColor.White },
                { SquareColor.Blue, SquareColor.Red },
            }
        },
        { 7, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Yellow },
                { SquareColor.Red, SquareColor.Green },
                { SquareColor.Yellow, SquareColor.Magenta },
                { SquareColor.Magenta, SquareColor.White },
                { SquareColor.Green, SquareColor.Blue },
                { SquareColor.Blue, SquareColor.Yellow },
            }
        },
        { 8, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Magenta },
                { SquareColor.Red, SquareColor.Green },
                { SquareColor.Yellow, SquareColor.White },
                { SquareColor.Magenta, SquareColor.Blue },
                { SquareColor.Blue, SquareColor.White },
            }
        },
        { 9, new Dictionary<SquareColor, SquareColor>
            {
                { SquareColor.White, SquareColor.Magenta },
                { SquareColor.Yellow, SquareColor.Green },
                { SquareColor.Magenta, SquareColor.Blue },
                { SquareColor.Green, SquareColor.Red },
                { SquareColor.Blue, SquareColor.Red },
            }
        },

    };

    // Use this for initialization
    void Start()
    {
        PrepModule();
    }

    void LogBoard(SquareColor[] squareColors)
    {
        if (squareColors.Length != 16) return;
        for (var x = 0; x < 4; x++)
            Log(squareColors.Skip(4 * x).Take(4).Select(a => a.ToString().First()).Join());
    }
    string QuickCoord(int idx)
    {
        return string.Format("{0}{1}" , "ABCD"[idx % 4], "1234"[idx / 4]);
    }
    void PrepModule()
    {
        var possibleColors = new[] { SquareColor.Red, SquareColor.Yellow, SquareColor.Green, SquareColor.Blue, SquareColor.Magenta };

        if (initialSquareColors == null)
            initialSquareColors = new SquareColor[16];
        if (solutionIdxes == null)
            solutionIdxes = new List<List<int>>();
        var serialNoLastDigit = bombInfo.GetSerialNumberNumbers().LastOrDefault();
        var curIterationCount = 0;
        var maxIteractions = 20;
        curIdxTransition = serialNoLastDigit;
        movesMadeOnModule = 0;
        do
        {
            solutionIdxes.Clear();
            possibleColors.Shuffle();
            // Generate a puzzle with the following conditions, 3 colors occuring exactly 1, 2 colors occuring at least twice (to avoid conflicts with Discolored Squares).
            var opposingCount = Random.Range(0, 10);
            var countSets = new[] { 1, 1, 1, 2 + opposingCount, 2 + (9 - opposingCount) };
            for (var x = 0; x < initialSquareColors.Length && countSets.Sum() > 0; x++)
            {
                var currentIdx = Enumerable.Range(0, countSets.Length).Where(a => countSets[a] > 0).PickRandom();

                initialSquareColors[x] = possibleColors[currentIdx];

                countSets[currentIdx]--;
            }

            _colors = initialSquareColors.ToArray();
            // Then see if the module is possible by configurating a series of moves with the given board.
            ObtainPossibleSolutions(_colors);
            curIterationCount++;
        }
        while (curIterationCount < maxIteractions && (!solutionIdxes.Any() || solutionIdxes.Any(a => a.Count <= 1)));
        if (!solutionIdxes.Any())
            Log("The module generated a potentially impossible case after {1} iteration(s) while searching a possible sequence within 3 moves.", maxIteractions);
        curIdxTransition = serialNoLastDigit;
        _colors = initialSquareColors.ToArray();
        Log("Initial square colors in reading order:");
        LogBoard(initialSquareColors);
        Log("Initial Pattern Index Used: {0}", curIdxTransition);
        if (solutionIdxes.Any())
        {
            LogDebug("All possible shortest sequences: [{0}]", solutionIdxes.Select(b => b.Select(a => a + 1).Join(",")).Join("];["));
            Log("One possible sequence (from {2} detected shortest possible sequence(s)) after {1} iteration(s) to solve: {0}", solutionIdxes.PickRandom().Select(a => QuickCoord(a)).Join(), curIterationCount, solutionIdxes.Count());
        }
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.All, true);
    }
    /**
     * <summary>Determines if any of the rules were active on that current board.</summary>
     * <returns>True if any of the rules are true, false if none of the rules are true.</returns>
     * <param name="currentSquareCombination">The 16 square combination provided on the given board.</param>
     * <param name="logStatus">Logs the rules that were and were not applied when this is invoked. Setting this to false disables logging.</param>
     */
    bool AnyRulesValid(SquareColor[] currentSquareCombination, bool logStatus = false)
    {
        if (currentSquareCombination.Length != 16) return false;
        var output = false;
        if (logStatus)
            Log("Active rules:");
        // Rule 1
        if (currentSquareCombination.Distinct().Count(a => currentSquareCombination.Count(b => b == a) == 1) == 3)
        {
            if (logStatus)
                Log("There are 3 colors that have the same amount of squares. (Rule 1 = True)");
            output = true;
        }
        else if (logStatus)
            Log("There are not 3 colors that have the same amount of squares. (Rule 1 = False)");
        // Rule 2
        var allTheSameColor = false;
        for (var idx = 0; idx < currentSquareCombination.Length; idx++)
        {
            var curX = idx % 4;
            var curY = idx / 4;

            var curIDxScan = new[] { (curY + 3) % 4 * 4 + curX,
                (curY * 4) + (curX + 1) % 4,
                (curY + 1) % 4 * 4 + curX,
                (curY * 4) + (curX + 3) % 4,};

            if (curIDxScan.Select(a => currentSquareCombination[a]).Distinct().Count() == 1)
                allTheSameColor = true;
        }
        if (logStatus)
            Log("Four squares neighboring any given square are {1}the same color. (Rule 2 = {0})", allTheSameColor, allTheSameColor ? "" : "not ");
        output |= allTheSameColor;
        // Rule 3
        if (currentSquareCombination.Count(a => a == SquareColor.Blue) == currentSquareCombination.Count(a => a == SquareColor.Magenta))
        {
            if (logStatus)
                Log("There are an equal amount of blue and magenta squares. (Rule 3 = True)");
            output = true;
        }
        else if (logStatus)
            Log("There are not an equal amount of blue and magenta squares. (Rule 3 = False)");
        // Rule 4
        if (currentSquareCombination.Count(a => a == SquareColor.Red) == currentSquareCombination.Count(a => a == SquareColor.Yellow))
        {
            if (logStatus)
                Log("There are an equal amount of red and yellow squares. (Rule 4 = True)");
            output = true;
        }
        else if (logStatus)
            Log("There are not an equal amount of red and yellow squares. (Rule 4 = False)");
        // Rule 5
        var uniquePatterns = true;
        foreach (SquareColor curColor in currentSquareCombination.Distinct())
        {
            var selectedIdxes = Enumerable.Range(0, 16).ToArray().Where(a => currentSquareCombination[a] == curColor);
            var patternsScanned = new List<SquareColor[]>();
            for (var idx = 0; idx < selectedIdxes.Count(); idx++)
            {
                var curX = selectedIdxes.ElementAt(idx) % 4;
                var curY = selectedIdxes.ElementAt(idx) / 4;

                var curIDxScan = new[] {
                    (curY + 3) % 4 * 4 + curX,
                    (curY * 4) + (curX + 1) % 4,
                    (curY + 1) % 4 * 4 + curX,
                    (curY * 4) + (curX + 3) % 4,};
                var currentPattern = curIDxScan.Select(a => currentSquareCombination[a]).ToArray();
                var flippedPatternV = new[] { 0, 3, 2, 1 }.Select(a => currentPattern[a]).ToArray();
                //var flippedPatternH = new[] { 2, 1, 0, 3 }.Select(a => currentPattern[a]).ToArray();
                var foundPattern = false;
                foreach (SquareColor[] scanningPattern in patternsScanned)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        var curPatternIdx = new[] { x, (x + 1) % 4, (x + 2) % 4, (x + 3) % 4 };
                        if (curPatternIdx.Select(b => currentPattern[x]).SequenceEqual(scanningPattern) ||
                            curPatternIdx.Select(b => flippedPatternV[x]).SequenceEqual(scanningPattern) ||
                            curPatternIdx.Select(b => scanningPattern[x]).SequenceEqual(flippedPatternV) ||
                            curPatternIdx.Select(b => scanningPattern[x]).SequenceEqual(currentPattern))
                        {
                            foundPattern = true;
                        }

                    }
                }
                if (!foundPattern)
                    patternsScanned.Add(currentPattern);
                else
                    uniquePatterns = false;
            }
        }
        if (logStatus)
            Log("Two squares of the same color {1} neighbors forming a similar pattern. (Rule 5 = {0})", !uniquePatterns, uniquePatterns ? "do not have" : "have");
        output |= !uniquePatterns;
        // Rule 6
        var similarRows = false;
        for (var x = 0; x < 3; x++)
        {
            var curRowBase = _colors.Skip(4 * x).Take(4);
            for (var y = x + 1; y < 4; y++)
            {
                var curRowScan = _colors.Skip(4 * y).Take(4);
                for (var e = 0; e < 4; e++)
                {
                    var shiftedRowScan = curRowScan.Skip(e).Concat(curRowScan.Take(e));
                    similarRows &= !curRowBase.SequenceEqual(shiftedRowScan);
                }
                var flippedRow = curRowScan.Reverse();
                for (var e = 0; e < 4; e++)
                {
                    var shiftedRowScan = flippedRow.Skip(e).Concat(flippedRow.Take(e));
                    similarRows &= !curRowBase.SequenceEqual(shiftedRowScan);
                }
            }
        }
        if (logStatus)
            Log("All of the rows {0}form similar patterns. (Rule 6 = {1})", similarRows ? "" : "do not ", similarRows);
        output |= similarRows;
        // Rule 7
        var similarColumns = false;
        var expectedIdxColScan = Enumerable.Range(0, 16).OrderBy(a => a % 4).ToArray();
        for (var x = 0; x < 3; x++)
        {
            var curColBase = expectedIdxColScan.Skip(4 * x).Take(4).Select(a => _colors[a]);
            for (var y = x + 1; y < 4; y++)
            {
                var curColScan = expectedIdxColScan.Skip(4 * y).Take(4).Select(a => _colors[a]);
                for (var e = 0; e < 4; e++)
                {
                    var shiftedColScan = curColScan.Skip(e).Concat(curColScan.Take(e));
                    similarColumns &= !curColBase.SequenceEqual(shiftedColScan);
                }
                var flippedCol = curColScan.Reverse();
                for (var e = 0; e < 4; e++)
                {
                    var shiftedColScan = flippedCol.Skip(e).Concat(flippedCol.Take(e));
                    similarColumns &= !curColBase.SequenceEqual(shiftedColScan);
                }
            }
        }
        if (logStatus)
            Log("All of the columns {0}form similar patterns. (Rule 7 = {1})", similarColumns ? "" : "do not ", similarColumns);
        output |= similarColumns;

        return output;
    }


    /**
     * <summary>Simulates the press given on the module based on what the current transition IDx is currently.</summary>
     * <param name="idxPress">The index of where the press is being made on the module.</param>
     * <param name="currentSquareCombination">The current board state on the module.</param>
     * <param name="currentTransitionIdxInput">The current index used for transitioning other squares.</param>
     * <returns>The mutated square combination: <paramref name="currentSquareCombination"/></returns>
     */
    SquareColor[] SimulatePress(SquareColor[] currentSquareCombination, int idxPress, int currentTransitionIdxInput)
    {
        if (!TransitionValues.ContainsKey(currentTransitionIdxInput) || idxPress < 0 || idxPress >= currentSquareCombination.Length) return currentSquareCombination;
        var currentTransitionSet = TransitionValues[currentTransitionIdxInput];
        var offsetSurroundings = new[] { -5, -4, -3, -1,
            1, 3, 4, 5 };
        var offsetRules = new[] { idxPress % 4 > 0, true,
            idxPress % 4 < 3, idxPress % 4 > 0,
            idxPress % 4 < 3, idxPress % 4 > 0,
            true, idxPress % 4 < 3};

        for (var x = 0; x < offsetSurroundings.Length; x++)
        {
            var squareIdxCurrent = idxPress + offsetSurroundings[x];
            if (squareIdxCurrent >= 0 && squareIdxCurrent < currentSquareCombination.Length && offsetRules[x])
            {
                currentSquareCombination[squareIdxCurrent] = currentTransitionSet.ContainsKey(currentSquareCombination[squareIdxCurrent]) ?
                    currentTransitionSet[currentSquareCombination[squareIdxCurrent]] :
                    currentSquareCombination[squareIdxCurrent];
            }
        }
        currentSquareCombination[idxPress] = SquareColor.White;

        return currentSquareCombination;
    }
    void ObtainPossibleSolutions(SquareColor[] currentSquareCombination, int maxDepth = 3, bool scanAllValidsWithinDepth = false)
    {
        var idxStoredPreviousStates = new List<IEnumerable<int>>();
        var idxCurrentPreviousStates = new List<IEnumerable<int>>();
        idxCurrentPreviousStates.Add(new int[0]);
        for (var x = 0; x < maxDepth; x++)
        {
            var nextPreviousStates = new List<IEnumerable<int>>();
            for (var a = 0; a < idxCurrentPreviousStates.Count; a++)
            {
                for (var b = 0; b < 16; b++)
                {
                    var curSequence = idxCurrentPreviousStates[a].Concat(new[] { b });
                    var curBoard = currentSquareCombination.ToArray();
                    for (var p = 0; p < curSequence.Count(); p++)
                        SimulatePress(curBoard, curSequence.ElementAt(p), (curIdxTransition + p) % 10);

                    var uniqueBoard = true;
                    for (var p = 0; p < idxStoredPreviousStates.Count; p++)
                    {
                        var boardToCheck = currentSquareCombination.ToArray();
                        for (var e = 0; e < idxStoredPreviousStates[p].Count(); e++)
                            SimulatePress(boardToCheck, idxStoredPreviousStates[p].ElementAt(e), (curIdxTransition + e) % 10);
                        uniqueBoard &= !boardToCheck.SequenceEqual(curBoard);
                    }
                    // Maybe unused since the modified board state will always be different.
                    /*
                    for (var p = 0; p < idxCurrentPreviousStates.Count; p++)
                    {
                        var boardToCheck = currentSquareCombination.ToArray();
                        for (var e = 0; e < idxCurrentPreviousStates.Count(); e++)
                            SimulatePress(curBoard, idxCurrentPreviousStates[p].ElementAt(e), (curIdxTransition + e) % 10);
                        uniqueBoard &= !boardToCheck.SequenceEqual(curBoard);
                    }
                    */
                    if (uniqueBoard)
                    {
                        if (AnyRulesValid(curBoard))
                            nextPreviousStates.Add(curSequence);
                        else
                            solutionIdxes.Add(curSequence.ToList());
                    }
                }
            }
            if (solutionIdxes.Any() && !scanAllValidsWithinDepth)
                break;
            idxStoredPreviousStates.AddRange(idxCurrentPreviousStates);
            idxCurrentPreviousStates.Clear();
            idxCurrentPreviousStates.AddRange(nextPreviousStates);
        }
    }

    IEnumerator AnimateDisarmAnim()
    {
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.All);
        while (IsCoroutineActive)
            yield return null;
        StartSquareColorsCoroutine(Enumerable.Repeat(SquareColor.Black,16).ToArray(), SquaresToRecolor.All);
        while (IsCoroutineActive)
            yield return null;
        ModulePassed();
    }
    IEnumerator AnimateStrikeAnim()
    {
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.All);
        while (IsCoroutineActive)
            yield return null;
        Strike();
        PrepModule();
        disableInteractions = false;
    }

    protected override void ButtonPressed(int index)
    {
        if (disableInteractions) return;
        PlaySound(index);
        SimulatePress(_colors, index, curIdxTransition);
        curIdxTransition = (curIdxTransition + 1) % 10;
        movesMadeOnModule++;
        Log("Pressing square {0} resulted in the following square colors:", QuickCoord(index));
        LogBoard(_colors);
        StartSquareColorsCoroutine(_colors, SquaresToRecolor.All);
        if (AnyRulesValid(_colors, true))
        {
            if (movesMadeOnModule >= 15)
            {
                Log("At least 15 moves have been made since the last reset. And the module is not solved. Starting over...");
                disableInteractions = true;
                StartCoroutine(AnimateStrikeAnim());
                return;
            }
            Log("Current Transition Index for press #{1}: {0}", curIdxTransition, movesMadeOnModule + 1);

            solutionIdxes.RemoveAll(a => a.ElementAt(debugPathSolution) != index);
            if (solutionIdxes.Any())
            {
                debugPathSolution++;
                Log("One solution sequence (from {2} remaining sequence(s)) after press #{1}: {0}", solutionIdxes.PickRandom().Skip(debugPathSolution).Select(a => QuickCoord(a)).Join(), movesMadeOnModule, solutionIdxes.Count());
            }
            else
            {
                debugPathSolution = 0;
                ObtainPossibleSolutions(_colors, Mathf.Min(3, 15 - movesMadeOnModule));
                if (solutionIdxes.Any())
                    Log("Deviated from generated possible solutions. One solution sequence (from {2} detected shortest possible sequence(s)) after press #{1}: {0}", solutionIdxes.PickRandom().Select(a => QuickCoord(a)).Join(), movesMadeOnModule, solutionIdxes.Count());
                else
                    Log("Deviated from generated possible solutions. And there were no possible sequences left after press #{0}. Might be done for.", movesMadeOnModule);
            }
            
        }
        else
        {
            disableInteractions = true;
            Log("No rules were active upon pressing this square.");
            StartCoroutine(AnimateDisarmAnim());
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        if (!solutionIdxes.Any())
            ObtainPossibleSolutions(_colors);
        List<int> currentSolutionSet = null;
        if (solutionIdxes.Any())
            currentSolutionSet = solutionIdxes.Where(a => a.Count <= solutionIdxes.Select(b => b.Count).Min()).PickRandom();
        else
        {
            LogDebug("NO VALID PATHS HAVE BEEN DETECTED. FORCE SOLVING.");
        }
        for (var x = 0; currentSolutionSet != null && x < currentSolutionSet.Count; x++)
        {
            Buttons[currentSolutionSet[x]].OnInteract();
            while (IsCoroutineActive)
                yield return true;
        }
    }

}
