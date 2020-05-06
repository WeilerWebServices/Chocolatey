chocolateytemplates
===================

The templates …


… it’s all about the templates.

##Welcome to the simple way of creating chocolatey packages
Take a look at this repository (note the _templates folder).
Now open a command line, navigate to your source code top level folder and type the following:

```cmd
 cinst warmup
 warmup addTextReplacement __CHOCO_PKG_MAINTAINER_NAME__ "Your Name"
 warmup addTextReplacement __CHOCO_PKG_MAINTAINER_REPO__ "Your Repository (contains your packages)"
 git clone https://github.com/chocolatey/chocolateytemplates.git
 cd chocolateytemplates\_templates
 warmup addTemplateFolder chocolatey "%cd%\chocolatey"
 warmup addTemplateFolder chocolatey3 "%cd%\chocolatey3"
 warmup addTemplateFolder chocolateyauto "%cd%\chocolateyauto"
 warmup addTemplateFolder chocolateyauto3 "%cd%\chocolateyauto3"
```

(Use `$pwd` instead of `%cd%` for use in PowerShell, e.g. `warmup addTemplateFolder chocolatey "$pwd\chocolatey"`)

 * The package maintainer name (old term: owner) (__CHOCO_PKG_MAINTAINER_NAME__) would be you.
 * Your packages repository (__CHOCO_PKG_MAINTAINER_REPO__) is part of a github repo just **ferventcoder/chocolatey-packages** if your repository is https://github.com/ferventcoder/chocolatey-packages. This is only used for image urls. This repository contains your packages, also automatic packages if you have them. It is not recommended to use separate repositories for manual and automatic packages.

Now whenever you want to create a new package you just open a command line and navigate to your packages repository source code folder (or install stexbar `cinst stexbar` and just hit Ctrl+M from explorer).

```cmd
 warmup templateName packageName
```

as in

```cmd
 warmup chocolatey3 notepadplusplus
```

**Note** that Warmup has problems with UTF-8 encoded files (see [the related issue at its GitHub repository](https://github.com/chucknorris/warmup/issues/21)). Therefore, the UTF-8 test in the *.nuspec files `<!-- Do not remove this test for UTF-8: if “Ω”…` might actually fail, i.e. you see `???` in your editor.

It is recommended to use `choco new` for initial package creation, see [Chocolatey's wiki entry on package creation](https://github.com/chocolatey/choco/wiki/CreatePackages).

