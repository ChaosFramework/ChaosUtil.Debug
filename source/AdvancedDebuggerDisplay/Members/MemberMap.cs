using System;
using System.Reflection;
using SysCol = System.Collections.Generic;

namespace ChaosUtil.Debug.AdvancedDebuggerDisplay.Members
{
    static class MemberMap
    {
        public static Member GetMember<Member>(
            SysCol.Dictionary<Type, SysCol.Dictionary<string, Member>> memberMap,
            Type type,
            string memberName,
            Func<BindingFlags, Member> getMember
            )
            where Member : class
        {
            SysCol.Dictionary<string, Member> typeMemberMap;
            if (!memberMap.TryGetValue(type, out typeMemberMap))
                memberMap[type] = typeMemberMap = new SysCol.Dictionary<string, Member>();

            Member member;
            if (!typeMemberMap.TryGetValue(memberName, out member))
                typeMemberMap[memberName] = member = getMember(BindingFlags.Public | BindingFlags.Instance) ?? getMember(BindingFlags.NonPublic | BindingFlags.Instance);

            return member;
        }
    }
}
