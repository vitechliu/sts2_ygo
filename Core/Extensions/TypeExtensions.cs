namespace VYgo.Core.Extensions;

public static class TypeExtensions {
    public static bool IsGenericTypeOf(this Type type, Type genericType) {
        if (!genericType.IsGenericTypeDefinition)
            throw new ArgumentException("genericType 必须是未指定泛型参数的类型定义，例如 typeof(TestClass<>)");

        while (type != null && type != typeof(object)) {
            // 检查当前类型是否是泛型，并且其泛型定义是否匹配
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType) {
                return true;
            }

            // 向上查找父类
            type = type.BaseType;
        }

        return false;
    }
}