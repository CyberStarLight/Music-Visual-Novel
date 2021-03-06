﻿using System;
using System.Reflection;
using System.Collections.Generic;

delegate object MethodHandler(object target, params object[] args);

interface IReflectionProvider
{
    T GetSingleAttributeOrDefault<T>(MemberInfo memberInfo) where T : Attribute, new();
    IEnumerable<MemberInfo> GetSerializableMembers(Type type);
    object Instantiate(Type type);
    object GetValue(MemberInfo member, object instance);
    void SetValue(MemberInfo member, object instance, object value);
    MethodHandler GetDelegate(MethodBase method);
}