using ChaosUtil.Primitives;
using System;
using System.Reflection;
using MethodMap = System.Collections.Generic.Dictionary<
    System.Type,
    System.Collections.Generic.Dictionary<
        string,
        System.Reflection.MethodInfo
        >
    >;

namespace ChaosUtil.Debug.AdvancedDebuggerDisplay.Members
{
    static class Method
    {
        static readonly MethodMap methodMap = new MethodMap();

        static readonly MethodInfo debugDisplayMethod = typeof(AdvancedDebuggerDisplayExtensions).GetMethod(
            nameof(AdvancedDebuggerDisplayExtensions.DebugDisplay),
            BindingFlags.Public | BindingFlags.Static
            );

        public static bool GetValue(ref Type targetType, ref object obj, string member)
            => GetMethodObject(ref targetType, ref obj, member)
            || GetMethodInvocation(ref targetType, ref obj, member);

        static MethodInfo FindMethod(Type targetType, string methodName, BindingFlags flags)
        {
            MethodInfo meth = targetType.GetMethod(methodName, flags, null, Array<Type>.empty, Array<ParameterModifier>.empty);
            if (meth != null)
                return meth;

            // TODO: properly support extension methods
            if (methodName == nameof(AdvancedDebuggerDisplayExtensions.DebugDisplay))
                return debugDisplayMethod.MakeGenericMethod(targetType);

            return null;
        }

        public static bool GetMethodObject(ref Type targetType, ref object obj, string methodName)
        {
            Type t = targetType;
            MethodInfo meth = MemberMap.GetMember(
                methodMap,
                targetType,
                methodName,
                flags => FindMethod(t, methodName, flags)
                );
            if (meth != null)
            {
                targetType = typeof(MethodInfo);
                obj = meth;
                return true;
            }
            else
                return false;
        }

        public static bool GetMethodInvocation(ref Type targetType, ref object obj, string invocation)
        {
            if (invocation.Length < 3)
                return false; // at least one letter for method name plus one pair of parantheses

            int paraOpen = invocation.IndexOf('(');
            if (paraOpen < 0)
                return false;

            int paraClose = invocation.LastIndexOf(')');
            if (paraClose != invocation.Length - 1)
                return false;

            int argsLength = paraClose - paraOpen - 1;
            if (argsLength != 0)
                // TODO: support method arguments
                throw new NotImplementedException("Currently only parameterless method calls are supported.");

            string methodName = invocation.Substring(0, paraOpen);
            Type t = targetType;
            MethodInfo meth = MemberMap.GetMember(
                methodMap,
                targetType,
                methodName,
                flags => FindMethod(t, methodName, flags)
                );
            if (meth != null)
            {
                if (meth.IsStatic)
                    obj = meth.Invoke(null, new[] { obj });
                else
                    obj = meth.Invoke(obj, Array<object>.empty);
                targetType = meth.ReturnType;
                return true;
            }
            else
                return false;
        }
    }
}
