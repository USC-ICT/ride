# Multilingual NVBG Technical Design

## Goal

Extend NVBG to support multilingual character output without regressing the current English behavior.

The design keeps the existing curated English NVBG pipeline intact and adds a separate multilingual fallback path for non-English utterances.

## Current state

The current NVBG wrapper is in:

- [D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.vh\Runtime\NonverbalBehaviorGeneratorSystem.cs](D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.vh\Runtime\NonverbalBehaviorGeneratorSystem.cs)

The main observations are:

1. NVBG rule XML is init-time data.
   - `NvbgOptions` is built with `ruleXml: initData.RuleXml`
   - `BuildInitData()` currently resolves one rule XML string before character creation

2. The code-level defaults currently point to Kevin assets.
   - `rule_input_ChrKevin.xml`
   - `saliency_map_init_kevin.xml`
   - in practice, these are overwritten in the Unity Editor and prefab configuration for real characters

3. The existing stack is English-specific beyond simple lexical keyword spotting.
   - lexical rules such as `you`, `my`, `yes`
   - parser-derived markers such as `NP`, `INTJ`, `first_NP`
   - English parser assets under `ParserModelEN`

4. Designers already use the existing inspector/prefab configuration to provide per-character NVBG setup.
   - `m_streams` is the effective source of truth for English NVBG assets in normal use
   - this must remain the authored baseline

5. Recreating NVBG currently affects only the internal `Nvbg` context, not the visible Unity character or conversation state.

## Design principles

1. Preserve current English behavior.
2. Keep runtime behavior deterministic.
3. Avoid per-utterance LLM calls.
4. Do not recreate visible characters or reset conversation history.
5. Switch language at the NVBG-context level, not the avatar level.
6. Prefer prewarmed or cached contexts over synchronous rebuilds during a turn.

## Proposed architecture

### 1. Split NVBG into two language modes

#### Curated English

Used when the utterance language is English.

Characteristics:

- existing curated English rule packs from the current per-character editor configuration
- existing parser-backed English pipeline
- no behavior change relative to current system

#### Multilingual fallback

Used when the utterance language is not English.

Characteristics:

- separate lexical rule packs keyed by language
- no assumption that the current English parser semantics transfer cleanly
- smaller but stable communicative-function coverage

Initial fallback coverage should focus on high-value signals:

- `you`
- `me`
- affirmation
- negation
- greeting
- thanks
- intensification
- contrast
- question emphasis

### 2. Route by utterance language

The primary trigger should come from ASR language detection.

Runtime flow:

1. ASR final result arrives with detected language.
2. Store the last detected user language.
3. When the character response is ready, resolve the NVBG route:
   - English -> curated English
   - non-English -> multilingual fallback
4. If the required NVBG context is not ready, prepare it before NVBG generation.

The final authority for NVBG language should be the character-output language when that becomes available. The ASR language is the early trigger.

### Route, profile key, rule pack, and context key

These terms are related, but they mean different things.

#### Language route

The language route is the result of `NvbgLanguageRouting.Resolve(languageTag)`.

It defines:

- the normalized language
- whether the utterance should use:
  - curated English
  - multilingual fallback

This is a routing decision, not an asset identity.

#### Profile key

The profile key identifies the authored English NVBG profile that the runtime is deriving from.

This matters because multilingual fallback rules are derived from the configured English baseline, not from language alone.

Two characters may share the same target language while using different authored English rule profiles. In that case, they should not share the same derived fallback context.

#### Rule pack

The rule pack is the resolved bundle of NVBG assets used to build a context.

It currently includes:

- `ProfileKey`
- `RuleXml`
- `SaliencyMapXml`

The rule pack answers:

- which rule XML should be used for this route?
- which saliency map belongs with it?
- which authored English profile did it come from?

#### Context key

The context key is the runtime/cache identity for one NVBG context instance.

It must include both:

- the route
- the profile key

because those solve different problems:

- the route identifies the language path
- the profile key identifies the authored English source profile

If the context key used only the language route, different characters or profiles could accidentally reuse the wrong fallback context.

### 3. Maintain multiple NVBG contexts per visible character

Do not think of this as recreating the avatar.

Instead, maintain one or more backend NVBG contexts behind a single visible Unity character.

Each context is keyed by:

- character NVBG configuration identity
- language mode
- normalized language tag

Example cache keys:

- `CC-Male|CuratedEnglish|en`
- `CC-Male|MultilingualFallback|fr-fr`

### 4. Context lifecycle

#### English

- always available
- initialized at startup or first use

#### Non-English

- created lazily on first use
- cached for the remainder of the session
- optionally persisted via generated language-pack files for reuse across sessions

### 5. Rule-pack generation strategy

#### Curated languages

For a small number of supported languages:

- ship curated offline rule packs
- store them as package assets or generated cached files

#### Generated languages

For unsupported languages:

