namespace J18nCore.Attributes;


[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
internal class AliasesAttribute : Attribute
{
    public string[] Aliases { get; set; }

    public AliasesAttribute(params string[] aliases) => Aliases = aliases;
}
