#!/usr/bin/env bash
export EnableNuGetPackageRestore="true"
xbuild Build/Build.proj /p:Configuration="Mono Release" /t:GoMono
