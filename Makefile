SHELL := /bin/bash
.ONESHELL:

# IS_WSL is "yes" if the project directory is WSL-emulated, and "" otherwise
IS_WSL := $(shell [[ -n "$$WSL_DISTRO_NAME" && $$PWD == /mnt/* ]] && echo yes)

# Avoid WSL-emulated I/O by running git.exe directly in the Windows filesystem
GIT := $(if $(IS_WSL),git.exe,git)



.PHONY: main clean update-seguiemj update-twemoji

main: build/flag-glyphs.txt
	echo "Success?"

clean:
	rm -rf build

update-seguiemj:
	powershell.exe -NoProfile -Command "cp C:\Windows\Fonts\seguiemj.ttf build"

update-twemoji:
	cd build/jdecked-twemoji
	$(GIT) fetch --no-tags --depth=1 origin main
	$(GIT) checkout FETCH_HEAD
	touch .git/HEAD



build/jdecked-twemoji/.git/HEAD:
	mkdir -p build && cd build
	rm -rf jdecked-twemoji
	$(GIT) clone --no-checkout --depth=1 --filter=tree:0 https://github.com/jdecked/twemoji jdecked-twemoji
	cd jdecked-twemoji
	$(GIT) sparse-checkout set --no-cone /assets/svg
	$(GIT) checkout origin/main

build/jdecked-twemoji/commit.sha: build/jdecked-twemoji/.git/HEAD
	@if [ ! -f $@ ] || ! cmp -s $@ $<; then \
		cp $< $@; \
	fi

build/flag-glyphs.txt: build/jdecked-twemoji/commit.sha
	dotnet.exe scripts/find_flag_glyphs.cs >$@


