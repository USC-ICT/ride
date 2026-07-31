# ASR Language Detection And Multilingual NVBG

## Purpose

This document describes the current language-detection flow and how it drives dynamic per-language NVBG rule-pack selection and generation.

The main goal is:

- keep the existing curated English NVBG behavior intact
- support non-English character utterances without replacing the current per-character authoring workflow
- allow users to refine generated non-English rule packs by editing cached XML files

## Current architecture

The pipeline is:

1. ASR recognizes the user's utterance
2. the ASR provider may return a detected language
3. `DemoControllerBase` stores the provider language
4. NVBG selects:
   - curated English rules for English
   - multilingual fallback rules for non-English
5. if a non-English fallback rule pack does not exist yet, it is generated from the authored English rule pack and cached to disk

This affects only the NVBG context selection. It does not reload the visible character or clear conversation history.

## Key files

### Language propagation

- `D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.abstract\Runtime\Interfaces\Cognition\ISpeechRecognitionSystem.cs`
- `D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.cognition\Runtime\ASR\SpeechRecognitionSystemUnity.cs`

### ASR providers

- `D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.cognition\Runtime\ASR\SpeechRecognitionSystemAzure.cs`
- `D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.cognition\Runtime\ASR\SpeechRecognitionSystemOpenAI.cs`
- `D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.cognition\Runtime\ASR\SpeechRecognitionSystemElevenLabs.cs`

### NVBG routing and generation

- `D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.vh\Runtime\nvbg\NvbgLanguageRouting.cs`
- `D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.vh\Runtime\NonverbalBehaviorGeneratorSystem.cs`

### Demo integration

- `D:\RIDE\Codex\VH\VHUnityURP-Internal\Assets\VHShared\Scripts\DemoControllerBase.cs`
- `D:\RIDE\Codex\VH\VHUnityURP-Internal\Assets\VHShared\Scripts\DebugMenus\DebugMenuASR.cs`

## Source of truth for English rules

The source of truth for English NVBG rules remains the existing per-character Unity editor configuration.

That means:

- designers still assign the authored rule XML and saliency XML through the current prefab/editor workflow
- the multilingual fallback system does not replace that workflow
- the generated non-English rules are derived from the authored English baseline

The old hardcoded Kevin filenames are only defaults. They are not the intended runtime authoring model.

## Language routing model

Language routing is handled by `NvbgLanguageRouting`.

Current policy:

- English:
  - `en`
  - `en-*`
  - route to `CuratedEnglish`
- anything else:
  - route to `MultilingualFallback`

If the ASR provider does not supply a language:

- the debug UI shows `unknown`
- the effective NVBG language falls back to English

This is intentional. Provider language and fallback behavior are separate decisions.

## Route, profile key, context key, and rule pack

These four concepts are related, but they are not the same thing.

### Language route

`NvbgLanguageRouting.Resolve(languageTag)` returns an `NvbgLanguageRoute`.

The route answers:

- what normalized language are we dealing with?
- should this use curated English or multilingual fallback?

So the route is a policy decision.

Examples:

- `en` -> `CuratedEnglish`
- `en-US` -> `CuratedEnglish`
- `nl` -> `MultilingualFallback`
- `fr` -> `MultilingualFallback`

### Profile key

The `ProfileKey` identifies the authored English NVBG profile that the current rule selection is based on.

This is important because multilingual fallback rules are derived from the configured English baseline.

Two characters may use different authored English rule sets, even if they both speak the same target language. In that case, they should not share the same derived fallback context.

So `ProfileKey` answers:

- which English authored rule profile is this based on?

It is not just the language, and it is not necessarily just the character name.

### Rule pack

`NvbgRulePack` is the resolved bundle of NVBG assets used to build a context.

It currently contains:

- `ProfileKey`
- `RuleXml`
- `SaliencyMapXml`

This allows the runtime to resolve:

- which rule XML should be used for this route
- which saliency map belongs with it
- which authored English profile it came from

For English, the rule pack uses the curated authored English XML.

For non-English, the rule pack may use:

- a cached generated fallback XML
- a newly generated fallback XML

### Context key

The `contextKey` is the unique identifier for one NVBG runtime context.

It is used to keep separate NVBG instances for different combinations of:

- character
- language route
- profile key

The reason both the route and the profile key are needed is that they solve different problems:

- the route identifies the language path
- the profile key identifies the authored English source profile

Example:

- Character A uses English profile `CC-Male`
- Character B uses English profile `ICT-Female`
- both speak Dutch

