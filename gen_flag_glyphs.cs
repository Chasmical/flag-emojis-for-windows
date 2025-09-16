var dir = @"D:\repos\windows-flags"; // <== Your working directory here

var ids = Enumerable.Range(0x1f1e6, 26).Select(x => Convert.ToString(x, 16));
var flagsIdPart = $"({string.Join("|", ids)})";
var regex = new Regex($"{flagsIdPart}-{flagsIdPart}");
var letterIds = ids.Select(id => "u" + id.ToUpper());

var glyphsFolder = Path.Join(dir, "twemoji-color-font", "assets", "twemoji-svg");
var flagIds = Directory.GetFiles(glyphsFolder)
    .Select(Path.GetFileNameWithoutExtension).Where(n => regex.IsMatch(n));

var outputFile = Path.Join(dir, "flags-glyphs.txt");
File.WriteAllLines(outputFile, letterIds.Concat(flagIds)!);
