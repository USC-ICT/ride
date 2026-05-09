# RIDE Vendor Package: Crc32.NET

This package redistributes Crc32.NET code used by RIDE packages. Crc32.NET provides a C# implementation of the CRC32 algorithm and is licensed under the MIT license.

## Contents

* Crc32.NET source under Runtime/Crc32.NET
* Third-party license notice
* Unity assembly definition for package integration

## Version

Crc32.NET version: 1.2.0

## Reference URLs

* https://github.com/force-net/Crc32.NET/releases

## Usage

This package is intended to be consumed by other RIDE packages through Unity Package Manager package dependencies. Projects normally should not reference this package directly unless they need access to the bundled vendor component.

## License and Notices

See LICENSE.md for license terms that apply to USC-authored package metadata, Unity integration files, and wrapper code in this package.

See Third Party Notices.md for license terms and notices that apply to redistributed third-party components.

## RIDE

The [RIDE](https://ride.ict.usc.edu) platform (Rapid Integration & Development Environment) is a prototyping testbed for real-time simulation research and development, developed at the [USC Institute for Creative Technologies](https://ict.usc.edu). It combines geospatial terrain, AI agents, virtual humans / embodied conversational agents, networking, multi-platform support, ML-platform integration, cloud AI integration, and user avatars into one integrated framework. RIDE is designed to be agnostic to underlying game and simulation engines, using the [Unity](https://unity.com) game engine as the main target.
