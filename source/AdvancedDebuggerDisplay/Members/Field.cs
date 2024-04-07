using System;
using System.Reflection;
using FieldMap = System.Collections.Generic.Dictionary<
    System.Type,
    System.Collections.Generic.Dictionary<
        string,
        System.Reflection.FieldInfo
        >
    >;

namespace ChaosUtil.Debug.AdvancedDebuggerDisplay.Members
{
    static class Field
    {
        static readonly FieldMap fieldMap = new FieldMap();

        public static bool GetValue(ref Type targetType, ref object obj, string fieldName)
        {
            Type t = targetType;
            FieldInfo field = MemberMap.GetMember(fieldMap, targetType, fieldName, flags => t.GetField(fieldName, flags));
            if (field != null)
            {
                obj = field.GetValue(obj);
                targetType = field.FieldType;
                return true;
            }

            return false;
        }
    }
}