They share the same route:

- `MultilingualFallback | nl`

But they should not share the same fallback context if their English bases differ.

So the context key must encode both:

- the language route
- the profile identity

Otherwise the runtime could accidentally reuse the wrong generated rules or saliency data.

## ASR provider behavior

### Azure

Azure is currently the reliable cloud provider for language-aware routing.

The implementation uses Azure language ID and continuous language identification. This allows language changes during a session.

### OpenAI Realtime

OpenAI Realtime remains a strong ASR provider, but the current realtime transcription event path does not provide a reliable documented language tag for this integration.

For that reason:

- it can be used for transcription
- it should not be treated as authoritative for multilingual NVBG routing unless the provider starts returning a stable language field

### ElevenLabs Realtime

The realtime ElevenLabs ASR path currently works for transcription, but not for reliable language detection.

Important findings:

- the realtime websocket session accepts:
  - `include_timestamps=true`
  - `include_language_detection=true`
  - `timestamps_granularity=word`
- in practice, the service still returns `committed_transcript` without `language_code`
- it does not consistently return `committed_transcript_with_timestamps` with detected language metadata

Conclusion:

- ElevenLabs realtime ASR is usable as a transcription provider
- it is not currently reliable as the language source for multilingual NVBG routing

## NVBG context switching

The system does not recreate the visible Unity character.

It only switches or creates NVBG contexts internally.

That means:

- no visual unload/reload of the avatar
- no loss of conversation history
- no reset of NLP state

The runtime maintains language-specific NVBG contexts behind the scenes.

`DemoControllerBase` uses the detected user language to:

1. prewarm the NVBG language context after ASR completes
2. request NVBG behavior generation with the effective language for the current utterance

## Generated fallback rule packs

For non-English languages, the system looks for a cached rule file first.

Cache location:

`Application.persistentDataPath\nvbg\generated-rules\{profileKey}.{language}.xml`

Examples:

- `kevin.nl.xml`
- `ict_male.fr.xml`

Generation policy:

1. if the cached file exists:
   - load it
   - do not regenerate
   - do not overwrite
2. if it does not exist:
   - generate it from the authored English rule XML
   - write it once to disk

This is deliberate. Generated files are meant to be user-editable.

## Editing generated rules

Users can tweak generated rule files directly.

That is currently the supported refinement path for missing lexical triggers.

Example:

- if the generated Dutch `you` rule is missing a useful word, the cached Dutch XML can be edited manually

Current overwrite behavior:

- runtime generation does not overwrite an existing cached file
- a cached file is only replaced if something external removes or replaces it, or if future code adds an explicit regenerate command

This makes the generated file a stable customization point.

## What is translated

The multilingual fallback is lexical, not a full multilingual parser port.

Translation maps live in:

- `D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.vh\Runtime\nvbg\NvbgLanguageRouting.cs`

The fallback generator preserves parser markers such as:

- `NP`
- `INTJ`
- `first_NP`

and translates supported lexical patterns for:

- Spanish
- French
- Dutch
- German
- Italian
- Portuguese

Unsupported languages fall back to curated English.

## Limitations

Current limitations are:

1. non-English fallback rules are lexical approximations
2. they do not provide a full multilingual parser path
3. OpenAI Realtime does not currently provide a reliable language tag for this workflow
4. ElevenLabs Realtime does not currently provide a reliable language tag for this workflow
5. unsupported languages fall back to English

## Practical recommendation

For multilingual NVBG routing today:

- use Azure ASR when reliable language detection matters
- use OpenAI Realtime or ElevenLabs Realtime for transcription quality/latency when needed
- do not assume they are authoritative language providers
- treat generated fallback rule XML files as editable user-owned artifacts

## Debugging

Useful logs include:

- `DemoControllerBase` ASR logs:
  - provider language
  - effective NVBG language
  - recognized text
- `NonverbalBehaviorGeneratorSystem` logs:
  - `PrepareLanguageContext`
  - chosen route
  - cached/generated fallback file path
- ElevenLabs realtime logs:
  - accepted `session_started.config`
  - committed event type
  - presence or absence of `language_code`

## Summary

The current system is intentionally hybrid:

- curated English rules remain designer-authored
- non-English rules are generated on demand from English
- generated non-English files are cached and user-editable
- Azure is the reliable language-detection provider
- OpenAI Realtime and ElevenLabs Realtime are currently transcription-first integrations, not reliable language-ID sources
