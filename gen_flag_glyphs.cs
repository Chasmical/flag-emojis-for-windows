var dir = @"D:\repos\flag-emojis-for-windows"; // <== Your working directory here

var regIndSyms = Enumerable.Range(0x1f1e6, 26).Select(x => Convert.ToString(x, 16));
var latinTagSyms = Enumerable.Range(0xe0061, 26).Select(x => Convert.ToString(x, 16));

var anyRegIndSym = $"({string.Join("|", regIndSyms)})";
var anyLatinTagSym = $"({string.Join("|", latinTagSyms)})";

var countryFlagRegex = new Regex($"^{anyRegIndSym}-{anyRegIndSym}$");
var regionFlagRegex = new Regex($"^1f3f4(-{anyLatinTagSym}){{5}}-e007f$");

var glyphsFolder = Path.Join(dir, "twemoji-color-font", "assets", "twemoji-svg");
var glyphNames = Directory.GetFiles(glyphsFolder).Select(Path.GetFileNameWithoutExtension);

var regIndSymGlyphs = regIndSyms.Select(s => "u" + s.ToUpper());
var latinTagSymGlyphs = latinTagSyms.Select(s => "u" + s.ToUpper());
var countryGlyphs = glyphNames.Where(x => countryFlagRegex.IsMatch(x!));
var regionGlyphs = glyphNames.Where(x => regionFlagRegex.IsMatch(x!));
string[] extraGlyphs = ["u1F3F4", "uE007F"];

var outputFile = Path.Join(dir, "flags-glyphs.txt");
File.WriteAllLines(
    outputFile,
    regIndSymGlyphs.Concat(latinTagSymGlyphs).Concat(extraGlyphs).Concat(countryGlyphs).Concat(regionGlyphs)!
);
