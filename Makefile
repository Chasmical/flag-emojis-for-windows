# I was learning Make as I was writing this, so I'll include a quick breakdown of everything
# happening in this file, so that those who don't know Make can understand what's happening.
#
# A Makefile consists of recipes like `target: dep1 dep2`, that tell Make how to make that target.
# Make runs the indented commands below, usually each command in separate shell instance, but I've
# specified the `.ONESHELL:` thing, so all commands in a recipe share a shell instance now, which
# allows changing current directory with `cd` and is over all a bit faster.
#
# Make figures out the order in which to run each recipe automatically, and it uses the files' last
# modified date to figure out when the recipes need to be re-run (e.g. dep2 is newer than target).
# If a recipe doesn't actually produce its target, then it will always re-run and trigger all its
# dependants (those are called phony targets; and as a convention they're listed in `.PHONY:`).
# This can be useful for one-time triggers, like `build`, `clean`, `rebuild`, etc.
#
# Prefixing a command line with `@` hides the command from the output (the command itself, e.g.
# `rm -rf build`; its output is still displayed), and prefixing it with `-` suppresses any errors
# (though its use isn't recommended - you should use non-erroring commands, or `|| true` them).
# Inside of recipes you can use some special variables: $@ - current target, $< - first dependency,
# $^ - all dependencies, $(@D) - target's directory.
#
# `VAR := EXPR` are simple variables (evaluated once), while `VAR = EXPR` get evaluated every time
# they're used (like a lambda: const x = () => expr; or a C# property). Variables and args in Make
# are strings (think yaml), and instead of true and false we've got "yes" and "" (empty string).
# You can substitute in variables with $(VAR) and call functions with $(function arg1,arg2,arg3).
# Spaces around commas are part of the arguments. A regular $ is escaped as $$.
#



# Use bash, and run commands in a recipe in the same shell instance
SHELL := /bin/bash
.ONESHELL:

# As I'm using a WSL to emulate Unix, some operations (particularly I/O intensive ones, like git)
# are better off-loaded back to Windows by using `git.exe` (from Windows' PATH) instead of `git`.
# `git.exe` is used instead of `git` automatically if: 1. it's on a WSL at all, and 2. the current
# directory is somewhere in /mnt/* (that's where Windows' partitions (e.g. C:, D:) are).

