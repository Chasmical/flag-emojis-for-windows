using System.Xml.Linq;

var dir = Path.GetDirectoryName(Directory.GetCurrentDirectory());

if (args.Length != 4) {
    Console.WriteLine("[ERROR] Expected 4 arguments: seguiemj flags.color flags.bw merged");
    Console.WriteLine($"Got {args.Length} arguments: {string.Join(" ", args.Select(x => $"\"{x}\""))}");
    Environment.Exit(1);
}

// 's' prefix - segoe ui emoji, 't' prefix - twemoji
var sdoc = XDocument.Load(Path.Join(dir, args[0])); // seguiemj.ttx
var tdoc = XDocument.Load(Path.Join(dir, args[1])); // twemoji.flags.color.ttx
var bwdoc = XDocument.Load(Path.Join(dir, args[2])); // twemoji.flags.bw.ttx
var saveAs = Path.Join(dir, args[3]); // merged.ttx



Dictionary<string, string> mapNames = [];
int nameId = 0;

// Find the last assigned id
var tglyphorder = tdoc.Root!.Element("GlyphOrder")!;
var sglyphorder = sdoc.Root!.Element("GlyphOrder")!;

static bool IsTag(ReadOnlySpan<char> name)
    => name.StartsWith('u') && Convert.ToUInt32(name[1..].ToString(), 16)
    is (>= 0xE0061 and <= 0xE007A);
static bool IsRegInd(ReadOnlySpan<char> name)
    => name.StartsWith('u') && Convert.ToUInt32(name[1..].ToString(), 16)
    is 0x1f3f4 or (>= 0x1f1e6 and <= 0x1f1ff);

int idCounter = sglyphorder.Elements()
    .Select(n => (int)n.Attribute("id")!).Max() + 1;

// Add flag glyphs' ids to Segoe's <GlyphOrder>
foreach (var glyphord in tglyphorder.Elements()) {
    string oldName = glyphord.Attribute("name")!.Value;
    var oldId = int.Parse(glyphord.Attribute("id")!.Value);

    // Don't redefine existing character ids
    if (oldName is ".notdef" or "space" || IsRegInd(oldName)) continue;

    var newId = idCounter++;
    string newName = oldName.StartsWith('u') ? oldName : $"flagglyph{nameId++:00000}";
    mapNames[oldName] = newName;

    sglyphorder.Add(new XElement("GlyphID", [
        new XAttribute("id", newId),
        new XAttribute("name", newName),
    ]));
}

// Copy flag glyphs to Segoe's <glyf> (rename to avoid conflicts with Segoe)
var tglyf = tdoc.Root!.Element("glyf")!;
var sglyf = sdoc.Root!.Element("glyf")!;
var sglyfCount = sglyf.Elements().Count();

foreach (var ttglyph in tglyf.Elements()) {
    string? name = mapNames.TryGetValue(ttglyph.Attribute("name")!.Value, out var xx) ? xx : null;
    // Don't redefine existing character glyphs
    if (name is null or ".notdef" or "space" ||
        sglyf.Elements().Any(x => x.Attribute("name")!.Value == name)) continue;

    var clone = new XElement(ttglyph);
    clone.SetAttributeValue("name", name);
    sglyf.Add(clone);

    if (clone.Element("component") is { } comp) {
        comp.SetAttributeValue("glyphName", mapNames[comp.Attribute("glyphName")!.Value]);
    }
}



// Copy flag glyphs' <mtx> metadata to Segoe's <hmtx>
var thmtx = tdoc.Root!.Element("hmtx")!;
var shmtx = sdoc.Root!.Element("hmtx")!;
foreach (var mtx in thmtx.Elements()) {
    string? name = mapNames.TryGetValue(mtx.Attribute("name")!.Value, out var xx) ? xx : null;
    // Don't redefine existing character metrics
    if (name is null or ".notdef" or "space" ||
        shmtx.Elements().Any(x => x.Attribute("name")!.Value == name)) continue;

    var clone = new XElement(mtx);
    clone.SetAttributeValue("name", name);
    shmtx.Add(clone);
}



// Copy tag latin letters to Segoe's <cmap>
var scmaps = sdoc.Root!.Element("cmap")!.Elements("cmap_format_12")!;
var tcmap = tdoc.Root!.Element("cmap")!.Element("cmap_format_12")!;
foreach (var smap in scmaps)
{
    foreach (var elem in tcmap.Elements())
    {
        string name = elem.Attribute("name")!.Value;
        string code = elem.Attribute("code")!.Value;
        // Don't redefine existing character mappings
        if (name is ".notdef" or "space" || smap.Elements().Any(x => x.Attribute("code")!.Value == code)) continue;

        var clone = new XElement(elem);
        clone.SetAttributeValue("name", mapNames[name]);
        smap.Add(clone);
    }
}



