using ColoredSquares;
using System.Collections.Generic;
using System.Linq;

public sealed class NotColoredSquaresScript : ColoredSquaresModuleBase
{
    public override string Name { get { return "Not Colored Squares"; } }

    static T[] newArray<T>(params T[] array) { return array; }

    private bool _onStageTwo;
    private int _stageOnePress;
    private static readonly SquareColor[] _colorsInUse = new[] { SquareColor.Red, SquareColor.Blue, SquareColor.Green, SquareColor.Yellow, SquareColor.Magenta };
    private static readonly SquareColor[][] _table = newArray(
        newArray(SquareColor.Blue, SquareColor.Blue, SquareColor.Blue, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Blue, SquareColor.Yellow, SquareColor.Green, SquareColor.Red, SquareColor.Blue, SquareColor.Green, SquareColor.Magenta, SquareColor.Blue, SquareColor.Red, SquareColor.Red),
        newArray(SquareColor.Blue, SquareColor.Blue, SquareColor.Red, SquareColor.Yellow, SquareColor.Green, SquareColor.Yellow, SquareColor.Green, SquareColor.Blue, SquareColor.Green, SquareColor.Green, SquareColor.Blue, SquareColor.Magenta, SquareColor.Green, SquareColor.Blue, SquareColor.Green, SquareColor.Yellow),
        newArray(SquareColor.Green, SquareColor.Yellow, SquareColor.Blue, SquareColor.Blue, SquareColor.Yellow, SquareColor.Red, SquareColor.Blue, SquareColor.Green, SquareColor.Red, SquareColor.Magenta, SquareColor.Green, SquareColor.Red, SquareColor.Blue, SquareColor.Green, SquareColor.Magenta, SquareColor.Magenta),
        newArray(SquareColor.Red, SquareColor.Magenta, SquareColor.Blue, SquareColor.Magenta, SquareColor.Magenta, SquareColor.Red, SquareColor.Red, SquareColor.Blue, SquareColor.Magenta, SquareColor.Red, SquareColor.Blue, SquareColor.Blue, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Red, SquareColor.Red),
        newArray(SquareColor.Magenta, SquareColor.Yellow, SquareColor.Blue, SquareColor.Blue, SquareColor.Green, SquareColor.Red, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Green, SquareColor.Blue, SquareColor.Red, SquareColor.Blue, SquareColor.Yellow, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Magenta),
        newArray(SquareColor.Green, SquareColor.Green, SquareColor.Yellow, SquareColor.Blue, SquareColor.Red, SquareColor.Yellow, SquareColor.Green, SquareColor.Yellow, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Red, SquareColor.Blue, SquareColor.Blue, SquareColor.Red, SquareColor.Blue, SquareColor.Green),
        newArray(SquareColor.Yellow, SquareColor.Blue, SquareColor.Yellow, SquareColor.Red, SquareColor.Yellow, SquareColor.Blue, SquareColor.Red, SquareColor.Green, SquareColor.Magenta, SquareColor.Magenta, SquareColor.Blue, SquareColor.Magenta, SquareColor.Green, SquareColor.Green, SquareColor.Yellow, SquareColor.Green),
        newArray(SquareColor.Yellow, SquareColor.Magenta, SquareColor.Magenta, SquareColor.Magenta, SquareColor.Red, SquareColor.Magenta, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Yellow, SquareColor.Yellow, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Blue),
        newArray(SquareColor.Red, SquareColor.Blue, SquareColor.Yellow, SquareColor.Green, SquareColor.Green, SquareColor.Magenta, SquareColor.Green, SquareColor.Magenta, SquareColor.Magenta, SquareColor.Red, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Green, SquareColor.Yellow, SquareColor.Red, SquareColor.Green),
        newArray(SquareColor.Magenta, SquareColor.Green, SquareColor.Red, SquareColor.Green, SquareColor.Red, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Yellow, SquareColor.Blue, SquareColor.Red, SquareColor.Green, SquareColor.Blue),
        newArray(SquareColor.Magenta, SquareColor.Red, SquareColor.Yellow, SquareColor.Blue, SquareColor.Blue, SquareColor.Magenta, SquareColor.Red, SquareColor.Blue, SquareColor.Yellow, SquareColor.Blue, SquareColor.Yellow, SquareColor.Blue, SquareColor.Magenta, SquareColor.Green, SquareColor.Red, SquareColor.Blue),
        newArray(SquareColor.Red, SquareColor.Blue, SquareColor.Magenta, SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Green, SquareColor.Red, SquareColor.Blue, SquareColor.Red, SquareColor.Red, SquareColor.Blue, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Green, SquareColor.Red),
        newArray(SquareColor.Blue, SquareColor.Blue, SquareColor.Red, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Blue, SquareColor.Yellow, SquareColor.Yellow, SquareColor.Blue, SquareColor.Blue, SquareColor.Blue, SquareColor.Green, SquareColor.Red, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Red),
        newArray(SquareColor.Red, SquareColor.Red, SquareColor.Blue, SquareColor.Green, SquareColor.Red, SquareColor.Yellow, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Red, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Red, SquareColor.Red, SquareColor.Blue, SquareColor.Red, SquareColor.Blue),
        newArray(SquareColor.Blue, SquareColor.Magenta, SquareColor.Magenta, SquareColor.Green, SquareColor.Blue, SquareColor.Magenta, SquareColor.Yellow, SquareColor.Green, SquareColor.Red, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Red, SquareColor.Green, SquareColor.Blue, SquareColor.Yellow, SquareColor.Blue),
        newArray(SquareColor.Blue, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Red, SquareColor.Yellow, SquareColor.Green, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Blue, SquareColor.Yellow, SquareColor.Magenta, SquareColor.Blue, SquareColor.Red, SquareColor.Magenta, SquareColor.Green, SquareColor.Green)
        );

