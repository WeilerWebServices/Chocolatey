# Chocolatey Package Verifier

The verifier is a service that checks the correctness (that the package actually works), that it installs and uninstalls correctly, has the right dependencies to ensure it is installed properly and can be installed silently. The verifier runs against both submitted packages and existing packages (checking every two weeks that a package can still install and sending notice when it fails). We like to think of the verifier as integration testing. It’s testing all the parts and ensuring everything is good. See the [wiki](https://github.com/chocolatey/package-verifier/wiki) for more information

![Chocolatey Logo](https://github.com/chocolatey/chocolatey/raw/master/docs/logo/chocolateyicon.gif "Chocolatey")

## Chat Room

Want quick feedback to your questions? [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/chocolatey/choco?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Requirements
* .NET Framework 4.0

### License / Credits
Apache 2.0 - see [LICENSE](https://github.com/chocolatey/package-verifier/blob/master/LICENSE) and [NOTICE](https://github.com/chocolatey/package-verifier/blob/master/NOTICE) files.

## Contributing

If you would like to contribute code or help squash a bug or two, that's awesome. Please familiarize yourself with [CONTRIBUTING](https://github.com/chocolatey/package-verifier/blob/master/CONTRIBUTING.md).

## Committers

Committers, you should be very familiar with [COMMITTERS](https://github.com/chocolatey/package-verifier/blob/master/COMMITTERS.md).

### Compiling / Building Source

There is a `build.bat`/`build.sh` file that creates a necessary generated file named `SolutionVersion.cs`. It must be run at least once before Visual Studio will build.

#### Windows

Prerequisites:

 * .NET Framework 4+
 * Visual Studio is helpful for working on source.
 * ReSharper is immensely helpful (and there is a `.sln.DotSettings` file to help with code conventions).

Build Process:

 * Run `build.bat`.

Running the build on Windows should produce an artifact that is tested and ready to be used.


## Setup

You need the following installed on a machine that you will use the verifier with:

* Vagrant (right now 1.6.5 or 1.7.3 is recommended) - you should verify it works.
* The vagrant box(es) should be already installed on the machine. 1 box at a time currently supported.
* The configuration file has the rest of what is necessary for config.
* Install the service and let it run.
