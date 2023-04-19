using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ColoredSquares;

public class PerspecoloredSquaresScript : ColoredSquaresModuleBase {
    public override string Name {get { return "Perspecolored Squares"; } }
    public TextMesh debugRotationTxt;
    SquareColor[] altSquareColors = new SquareColor[16];
    [SerializeField]
    private Vector3[] possibleViewAngles;
    private Vector3 selectedViewAngle;
    bool displayAltColors = false;
    List<int> expectedIdxPresses;
    int curIdxPressList;
    int[,] squareColorValuesAll;
    Dictionary<int, List<int>> valuesToIdxCombinations;
    //Coroutine activeRotationHandler;
    void HandleRuleSeed()
    {
        var rng = RuleSeedable.GetRNG() ?? new MonoRandom(1);
        if (rng.Seed == 1)
        {
            squareColorValuesAll = new[,] {
                { 1, 12, 14, 3, 7 },
                { 11, 15, 2, 5, 16 },
                { 8, 9, 15, 4, 6 },
                { 10, 13, 7, 12, 14 },
                { 2, 4, 15, 6, 8 },
            };
        }
    }

    // Use this for initialization
    void Start () {
        HandleRuleSeed();
        valuesToIdxCombinations = new Dictionary<int, List<int>>();
        for (var x = 0; x < 25; x++)
        {
            var rowIdx = x / 5;
            var colIdx = x % 5;
            var curValue = squareColorValuesAll[rowIdx, colIdx];
            List<int> obtainedCombinations;
            if (!valuesToIdxCombinations.TryGetValue(curValue, out obtainedCombinations))
            {
                obtainedCombinations = new List<int>();
                valuesToIdxCombinations.Add(curValue, obtainedCombinations);
            }
            obtainedCombinations.Add(x);
        }
        LogDebug("Possible values from idx combinations: {0}", valuesToIdxCombinations.Select(a => string.Format("[{0}: {1}]", a.Key, a.Value.Join(","))).Join(";"));
        expectedIdxPresses = new List<int>();
        expectedIdxPresses.AddRange(Enumerable.Range(0, 16));
        ResetModule();
	}
	IEnumerator HandleAutoRotateWhileActive()
    {
        var storedRememberedStateRot = displayAltColors;
        var storedIdxCount = curIdxPressList;
        while (IsCoroutineActive)
            yield return null;
        while (curIdxPressList == storedIdxCount)
        {
            if (storedRememberedStateRot ^ displayAltColors)
            {
                storedRememberedStateRot = displayAltColors;
                StartSquareColorsCoroutine(displayAltColors ? altSquareColors : _colors, SquaresToRecolor.NonwhiteOnly);
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
        selectedViewAngle = possibleViewAngles == null || !possibleViewAngles.Any() ? Vector3.zero : possibleViewAngles.PickRandom();

        expectedIdxPresses.Shuffle();
        LogDebug("Expected idxes to press: {0}", expectedIdxPresses.Join());
        LogDebug("New rotation angle (global): {0}", selectedViewAngle.ToString());
        CalculateExpected(true);
    }
    void CalculateExpected(bool guarenteeDifferentColorPairings = false)
    {
        var squareColorRowCol = new[] { SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Yellow, SquareColor.Magenta };
        var whiteSquareIdxes = expectedIdxPresses.Take(curIdxPressList);
        foreach (var idx in whiteSquareIdxes)
        {
            altSquareColors[idx] = SquareColor.White;
            _colors[idx] = SquareColor.White;
            SetButtonColor(idx, SquareColor.White);
        }
        retryGuarenteeNonUnique:
        var nonWhiteSquareIdxes = expectedIdxPresses.Skip(curIdxPressList);
        var sumAll = 0;
        for (var x = 0; x < nonWhiteSquareIdxes.Count() - 1; x++)
        {
            var curIdx = nonWhiteSquareIdxes.ElementAt(x);
            var pickedPairing = valuesToIdxCombinations.PickRandom();
            sumAll += pickedPairing.Key;
            var pickedTableCellIdx = pickedPairing.Value.PickRandom();
            _colors[curIdx] = squareColorRowCol[pickedTableCellIdx % 5];
            altSquareColors[curIdx] = squareColorRowCol[pickedTableCellIdx / 5];
            SetButtonBlack(curIdx);
        }
        var curPosToPress = expectedIdxPresses[curIdxPressList];
        var lastIdxNonWhite = nonWhiteSquareIdxes.Last();
        var allowedSumsMod16 = new List<int> { curPosToPress + 1 };
        for (var x = 1; x <= 15; x++)
        {
            var backScanCurIdx = (15 * x + curPosToPress) % 16;
            if (!whiteSquareIdxes.Contains(backScanCurIdx))
                break;
            allowedSumsMod16.Add(backScanCurIdx + 1);
        }
        LogDebug("Allowed sums, kept within 1 - 16: {0}", allowedSumsMod16.Join());
        var allowedValueToAdd = new List<int>();
        for (var x = valuesToIdxCombinations.Keys.Min(); x <= valuesToIdxCombinations.Keys.Max(); x++)
            if (allowedSumsMod16.Contains((sumAll + x - 1) % 16 + 1))
            {
                allowedValueToAdd.Add(x);
            }

        var selectedPairing = valuesToIdxCombinations[allowedValueToAdd.PickRandom()].PickRandom();
        _colors[lastIdxNonWhite] = squareColorRowCol[selectedPairing % 5];
        altSquareColors[lastIdxNonWhite] = squareColorRowCol[selectedPairing / 5];
        SetButtonBlack(lastIdxNonWhite);
        if (guarenteeDifferentColorPairings && Enumerable.Range(0, 16).All(a => _colors[a] == altSquareColors[a]))
            goto retryGuarenteeNonUnique;

        Log("{0} square(s) left to press, the squares in reading order when looking straight is {1}", 16 - curIdxPressList, _colors.Select(a => a.ToString()[0]).Join());
        Log("{0} square(s) left to press, the squares in reading order when looking at a different angle is {1}", 16 - curIdxPressList, altSquareColors.Select(a => a.ToString()[0]).Join());
        Log("Press square #{0} in reading order to continue.", expectedIdxPresses[curIdxPressList] + 1);
        StopAllCoroutines();
        
        StartSquareColorsCoroutine(displayAltColors ? altSquareColors : _colors, SquaresToRecolor.NonwhiteOnly, true);
        StartCoroutine(HandleAutoRotateWhileActive());
    }
    protected override void ButtonPressed(int index)
    {
        if (expectedIdxPresses[curIdxPressList] == index)
        {
            PlaySound(index);
            curIdxPressList++;
            if (curIdxPressList >= 16)
            {
                Log("All of the squares have been correctly pressed.");
                ModulePassed();
            }
            else
            {
                SetButtonColor(index, SquareColor.White);
                CalculateExpected();
            }
        }
        else
        {
            curIdxPressList = 0;
            Log("Pressing square #{0} was not correct. Starting over...", index + 1);
            Strike();
            ResetModule();
        }
    }
    bool CheckEularAnglesWithinMarginOfError(Vector3 firstVector, Vector3 secondVector, float errorMargin = 1f)
    {
        var withinX = Mathf.Abs(firstVector.x - secondVector.x) <= errorMargin || (Mathf.Abs(firstVector.x - secondVector.x) >= 360f - errorMargin && Mathf.Abs(firstVector.x - secondVector.x) <= 360f + errorMargin);
        var withinY = Mathf.Abs(firstVector.y - secondVector.y) <= errorMargin || (Mathf.Abs(firstVector.y - secondVector.y) >= 360f - errorMargin && Mathf.Abs(firstVector.y - secondVector.y) <= 360f + errorMargin);
        var withinZ = Mathf.Abs(firstVector.z - secondVector.z) <= errorMargin || (Mathf.Abs(firstVector.z - secondVector.z) >= 360f - errorMargin && Mathf.Abs(firstVector.z - secondVector.z) <= 360f + errorMargin);

        return withinX && withinY && withinZ;
    }
    bool CheckQuaternionWithinMarginOfError(Quaternion firstVector, Quaternion secondVector, float errorMargin = 1f)
    {
        var withinX = Mathf.Abs(firstVector.x - secondVector.x) <= errorMargin || (Mathf.Abs(firstVector.x - secondVector.x) >= 360f - errorMargin && Mathf.Abs(firstVector.x - secondVector.x) <= 360f + errorMargin);
        var withinY = Mathf.Abs(firstVector.y - secondVector.y) <= errorMargin || (Mathf.Abs(firstVector.y - secondVector.y) >= 360f - errorMargin && Mathf.Abs(firstVector.y - secondVector.y) <= 360f + errorMargin);
        var withinZ = Mathf.Abs(firstVector.z - secondVector.z) <= errorMargin || (Mathf.Abs(firstVector.z - secondVector.z) >= 360f - errorMargin && Mathf.Abs(firstVector.z - secondVector.z) <= 360f + errorMargin);
        var withinW = Mathf.Abs(firstVector.w - secondVector.w) <= errorMargin || (Mathf.Abs(firstVector.w - secondVector.w) >= 360f - errorMargin && Mathf.Abs(firstVector.w - secondVector.w) <= 360f + errorMargin);

        return withinX && withinY && withinZ && withinW;
    }

    // Update is called once per frame
    void Update () {
        var firstAngle = Vector3.Angle(transform.up, Camera.main.transform.up);
        var secondAngle = Vector3.Angle(transform.up, Camera.main.transform.right);
        var combinedAngleView = new Vector3(firstAngle, 0, secondAngle);
        if (debugRotationTxt != null)
        {
            debugRotationTxt.text = string.Format("{0}\n{1}\n{2}", transform.eulerAngles.ToString(), selectedViewAngle.ToString(), combinedAngleView.ToString());
            debugRotationTxt.color = //CheckQuaternionWithinMarginOfError(transform.rotation, Quaternion.Euler(selectedViewAngle), 1f) ? Color.cyan :
                CheckEularAnglesWithinMarginOfError(selectedViewAngle, combinedAngleView, 15f) ? Color.green : Color.white;
        }
        if (!_isSolved)
            displayAltColors = CheckEularAnglesWithinMarginOfError(selectedViewAngle, combinedAngleView, 15f);
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_isSolved)
        {
            Buttons[expectedIdxPresses[curIdxPressList]].OnInteract();
            if (curIdxPressList + 1 < expectedIdxPresses.Count)
                while (IsCoroutineActive)
                    yield return true;
            else
                yield return new WaitForSeconds(0.1f);
        }
    }
}
