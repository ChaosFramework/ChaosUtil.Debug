namespace ChaosUtil.Debug.AdvancedDebuggerDisplay
{
    class TerminalToken : Token
    {
        public TerminalToken(string token)
            : base(token)
        { }

        public override string Value()
            => token;
    }
}
