using System.Globalization;

// dotnet.exe scripts/gen_test_glyphs.cs build/glyph-list.txt
var repoDir = Path.GetDirectoryName(Directory.GetCurrentDirectory());
var glyphsList = Path.Join(repoDir, args[0]);

foreach (string path in File.ReadLines(glyphsList)) {
    if (path is null or "") continue;

    var codes = Path.GetFileNameWithoutExtension(path).Split('-');
    var codepoints = codes.Select(x => char.ConvertFromUtf32(Convert.ToInt32(x, 16)));

    Console.Write(string.Join("", codepoints));
    Console.Write('\n');
}
