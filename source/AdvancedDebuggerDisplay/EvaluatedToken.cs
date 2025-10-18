using System;
using System.Linq;

namespace ChaosUtil.Debug.AdvancedDebuggerDisplay
{
    class EvaluatedToken : Token
    {
        static string Trim(string str) => str.Trim();

        readonly Type targetType;
        readonly object obj;

        public EvaluatedToken(object obj, string token)
            : base(token)
        {
            Type targetType = obj.GetType();
            foreach (string member in token.Split('.').Select(Trim))
            {
                // TODO: support indexers

                if (Members.Field.GetValue(ref targetType, ref obj, member))
                    continue;

                if (Members.Property.GetValue(ref targetType, ref obj, member))
                    continue;

                if (Members.Method.GetValue(ref targetType, ref obj, member))
                    continue;

                throw new Exception($"Could not evaluate \"{member}\".");
            }

            this.targetType = targetType;
            this.obj = obj;
        }

        public override string Value()
            => AdvancedDebuggerDisplayExtensions.DebugDisplayInternal(targetType, obj);
    }
}
