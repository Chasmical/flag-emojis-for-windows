var dir = @"D:\repos\flag-emojis-for-windows";

// 's' prefix - segoe ui emoji, 't' prefix - twemoji
var sdoc = new XmlDocument();
sdoc.Load(Path.Join(dir, "seguiemj.ttx"));
var tdoc = new XmlDocument();
tdoc.Load(Path.Join(dir, "twemoji.subset.ttx"));



Dictionary<string, string> mapNames = [];
Dictionary<int, int> oldIdToNewId = [];
Dictionary<int, string> oldIdToOldName = [];
int nameId = 0;

// Find the last assigned id
var tglyphorder = tdoc.SelectSingleNode("/ttFont/GlyphOrder")!;
var sglyphorder = sdoc.SelectSingleNode("/ttFont/GlyphOrder")!;

int idCounter = sglyphorder.ChildNodes.OfType<XmlElement>()
	.Select(n => int.Parse(n.GetAttribute("id"))).Max() + 1;

// Add flag glyphs' ids to Segoe's <GlyphOrder>
foreach (var glyphord in tglyphorder.ChildNodes.OfType<XmlElement>()) {
	string oldName = glyphord.GetAttribute("name");
	var oldId = int.Parse(glyphord.GetAttribute("id"));
	oldIdToOldName[oldId] = oldName;

	if (oldName == ".notdef") continue;
	if (oldName.StartsWith('u')) continue;

	var newId = idCounter++;
	string newName = $"flagglyph{nameId++:00000}";
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
    if (name is null or ".notdef" || name.StartsWith('u')) continue;

	ttglyph.SetAttribute("name", mapNames[name]);
    XmlNode clone = sdoc.ImportNode(ttglyph, deep: true);
    sglyf.AppendChild(clone);
}



// Copy flag glyphs' <mtx> metadata to Segoe's <hmtx>
var thmtx = tdoc.SelectSingleNode("/ttFont/hmtx")!;
var shmtx = sdoc.SelectSingleNode("/ttFont/hmtx")!;
foreach (var mtx in thmtx.ChildNodes.OfType<XmlElement>()) {
    string name = mtx.GetAttribute("name");
    if (name == ".notdef" || name.StartsWith('u')) continue;
    mtx.SetAttribute("name", mapNames[name]);
	var clone = sdoc.ImportNode(mtx, deep: true);
	shmtx.AppendChild(clone);
}



