namespace Spectre.Console.Cli;

internal static class ConfigurationHelper
{
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063:AnnotationMismatch",
        Justification = "ICommand<T> is annotated as requiring PublicProperties. The ICommand annotation and this methods return annotation must be in sync.")]
    public static Type? GetSettingsType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type commandType)
    {
        if (typeof(ICommand).GetTypeInfo().IsAssignableFrom(commandType) &&
            GetGenericTypeArguments(commandType, typeof(ICommand<>), out var result))
        {
            return result[0];
        }

        return null;
    }

    private static bool GetGenericTypeArguments(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type? type,
        Type genericType,
        [NotNullWhen(true)] out Type[]? genericTypeArguments)
    {
        while (type != null)
        {
            foreach (var @interface in type.GetTypeInfo().GetInterfaces())
            {
                if (!@interface.GetTypeInfo().IsGenericType || @interface.GetGenericTypeDefinition() != genericType)
                {
                    continue;
                }

                genericTypeArguments = @interface.GenericTypeArguments;
                return true;
            }

            type = type.GetTypeInfo().BaseType;
        }

        genericTypeArguments = null;
        return false;
    }
}