![](./img/repo-banner.png)

<h1 align="center">Add Country Flag Emojis to Windows 8-11</h1>

<div align="center">
  <p>
    <a href="https://github.com/Chasmical/flag-emojis-for-windows/releases">
      <img src="https://img.shields.io/github/downloads/Chasmical/flag-emojis-for-windows/total?label=Downloads&style=flat" alt="Downloads"/>
    </a>
    <a href="https://github.com/Chasmical/flag-emojis-for-windows/stargazers">
      <img src="https://img.shields.io/github/stars/Chasmical/flag-emojis-for-windows?color=green&label=Stars&style=flat" alt="Stars"/>
    </a>
    <a href="https://github.com/Chasmical/flag-emojis-for-windows/subscription">
      <img src="https://img.shields.io/github/watchers/Chasmical/flag-emojis-for-windows?color=green&label=Watchers&style=flat" alt="Watchers"/>
    </a>
    <a href="https://chsm.dev/blog/2026/08/27/flag-emojis-for-windows-8-11-reworked">
      <img src="https://img.shields.io/badge/Flag%20Emojis%20for%20Windows%208--11%20Reworked-link?style=flat&label=my%20blog" alt="Read my blog"/>
    </a>
  </p>
</div>

<h2 align="center">This font adds <b>country flag emojis</b> to Windows, while keeping all <b>Win11's original emojis</b>! 🇬🇧🧑‍💻🇯🇵😎🇰🇷💀🇨🇳🤖🇫🇷✨🇪🇸🐛🇮🇹</h2>



Unlike literally any other platform or OS, Windows never had flag emojis, and that always irked me a little bit. Having to guess what flag someone else is trying to send by just two letters isn't a great user experience, you know. And I always just kinda sighed at it, helplessly.

But not today... Today I woke up, and the absence of flag emojis in Windows has triggered me like never before, and so I've spent over 14 hours hyperfocused on this task of bringing flag emojis to Windows (without replacing *all* the emojis, that is, like some other projects did). *Upd: I have since spent a hundred more hours or so on this project.*

