using ColorHelper;
using Parsadox.Parser.Nodes;
using Parsadox.Parser.SaveGames;
using System.Text.RegularExpressions;

RGB BASE_COLOR = new(3, 144, 252);
var COA_COLORS = new[] { "blue", "blue", "blue", "blue_light", "blue_light", "blue_light", "white", "grey", "black" };

if (!args.Any())
    Console.Error.WriteLine("Üsage: Ü <save-game-file>");

foreach (var arg in args)
    Üify(arg);

void Üify(string path)
{
    Console.WriteLine($"Loading {path}...");
    var saveGame = SaveGameFactory.LoadFile(path).DisableIronman();
    var ü = saveGame.Root["gamestate"];

    var metaData = ü.GetChildOrNull("meta_data"); // autosaves can omit meta_data
    if (!string.IsNullOrEmpty(metaData?.GetChildOrNull("meta_player_name")?.ValueOrNull?.Text))
        metaData["meta_player_name"].SetValue("Ü");
    metaData?.GetChildOrNull("meta_title_name")?.SetValue("Ü");
    metaData?.GetChildOrNull("meta_house_name")?.SetValue("Ü");

    foreach (var node in ü["landed_titles"]["landed_titles"].Where(x => x.HasChildrenStorage))
    {
        if (node.GetChildOrNull("key")?.ValueOrNull?.Text == "k_u") // don't mess with the OG
            continue;
        node.GetOrCreateChild("name").SetValue("Ü");
        node.GetOrCreateChild("adj").SetValue("Ü");
        MakeNewÜColor(node.GetOrCreateChild("color"));
        node.GetOrCreateChild("renamed").SetValue(true);
    }

    foreach (var node in ü["dynasties"]["dynasty_house"].Where(x => x.HasChildrenStorage))
    {
        node.GetOrCreateChild("name").SetValue("Ü");
        node.RemoveChild("key");
        node.RemoveChild("localized_name");
    }

    foreach (var node in ü["dynasties"]["dynasties"].Where(x => x.HasChildrenStorage))
    {
        node.GetOrCreateChild("name").SetValue("Ü");
        node.RemoveChild("key");
        node.RemoveChild("localized_name");
    }

    var people = ü["living"]
        .Concat(ü["dead_unprunable"])
        .Concat(ü["characters"]["dead_prunable"])
        .Where(x => x.HasChildrenStorage);
    foreach (var node in people)
    {
        node.GetOrCreateChild("first_name").SetValue("Ü");
        node.RemoveChild("regnal_name");
        foreach (var host in node.GetChildOrNull("landed_data")?.GetChildOrNull("event_troops") ?? Enumerable.Empty<INode>())
            host.GetOrCreateChild("name").SetValue("Ü");
    }

    ü["characters"]["sexuality_chances"]["he"].Value.AsDecimal = 15;
    ü["characters"]["sexuality_chances"]["ho"].Value.AsDecimal = 15;
    ü["characters"]["sexuality_chances"]["bi"].Value.AsDecimal = 69; // nice
    ü["characters"]["sexuality_chances"]["as"].Value.AsDecimal = 1;

    foreach (var node in ü["armies"]["armies"].Where(x => x.HasChildrenStorage))
        node.GetOrCreateChild("name").GetOrCreateChild("name").SetValue("Ü");

    foreach (var node in ü["religion"]["faiths"].Where(x => x.HasChildrenStorage))
    {
        node.GetOrCreateChild("name").SetValue("Ü");
        node.GetOrCreateChild("adjective").SetValue("Ü");
        node.GetOrCreateChild("adherent").SetValue("Ü");
        node.GetOrCreateChild("adherent_plural").SetValue("Ü");
        node.GetOrCreateChild("desc").SetValue("Ü");
        MakeNewÜColor(node.GetOrCreateChild("color"));
    }

    foreach (var node in ü["culture_manager"]["cultures"].Where(x => x.HasChildrenStorage))
    {
        node.GetOrCreateChild("name").SetValue("Ü");
        MakeNewÜColor(node.GetOrCreateChild("color"));
    }

    foreach (var node in ü["artifacts"]["artifacts"].Where(x => x.HasChildrenStorage))
    {
        node.GetOrCreateChild("name").SetValue("Ü");
        node.GetOrCreateChild("description").SetValue("Ü");
    }

    ChangeAllColorNodes(ü);

    if (path.EndsWith(".ck3"))
        path = path[..^4];
    path += "_üified.ck3";
    saveGame.WriteFile(path);
    Console.WriteLine($"Created {path}");
}

void ChangeAllColorNodes(INode root)
{
    // Hit all coats of arms
    foreach (var node in root)
    {
        if (node.HasValue && Regex.IsMatch(node.Content.Text, @"^color\d$"))
        {
            if (node.HasChildrenStorage)
                MakeNewÜColor(node);
            else
                node.SetValue(COA_COLORS[Random.Shared.Next(0, COA_COLORS.Length)]);
        }
        else if (node.HasChildrenStorage)
            ChangeAllColorNodes(node); // recurse
    }
}

void MakeNewÜColor(INode node)
{
    // Mess with hue/saturation/lightness to get a color variant
    HSL hsl = ColorConverter.RgbToHsl(BASE_COLOR);
    hsl.H += Random.Shared.Next(-8, 8);
    hsl.S = Cap(hsl.S + Random.Shared.Next(-38, 2));
    hsl.L = Cap(hsl.L + Random.Shared.Next(-12, 12));
    var rgb = ColorConverter.HslToRgb(hsl);

    node.SetColorRgb(rgb.R, rgb.G, rgb.B);
}

byte Cap(int value) => (byte)Math.Min(Math.Max(0, value), byte.MaxValue);
