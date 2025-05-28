using System;
using System.Collections.Generic;
using MacroMate.Extensions.Dalamud.Macros;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MacroMate.Subscription;

public class SubscriptionManifestYAML {
    /// <summary>
    /// The name of this subscription, used as the root group name
    /// </summary>
    public string Name { get; set; } = null!;

    public List<MacroYAML> Macros { get; set; } = null!;

    public static SubscriptionManifestYAML From(string input) {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<SubscriptionManifestYAML>(input);
    }
}

public class MacroYAML {
    public string? Name { get; set; }
    public string? Group { get; set; }
    public int? IconId { get; set; }
    public string? Lines { get; set; }
    public string? Notes { get; set; }

    /// If present, all other fields are optional and are treated
    /// as overrides for the Markdown-parsed content
    public string? MarkdownUrl { get; set; } = null!;

    /// If present, Lines should not be set and instead
    public int? MarkdownMacroCodeBlockIndex { get; set; }

    /// If present, Lines is optional and the raw text returned from
    /// this URL will be used as the macro body
    public string? RawUrl { get; set; } = null!;
}
