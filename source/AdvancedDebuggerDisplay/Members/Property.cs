using System;
using System.Reflection;
using PropertyMap = System.Collections.Generic.Dictionary<
    System.Type,
    System.Collections.Generic.Dictionary<
        string,
        System.Reflection.PropertyInfo
        >
    >;

namespace ChaosUtil.Debug.AdvancedDebuggerDisplay.Members
{
    static class Property
    {
        static readonly PropertyMap propertyMap = new PropertyMap();

        public static bool GetValue(ref Type targetType, ref object obj, string member)
        {
            Type t = targetType;
            PropertyInfo property = MemberMap.GetMember(propertyMap, targetType, member, flags => t.GetProperty(member, flags));
            if (property != null)
            {
                obj = property.GetValue(obj);
                targetType = property.PropertyType;
                return true;
            }

            return false;
        }
    }
}
