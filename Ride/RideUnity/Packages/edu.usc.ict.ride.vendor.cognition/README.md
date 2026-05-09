# RIDE Vendor Package: Cognition

This package redistributes third-party cognition libraries and native speech SDK components used by RIDE packages.

## Contents

* ipa-dict data under Runtime/ipa-dict
* NativeWebSocket source under Runtime/NativeWebSocket
* Microsoft Cognitive Services Speech SDK files under Runtime/SpeechSDK

## Version

Microsoft Cognitive Services Speech SDK package: Microsoft.CognitiveServices.Speech.1.31.0.unitypackage
NativeWebSocket version: 1.1.5
ipa-dict version: commit: 43c3570eb3553bdd19fccd2bd0091534889af023 5/23/2025

## Reference URLs

* https://github.com/Azure-Samples/cognitive-services-speech
* https://aka.ms/csspeech/unitypackage
* https://github.com/open-dict-data/ipa-dict
* https://github.com/endel/NativeWebSocket

## Usage

This package is intended to be consumed by other RIDE packages through Unity Package Manager package dependencies. Projects normally should not reference this package directly unless they need access to the bundled vendor component.

## License and Notices

See LICENSE.md for license terms that apply to USC-authored package metadata, Unity integration files, and wrapper code in this package.

See Third Party Notices.md for license terms and notices that apply to redistributed third-party components.

## RIDE

The [RIDE](https://ride.ict.usc.edu) platform (Rapid Integration & Development Environment) is a prototyping testbed for real-time simulation research and development, developed at the [USC Institute for Creative Technologies](https://ict.usc.edu). It combines geospatial terrain, AI agents, virtual humans / embodied conversational agents, networking, multi-platform support, ML-platform integration, cloud AI integration, and user avatars into one integrated framework. RIDE is designed to be agnostic to underlying game and simulation engines, using the [Unity](https://unity.com) game engine as the main target.
