using System.Xml;

var dir = Directory.GetCurrentDirectory();

// 's' prefix - segoe ui emoji, 't' prefix - twemoji
var sdoc = new XmlDocument();
sdoc.Load(Path.Join(dir, "seguiemj.ttx"));
var tdoc = new XmlDocument();
tdoc.Load(Path.Join(dir, "twemoji.flags.ttx"));



Dictionary<string, string> mapNames = [];
Dictionary<int, int> oldIdToNewId = [];
Dictionary<int, string> oldIdToOldName = [];
int nameId = 0;

// Find the last assigned id
var tglyphorder = tdoc.SelectSingleNode("/ttFont/GlyphOrder")!;
var sglyphorder = sdoc.SelectSingleNode("/ttFont/GlyphOrder")!;

static bool IsRegInd(ReadOnlySpan<char> name)
	=> name.StartsWith('u') && Convert.ToUInt32(name[1..].ToString(), 16) is 0x1f3f4 or (>= 0x1f1e6 and <= 0x1f1ff);

int idCounter = sglyphorder.ChildNodes.OfType<XmlElement>()
	.Select(n => int.Parse(n.GetAttribute("id"))).Max() + 1;

// Add flag glyphs' ids to Segoe's <GlyphOrder>
foreach (var glyphord in tglyphorder.ChildNodes.OfType<XmlElement>()) {
	string oldName = glyphord.GetAttribute("name");
	var oldId = int.Parse(glyphord.GetAttribute("id"));
	oldIdToOldName[oldId] = oldName;

	if (oldName is ".notdef" or "space" || IsRegInd(oldName)) continue;

	var newId = idCounter++;
	string newName = oldName.StartsWith('u') ? oldName : $"flagglyph{nameId++:00000}";
	oldIdToNewId[oldId] = newId;
	mapNames[oldName] = newName;

    var glyphID = sdoc.CreateElement("GlyphID");
	glyphID.SetAttribute("id", newId.ToString());
	glyphID.SetAttribute("name", newName);
	sglyphorder.AppendChild(glyphID);
}

// Copy flag glyphs to Segoe's <glyf> (rename to avoid conflicts with Segoe)
XmlNode tglyf = tdoc.SelectSingleNode("/ttFont/glyf")!;
XmlNode sglyf = sdoc.SelectSingleNode("/ttFont/glyf")!;
var sglyfCount = sglyf.ChildNodes.OfType<XmlElement>().Count();

foreach (var ttglyph in tglyf.ChildNodes.OfType<XmlElement>()) {
	string? name = ttglyph.GetAttribute("name");
    if (name is null or ".notdef" or "space" || IsRegInd(name)) continue;

	ttglyph.SetAttribute("name", mapNames[name]);
    XmlNode clone = sdoc.ImportNode(ttglyph, deep: true);
    sglyf.AppendChild(clone);

	if (clone.SelectSingleNode("component") is XmlElement comp) {
		comp.SetAttribute("glyphName", mapNames[comp.GetAttribute("glyphName")]);
	}
}



// Copy flag glyphs' <mtx> metadata to Segoe's <hmtx>
var thmtx = tdoc.SelectSingleNode("/ttFont/hmtx")!;
var shmtx = sdoc.SelectSingleNode("/ttFont/hmtx")!;
foreach (var mtx in thmtx.ChildNodes.OfType<XmlElement>()) {
    string name = mtx.GetAttribute("name");
    if (name is null or ".notdef" or "space" || IsRegInd(name)) continue;
    mtx.SetAttribute("name", mapNames[name]);
	var clone = sdoc.ImportNode(mtx, deep: true);
	shmtx.AppendChild(clone);
}



// Copy tag latin letters to Segoe's <cmap>
var scmaps = sdoc.SelectNodes("/ttFont/cmap/cmap_format_12")!;
var tcmap = tdoc.SelectSingleNode("/ttFont/cmap/cmap_format_12")!;
foreach (var smap in scmaps.OfType<XmlElement>())
{
	foreach (var elem in tcmap.ChildNodes.OfType<XmlElement>())
	{
		string name = elem.GetAttribute("name");
		if (name is ".notdef" or "space" || IsRegInd(name)) continue;

		var clone = (XmlElement)sdoc.ImportNode(elem, deep: true);
		clone.SetAttribute("name", mapNames[name]);
		smap.AppendChild(clone);
	}
}



