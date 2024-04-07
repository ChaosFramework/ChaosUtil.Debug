namespace ChaosUtil.Debug.AdvancedDebuggerDisplay
{
    abstract class Token
    {
        public static string Evaluate(Token token) => token.Value();

        public readonly string token;

        public Token(string token)
        {
            this.token = token;
        }

        public abstract string Value();
    }
}
