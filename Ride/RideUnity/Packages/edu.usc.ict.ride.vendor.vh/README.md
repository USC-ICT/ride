# RIDE Vendor Package: VH

This package redistributes Virtual Human third-party and USC-authored components used by RIDE packages. The bundled components are collectively licensed under USC-RL v3.0 and MIT licenses.

## Contents

* BLLIP parser files under Runtime/BLLIP parser
* Microsoft.Extensions.Logging.Abstractions assembly under Runtime/Microsoft.Extensions.Logging.Abstractions
* NonVerbalBehaviorGenerator files under Runtime/NonVerbalBehaviorGenerator

## Version

Microsoft.Extensions.Logging.Abstractions version: 6.0.1

## Component Notes

BLLIP Parser:
* License: current code is licensed under USC-RL v3.0, adapted from an Apache License 2.0 code base
* URL: https://github.com/BLLIP/bllip-parser

Microsoft.Extensions.Logging.Abstractions:
* License: MIT
* URL: https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/6.0.1
* Version: 6.0.1

NonVerbal Behavior Generator (NVBG):
* License: USC-RL v3.0

## Usage

This package is intended to be consumed by other RIDE packages through Unity Package Manager package dependencies. Projects normally should not reference this package directly unless they need access to the bundled vendor component.

## License and Notices

See LICENSE.md for license terms that apply to USC-authored package metadata, Unity integration files, and wrapper code in this package.

See Third Party Notices.md for license terms and notices that apply to redistributed third-party components.

## RIDE

The [RIDE](https://ride.ict.usc.edu) platform (Rapid Integration & Development Environment) is a prototyping testbed for real-time simulation research and development, developed at the [USC Institute for Creative Technologies](https://ict.usc.edu). It combines geospatial terrain, AI agents, virtual humans / embodied conversational agents, networking, multi-platform support, ML-platform integration, cloud AI integration, and user avatars into one integrated framework. RIDE is designed to be agnostic to underlying game and simulation engines, using the [Unity](https://unity.com) game engine as the main target.
