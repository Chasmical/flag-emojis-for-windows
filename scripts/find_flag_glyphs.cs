using System.Globalization;
using System.Text.RegularExpressions;
#pragma warning disable

var repoDir = Path.GetDirectoryName(Directory.GetCurrentDirectory());
var glyphsFolder = Path.Join(repoDir, "build", "jdecked-twemoji", "assets", "svg");

static uint[]? ParseCodepoints(ReadOnlySpan<char> name) {
	List<uint> list = [];
	foreach (var range in name.Split('-')) {
		if (!uint.TryParse(name[range], NumberStyles.HexNumber, null, out var num)) return null;
		list.Add(num);
	}
	return list.ToArray();
}
static bool IsCountryFlag(ReadOnlySpan<uint> codepoints)
	=> codepoints is [>=0x1F1E6 and <=0x1F1FF, >=0x1F1E6 and <=0x1F1FF];
static bool IsRegionalFlag(ReadOnlySpan<uint> codepoints) {
	return codepoints is [0x1F3F4, _, _, _, _, _, 0xE007F]
		&& codepoints[1..^1].ToArray().All(x => x is >=0xE0061 and <=0xE007A);
}

foreach (var glyphPath in Directory.GetFiles(glyphsFolder)) {
	var glyphName = Path.GetFileNameWithoutExtension(glyphPath);
	if (ParseCodepoints(glyphName) is { } cps && (IsCountryFlag(cps) || IsRegionalFlag(cps))) {
		Console.Write(Path.GetRelativePath(repoDir, glyphPath).Replace('\\', '/'));
		Console.Write('\n');
	}
}

string[] newCodepoints = [
	"build/jdecked-twemoji/assets/svg/1faea.svg",
	"build/jdecked-twemoji/assets/svg/1faef.svg",
	"build/jdecked-twemoji/assets/svg/1fac8.svg",
	"build/jdecked-twemoji/assets/svg/1facd.svg",
	"build/jdecked-twemoji/assets/svg/1f6d8.svg",
	"build/jdecked-twemoji/assets/svg/1fa8a.svg",
	"build/jdecked-twemoji/assets/svg/1fa8e.svg",
];

// Add new 17.0 emojis if a flag is specified
if (args.Length > 0 && args[0] == "--with-new-17.0") {
	foreach (string path in newCodepoints) {
		Console.Write(path);
		Console.Write('\n');
	}
}