// Copy flag glyphs' class defs to Segoe's <GDEF>
var sgdef = sdoc.SelectSingleNode("/ttFont/GDEF/GlyphClassDef")!;
foreach (var tGlyphName in mapNames.Values) {
	if (sgdef.ChildNodes.OfType<XmlElement>().Any(x => x.GetAttribute("glyph") == tGlyphName)) continue;
	var def = sdoc.CreateElement("ClassDef");
	def.SetAttribute("glyph", tGlyphName);
	def.SetAttribute("class", "2");
	sgdef.AppendChild(def);
}



// Determine duplicates or new ids for palettes
Dictionary<string, int> colorToSegoeId = [];
int dups = 0;

var spalette = sdoc.SelectSingleNode("/ttFont/CPAL/palette")!;
var tpalette = tdoc.SelectSingleNode("/ttFont/CPAL/palette")!;
foreach (var entry in spalette.ChildNodes.OfType<XmlElement>()) {
    var id = int.Parse(entry.GetAttribute("index"));
    if (!colorToSegoeId.TryAdd(entry.GetAttribute("value"), id)) dups++;
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

List<string> flagsColors = tpalette.ChildNodes.OfType<XmlElement>()!
    .Select(x => x.GetAttribute("value")).ToList();
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
var sPaletteNumEntries = (XmlElement)sdoc.SelectSingleNode("/ttFont/CPAL/numPaletteEntries")!;
int paletteCounter = int.Parse(sPaletteNumEntries.GetAttribute("value"));

Dictionary<string, int> colorToId = [];
Dictionary<int, int> mapPaletteEntries = [];
foreach (var (key, val) in colorToSegoeId) {
    colorToId[key] = val;
}

foreach (var flagColor in uniqueFlagsColors) {
	var tEntry = tpalette.ChildNodes.OfType<XmlElement>()
		.First(e => e.GetAttribute("value") == flagColor);
	var oldId = int.Parse(tEntry.GetAttribute("index"));

	    // Add to Segoe's <CPAL> with new id
		int newId = paletteCounter++;
		colorToId[flagColor] = newId;

		mapPaletteEntries[oldId] = newId;
	    var clone = (XmlElement)sdoc.ImportNode(tEntry, deep: true);
    	clone.SetAttribute("index", newId.ToString());
	    spalette.AppendChild(clone);
}

bool allDone = false;

while (!allDone) {

allDone = true;
foreach (var flagColor in dissimilarColors.Keys) {
	var tEntry = tpalette.ChildNodes.OfType<XmlElement>()
		.First(e => e.GetAttribute("value") == flagColor);
	var oldId = int.Parse(tEntry.GetAttribute("index"));

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
var newPaletteSize = spalette.ChildNodes.OfType<XmlElement>().Count();
sPaletteNumEntries.SetAttribute("value", newPaletteSize.ToString());



var sname = sdoc.SelectSingleNode("/ttFont/name")!;
{
	var desc = sdoc.CreateElement("namerecord");
	desc.SetAttribute("nameID", "10");
	desc.SetAttribute("platformID", "3");
	desc.SetAttribute("platEncID", "1");
	desc.SetAttribute("langID", "0x409");
	desc.InnerText = "https://github.com/Chasmical/flag-emojis-for-windows";
	sname.AppendChild(desc);

	var sample = sname.SelectSingleNode("namerecord[@nameID='19']")!;
	sample.InnerText = "😂😍😭💁👍💀🏠🛫🦈🦄🐼🐦‍🔥🏳️‍⚧️🇬🇧";
}



List<(string glyph, List<(int colorId, string glyph)> Layers)> tColorGlyphs = [];

var tbaseglyphrecs = tdoc.SelectSingleNode("/ttFont/COLR")!.ChildNodes
	.OfType<XmlElement>().Where(x => x.Name == "ColorGlyph");
foreach (var colorGlyph in tbaseglyphrecs) {
	tColorGlyphs.Add((
	    colorGlyph.GetAttribute("name"),
		colorGlyph.ChildNodes.OfType<XmlElement>().Select(x => {
			return (int.Parse(x.GetAttribute("colorID")), x.GetAttribute("name"));
		}).ToList()
	));
}

// Write layers from COLRv0 to COLRv1's LayerRecordArray

var sBaseGlyphRecords = sdoc.SelectSingleNode("/ttFont/COLR/BaseGlyphRecordArray")!;
var sLayerRecords = sdoc.SelectSingleNode("/ttFont/COLR/LayerRecordArray")!;
var sBaseGlyphRecordCount = sBaseGlyphRecords.ChildNodes.OfType<XmlElement>().Count();
var sLayerRecordCount = sLayerRecords.ChildNodes.OfType<XmlElement>().Count();

foreach (var (glyph, layers) in tColorGlyphs) {
	if (glyph is ".notdef" or "space" || IsRegInd(glyph)) throw new Exception();

	var baseGlyph = sdoc.CreateElement("BaseGlyphRecord");
	baseGlyph.SetAttribute("index", (sBaseGlyphRecordCount++).ToString());
	sBaseGlyphRecords.AppendChild(baseGlyph);

	var x = sdoc.CreateElement("BaseGlyph");
	x.SetAttribute("value", mapNames[glyph]);
	baseGlyph.AppendChild(x);

	var y = sdoc.CreateElement("FirstLayerIndex");
	y.SetAttribute("value", sLayerRecordCount.ToString());
	baseGlyph.AppendChild(y);

	var z = sdoc.CreateElement("NumLayers");
	z.SetAttribute("value", layers.Count.ToString());
	baseGlyph.AppendChild(z);

	foreach (var (colorId, glyphName) in layers) {
		var layerRec = sdoc.CreateElement("LayerRecord");
		layerRec.SetAttribute("index", (sLayerRecordCount++).ToString());
		sLayerRecords.AppendChild(layerRec);

		var a = sdoc.CreateElement("LayerGlyph");
		a.SetAttribute("value", mapNames[glyphName]);
		layerRec.AppendChild(a);

		var b = sdoc.CreateElement("PaletteIndex");
		b.SetAttribute("value", mapPaletteEntries[colorId].ToString());
		layerRec.AppendChild(b);
	}
}



// Copy flag glyphs' svg docs to Segoe's <SVG>

// COLRv0 file doesnt have SVG embedded
/*
var ssvg = sdoc.CreateElement("SVG");
sdoc.SelectSingleNode("/ttFont")!.AppendChild(ssvg);
var tsvg = tdoc.SelectSingleNode("/ttFont/SVG")!;
foreach (var svgdoc in tsvg.ChildNodes.OfType<XmlElement>()) {
    var oldEnd = int.Parse(svgdoc.GetAttribute("endGlyphID"));
	if (oldIdToOldName[oldEnd].StartsWith("u1F")) continue;
    svgdoc.SetAttribute("endGlyphID", oldIdToNewId[oldEnd].ToString());
    var oldStart = int.Parse(svgdoc.GetAttribute("startGlyphID"));
    svgdoc.SetAttribute("startGlyphID", oldIdToNewId[oldStart].ToString());

	var clone = sdoc.ImportNode(svgdoc, deep: true);
	ssvg.AppendChild(clone);
}
*/


// Note: Segoe's <cmap> already maps regional indicator symbols



// TODO: Copy flag glyphs' ligatures to Segoe's <GSUB>
var slookups = sdoc.SelectSingleNode("/ttFont/GSUB/LookupList")!;
var tlookups = tdoc.SelectSingleNode("/ttFont/GSUB/LookupList")!;
var lookupCounter = slookups.ChildNodes.OfType<XmlElement>().Count();
XmlElement? sCcmpFeature = (sdoc.SelectSingleNode("/ttFont/GSUB/FeatureList/FeatureRecord[FeatureTag/@value='ccmp']/Feature") as XmlElement)!;

foreach (var lookup in tlookups.ChildNodes.OfType<XmlElement>()) {
    lookup.SetAttribute("index", (lookupCounter++).ToString());
    foreach (var ligset in lookup.SelectNodes("LigatureSubst/LigatureSet")!.OfType<XmlElement>()) {
	    foreach (var lig in ligset.ChildNodes.OfType<XmlElement>()) {
		    string name = lig.GetAttribute("glyph");
			lig.SetAttribute("glyph", mapNames[name]);
		}
	}
	var lookupType = ((XmlElement)lookup.SelectSingleNode("LookupType")!).GetAttribute("value");
	if (lookupType == "4") {
		 var index = sdoc.CreateElement("LookupListIndex");
		 var indexIndex = sCcmpFeature.ChildNodes.OfType<XmlElement>().Count();
		 index.SetAttribute("index", indexIndex.ToString());
		 index.SetAttribute("value", lookup.GetAttribute("index"));
		 sCcmpFeature.AppendChild(index);
	}

	var clone = sdoc.ImportNode(lookup, deep: true);
	slookups.AppendChild(clone);
}




sdoc.Save(Path.Join(dir, "merged.ttx"));
Console.WriteLine("Done!");