// Copy flag glyphs' class defs to Segoe's <GDEF>
var tgdef = tdoc.SelectSingleNode("/ttFont/GDEF/GlyphClassDef")!;
var sgdef = sdoc.SelectSingleNode("/ttFont/GDEF/GlyphClassDef")!;
foreach (var def in tgdef.ChildNodes.OfType<XmlElement>()) {
    string name = def.GetAttribute("glyph");
    def.SetAttribute("glyph", name.StartsWith('u') ? name : mapNames[name]);
	var clone = sdoc.ImportNode(def, deep: true);
	sgdef.AppendChild(clone);
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
print($"Segoe palette duplicates: {dups}");
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
print($"{segoeColorCount} + {flagsColors.Count} - {(tooMany ? "too many" : "ok")}");

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
	print($"Tones lost <= {similarity} ({flagsColors.Count} => {uniqueFlagsColors.Count})");
    print($"{segoeColorCount} + {uniqueFlagsColors.Count} - {(tooMany ? "too many" : "ok")}");
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

// Determine layer offset
int sLayerCount = sdoc.SelectSingleNode("/ttFont/COLR/LayerList")!
    .ChildNodes.OfType<XmlElement>().Count();



// Copy flag glyphs' layers to Segoe's <COLR><LayerList>
var tlayers = tdoc.SelectSingleNode("/ttFont/COLR/LayerList")!;
var slayers = sdoc.SelectSingleNode("/ttFont/COLR/LayerList")!;

bool RecurseLayers(IEnumerable<XmlElement> elements) {
    foreach (var elem in elements) {
	    if (elem.Name == "Glyph") {
		    string name = elem.GetAttribute("value");
			if (name.StartsWith('u')) return false;
		    elem.SetAttribute("value", mapNames[name]);
		} else if (elem.Name == "PaletteIndex") {
		    var oldIndex = int.Parse(elem.GetAttribute("value"));
			elem.SetAttribute("value", mapPaletteEntries[oldIndex].ToString());
		} else if (elem.Name == "FirstLayerIndex") {
		    // NumLayers stays the same
		    var oldIndex = int.Parse(elem.GetAttribute("value"));
			elem.SetAttribute("value", (sLayerCount + oldIndex).ToString());
		}
	    if (!RecurseLayers(elem.ChildNodes.OfType<XmlElement>())) return false;
	}
	return true;
}

foreach (var rootPaint in tlayers.ChildNodes.OfType<XmlElement>()) {
    var oldIndex = int.Parse(rootPaint.GetAttribute("index"));
    rootPaint.SetAttribute("index", (sLayerCount + oldIndex).ToString());
	if (!RecurseLayers(rootPaint.ChildNodes.OfType<XmlElement>())) continue;

	var clone = sdoc.ImportNode(rootPaint, deep: true);
	slayers.AppendChild(clone);
}



// Copy flag glyphs' clips to Segoe's <COLR><ClipList>
var tclips = tdoc.SelectSingleNode("/ttFont/COLR/ClipList")!;
var sclips = sdoc.SelectSingleNode("/ttFont/COLR/ClipList")!;
foreach (var clip in tclips.ChildNodes.OfType<XmlElement>()) {
    foreach (var gl in clip.ChildNodes.OfType<XmlElement>().ToArray()) {
	    if (gl.Name == "Glyph") {
		    string name = gl.GetAttribute("value");
			if (name.StartsWith('u')) {
			    clip.RemoveChild(gl);
			    continue;
			}
	    	gl.SetAttribute("value", name.StartsWith('u') ? name : mapNames[name]);
		}
	}
	var clone = sdoc.ImportNode(clip, deep: true);
	sclips.AppendChild(clone);
}



// Copy flag glyphs' base glyphs to Segoe's <COLR><BaseGlyphList>
var sBaseGlyphRecordCount = sdoc.SelectSingleNode("/ttFont/COLR/BaseGlyphList")!
    .ChildNodes.OfType<XmlElement>().Count();
var tbaseglyphs = tdoc.SelectSingleNode("/ttFont/COLR/BaseGlyphList")!;
var sbaseglyphs = sdoc.SelectSingleNode("/ttFont/COLR/BaseGlyphList")!;
foreach (var record in tbaseglyphs.ChildNodes.OfType<XmlElement>()) {
    var oldIndex = int.Parse(record.GetAttribute("index"));
	record.SetAttribute("index", (sBaseGlyphRecordCount + oldIndex).ToString());

	var baseglyph = (XmlElement)record.SelectSingleNode("BaseGlyph")!;
	string name = baseglyph.GetAttribute("value");
	if (name.StartsWith('u')) continue;
	baseglyph.SetAttribute("value", name.StartsWith('u') ? name : mapNames[name]);

	if (record.SelectSingleNode("Paint/FirstLayerIndex") is XmlElement elem) {
	    var oldIndex2 = int.Parse(elem.GetAttribute("value"));
		elem.SetAttribute("value", (sLayerCount + oldIndex2).ToString());
	}

	var clone = sdoc.ImportNode(record, deep: true);
	sbaseglyphs.AppendChild(clone);
}



// Copy flag glyphs' svg docs to Segoe's <SVG>
var ssvg = sdoc.CreateElement("SVG");
sdoc.SelectSingleNode("/ttFont")!.AppendChild(ssvg);
var tsvg = tdoc.SelectSingleNode("/ttFont/SVG")!;
foreach (var svgdoc in tsvg.ChildNodes.OfType<XmlElement>()) {
    var oldEnd = int.Parse(svgdoc.GetAttribute("endGlyphID"));
	if (oldIdToOldName[oldEnd].StartsWith('u')) continue;
    svgdoc.SetAttribute("endGlyphID", oldIdToNewId[oldEnd].ToString());
    var oldStart = int.Parse(svgdoc.GetAttribute("startGlyphID"));
    svgdoc.SetAttribute("startGlyphID", oldIdToNewId[oldStart].ToString());

	var clone = sdoc.ImportNode(svgdoc, deep: true);
	ssvg.AppendChild(clone);
}



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
	    // Indicate that this is a ligature lookup in the feature record
		// if (sLigaFeature is null) {
        //     var sFeatureList = sdoc.SelectSingleNode("/ttFont/GSUB/FeatureList")!;
		// 	var record = sdoc.CreateElement("FeatureRecord");
		// 	var tag = sdoc.CreateElement("FeatureTag");
		// 	record.AppendChild(tag);
		// 	var feature = sdoc.CreateElement("Feature");
		// 	record.AppendChild(feature);

		// 	record.SetAttribute("index", sFeatureList.ChildNodes.OfType<XmlElement>().Count().ToString());
		// 	sFeatureList.AppendChild(record);
		// 	tag.SetAttribute("value", "liga");
		// 	sLigaFeature = feature;
		// }
//		if (sLatnScript is null) {
//            var sScriptList = sdoc.SelectSingleNode("/ttFont/GSUB/ScriptList")!;
//			var record = sdoc.CreateElement("ScriptRecord");
//			var tag = sdoc.CreateElement("ScriptTag");
//			record.AppendChild(tag);
//			var script = sdoc.CreateElement("Script");
//			record.AppendChild(script);
//			var langSys = sdoc.CreateElement("DefaultLangSys");
//			script.AppendChild(langSys);
//
//			record.SetAttribute("index", sScriptList.ChildNodes.OfType<XmlElement>().Count().ToString());
//			sScriptList.AppendChild(record);
//			tag.SetAttribute("value", "latn");
//			sLatnScript = langSys;
//		}

		 var index = sdoc.CreateElement("LookupListIndex");
		 var indexIndex = sCcmpFeature.ChildNodes.OfType<XmlElement>().Count();
		 index.SetAttribute("index", indexIndex.ToString());
		 index.SetAttribute("value", lookup.GetAttribute("index"));
		 sCcmpFeature.AppendChild(index);

		// foreach (var script in sdoc.SelectNodes("/ttFont/GSUB/ScriptList/ScriptRecord/Script/DefaultLangSys")!.OfType<XmlElement>()) {

		// var index2 = sdoc.CreateElement("FeatureIndex");
		// var indexIndex2 = script.ChildNodes.OfType<XmlElement>().Count() - 1;
		// index2.SetAttribute("index", indexIndex2.ToString());
		// var parentsChildren = sLigaFeature.ParentNode!.ChildNodes.OfType<XmlElement>().ToList();
		// index2.SetAttribute("value", parentsChildren.IndexOf(sLigaFeature).ToString());
		// script.AppendChild(index2);

		// }

	}

	var clone = sdoc.ImportNode(lookup, deep: true);
	slookups.AppendChild(clone);
}



sdoc.Save(Path.Join(dir, "merged.ttx"));
Console.WriteLine("Done!");
