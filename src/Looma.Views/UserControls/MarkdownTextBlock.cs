// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text.

using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Looma.Views.UserControls;

public partial class MarkdownTextBlock : StackPanel
{
    public static readonly StyledProperty<string> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownTextBlock, string>(nameof(Markdown), string.Empty);

    public string Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    static MarkdownTextBlock()
    {
        MarkdownProperty.Changed.AddClassHandler<MarkdownTextBlock>((control, _) => control.RenderMarkdown());
    }

    public MarkdownTextBlock()
    {
        Spacing = 8;
        RenderMarkdown();
    }

    private void RenderMarkdown()
    {
        Children.Clear();

        if (string.IsNullOrWhiteSpace(Markdown))
        {
            Children.Add(CreateParagraph("Aucune note de version disponible."));
            return;
        }

        var lines = Markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var inCodeBlock = false;
        var codeLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                if (inCodeBlock)
                {
                    Children.Add(CreateCodeBlock(string.Join(Environment.NewLine, codeLines)));
                    codeLines.Clear();
                    inCodeBlock = false;
                }
                else
                {
                    inCodeBlock = true;
                }

                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(line);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("### ", StringComparison.Ordinal))
            {
                Children.Add(CreateHeading(trimmed[4..], 16));
                continue;
            }

            if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                Children.Add(CreateHeading(trimmed[3..], 18));
                continue;
            }

            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                Children.Add(CreateHeading(trimmed[2..], 20));
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                Children.Add(CreateParagraph($"• {NormalizeInlineMarkdown(trimmed[2..])}", 13));
                continue;
            }

            Children.Add(CreateParagraph(NormalizeInlineMarkdown(trimmed)));
        }

        if (codeLines.Count > 0)
        {
            Children.Add(CreateCodeBlock(string.Join(Environment.NewLine, codeLines)));
        }
    }

    private static TextBlock CreateHeading(string text, double fontSize)
    {
        var textBlock = new TextBlock
        {
            Text = NormalizeInlineMarkdown(text),
            FontSize = fontSize,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        };

        textBlock.Bind(TextBlock.ForegroundProperty, textBlock.GetResourceObservable("SectionTitleForegroundBrush"));
        return textBlock;
    }

    private static TextBlock CreateParagraph(string text, double fontSize = 14)
    {
        var textBlock = new TextBlock
        {
            Text = NormalizeInlineMarkdown(text),
            FontSize = fontSize,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 20
        };

        textBlock.Bind(TextBlock.ForegroundProperty, textBlock.GetResourceObservable("TextPrimaryBrush"));
        return textBlock;
    }

    private static Border CreateCodeBlock(string text)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontFamily = FontFamily.Parse("Consolas,Menlo,Monospace"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        textBlock.Bind(TextBlock.ForegroundProperty, textBlock.GetResourceObservable("TextPrimaryBrush"));

        return new Border
        {
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(6),
            Background = new SolidColorBrush(Color.FromArgb(18, 0, 0, 0)),
            Child = textBlock
        };
    }

    private static string NormalizeInlineMarkdown(string text)
    {
        var normalized = Regex.Replace(text, @"\[([^\]]+)\]\(([^)]+)\)", "$1 ($2)");
        normalized = Regex.Replace(normalized, @"\*\*([^*]+)\*\*", "$1");
        normalized = Regex.Replace(normalized, @"__([^_]+)__", "$1");
        normalized = Regex.Replace(normalized, @"`([^`]+)`", "$1");
        return normalized.Trim();
    }
}