// Copy flag glyphs' class defs to Segoe's <GDEF>
var sgdef = sdoc.Root!.Element("GDEF")!.Element("GlyphClassDef")!;
foreach (var tGlyphName in mapNames.Values) {
    if (sgdef.Elements().Any(x => x.Attribute("glyph")!.Value == tGlyphName)) continue;
    var def = new XElement("ClassDef", [
        new XAttribute("glyph", tGlyphName),
        new XAttribute("class", "2"),
    ]);
    sgdef.Add(def);
}



// Determine duplicates or new ids for palettes
Dictionary<string, int> colorToSegoeId = [];
int dups = 0;

var spalette = sdoc.Root!.Element("CPAL")!.Element("palette")!;
var tpalette = tdoc.Root!.Element("CPAL")!.Element("palette")!;
foreach (var entry in spalette.Elements()) {
    var id = (int)entry.Attribute("index")!;
    if (!colorToSegoeId.TryAdd(entry.Attribute("value")!.Value, id)) dups++;
}
Console.WriteLine($"Segoe palette duplicates: {dups}");
var segoeColorCount = colorToSegoeId.Count;



// Find best de-duplication similarity value
static bool AreColorsSimilar(string a, string b, int sim) {
    if (a.Length < 9 || b.Length < 9) return false;
    var diff = (
        Math.Abs(ParseHex(a[1], a[2]) - ParseHex(b[1], b[2])) +
        Math.Abs(ParseHex(a[3], a[4]) - ParseHex(b[3], b[4])) +
        Math.Abs(ParseHex(a[5], a[6]) - ParseHex(b[5], b[6])) +
        Math.Abs(ParseHex(a[7], a[8]) - ParseHex(b[7], b[8]))
    );
    return diff <= sim;
}
static int ParseHex(char a, char b) {
    int x = a - (a >= 'A' ? 'A' - 10 : '0');
    int y = b - (b >= 'A' ? 'A' - 10 : '0');
    return (x << 4) + y;
}

List<string> flagsColors = tpalette.Elements()!
    .Select(x => x.Attribute("value")!.Value).ToList();
List<string> uniqueFlagsColors = flagsColors;
Dictionary<string, string> dissimilarColors = [];

bool tooMany = segoeColorCount + flagsColors.Count >= ushort.MaxValue;
Console.WriteLine($"{segoeColorCount} + {flagsColors.Count} - {(tooMany ? "too many" : "ok")}");

int similarity = 1;
while (tooMany) {
    dissimilarColors.Clear();

    uniqueFlagsColors = flagsColors.FindAll(c => {
        foreach (string segoeColor in colorToSegoeId.Keys) {
            if (ReferenceEquals(c, segoeColor)) continue;
            if (dissimilarColors.ContainsKey(segoeColor)) continue;
            if (AreColorsSimilar(c, segoeColor, similarity)) {
                dissimilarColors.Add(c, segoeColor);
                return false;
            }
        }
        foreach (string otherColor in flagsColors) {
            if (ReferenceEquals(c, otherColor)) continue;
            if (dissimilarColors.ContainsKey(otherColor)) continue;
            if (AreColorsSimilar(c, otherColor, similarity)) {
                dissimilarColors.Add(c, otherColor);
                return false;
            }
        }
        return true;
    });

    tooMany = segoeColorCount + uniqueFlagsColors.Count >= ushort.MaxValue;
    Console.WriteLine($"Tones lost <= {similarity} ({flagsColors.Count} => {uniqueFlagsColors.Count})");
    Console.WriteLine($"{segoeColorCount} + {uniqueFlagsColors.Count} - {(tooMany ? "too many" : "ok")}");
    similarity++;
}



// De-duplicate palette color ids
var sPaletteNumEntries = sdoc.Root!.Element("CPAL")!.Element("numPaletteEntries")!;
int paletteCounter = (int)sPaletteNumEntries.Attribute("value")!;

Dictionary<string, int> colorToId = [];
Dictionary<int, int> mapPaletteEntries = [];
foreach (var (key, val) in colorToSegoeId) {
    colorToId[key] = val;
}

foreach (var flagColor in uniqueFlagsColors) {
    var tEntry = tpalette.Elements()
        .First(e => e.Attribute("value")!.Value == flagColor);
    var oldId = (int)tEntry.Attribute("index")!;

    // Add to Segoe's <CPAL> with new id
    int newId = paletteCounter++;
    colorToId[flagColor] = newId;

    mapPaletteEntries[oldId] = newId;
    var clone = new XElement(tEntry);
    clone.SetAttributeValue("index", newId);
    spalette.Add(clone);
}

bool allDone = false;

