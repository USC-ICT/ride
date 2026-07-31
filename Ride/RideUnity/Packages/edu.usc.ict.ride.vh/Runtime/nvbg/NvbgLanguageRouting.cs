using System;
using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// High-level strategy for selecting NVBG rules for a given utterance language.
    /// </summary>
    public enum NvbgLanguageMode
    {
        CuratedEnglish = 0,
        MultilingualFallback = 1,
    }

    /// <summary>
    /// Immutable routing result for selecting an NVBG rule pack.
    /// </summary>
    public readonly struct NvbgLanguageRoute : IEquatable<NvbgLanguageRoute>
    {
        public NvbgLanguageRoute(string originalLanguage, string normalizedLanguage, NvbgLanguageMode mode)
        {
            OriginalLanguage = originalLanguage ?? string.Empty;
            NormalizedLanguage = normalizedLanguage ?? string.Empty;
            Mode = mode;
        }

        public string OriginalLanguage { get; }

        public string NormalizedLanguage { get; }

        public NvbgLanguageMode Mode { get; }

        public bool IsEnglish => Mode == NvbgLanguageMode.CuratedEnglish;

        public bool Equals(NvbgLanguageRoute other) =>
            string.Equals(OriginalLanguage, other.OriginalLanguage, StringComparison.Ordinal) &&
            string.Equals(NormalizedLanguage, other.NormalizedLanguage, StringComparison.Ordinal) &&
            Mode == other.Mode;

        public override bool Equals(object obj) => obj is NvbgLanguageRoute other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(OriginalLanguage, NormalizedLanguage, (int)Mode);

        public override string ToString() => $"{Mode}:{NormalizedLanguage}";
    }

    /// <summary>
    /// Pure helper that resolves utterance languages into stable NVBG routing decisions.
    /// This is intentionally independent from the current NVBG implementation so it can be
    /// unit tested before runtime switching is added to the live system.
    /// </summary>
    public static class NvbgLanguageRouting
    {
        public const string DefaultEnglishLanguage = "en";

        public static NvbgLanguageRoute Resolve(string languageTag)
        {
            string normalized = NormalizeLanguageTag(languageTag);
            NvbgLanguageMode mode = IsEnglishLanguage(normalized)
                ? NvbgLanguageMode.CuratedEnglish
                : NvbgLanguageMode.MultilingualFallback;

            return new NvbgLanguageRoute(languageTag, normalized, mode);
        }

        public static bool RequiresRouteSwitch(NvbgLanguageRoute currentRoute, NvbgLanguageRoute nextRoute) =>
            currentRoute.Mode != nextRoute.Mode ||
            !string.Equals(currentRoute.NormalizedLanguage, nextRoute.NormalizedLanguage, StringComparison.Ordinal);

        public static string BuildCacheKey(string characterProfileKey, NvbgLanguageRoute route)
        {
            string profile = string.IsNullOrWhiteSpace(characterProfileKey)
                ? "default"
                : characterProfileKey.Trim();

            return $"{profile}|{route.Mode}|{route.NormalizedLanguage}";
        }

        public static string NormalizeLanguageTag(string languageTag)
        {
            if (string.IsNullOrWhiteSpace(languageTag))
                return DefaultEnglishLanguage;

            string normalized = languageTag.Trim().Replace('_', '-').ToLowerInvariant();
            return string.IsNullOrWhiteSpace(normalized) ? DefaultEnglishLanguage : normalized;
        }

        public static bool IsEnglishLanguage(string normalizedLanguageTag) =>
            string.Equals(normalizedLanguageTag, DefaultEnglishLanguage, StringComparison.Ordinal) ||
            (!string.IsNullOrEmpty(normalizedLanguageTag) &&
             normalizedLanguageTag.StartsWith($"{DefaultEnglishLanguage}-", StringComparison.Ordinal));
    }

    public static class NvbgFallbackRulePackGenerator
    {
        private static readonly HashSet<string> s_passthroughPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "first_NP",
            "NP",
            "INTJ",
            "DOWNLEFT",
            "DOWNRIGHT",
            "POLAR 0",
        };

        private static readonly Dictionary<string, Dictionary<string, string[]>> s_languageLexicon =
            new Dictionary<string, Dictionary<string, string[]>>(StringComparer.OrdinalIgnoreCase)
            {
                ["es"] = BuildSpanishMap(),
                ["fr"] = BuildFrenchMap(),
                ["nl"] = BuildDutchMap(),
                ["de"] = BuildGermanMap(),
                ["it"] = BuildItalianMap(),
                ["pt"] = BuildPortugueseMap()
            };

        public static bool TryGenerate(string englishRuleXml, string normalizedLanguage, out string generatedRuleXml)
        {
            generatedRuleXml = string.Empty;

            if (string.IsNullOrWhiteSpace(englishRuleXml))
                return false;

            string languageBase = GetLanguageBase(normalizedLanguage);
            if (string.IsNullOrWhiteSpace(languageBase) || !s_languageLexicon.TryGetValue(languageBase, out Dictionary<string, string[]> translations))
                return false;

            var document = new System.Xml.XmlDocument { PreserveWhitespace = true };
            document.LoadXml(englishRuleXml);

            System.Xml.XmlNodeList patternNodes = document.SelectNodes("//rule/pattern");
            if (patternNodes == null)
                return false;

            bool changed = false;
            foreach (System.Xml.XmlNode patternNode in patternNodes)
            {
                if (patternNode?.InnerText == null)
                    continue;

                string sourcePattern = patternNode.InnerText.Trim();
                if (string.IsNullOrEmpty(sourcePattern) || s_passthroughPatterns.Contains(sourcePattern))
                    continue;

                if (!TryTranslatePattern(sourcePattern, translations, out string translatedPattern))
                    continue;

                if (!string.Equals(sourcePattern, translatedPattern, StringComparison.Ordinal))
                {
                    patternNode.InnerText = translatedPattern;
                    changed = true;
                }
            }

            if (!changed)
                return false;

            generatedRuleXml = document.OuterXml;
            return true;
        }

        private static bool TryTranslatePattern(string sourcePattern, Dictionary<string, string[]> translations, out string translatedPattern)
        {
            translatedPattern = sourcePattern;

            string trimmed = sourcePattern.Trim();
            string punctuationSuffix = string.Empty;
            if (trimmed.EndsWith(",", StringComparison.Ordinal))
            {
                punctuationSuffix = ",";
                trimmed = trimmed.Substring(0, trimmed.Length - 1).TrimEnd();
            }

            string lookupKey = trimmed.ToLowerInvariant();
            if (!translations.TryGetValue(lookupKey, out string[] values) || values == null || values.Length == 0)
                return false;

            string candidate = null;
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    candidate = value;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(candidate))
                return false;

            translatedPattern = candidate + punctuationSuffix;
            return true;
        }

        private static string GetLanguageBase(string normalizedLanguage)
        {
            if (string.IsNullOrWhiteSpace(normalizedLanguage))
                return string.Empty;

            int separatorIndex = normalizedLanguage.IndexOf('-');
            return separatorIndex >= 0 ? normalizedLanguage.Substring(0, separatorIndex) : normalizedLanguage;
        }

        private static void Add(Dictionary<string, string[]> map, string source, params string[] values) => map[source] = values;

        private static Dictionary<string, string[]> CreateBaseMap() => new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, string[]> BuildSpanishMap()
        {
            var map = CreateBaseMap();
            Add(map, "yes", "si");
            Add(map, "yeah", "si");
            Add(map, "plenty", "mucho");
            Add(map, "fully", "totalmente");
            Add(map, "completely", "completamente");
            Add(map, "really", "realmente");
            Add(map, "very", "muy");
            Add(map, "quite", "bastante");
            Add(map, "wonderful", "maravilloso");
            Add(map, "great", "genial", "excelente");
            Add(map, "absolutely", "absolutamente");
            Add(map, "huge", "enorme");
            Add(map, "fantastic", "fantastico");
            Add(map, "so", "tan");
            Add(map, "amazing", "increible");
            Add(map, "important", "importante");
            Add(map, "good", "bueno");
            Add(map, "nice", "agradable");
            Add(map, "you", "tu", "usted");
            Add(map, "your", "tu", "su");
            Add(map, "yours", "tuyo", "suyo");
            Add(map, "i", "yo");
            Add(map, "i'm", "soy", "estoy");
            Add(map, "me", "me");
            Add(map, "my", "mi");
            Add(map, "mine", "mio");
            Add(map, "is", "es", "esta");
            Add(map, "are", "son", "estas");
            Add(map, "were", "eran", "fueron");
            Add(map, "was", "era", "fue");
            Add(map, "have been", "han sido", "ha sido");
            Add(map, "has been", "ha sido");
            Add(map, "at", "en");
            Add(map, "stands", "esta", "se encuentra");
            Add(map, "come", "venir", "ven");
            Add(map, "like", "como", "gustar");
            Add(map, "no", "no");
            Add(map, "not", "no");
            Add(map, "nothing", "nada");
            Add(map, "cannot", "no puede");
            Add(map, "can't", "no puede");
            Add(map, "cant", "no puede");
            Add(map, "don't", "no");
            Add(map, "dont", "no");
            Add(map, "didn't", "no");
            Add(map, "couldn't", "no pudo");
            Add(map, "couldnt", "no pudo");
            Add(map, "isn't", "no es");
            Add(map, "isnt", "no es");
            Add(map, "wasn't", "no fue");
            Add(map, "wasnt", "no fue");
            Add(map, "but", "pero");
            Add(map, "however", "sin embargo");
            Add(map, "maybe", "quizas", "tal vez");
            Add(map, "perhaps", "quizas");
            Add(map, "why", "por que");
            Add(map, "where", "donde");
            Add(map, "who", "quien");
            Add(map, "how", "como");
            Add(map, "when", "cuando");
            Add(map, "there", "alli", "ahi");
            Add(map, "here", "aqui");
            Add(map, "must", "debe");
            Add(map, "have to", "tener que");
            Add(map, "terrain", "terreno");
            Add(map, "platform", "plataforma");
            Add(map, "platforms", "plataformas");
            Add(map, "sensing", "deteccion");
            Add(map, "recognition", "reconocimiento");
            Add(map, "processing", "procesamiento");
            Add(map, "generation", "generacion");
            Add(map, "real-time", "tiempo real");
            Add(map, "github", "github");
            Add(map, "edu", "edu");
            return map;
        }

        private static Dictionary<string, string[]> BuildFrenchMap()
        {
            var map = CreateBaseMap();
            Add(map, "yes", "oui");
            Add(map, "yeah", "oui");
            Add(map, "plenty", "beaucoup");
            Add(map, "fully", "entierement");
            Add(map, "completely", "completement");
            Add(map, "really", "vraiment");
            Add(map, "very", "tres");
            Add(map, "quite", "assez");
            Add(map, "wonderful", "merveilleux");
            Add(map, "great", "excellent", "super");
            Add(map, "absolutely", "absolument");
            Add(map, "huge", "enorme");
            Add(map, "fantastic", "fantastique");
            Add(map, "so", "si");
            Add(map, "amazing", "incroyable");
            Add(map, "important", "important");
            Add(map, "good", "bon");
            Add(map, "nice", "agreable");
            Add(map, "you", "toi", "vous");
            Add(map, "your", "ton", "votre");
            Add(map, "yours", "le tien", "le votre");
            Add(map, "i", "je");
            Add(map, "i'm", "je suis");
            Add(map, "me", "moi");
            Add(map, "my", "mon", "ma");
            Add(map, "mine", "le mien", "la mienne");
            Add(map, "is", "est");
            Add(map, "are", "sont", "etes");
            Add(map, "were", "etaient");
            Add(map, "was", "etait");
            Add(map, "have been", "ont ete", "a ete");
            Add(map, "has been", "a ete");
            Add(map, "at", "a");
            Add(map, "stands", "se trouve", "est");
            Add(map, "come", "venir", "viens");
            Add(map, "like", "comme", "aimer");
            Add(map, "no", "non");
            Add(map, "not", "pas");
            Add(map, "nothing", "rien");
            Add(map, "cannot", "ne peut pas");
            Add(map, "can't", "ne peut pas");
            Add(map, "cant", "ne peut pas");
            Add(map, "don't", "ne");
            Add(map, "dont", "ne");
            Add(map, "didn't", "n'a pas");
            Add(map, "couldn't", "n'a pas pu");
            Add(map, "couldnt", "n'a pas pu");
            Add(map, "isn't", "n'est pas");
            Add(map, "isnt", "n'est pas");
            Add(map, "wasn't", "n'etait pas");
            Add(map, "wasnt", "n'etait pas");
            Add(map, "but", "mais");
            Add(map, "however", "cependant");
            Add(map, "maybe", "peut-etre");
            Add(map, "perhaps", "peut-etre");
            Add(map, "why", "pourquoi");
            Add(map, "where", "ou");
            Add(map, "who", "qui");
            Add(map, "how", "comment");
            Add(map, "when", "quand");
            Add(map, "there", "la-bas");
            Add(map, "here", "ici");
            Add(map, "must", "doit");
            Add(map, "have to", "devoir");
            Add(map, "terrain", "terrain");
            Add(map, "platform", "plateforme");
            Add(map, "platforms", "plateformes");
            Add(map, "sensing", "detection");
            Add(map, "recognition", "reconnaissance");
            Add(map, "processing", "traitement");
            Add(map, "generation", "generation");
            Add(map, "real-time", "temps reel");
            Add(map, "github", "github");
            Add(map, "edu", "edu");
            return map;
        }

        private static Dictionary<string, string[]> BuildDutchMap()
        {
            var map = CreateBaseMap();
            Add(map, "yes", "ja");
            Add(map, "yeah", "ja");
            Add(map, "plenty", "veel");
            Add(map, "fully", "volledig");
            Add(map, "completely", "helemaal");
            Add(map, "really", "echt");
            Add(map, "very", "erg");
            Add(map, "quite", "best");
            Add(map, "wonderful", "geweldig");
            Add(map, "great", "prima", "geweldig");
            Add(map, "absolutely", "absoluut");
            Add(map, "huge", "enorm");
            Add(map, "fantastic", "fantastisch");
            Add(map, "so", "zo");
            Add(map, "amazing", "verbluffend");
            Add(map, "important", "belangrijk");
            Add(map, "good", "goed");
            Add(map, "nice", "leuk");
            Add(map, "you", "jij", "u");
            Add(map, "your", "jouw", "uw");
            Add(map, "yours", "de jouwe", "de uwe");
            Add(map, "i", "ik");
            Add(map, "i'm", "ik ben");
            Add(map, "me", "mij");
            Add(map, "my", "mijn");
            Add(map, "mine", "de mijne");
            Add(map, "is", "is");
            Add(map, "are", "zijn", "bent");
            Add(map, "were", "waren");
            Add(map, "was", "was");
            Add(map, "have been", "zijn geweest", "ben geweest");
            Add(map, "has been", "is geweest");
            Add(map, "at", "bij");
            Add(map, "stands", "staat");
            Add(map, "come", "komen");
            Add(map, "like", "zoals", "houden van");
            Add(map, "no", "nee");
            Add(map, "not", "niet");
            Add(map, "nothing", "niets");
            Add(map, "cannot", "kan niet");
            Add(map, "can't", "kan niet");
            Add(map, "cant", "kan niet");
            Add(map, "don't", "niet");
            Add(map, "dont", "niet");
            Add(map, "didn't", "deed niet");
            Add(map, "couldn't", "kon niet");
            Add(map, "couldnt", "kon niet");
            Add(map, "isn't", "is niet");
            Add(map, "isnt", "is niet");
            Add(map, "wasn't", "was niet");
            Add(map, "wasnt", "was niet");
            Add(map, "but", "maar");
            Add(map, "however", "echter");
            Add(map, "maybe", "misschien");
            Add(map, "perhaps", "misschien");
            Add(map, "why", "waarom");
            Add(map, "where", "waar");
            Add(map, "who", "wie");
            Add(map, "how", "hoe");
            Add(map, "when", "wanneer");
            Add(map, "there", "daar");
            Add(map, "here", "hier");
            Add(map, "must", "moet");
            Add(map, "have to", "moeten");
            Add(map, "terrain", "terrein");
            Add(map, "platform", "platform");
            Add(map, "platforms", "platforms");
            Add(map, "sensing", "detectie");
            Add(map, "recognition", "herkenning");
            Add(map, "processing", "verwerking");
            Add(map, "generation", "generatie");
            Add(map, "real-time", "realtime");
            Add(map, "github", "github");
            Add(map, "edu", "edu");
            return map;
        }

        private static Dictionary<string, string[]> BuildGermanMap()
        {
            var map = CreateBaseMap();
            Add(map, "yes", "ja");
            Add(map, "yeah", "ja");
            Add(map, "plenty", "viel");
            Add(map, "fully", "vollstandig");
            Add(map, "completely", "komplett");
            Add(map, "really", "wirklich");
            Add(map, "very", "sehr");
            Add(map, "quite", "ziemlich");
            Add(map, "wonderful", "wunderbar");
            Add(map, "great", "grossartig", "toll");
            Add(map, "absolutely", "absolut");
            Add(map, "huge", "riesig");
            Add(map, "fantastic", "fantastisch");
            Add(map, "so", "so");
            Add(map, "amazing", "erstaunlich");
            Add(map, "important", "wichtig");
            Add(map, "good", "gut");
            Add(map, "nice", "nett");
            Add(map, "you", "du", "sie");
            Add(map, "your", "dein", "ihr");
            Add(map, "yours", "deins", "ihres");
            Add(map, "i", "ich");
            Add(map, "i'm", "ich bin");
            Add(map, "me", "mich");
            Add(map, "my", "mein");
            Add(map, "mine", "meins");
            Add(map, "is", "ist");
            Add(map, "are", "sind", "seid");
            Add(map, "were", "waren");
            Add(map, "was", "war");
            Add(map, "have been", "sind gewesen", "ist gewesen");
            Add(map, "has been", "ist gewesen");
            Add(map, "at", "bei");
            Add(map, "stands", "steht");
            Add(map, "come", "kommen");
            Add(map, "like", "wie", "mogen");
            Add(map, "no", "nein");
            Add(map, "not", "nicht");
            Add(map, "nothing", "nichts");
            Add(map, "cannot", "kann nicht");
            Add(map, "can't", "kann nicht");
            Add(map, "cant", "kann nicht");
            Add(map, "don't", "nicht");
            Add(map, "dont", "nicht");
            Add(map, "didn't", "tat nicht");
            Add(map, "couldn't", "konnte nicht");
            Add(map, "couldnt", "konnte nicht");
            Add(map, "isn't", "ist nicht");
            Add(map, "isnt", "ist nicht");
            Add(map, "wasn't", "war nicht");
            Add(map, "wasnt", "war nicht");
            Add(map, "but", "aber");
            Add(map, "however", "jedoch");
            Add(map, "maybe", "vielleicht");
            Add(map, "perhaps", "vielleicht");
            Add(map, "why", "warum");
            Add(map, "where", "wo");
            Add(map, "who", "wer");
            Add(map, "how", "wie");
            Add(map, "when", "wann");
            Add(map, "there", "dort");
            Add(map, "here", "hier");
            Add(map, "must", "muss");
            Add(map, "have to", "mussen");
            Add(map, "terrain", "gelande");
            Add(map, "platform", "plattform");
            Add(map, "platforms", "plattformen");
            Add(map, "sensing", "erfassung");
            Add(map, "recognition", "erkennung");
            Add(map, "processing", "verarbeitung");
            Add(map, "generation", "generierung");
            Add(map, "real-time", "echtzeit");
            Add(map, "github", "github");
            Add(map, "edu", "edu");
            return map;
        }

        private static Dictionary<string, string[]> BuildItalianMap()
        {
            var map = CreateBaseMap();
            Add(map, "yes", "si");
            Add(map, "yeah", "si");
            Add(map, "plenty", "molto");
            Add(map, "fully", "pienamente");
            Add(map, "completely", "completamente");
            Add(map, "really", "davvero");
            Add(map, "very", "molto");
            Add(map, "quite", "abbastanza");
            Add(map, "wonderful", "meraviglioso");
            Add(map, "great", "ottimo", "fantastico");
            Add(map, "absolutely", "assolutamente");
            Add(map, "huge", "enorme");
            Add(map, "fantastic", "fantastico");
            Add(map, "so", "cosi");
            Add(map, "amazing", "incredibile");
            Add(map, "important", "importante");
            Add(map, "good", "buono");
            Add(map, "nice", "bello");
            Add(map, "you", "tu", "voi");
            Add(map, "your", "tuo", "vostro");
            Add(map, "yours", "tuo", "vostro");
            Add(map, "i", "io");
            Add(map, "i'm", "sono");
            Add(map, "me", "me");
            Add(map, "my", "mio");
            Add(map, "mine", "mio");
            Add(map, "is", "e");
            Add(map, "are", "sono", "siete");
            Add(map, "were", "erano");
            Add(map, "was", "era");
            Add(map, "have been", "sono stati", "e stato");
            Add(map, "has been", "e stato");
            Add(map, "at", "a");
            Add(map, "stands", "sta");
            Add(map, "come", "venire");
            Add(map, "like", "come", "piacere");
            Add(map, "no", "no");
            Add(map, "not", "non");
            Add(map, "nothing", "niente");
            Add(map, "cannot", "non puo");
            Add(map, "can't", "non puo");
            Add(map, "cant", "non puo");
            Add(map, "don't", "non");
            Add(map, "dont", "non");
            Add(map, "didn't", "non ha");
            Add(map, "couldn't", "non poteva");
            Add(map, "couldnt", "non poteva");
            Add(map, "isn't", "non e");
            Add(map, "isnt", "non e");
            Add(map, "wasn't", "non era");
            Add(map, "wasnt", "non era");
            Add(map, "but", "ma");
            Add(map, "however", "tuttavia");
            Add(map, "maybe", "forse");
            Add(map, "perhaps", "forse");
            Add(map, "why", "perche");
            Add(map, "where", "dove");
            Add(map, "who", "chi");
            Add(map, "how", "come");
            Add(map, "when", "quando");
            Add(map, "there", "li", "la");
            Add(map, "here", "qui");
            Add(map, "must", "deve");
            Add(map, "have to", "dovere");
            Add(map, "terrain", "terreno");
            Add(map, "platform", "piattaforma");
            Add(map, "platforms", "piattaforme");
            Add(map, "sensing", "rilevamento");
            Add(map, "recognition", "riconoscimento");
            Add(map, "processing", "elaborazione");
            Add(map, "generation", "generazione");
            Add(map, "real-time", "tempo reale");
            Add(map, "github", "github");
            Add(map, "edu", "edu");
            return map;
        }

        private static Dictionary<string, string[]> BuildPortugueseMap()
        {
            var map = CreateBaseMap();
            Add(map, "yes", "sim");
            Add(map, "yeah", "sim");
            Add(map, "plenty", "muito");
            Add(map, "fully", "totalmente");
            Add(map, "completely", "completamente");
            Add(map, "really", "realmente");
            Add(map, "very", "muito");
            Add(map, "quite", "bastante");
            Add(map, "wonderful", "maravilhoso");
            Add(map, "great", "otimo", "excelente");
            Add(map, "absolutely", "absolutamente");
            Add(map, "huge", "enorme");
            Add(map, "fantastic", "fantastico");
            Add(map, "so", "tao");
            Add(map, "amazing", "incrivel");
            Add(map, "important", "importante");
            Add(map, "good", "bom");
            Add(map, "nice", "agradavel");
            Add(map, "you", "voce", "tu");
            Add(map, "your", "seu", "teu");
            Add(map, "yours", "seu", "teu");
            Add(map, "i", "eu");
            Add(map, "i'm", "sou", "estou");
            Add(map, "me", "me");
            Add(map, "my", "meu");
            Add(map, "mine", "meu");
            Add(map, "is", "e");
            Add(map, "are", "sao", "esta");
            Add(map, "were", "eram");
            Add(map, "was", "era");
            Add(map, "have been", "tem sido", "foram");
            Add(map, "has been", "tem sido");
            Add(map, "at", "em");
            Add(map, "stands", "fica", "esta");
            Add(map, "come", "vir");
            Add(map, "like", "como", "gostar");
            Add(map, "no", "nao");
            Add(map, "not", "nao");
            Add(map, "nothing", "nada");
            Add(map, "cannot", "nao pode");
            Add(map, "can't", "nao pode");
            Add(map, "cant", "nao pode");
            Add(map, "don't", "nao");
            Add(map, "dont", "nao");
            Add(map, "didn't", "nao fez");
            Add(map, "couldn't", "nao podia");
            Add(map, "couldnt", "nao podia");
            Add(map, "isn't", "nao e");
            Add(map, "isnt", "nao e");
            Add(map, "wasn't", "nao era");
            Add(map, "wasnt", "nao era");
            Add(map, "but", "mas");
            Add(map, "however", "no entanto");
            Add(map, "maybe", "talvez");
            Add(map, "perhaps", "talvez");
            Add(map, "why", "por que");
            Add(map, "where", "onde");
            Add(map, "who", "quem");
            Add(map, "how", "como");
            Add(map, "when", "quando");
            Add(map, "there", "la");
            Add(map, "here", "aqui");
            Add(map, "must", "deve");
            Add(map, "have to", "ter que");
            Add(map, "terrain", "terreno");
            Add(map, "platform", "plataforma");
            Add(map, "platforms", "plataformas");
            Add(map, "sensing", "deteccao");
            Add(map, "recognition", "reconhecimento");
            Add(map, "processing", "processamento");
            Add(map, "generation", "geracao");
            Add(map, "real-time", "tempo real");
            Add(map, "github", "github");
            Add(map, "edu", "edu");
            return map;
        }
    }
}
