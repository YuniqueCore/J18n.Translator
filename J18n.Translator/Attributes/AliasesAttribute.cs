using System.Reflection;

namespace J18n.Translator.Attributes;


[AttributeUsage(AttributeTargets.All , Inherited = false , AllowMultiple = false)]
internal class AliasesAttribute : Attribute
{
    public string[] Aliases { get; set; }

    public AliasesAttribute(params string[] aliases) => Aliases = aliases;
}

public static class EnumExtensions
{
    public static IEnumerable<IEnumerable<string>> GetEnumAliases<TEnum>( )
    {
        Type enumType = typeof(TEnum);

        foreach(FieldInfo field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            AliasesAttribute attribute = field.GetCustomAttribute<AliasesAttribute>();
            if(attribute != null)
            {
                //yield return attribute.Aliases.FirstOrDefault() ?? field.Name;
                yield return new string[] { field.Name }.Union(attribute.Aliases);
            }
            else
            {
                //yield return field.Name;
                yield return new string[1] { field.Name };
            }
        }
    }
}