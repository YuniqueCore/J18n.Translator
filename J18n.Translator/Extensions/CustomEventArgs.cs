namespace J18n.Translator.Extensions;

public class BoolStateChangedEventArgs : EventArgs
{
    public bool NewValue { get; }
    public BoolStateChangedEventArgs(bool newValue)
    {
        NewValue = newValue;
    }
}
