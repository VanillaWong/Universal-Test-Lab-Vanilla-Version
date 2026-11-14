using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UniversalTestLab
{
    // Ask3lad-style category strip for a stack of option cards: one row of chips on
    // top, only the cards of the selected category stay visible. Cards are matched by
    // their heading text, so the existing Card("TITLE") builders need no changes.
    internal static class OptionCategory
    {
        internal static void Apply(StackPanel host, Dictionary<string, string[]> groups)
        {
            if (host == null || groups == null || groups.Count < 2) return;

            List<Border> cards = new List<Border>();
            List<string> titles = new List<string>();
            foreach (UIElement child in host.Children)
            {
                Border border = child as Border;
                if (border == null) continue;
                StackPanel stack = border.Child as StackPanel;
                if (stack == null || stack.Children.Count == 0) continue;
                TextBlock heading = stack.Children[0] as TextBlock;
                if (heading == null) continue;
                cards.Add(border);
                titles.Add(heading.Text ?? "");
            }
            if (cards.Count < 2) return;

            string[] category = new string[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                foreach (KeyValuePair<string, string[]> group in groups)
                {
                    if (group.Value.Any(t => String.Equals(t, titles[i], StringComparison.OrdinalIgnoreCase)))
                    {
                        category[i] = group.Key;
                        break;
                    }
                }
                if (category[i] == null) category[i] = groups.Keys.First();
            }

            StackPanel strip = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            List<Button> chips = new List<Button>();
            List<KeyValuePair<string, Button>> pairs = new List<KeyValuePair<string, Button>>();

            Action<string> select = delegate(string key)
            {
                for (int i = 0; i < cards.Count; i++)
                    cards[i].Visibility = String.Equals(category[i], key, StringComparison.OrdinalIgnoreCase)
                        ? Visibility.Visible : Visibility.Collapsed;
                foreach (KeyValuePair<string, Button> pair in pairs)
                {
                    bool active = String.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase);
                    pair.Value.Background = ModernPalette.Brush(active ? "#FF1F8A5B" : "#FF16283E");
                    pair.Value.Foreground = ModernPalette.Brush(active ? "#FFFFFFFF" : ModernPalette.Muted);
                }
            };

            foreach (string key in groups.Keys)
            {
                // Skip categories that own no card on this vehicle: the same strip is
                // shared by aircraft, ground and naval panels.
                if (!category.Any(c => String.Equals(c, key, StringComparison.OrdinalIgnoreCase))) continue;

                Button chip = new Button
                {
                    Content = key,
                    Height = 26,
                    Padding = new Thickness(14, 0, 14, 0),
                    Margin = new Thickness(0, 0, 6, 0),
                    BorderThickness = new Thickness(1),
                    BorderBrush = ModernPalette.Brush(ModernPalette.Border),
                    Background = ModernPalette.Brush("#FF16283E"),
                    Foreground = ModernPalette.Brush(ModernPalette.Muted),
                    FontSize = 12,
                    Cursor = System.Windows.Input.Cursors.Hand,
                };
                string captured = key;
                chip.Click += delegate { select(captured); };
                pairs.Add(new KeyValuePair<string, Button>(key, chip));
                chips.Add(chip);
                strip.Children.Add(chip);
            }

            if (pairs.Count < 2) return;
            host.Children.Insert(0, strip);
            select(pairs[0].Key);
        }
    }
}