And now you too can say *"No!"* to Windows, *"I want the flag emojis that everyone else has!"*, [download and install this font](#installation), restart your PC, and finally get to enjoy the full emoji experience on Windows!

&nbsp;

This font is based on Segoe UI Emoji v1.70 ([3D Fluent 17.0](https://emojipedia.org/microsoft-3D-fluent/windows-11-26h2-august-2026-update); Win11 26H2; 2026-08-27) and contains 262 flags from Twemoji v17.0.3 ([`jdecked/twemoji`](https://github.com/jdecked/twemoji)@[`b6b55fe`](https://github.com/jdecked/twemoji/commit/b6b55fef1e8636b540a6d016a4729ca8cdf2e60b) 2026-06-01). You can build it yourself, if you'd like (see the "How to build it yourself" section in the end).



## Table of contents

1. [Installation](#installation)
2. [Screenshots](#screenshots)
3. [Similar projects & comparison](#similar-projects--comparison)
4. [How to build it yourself](#how-to-build-it-yourself)

Also, you can read my two blog posts on this project:

- 23-Sep-2025, v1: [Bringing flag emojis to Windows 11](https://chsm.dev/blog/2025/09/23/bringing-flag-emojis-to-windows-11)
- 27-Aug-2026, v2: [Flag Emojis for Windows 8-11 Reworked](https://chsm.dev/blog/2026/08/27/flag-emojis-for-windows-8-11-reworked)



## Installation

### [Download the font](https://github.com/Chasmical/flag-emojis-for-windows/releases/latest/download/Segoe.UI.Emoji.with.Twemoji.Flags.ttf) and install it ***for all users***! Restart your PC to apply changes.

![](./img/install-for-all-users.png)

**"Install for all users" (recommended)** will attempt to render country flags in the system and many other apps too.

Regular **"Install"** will only affect a few certain apps: Chromium-based browsers (Chrome, Opera, Vivaldi, etc), and Electron-based apps (Discord, VS Code, etc), so if that's enough for you, you can do this type of install.

### Or, alternatively, run this command in PowerShell (Admin):

```sh
irm https://chsm.dev/get-flag-emojis | iex
```

The installer script compares your font's SHA256 checksum with the latest one before downloading, and it also uses less traffic because it downloads the font in a ZIP archive. It also logs errors, which is nice.

<img src="./img/installing-with-the-script.png" width="582" height="432" />



### If both installation methods fail, check the [Troubleshooting Guide](./TROUBLESHOOTING.md)!



### Uninstalling for all users

Open Terminal (PowerShell) and run this command to recover the original Segoe UI Emoji file:

```sh
Copy-Item "C:\Windows\Fonts\seguiemj.ttf" "~\Desktop"
```

This will copy the original font file to your desktop. Install it with "Install for all users",
and then restart your PC.

### Uninstalling for this user

Simply go to "Settings > Personalization > Fonts", find and click on "Segoe UI Emoji", select the
font file with the longest path and press "Uninstall". You should still have the original font file
installed system-wide in there.



## Screenshots

### It works perfectly in Chromium-based browsers (Chrome, Opera, Vivaldi, etc):

<img src="./img/sc-vivaldi.png" width="453" />

### As well as all Electron-based apps (Discord, VS Code, etc):

<img src="./img/sc-vscode.png" width="529" />

### And most non-system apps too (e.g. Notepad++):

<img src="./img/sc-notepadplusplus.png" width="480" />

### It also works in UWP apps (most of modern system UI):

In a Microsoft Word document:

<img src="./img/sc-word.png" width="550" />

Fonts preview in "Settings > Personalizations > Fonts":

<img src="./img/sc-system-settings.png" width="466" />

In the system Start menu:

<img src="./img/sc-system-start.png" width="625" />

In the system task bar:

<img src="./img/sc-system-taskbar.png" width="416" />

### System Limitations

Country flags are uncolored in the Explorer, but so are the Fluent 3D emojis. So it's not something that **any** font can fix, — it's a limitation of the system itself. Maybe in future versions Windows will be able to render emojis consistently everywhere, but at the moment, it's the best that can be done.

<img src="./img/sc-explorer-1.png" width="240" /><img src="./img/sc-explorer-2.png" width="240" />



## Similar projects & comparison

- [`perguto/Country-Flag-Emojis-for-Windows`](https://github.com/perguto/Country-Flag-Emojis-for-Windows) replaces Segoe UI Emoji with Google Noto emojis.

- [`quarrel/broken-flag-emojis-win11-twemoji`](https://github.com/quarrel/broken-flag-emojis-win11-twemoji) replaces Segoe UI Emoji with Twemoji emojis.

Here are the emojis that you get in all projects, for comparison:

![](./img/comparison-emojis.png)

I personally prefer the original Fluent 3D set. A touch of 3D shading looks really nice and it brings some life to the emojis. Fluent 3D's people emojis have actual eyes, while others' have creepy dot eyes and blank stares. Also, as someone with entomophobia, Fluent 3D's bug emoji is the easiest to look at, and as a developer I have to look at it pretty often. And look at Fluent 3D's animals! So cute!

And here's a comparison of flags as well:

![](./img/comparison-flags.png)

I decided to use Twitter's flag emojis, since Noto's wavy ones just look weird — straight lines become curves, circles become ovals, there's a weird gray glow around the flags, and they also don't downscale well.



## How to build it yourself

The project's build process is pretty complicated and takes a long time (full run = 5 mins on 20-core CPU), so I put it all in a Makefile, to cache and reuse intermediate results. If you're on Windows, you'll need [WSL](https://learn.microsoft.com/en-us/windows/wsl/install) to run Make and all the Unix commands.

### Prerequisites

- [git](https://git-scm.com/install/windows) for cloning [`jdecked/twemoji`](https://github.com/jdecked/twemoji)'s SVG assets,
- [.NET SDK 10+](https://dotnet.microsoft.com/en-us/download) for running scripts and merging fonts,
- [Python/pip](https://www.python.org/downloads/) for [`nanoemoji`](https://github.com/googlefonts/nanoemoji) and [`fonttools`](https://github.com/fonttools/fonttools), [de]compiling fonts,
- `pip install nanoemoji fonttools[lxml]`
- [Node.js/npm](https://nodejs.org/en/download/current) for [`svgo`](https://github.com/svg/svgo), optimizing SVG assets,
- `npm install -g svgo`
- [Inkscape](https://inkscape.org/release/) for rasterizing SVGs into PNGs,
- [ImageMagick](https://imagemagick.org/download/) for converting PNGs into BMPs,
- [Potrace](https://potrace.sourceforge.net/#downloading) for tracing BMPs into single-color SVGs,
- [HarfBuzz ≥14.2.0 (Apr 2026)](https://github.com/harfbuzz/harfbuzz/releases/latest) for font render tests,
- [7z](https://www.7-zip.org) for packaging the font into a zip file,
- And don't forget to update your `PATH`.

> [!NOTE]
> If you're on a Unix OS natively, you can run this to install all dependencies:
> ```sh
> sudo apt update
> sudo apt install make git dotnet python3 nodejs npm inkscape imagemagick potrace p7zip-full
> sudo pip install nanoemoji fonttools[lxml]
> sudo npm install -g svgo
> ```
> HarfBuzz needs to be built from sources. You'll also need to get `build/seguiemj.ttf` from somewhere.

### Commands

After installing everything, you can run these Make commands (if you're on Windows, run them in WSL):

- `make build` (default) builds the font (`build/merged.ttf`).

- `make package` builds and then copies to `Segoe.UI.Emoji.with.Twemoji.Flags.{ttf,zip}`.

- `make test-vars` prints the commands for the tools that will be used.

- `make test` renders PNGs with all of the flags (`build/tests/`; configurable `make test FLAGS_PER_LINE=8`).

- `make clean` cleans the build cache (simply `rm -rf build`).

- `make rebuild` is a combo of `clean` followed by `build`.

### Notes

The C# merge script I wrote is a bit janky and disorganized, since I've had to try so many different things to finally get it to work... And it probably won't work with any other fonts, despite all my attempts to keep it as generic as possible.

If you want to add emojis to Segoe UI Emoji from some other font, here's a list of resources that I found useful:

- [fontTools ttx](https://fonttools.readthedocs.io/en/latest/ttx.html) can decompile TTF into a readable and editable XML, and recompiles it back losslessly.
- [OpenType's spec on Microsoft Learn](https://learn.microsoft.com/en-us/typography/opentype/spec/) explains the overall structure of a TTF file and its tables, and what different type ids mean, and etc.
- [GSUB docs on FontForge](https://fontforge.org/docs/techref/gposgsub.html) clarifies some stuff about substitution lookups.
- [HarfBuzz](https://harfbuzz.github.io/utilities.html#utilities-command-line-hbview) brought the project to the finish line! It not only renders font characters into the terminal, but also shows the entire textshaping process (run with option `-V`). I was stuck for a while on script and feature switches, not realizing that they disable rendering the ligatures in some places.

You can also read my two blog posts explaining some of the details:

- 23-Sep-2025, v1: [Bringing flag emojis to Windows 11](https://chsm.dev/blog/2025/09/23/bringing-flag-emojis-to-windows-11)
- 27-Aug-2026, v2: [Flag Emojis for Windows 8-11 Reworked](https://chsm.dev/blog/2026/08/27/flag-emojis-for-windows-8-11-reworked)


