using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ModifyReadonlyProperties
{
    public static class ModifyReadonlyProperties
    {
        public static bool ModifyReadOnlyProperty<TThis, TProperty>(this TThis @this, Expression<Func<TThis, TProperty>> expression, in TProperty value)
            where TThis : notnull
            => ModifyReadOnlyProperty(@this, expression.Body, value);

        public static bool ModifyReadOnlyProperty<T>(Expression<Func<T>> expression, in T value)
        {
            dynamic d = expression.Compile().Target!;
            var t = (object)d.Constants[0];
            return ModifyReadOnlyProperty(t, expression.Body, value);
        }

        private static bool ModifyReadOnlyProperty<T>(object @this, Expression body, in T value)
        {
            if (body is MemberExpression prop)
                if (prop.Member is PropertyInfo propInfo)
                    if (propInfo.CanRead && !propInfo.CanWrite)
                        if (propInfo.GetMethod!.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) is { })
                        {
                            var backingField = @this.GetType().GetAllFields().FirstOrDefault(field => field.Name == $@"<{propInfo.Name}>k__BackingField");
                            if (backingField is null)
                                return false;
                            backingField.SetValue(@this, value);
                            return true;
                        }
            return false;
        }

        public static IEnumerable<FieldInfo> GetAllFields(this Type t)
        {
            return GetAllFieldsImpl(t);

            static IEnumerable<FieldInfo> GetAllFieldsImpl(Type? t)
            {
                if (t == null)
                    return Enumerable.Empty<FieldInfo>();

                var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                return t.GetFields(flags).Concat(GetAllFieldsImpl(t.BaseType));
            }
        }
    }
}
