# RIDE Vendor Package: AWS

This package redistributes AWS SDK for .NET assemblies used by RIDE packages. The AWS SDK is created by Amazon and licensed under the Apache License 2.0.

## Contents

* AWS SDK for .NET assemblies under Runtime/AWSSDK
* AWS SDK license and notice files
* Unity assembly definition for package integration

## Version

AWS SDK for .NET version: 3.7.569.0

## Reference URLs

* https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-obtain-assemblies.html#download-zip-files
* https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/unity-special.html
* https://aws.amazon.com/sdk-for-net

## Usage

This package is intended to be consumed by other RIDE packages through Unity Package Manager package dependencies. Projects normally should not reference this package directly unless they need access to the bundled vendor component.

## License and Notices

See LICENSE.md for license terms that apply to USC-authored package metadata, Unity integration files, and wrapper code in this package.

See Third Party Notices.md for license terms and notices that apply to redistributed third-party components.

## RIDE

The [RIDE](https://ride.ict.usc.edu) platform (Rapid Integration & Development Environment) is a prototyping testbed for real-time simulation research and development, developed at the [USC Institute for Creative Technologies](https://ict.usc.edu). It combines geospatial terrain, AI agents, virtual humans / embodied conversational agents, networking, multi-platform support, ML-platform integration, cloud AI integration, and user avatars into one integrated framework. RIDE is designed to be agnostic to underlying game and simulation engines, using the [Unity](https://unity.com) game engine as the main target.