IS_WSL := $(shell [[ -n "$$WSL_DISTRO_NAME" && $$PWD == /mnt/* ]] && echo yes)

define find_exe
$(if $(IS_WSL),$(shell command -v $1.exe >/dev/null && echo $1.exe || echo $1),$1)
endef
define find_ps1
$(if $(IS_WSL),$(shell command -v $1.ps1 >/dev/null && echo pwsh.exe -nop -c $1.ps1 || echo $1),$1)
endef

GIT := $(call find_exe,git)
DOTNET := $(call find_exe,dotnet)
NANOEMOJI := $(call find_exe,nanoemoji)
INKSCAPE := $(call find_exe,inkscape)
MAGICK := $(call find_exe,magick)
MKBITMAP := $(call find_exe,mkbitmap)
POTRACE := $(call find_exe,potrace)
FONTTOOLS := $(call find_exe,fonttools)
HB_VIEW := $(call find_exe,hb-view)
7Z := $(call find_exe,7z)

SVGO := $(call find_ps1,svgo)

# This is used for parallelization in recipes. The recipes themselves are run in an order.
CPU_CORES := $(shell cat /proc/cpuinfo | grep processor | wc -l)

# These are the only commands that should be run through the CLI
.PHONY: build package test test-vars clean rebuild



build: build/merged.ttf

package: build/Segoe.UI.Emoji.with.Twemoji.Flags.zip

FLAGS_PER_LINE ?= 16
# Can be overriden with `make test FLAGS_PER_LINE=8`
test: build/tests/flags_$(FLAGS_PER_LINE).png build/tests/flags_$(FLAGS_PER_LINE)_bw.png

test-vars:
	@printf "%s\n" GIT=$(GIT) DOTNET=$(DOTNET) \
		NANOEMOJI=$(NANOEMOJI) INKSCAPE=$(INKSCAPE) \
		MAGICK=$(MAGICK) MKBITMAP=$(MKBITMAP) POTRACE=$(POTRACE) \
		FONTTOOLS=$(FONTTOOLS) HB_VIEW=$(HB_VIEW) \
		7Z=$(7Z) SVGO="$(SVGO)" \
		CPU_CORES=$(CPU_CORES)

clean:
	rm -rf build

rebuild: clean
	@$(MAKE) build

# TODO: add update actions for twemoji and seguiemj
#
# update-twemoji: init
# 	cd build/jdecked-twemoji
# 	$(GIT) fetch --no-tags --depth=1 origin main
# 	$(GIT) checkout FETCH_HEAD
# 	touch .git/HEAD



# Get the seguiemj.ttf from C:\Windows\Fonts
build/seguiemj.ttf:
	@mkdir -p $(@D)
	@pwsh.exe -nop -c "cp C:\Windows\Fonts\seguiemj.ttf $@.tmp"
	@cmp -s $@.tmp $@ && rm $@.tmp || mv $@.tmp $@

# Clone the jdecked/twemoji repository
build/jdecked-twemoji/.git/HEAD:
	@set -e
	@rm -rf build/jdecked-twemoji
	@$(GIT) clone --no-checkout --depth=1 --filter=tree:0 \
		https://github.com/jdecked/twemoji build/jdecked-twemoji
	@cd build/jdecked-twemoji
	@$(GIT) sparse-checkout set --no-cone /assets/svg
	@$(GIT) -c advice.detachedHead=false checkout origin/main

# Use the script to find all flag glyphs in jdecked/twemoji
build/glyph-paths.txt: scripts/find_flag_glyphs.cs build/jdecked-twemoji/.git/HEAD
	@$(DOTNET) $< >$@.tmp && mv $@.tmp $@



# I could have used Make's wildcards or patterns (*.svg, %.svg), but decided against them, as they
# bloated the process and slowed Make down significantly (it's definitely because I'm using a WSL
# instead of a proper Unix environment). So I decided to use manifests, that keep track of files
# matching a pattern, and that get updated only when something changes.

# When glyph-paths.txt changes, copy the glyphs over to svg-color/*.svg
build/svg-color/.manifest: build/glyph-paths.txt
	@mkdir -p $(@D)

	changed=$$(for glyph in $$(cat $<); do
		color=$(@D)/$${glyph##*/};
		[ "$$color" -nt "$$glyph" ] || echo "$$glyph";
	done);

	if [ -n "$$changed" ]; then
		count=$$(echo "$$changed" | wc -l)
		echo "Copying $$count glyphs from twemoji/jdecked..."

		echo "$$changed" | xargs -n 20 -P "$(CPU_CORES)" $(SVGO) --quiet --multipass -o $(@D) -i
		touch $@
	fi

# When svg-color/*.svg change, re-build whatever changed in svg-bw/*.svg
build/svg-bw/.manifest: build/svg-color/.manifest
	@mkdir -p $(@D)

	changed=$$(for color in build/svg-color/*.svg; do
		bw=$(@D)/$${color##*/};
		[ "$$bw" -nt "$$color" ] || echo "$$color";
	done);

	if [ -n "$$changed" ]; then
		count=$$(echo "$$changed" | wc -l)
		echo "Converting $$count glyphs to B&W..."

		echo "$$changed" | xargs -n 1 -P "$(CPU_CORES)" sh -c '
			bw=$(@D)/$${1##*/};
			echo "Converting to B&W $${bw##*/}...";
# https://gitlab.com/inkscape/inkscape/-/work_items/4716#note_1898150983
			export SELF_CALL=xxx;

			$(INKSCAPE) -w 1000 -h 1000 --export-filename "$$bw.png" "$$1";
			$(MAGICK) "$$bw.png" -gravity center -extent 1066x1066 "$$bw.bmp";
			$(MKBITMAP) -g -s 1 -f 10 -o "$$bw.pgm" "$$bw.bmp";
			$(POTRACE) --flat -s -W 36pt -H 36pt -o "$$bw" "$$bw.pgm";
# Note: SVGO can't meaningfully optimize Potrace's output. It removes metadata and transforms,
# and converts int coords to float coords, resulting in an average 50% size increase. But, we
# do need to change the width and height from 36pt to just 36, so it's sized correctly.
			sed -i '\''s/width="36.000000pt" height="36.000000pt" //g'\'' "$$bw";
			rm "$$bw.png" "$$bw.bmp" "$$bw.pgm";
		' sh

		touch $@
	fi



# When the svg-color/ manifest changes, rebuild the font with color flags
build/twemoji.flags.color/Font.ttf: build/svg-color/.manifest
	@echo "Building $@..."
	@$(NANOEMOJI) --color_format glyf_colr_0 --upem 2048 --width 2812 \
		--transform "scale(1.666666) translate(-554.666666, 85.333333)" \
		--build_dir build/twemoji.flags.color \
		$$(ls -1 build/svg-color/*.svg)

# When the svg-bw/ manifest changes, rebuild the font with b&w flags
build/twemoji.flags.bw/Font.ttf: build/svg-bw/.manifest
	@echo "Building $@..."
	@$(NANOEMOJI) --color_format glyf --upem 2048 --width 2812 \
		--transform "scale(1.95) translate(-554.666666, 21.333333)" \
		--build_dir build/twemoji.flags.bw \
		$$(ls -1 build/svg-bw/*.svg)



# Decompile the fonts to .ttx
build/seguiemj.ttx: build/seguiemj.ttf
	@rm -f $@
	@echo "Decompiling $<..."
	@$(FONTTOOLS) ttx $<

build/twemoji.flags.color/Font.ttx: build/twemoji.flags.color/Font.ttf
	@rm -f $@
	@echo "Decompiling $<..."
	@$(FONTTOOLS) ttx $<

build/twemoji.flags.bw/Font.ttx: build/twemoji.flags.bw/Font.ttf
	@rm -f $@
	@echo "Decompiling $<..."
	@$(FONTTOOLS) ttx $<

# Merge all the fonts into one
build/merged.ttx: scripts/gen_merged_font.cs build/seguiemj.ttx build/twemoji.flags.color/Font.ttx build/twemoji.flags.bw/Font.ttx
	@echo "Generating $@..."
	@$(DOTNET) $^ $@

build/merged.ttf: build/merged.ttx
	@rm -f $@
	@echo "Recompiling $@..."
	@$(FONTTOOLS) ttx $<



# Rename merged.ttf to this, and also make a .zip with it
build/Segoe.UI.Emoji.with.Twemoji.Flags.ttf: build/merged.ttf
	cp $< $@

build/Segoe.UI.Emoji.with.Twemoji.Flags.zip: build/Segoe.UI.Emoji.with.Twemoji.Flags.ttf
	$(7Z) a -tzip -mx=9 $@ $<



# Group flags into lines and render them using hb-view
build/tests/flags_$(FLAGS_PER_LINE).txt: scripts/print_glyphs.cs build/glyph-paths.txt
	@mkdir -p $(@D)
	@$(DOTNET) $^ | xargs -n $(FLAGS_PER_LINE) | tr -d " " | sed 's/^/🏳️‍⚧️/; s/$$/🏳️‍🌈/' >$@

build/tests/flags_$(FLAGS_PER_LINE).png: build/merged.ttf build/tests/flags_$(FLAGS_PER_LINE).txt
	@$(HB_VIEW) $< --output-file="$@" --text-file="$(word 2,$^)" --background=none

build/tests/flags_$(FLAGS_PER_LINE)_bw.png: build/merged.ttf build/tests/flags_$(FLAGS_PER_LINE).txt
	@$(HB_VIEW) $< --output-file="$@" --text-file="$(word 2,$^)" --draw
