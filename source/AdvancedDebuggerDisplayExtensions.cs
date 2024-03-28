using ChaosUtil.Debug.AdvancedDebuggerDisplay;
using ChaosUtil.Reflection;
using System;
using System.Diagnostics;
using System.Linq;
using SysCol = System.Collections.Generic;

/// <summary> Provides a way of evaluating debugger displays at runtime and constructing recursive debugger displays. </summary>
public static class AdvancedDebuggerDisplayExtensions
{
    // TODO: Provide methods to generate debugger displays that automatically list public (or public and non-public) members

    static readonly SysCol.Dictionary<Type, DebuggerDisplayAttribute> displayAttributes
        = new SysCol.Dictionary<Type, DebuggerDisplayAttribute>();
    
    /// <summary>
    ///     Returns a string representation of <paramref name="obj"/> as displayed by the debugger.
    ///     If a <see cref="DebuggerDisplayAttribute"/> is attached to <typeparamref name="T"/>,
    ///     it will be used to generate the debugger display.
    ///     Otherwise <see cref="object.ToString"/> will be used as fallback.
    /// </summary>
    /// <typeparam name="T"> The type of the object to be displayed. </typeparam>
    /// <param name="obj"> The object to be displayed. </param>
    public static string DebugDisplay<T>(this T obj)
        => DebugDisplayInternal(typeof(T), obj);

    internal static string DebugDisplayInternal(Type t, object obj)
    {
        DebuggerDisplayAttribute debuggerDisplay;
        if (!displayAttributes.TryGetValue(t, out debuggerDisplay))
            displayAttributes[t] = debuggerDisplay = t.GetAttributes<DebuggerDisplayAttribute>(true).FirstOrDefault();

        if (debuggerDisplay == null)
            return obj?.ToString() ?? "null";

        return string.Concat(Tokenize(obj, debuggerDisplay.Value).Select(Token.Evaluate));
    }

    static SysCol.IEnumerable<Token> Tokenize(object context, string debuggerDisplay)
    {
        bool isComplicated = false;
        int start = 0;

        while (start < debuggerDisplay.Length)
            if (isComplicated)
            {
                int depth = 0;
                for (int end = start; end < debuggerDisplay.Length; end++)
                    switch (debuggerDisplay[end])
                    {
                        case '{':
                            depth++;
                            break;

                        case '}':
                            if (--depth < 0)
                            {
                                string token = debuggerDisplay.Substring(start, end - start);
                                yield return new EvaluatedToken(context, token);
                                start = end + 1;
                                isComplicated = false;
                                goto token_found;
                            }
                            break;
                    }

                // TODO: allow escaping braces with double braces
                throw new Exception("Illegal braces!");

            token_found:;
            }
            else
            {
                int end = debuggerDisplay.IndexOf('{', start);
                if (end < 0)
                    end = debuggerDisplay.Length;

                string terminal = debuggerDisplay.Substring(start, end - start);
                yield return new TerminalToken(terminal);
                start = end + 1;
                isComplicated = true;
            }
    }
}
