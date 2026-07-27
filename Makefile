# I was learning Make as I was writing this, so I'll include a quick breakdown of everything
# happening in this file, so that those who don't know Make can understand what's happening.
#
# # Quick breakdown of Make
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
GIT := $(if $(IS_WSL),git.exe,git)

# TODO: automatically determine if we need to use % or %.exe for:
# dotnet, svgo, inkscape, magick, mkbitmap, potrace, nanoemoji.

# TODO: figure out parallelization (probably with just `fast:\n  $(MAKE) -j $(CPU_CORES)`)
CPU_CORES := $(shell cat /proc/cpuinfo | grep processor | wc -l)

# These are the only commands that should be run through the CLI
.PHONY: build clean rebuild



build: build/twemoji.flags.color.ttf

clean:
	rm -rf build

rebuild: clean
	$(MAKE) build

# TODO: add update actions for twemoji and seguiemj
#
# update-twemoji: init
# 	cd build/jdecked-twemoji
# 	$(GIT) fetch --no-tags --depth=1 origin main
# 	$(GIT) checkout FETCH_HEAD
# 	touch .git/HEAD



# I could have used Make's wildcards or patterns (*.svg, %.svg), but decided against them, as they
# bloated the process and slowed Make down significantly (it's definitely because I'm using a WSL
# instead of a proper Unix environment). So I decided to use manifests, that keep track of files
# matching a pattern, and that get updated only when something changes.

# $1: manifest file, $2: tracked files
define update_manifest
	@stat -c '%n %y' $2 >$1.tmp; \
	if cmp -s $1.tmp $1; then \
		rm -f $1.tmp; echo "$1: up-to-date"; \
	else \
		mv $1.tmp $1; echo "$1: sources updated"; \
	fi
endef

# Anything depending on ALWAYS_REBUILD will always be rebuilt (useful for manifests)
.PHONY: ALWAYS_REBUILD
ALWAYS_REBUILD:



# Get the seguiemj.ttf from C:\Windows\Fonts
build/seguiemj.ttf:
	@powershell.exe -NoProfile -Command "cp C:\Windows\Fonts\seguiemj.ttf $@.tmp"
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

# Keep track of jdecked/twemoji's current commit
build/jdecked-twemoji/commit.sha: build/jdecked-twemoji/.git/HEAD
	@cmp -s $@ $< || cp $< $@

# When the commit or the script change, rebuild glyph-list.txt
build/glyph-list.txt: scripts/find_flag_glyphs.cs build/jdecked-twemoji/commit.sha
	@echo "Rebuilding $@..."
	@dotnet.exe $< >$@.tmp && mv $@.tmp $@

# When glyph-list.txt changes, copy the glyphs over to build/svg-color/*.svg
build/svg-color/.updated: build/glyph-list.txt
	@echo "Copying over twemoji glyphs..."
	@mkdir -p $(@D)
	@rm -f $(@D)/*.svg
	@xargs -a $< cp -t $(@D)
	@touch $@

# Keep track of build/svg-color/*.svg in a manifest
build/svg-color/.manifest: build/svg-color/.updated ALWAYS_REBUILD
	@$(call update_manifest,$@,$(@D)/*.svg)

# When the manifest changes, rebuild the font with color flags
build/twemoji.flags.color.ttf: build/svg-color/.manifest
	@echo "Building $@..."
	@nanoemoji.exe --color_format glyf_colr_0 --upem 2048 --width 2812 \
		--transform "scale(1.666666) translate(-554.666666, 85.333333)" \
		--build_dir build/build.twemoji.flags.color \
		$$(cat build/glyph-list.txt)
	@cp build/build.twemoji.flags.color/Font.ttf $@



# TODO: compile b&w glyphs

# TODO: build b&w flags font

# TODO: merge seguiemj, color flags and b&w flags fonts