while (!allDone) {

allDone = true;
foreach (var flagColor in dissimilarColors.Keys) {
    var tEntry = tpalette.Elements()
        .First(e => e.Attribute("value")!.Value == flagColor);
    var oldId = (int)tEntry.Attribute("index")!;

    if (dissimilarColors.TryGetValue(flagColor, out var simColor)) {
        // if duplicate, just add the existing id
        if (colorToId.TryGetValue(simColor, out var id)) {
            colorToId[flagColor] = id;
            mapPaletteEntries[oldId] = id;
        } else {
            allDone = false;
        }
    }
}

}

// Write new palette num entries
var newPaletteSize = spalette.Elements().Count();
sPaletteNumEntries.SetAttributeValue("value", newPaletteSize);



var sname = sdoc.Root!.Element("name")!;
{
    sname.Add(new XElement("namerecord", [
        new XAttribute("nameID", "10"),
        new XAttribute("platformID", "3"),
        new XAttribute("platEncID", "1"),
        new XAttribute("langID", "0x409"),
        "https://github.com/Chasmical/flag-emojis-for-windows",
    ]));

    var sample = sname.Elements("namerecord").First(x => x.Attribute("nameID")!.Value == "19")!;
    sample.Value = "😂😍😭💁👍💀🏠🛫🦈🦄🐼🐦‍🔥🏳️‍⚧️🇬🇧";
}



List<(string glyph, List<(int colorId, string glyph)> Layers)> tColorGlyphs = [];

var tbaseglyphrecs = tdoc.Root!.Element("COLR")!.Elements("ColorGlyph");
foreach (var colorGlyph in tbaseglyphrecs) {
    tColorGlyphs.Add((
        colorGlyph.Attribute("name")!.Value,
        colorGlyph.Elements().Select(x => {
            return ((int)x.Attribute("colorID")!, x.Attribute("name")!.Value);
        }).ToList()
    ));
}

// Write layers from COLRv0 to COLRv1's LayerRecordArray

var sBaseGlyphRecords = sdoc.Root!.Element("COLR")!.Element("BaseGlyphRecordArray")!;
var sLayerRecords = sdoc.Root!.Element("COLR")!.Element("LayerRecordArray")!;
var sBaseGlyphRecordCount = sBaseGlyphRecords.Elements().Count();
var sLayerRecordCount = sLayerRecords.Elements().Count();

foreach (var (glyph, layers) in tColorGlyphs) {
    if (glyph is ".notdef" or "space" || IsTag(glyph) || IsRegInd(glyph)) throw new Exception();

    sBaseGlyphRecords.Add(new XElement("BaseGlyphRecord", [
        new XAttribute("index", sBaseGlyphRecordCount++),
        new XElement("BaseGlyph", new XAttribute("value", mapNames[glyph])),
        new XElement("FirstLayerIndex", new XAttribute("value", sLayerRecordCount)),
        new XElement("NumLayers", new XAttribute("value", layers.Count)),
    ]));

    foreach (var (colorId, glyphName) in layers) {
        sLayerRecords.Add(new XElement("LayerRecord", [
            new XAttribute("index", sLayerRecordCount++),
            new XElement("LayerGlyph", new XAttribute("value", mapNames[glyphName])),
            new XElement("PaletteIndex", new XAttribute("value", mapPaletteEntries[colorId])),
        ]));
    }
}



// Note: Segoe's <cmap> already maps regional indicator symbols



// Copy flag glyphs' ligatures to Segoe's <GSUB>
var slookups = sdoc.Root!.Element("GSUB")!.Element("LookupList")!;
var tlookups = tdoc.Root!.Element("GSUB")!.Element("LookupList")!;
var lookupCounter = slookups.Elements().Count();

var sCcmpFeature = sdoc.Root!.Element("GSUB")!.Element("FeatureList")!.Elements("FeatureRecord")
    .First(x => x.Element("FeatureTag")?.Attribute("value")!.Value == "ccmp")!.Element("Feature")!;

foreach (var lookup in tlookups.Elements()) {
    lookup.Attribute("index")!.SetValue(lookupCounter++);
    foreach (var ligset in lookup.Elements("LigatureSubst").SelectMany(x => x.Elements("LigatureSet"))) {
        foreach (var lig in ligset.Elements()) {
            string name = lig.Attribute("glyph")!.Value;
            lig.SetAttributeValue("glyph", mapNames[name]);
        }
    }
    var lookupType = lookup.Element("LookupType")!.Attribute("value")!.Value;
    if (lookupType == "4") {
         sCcmpFeature.Add(new XElement("LookupListIndex", [
             new XAttribute("index", sCcmpFeature.Elements().Count()),
             new XAttribute("value", lookup.Attribute("index")!.Value),
         ]));
    }

    slookups.Add(new XElement(lookup));
}



sdoc.Save(saveAs);
Console.WriteLine("Done!");