    private void Start()
    {
        SetInitialState();
    }

    private void SetInitialState()
    {
        tryAgain:
        var counts = new Dictionary<SquareColor, int>();
        foreach(var c in _colorsInUse)
            counts[c] = 1;
        var indexes = Enumerable.Range(0, 16).ToList().Shuffle().Take(_colorsInUse.Length).ToArray();
        for(var i = 0; i < _colorsInUse.Length; i++)
            _colors[indexes[i]] = _colorsInUse[i];
        for(int i = 0; i < 16; i++)
            if(!indexes.Contains(i))
            {
                _colors[i] = _colorsInUse.PickRandom();
                counts[_colors[i]]++;
            }
        var minCount = _colorsInUse.Min(c => counts[c]);
        if(minCount > 1)
            goto tryAgain;
        if(counts.Count(kvp => kvp.Value == minCount) > 1)
            goto tryAgain;

        var minCountColor = _colorsInUse.First(c => counts[c] == minCount);

        _stageOnePress = -1;
        for(int i = 0; i < 16; i++)
            if(_colors[i] == minCountColor)
                _stageOnePress = i;

        StartSquareColorsCoroutine(_colors, SquaresToRecolor.All, delay: true);
        Log("First stage color is {0}. Count: {1}.", minCountColor, minCount);
        LogDebug("Colors: {0}", _colors.JoinString(", "));
    }

    protected override void ButtonPressed(int index)
    {
        if(_isSolved)
            return;

        if(!_onStageTwo)
        {
            if(index == _stageOnePress)
            {
                _onStageTwo = true;
                PlaySound(index);

                _colors[index] = SquareColor.Black;
                SetButtonColor(index, SquareColor.Black);

                var nonBlack = Enumerable.Range(0, 16).Where(i => _colors[i] != SquareColor.Black).ToArray();
                var shuff = new SquareColor[16];
                _table[_stageOnePress].CopyTo(shuff, 0);
                shuff = shuff.Shuffle();
                foreach(var i in nonBlack)
                {
                    SetButtonColor(i, SquareColor.White);
                    _colors[i] = shuff[i];
                }

                Log("The starting colors for stage two are: {0}", _colors.Join(", "));

                StartSquareColorsCoroutine(_colors, SquaresToRecolor.NonblackOnly, delay: true);
            }
            else
                Strike("Button #{0} ({1}) was incorrect at this time.", index, _colors[index]);
        }
        else
        {
            if(_colors[index] != SquareColor.Black)
            {
                PlaySound(index);
                int blackIx = Enumerable.Range(0, 16).First(i => _colors[i] == SquareColor.Black);
                if(index / 4 == blackIx / 4 || index % 4 == blackIx % 4)
                {
                    SetButtonColor(blackIx, _colors[index]);
                    _colors[blackIx] = _colors[index];
                    SetButtonColor(index, SquareColor.Black);
                    _colors[index] = SquareColor.Black;
                }
            }
            else
            {
                if(Enumerable.Range(0, 16).All(i => _colors[i] == _table[_stageOnePress][i] || _colors[i] == SquareColor.Black))
                    ModulePassed();
                else
                    Strike("That wasn't the correct position! (Wrong: {0})", Enumerable.Range(0, 16).Where(i => _colors[i] != _table[_stageOnePress][i] && _colors[i] != SquareColor.Black).Select(i => string.Format( @"{0} ({1})", i, _colors[i])).Join(", "));
            }
        }
    }

    private void Strike(string message = null, params object[] args)
    {
        if(message != null)
            Log(message, args);
        base.Strike();
        SetInitialState();
        _onStageTwo = false;
    }

    private System.Collections.IEnumerator TwitchHandleForcedSolve()
    {
        while(!_isSolved)
        {
            // TODO: Make this functional
            ModulePassed();
        }
        yield break;
    }
}
