using Json.Schema.Keywords;

namespace Graeae.Models.SchemaDraft4;

public static class Dialect
{
    public static Json.Schema.Dialect Draft4 { get; } =
        new(Json.Schema.Keywords.Draft06.AdditionalItemsKeyword.Instance,
            AdditionalPropertiesKeyword.Instance,
            AllOfKeyword.Instance,
            AnyOfKeyword.Instance,
            CommentKeyword.Instance,
            Json.Schema.Keywords.Draft06.ContainsKeyword.Instance,
            DefaultKeyword.Instance,
            Json.Schema.Keywords.Draft06.DefinitionsKeyword.Instance,
            Json.Schema.Keywords.Draft06.DependenciesKeyword.Instance,
            DescriptionKeyword.Instance,
            EnumKeyword.Instance,
            ExamplesKeyword.Instance,
            ExclusiveMaximumKeyword.Instance,
            ExclusiveMinimumKeyword.Instance,
            Json.Schema.Keywords.Draft06.FormatKeyword.Annotate,
            IdKeyword.Instance,
            Json.Schema.Keywords.Draft06.ItemsKeyword.Instance,
            MaximumKeyword.Instance,
            MaxItemsKeyword.Instance,
            MaxLengthKeyword.Instance,
            MaxPropertiesKeyword.Instance,
            MinimumKeyword.Instance,
            MinItemsKeyword.Instance,
            MinLengthKeyword.Instance,
            MinPropertiesKeyword.Instance,
            MultipleOfKeyword.Instance,
            NotKeyword.Instance,
            NullableKeyword.Instance,
            OneOfKeyword.Instance,
            PatternKeyword.Instance,
            PatternPropertiesKeyword.Instance,
            PropertiesKeyword.Instance,
            PropertyNamesKeyword.Instance,
            RefKeyword.Instance,
            RequiredKeyword.Instance,
            SchemaKeyword.Instance,
            TitleKeyword.Instance,
            TypeKeyword.Instance,
            UniqueItemsKeyword.Instance)
        {
            Id = MetaSchema.Draft4Id,
            AllowUnknownKeywords = true,
            RefIgnoresSiblingKeywords = true
        };
}