- use an LLM once per language to bootstrap a fallback lexical rule pack
- write the result to app-owned cache storage
- reuse it deterministically

Do not call an LLM per utterance.

## Required code changes

### A. Preserve the current authored English setup

The current per-character editor workflow must remain the source of truth for English NVBG assets.

That means:

- the existing `m_streams` / prefab configuration remains authoritative for English
- multilingual support layers on top of that configuration
- no code-only replacement of the designer workflow

### B. Refactor rule-pack selection

`NonverbalBehaviorGeneratorSystem` should stop treating the Kevin defaults as the effective runtime configuration.

It should instead resolve:

- the current authored English pack from the configured `m_streams`
- a language route
- the fallback pack to use for non-English

### C. Add routing helpers

Pure routing helpers should remain independent from the live NVBG runtime.

Current file:

- [D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.vh\Runtime\Nvbg\NvbgLanguageRouting.cs](D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.vh\Runtime\Nvbg\NvbgLanguageRouting.cs)

Responsibilities:

- normalize language tags
- decide curated English vs multilingual fallback
- detect whether a route switch is required
- build stable cache keys

### D. Add rule-pack resolution layer

Introduce a new internal resolver component, conceptually:

- `NvbgRulePackResolver`

Responsibilities:

- derive a stable configuration identity from the current authored English setup
- read English rule assets from the current configured `m_streams`
- resolve multilingual fallback pack for a language
- read cached generated packs from disk
- provide rule XML and saliency XML strings to NVBG init

### E. Add NVBG context cache

Introduce a cache, conceptually:

- `Dictionary<string, Nvbg>`

Cache key:

- `configuredNvbgProfileKey|mode|normalizedLanguage`

Behavior:

- prewarm English
- create fallback contexts lazily
- retain contexts per session

### F. Add asynchronous prewarm/swap path

When a new non-English language appears:

1. keep current context usable
2. build the new context in the background
3. swap only after the new context is ready

This avoids visual impact and minimizes missed turns.

## Character and language abstraction

The runtime needs to distinguish:

### Visible character identity

The Unity character/avatar in the scene.

### NVBG authored profile

The existing designer-authored backend configuration that selects:

- rule XML
- saliency map
- posture-specific animation clips

This is currently provided through the Unity Editor / prefab configuration and must remain the English source of truth.

This design assumes:

- one visible character can own multiple NVBG contexts
- each context corresponds to one language route

## LLM usage policy

### Allowed

- bootstrap a language pack once
- expand synonym sets offline
- generate a cached first-pass translation set for supported communicative functions

### Not allowed

- per-utterance rule translation
- per-utterance keyword rewriting
- runtime nondeterministic rule mutation during normal character turns

## Persistence

Generated language packs should be written to app-owned storage, not package folders.

Recommended location:

- `Application.persistentDataPath`

Persistence contents:

- normalized language tag
- source rule profile
- generated XML rule pack
- generation metadata and version

## Rollout phases

### Phase 1

- add language-routing helpers
- add unit tests
- document architecture
- no live behavior change

### Phase 2

- replace hardcoded Kevin rule selection with a character-profile resolver
- preserve English behavior

### Phase 3

- add multilingual fallback contexts
- add language-route-based context caching

### Phase 4

- add curated non-English packs for a few languages
- add generated cached packs for unsupported languages

### Phase 5

- add telemetry for:
  - detected language
  - route selected
  - cache hit/miss
  - context prewarm time
  - fallback generation time

## Unit test scope

Current tests cover the pure routing contract:

- [D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.vh\Tests\Editor\NvbgLanguageRoutingTests.cs](D:\RIDE\Codex\svn_ride_trunk\RideUnity\Packages\edu.usc.ict.ride.vh\Tests\Editor\NvbgLanguageRoutingTests.cs)

Covered behavior:

- missing language defaults to curated English
- English language tags route to curated English
- non-English tags route to multilingual fallback
- route-switch detection is stable
- cache keys are deterministic

Future tests should cover:

- authored-profile rule-pack resolution
- cached pack lookup
- fallback pack generation metadata handling
- context-cache keying and eviction policy

## Risks

1. The existing English parser path may not degrade gracefully for non-English text.
   - mitigation: use separate multilingual fallback packs rather than pretending the English parser is multilingual

2. A refactor could accidentally bypass the current per-character authored NVBG setup.
   - mitigation: explicitly preserve the current editor-configured English path as the source of truth

3. Background context creation may miss a turn if not ready in time.
   - mitigation: keep current context active until replacement is fully initialized

4. Generated packs may be low quality without review.
   - mitigation: use curated packs for priority languages and treat generated packs as fallback only

## Recommendation

Proceed with:

1. English curated path unchanged
2. preserve the current per-character editor-authored NVBG setup as the English source of truth
3. routing by ASR-detected language
4. backend NVBG context caching by configured profile and language route
5. multilingual fallback packs for non-English
6. LLM generation only for cached language-pack bootstrapping, never per utterance
