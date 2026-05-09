#nullable disable

using BllipParser;
using Microsoft.Extensions.Logging;
using NonverbalBehaviorGenerator.LegacyInterop;
using NonverbalBehaviorGenerator.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Text;

#if NATIVE_BLLIP_PARSER
using BllipParser.Native;
#else
using BllipParser.DotNet;
#endif

namespace NonverbalBehaviorGenerator.Legacy
{
    internal sealed class SpeakerBehaviorManager
    {
        #region Parser Initialization
        private static readonly object _initLock = new object();
        private static string modelDirectory;

#if NATIVE_BLLIP_PARSER
        private static IParser parser = new NativeParser();
#else
        private static IParser parser = new DotNetParser();
#endif

        public static void Initialize(string modelDirectory, Dictionary<string, Stream> streams)
        {
            lock (_initLock)
            {
                if (SpeakerBehaviorManager.modelDirectory != null)
                {
                    if (SpeakerBehaviorManager.modelDirectory == modelDirectory)
                    {
                        return;
                    }
                    else
                    {
                        throw new InvalidOperationException("Parser model directory should be the same");
                    }
                }
                var parserConf = new ParserConfiguration()
                {
                    ModelDirectory = modelDirectory,
                    Untokenized = true,
                    OverParsingLevel = 40,
                };

                parser.InitializeAsync(parserConf, streams).Wait();
                SpeakerBehaviorManager.modelDirectory = modelDirectory; 
            }
        }
        #endregion

        private readonly ILogger _logger;

        private XmlDocument m_inputDoc;
        private bool m_fmlBml = false;

        //The ssml hashtable which contains words to words-with-ssml-tag-associations
        Dictionary<string, List<WordProcessed>> m_ssmlWords;

        //class for saving whether the specific utterance of word has been processed
        public class WordProcessed
        {
            public string word;
            public bool processed = false;
        }

        List<string> m_parseTreeBuffer;
        private IContext _context;
        public static string parserResultString;
        public const int parseWaitTime = 3;
        private int m_totalTimeMarkers = 0;
        List<string> m_processedSentences;
        private int m_fmlBmlTagCount;
        private string m_completeUtterance;
        //private VHMessage m_currentMessage;
        private XmlNode m_bmlNode;
        private bool m_useExpressionsDatabase = false;

        // current available face express type
        enum faceExpressType
        {
            joy = 0,
            fear,
            anger,
            anxiety,
            surprise,
            distress,
            shame,
            pride,
            hope,
            num
        }

        // this stores the <au, amount> pairs of needed sbm face commands
        // for each type of face expression
        // the first dimension is the face expression type
        // the second dimenstion is a list of <au, amount> pairs for compositing that face expression
        //private List<List<KeyValuePair<int, float>>> m_faceExpressDatabase;

        private Dictionary<string, List<KeyValuePair<int, float>>> m_faceExpressDatabase;

        /// <summary>
        /// constructor
        /// </summary>
        public SpeakerBehaviorManager(ILogger logger, IContext context)
        {
            if (modelDirectory is null)
            {
                throw new InvalidOperationException("Call SpeakerBehaviorManager.Initialize() first");
            }

            _logger = logger;

            _context = context;
            m_fmlBml = false;
            m_ssmlWords = new Dictionary<string, List<WordProcessed>>();
            m_parseTreeBuffer = new List<string>();
            parserResultString = "";
            m_totalTimeMarkers = 0;
            m_fmlBmlTagCount = 0;
            m_processedSentences = new List<string>();
            m_completeUtterance = "";

            
        }

        /// <summary>
        /// Process the dialog message.
        /// Gets the input sentence and sends it out to the parser to get parse tree.
        /// Creates rules for generating behavior and also processes fml-bml tags.
        /// </summary>
        /// <param name="_inputDoc"></param>
        /// <param name="_data"></param>
        /// <param name="_currentMessage"></param>
        public async Task ProcessDialogMessageAsync(XmlDocument _inputDoc, IContext _data, LegacyRequest _currentMessage, XmlNode _bmlNode)
        {
            if (m_faceExpressDatabase is null) {

                //The following code was moved here from the original ctor.
                XmlDocument face_express_xml = new XmlDocument();
                m_faceExpressDatabase = new Dictionary<string, List<KeyValuePair<int, float>>>();
                if (await _context.GetHasFacialExpressionXmlAsync())
                {
                    try
                    {
                        var facialExpressionXml = await _context.GetFacialExpressionXmlAsync();
                        face_express_xml.LoadXml(facialExpressionXml);

                        XmlNode expressions = face_express_xml.SelectNodes("expressions")[0];

                        XmlNodeList characters = expressions.ChildNodes;

                        for (int i = 0; i < characters.Count; ++i)
                        {
                            if (characters[i].Name.Equals(await _context.AgentInfo.GetCharacterIdAsync()))
                            {
                                XmlNodeList emotions = characters[i].ChildNodes;

                                for (int j = 0; j < emotions.Count; ++j)
                                {
                                    string emotionName = emotions[j].Name;

                                    m_faceExpressDatabase.Add(emotionName, new List<KeyValuePair<int, float>>()); ;

                                    XmlNodeList facs = emotions[j].ChildNodes;

                                    for (int k = 0; k < facs.Count; ++k)
                                    {
                                        XmlNode facNode = facs[k];
                                        string au = facNode.Attributes["au"].InnerText;
                                        string amount = facNode.Attributes["amount"].InnerText;
                                        m_faceExpressDatabase[emotionName].Add(new KeyValuePair<int, float>(Convert.ToInt32(au), (float)Convert.ToDouble(amount)));
                                    }
                                }
                            }
                        }

                        _logger?.LogInformation("Expressions file loaded successfully");
                        m_useExpressionsDatabase = true;
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "Error loading Expressions file");
                        _logger?.LogInformation("Proceeding without expressions file");
                    }
                }
            }

            _context = _data;
            m_inputDoc = _inputDoc;
            m_fmlBml = false;
            //m_currentMessage = _currentMessage;
            m_bmlNode = _bmlNode;
            bool m_faceExpress = false;

            if (m_inputDoc.GetElementsByTagName("fml-bml").Count > 0)
            {
                _logger?.LogInformation("<fml-bml> tag detected in input text");
                m_fmlBml = true;
            }

            // check if there are face expression tags
            if (m_inputDoc.GetElementsByTagName("face-express").Count > 0)
            {
                _logger?.LogInformation("<face-express tag detected in input text>");
                m_faceExpress = true;
            }

            int numberOfSpeechTags = m_inputDoc.GetElementsByTagName("speech").Count;

            for (int i = 0; i < numberOfSpeechTags; ++i)
            {
                XmlNode speechTag = m_inputDoc.GetElementsByTagName("speech")[i];
                string sentence = speechTag.InnerText;

                //This is to check if the innerxml and innertext are same, if they are not, that means this speech tag contains ssml tags
                //They are processed, added to the hashtable and then later on used when the results return from the parser.
                if (!sentence.Equals(speechTag.InnerXml))
                {
                    string[] separator = { " " };
                    string[] words = sentence.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    string[] ssmlWords = speechTag.InnerXml.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                    for (int numWords = 0, numSSMLWords = 0; true;)
                    {
                        if (numWords < words.Length && numSSMLWords < ssmlWords.Length)
                        {
                            if (words[numWords].Equals(ssmlWords[numSSMLWords]))
                            {
                                ++numWords;
                                ++numSSMLWords;
                                continue;
                            }
                            else if (String.IsNullOrEmpty(words[numWords]))
                            {
                                if (numWords + 1 < words.Length)
                                {
                                    ++numWords;
                                    continue;
                                }
                                else
                                {
                                    if (!ssmlWords[numSSMLWords].Contains("<") && numSSMLWords + 1 < ssmlWords.Length)
                                    {
                                        ++numSSMLWords;
                                    }
                                }
                            }

                            //first let us find the part before the word
                            if (ssmlWords[numSSMLWords].Contains("<"))
                            {
                                //int startIndex = numSSMLWords;
                                string ssmlString = ssmlWords[numSSMLWords];
                                for (int numSSL = numSSMLWords + 1; numSSL < ssmlWords.Length; ++numSSL)
                                {
                                    ssmlString += " " + ssmlWords[numSSL];
                                    /*if (ssmlWords[numSSL].Contains("</"))
                                    {
                                        numSSMLWords = numSSL;
                                        break;
                                    }
                                    else if (ssmlWords[numSSL].Contains("/>"))
                                    {
                                        numSSMLWords = numSSL;
                                        break;
                                    }*/
                                    if (ssmlWords[numSSL] == words[numWords])
                                    {
                                        numSSMLWords = numSSL;
                                        break;
                                    }
                                }

                                if (ssmlString.StartsWith("<"))
                                {
                                    if (!string.IsNullOrEmpty(words[numWords]))
                                    {
                                        string wordToBeReplaced = words[numWords];
                                        wordToBeReplaced = wordToBeReplaced.Replace(".", "");
                                        wordToBeReplaced = wordToBeReplaced.Replace("!", "");
                                        wordToBeReplaced = wordToBeReplaced.Replace(",", "");
                                        wordToBeReplaced = wordToBeReplaced.Replace("?", "");
                                        //Check if dictionary already contains the word 
                                        //If it does, we need to add a new entry to the list
                                        if (m_ssmlWords.ContainsKey(wordToBeReplaced))
                                        {
                                            WordProcessed wordProc = new WordProcessed();
                                            //Check if the ssml string contains the word, if it does, then do not add the word to the replacement string
                                            if (ssmlString.Contains(words[numWords]))
                                            {
                                                ////This means the text starts with ssml tag
                                                //if (startIndex == 0)
                                                //{
                                                //    //m_ssmlWords[wordToBeReplaced] = m_ssmlWords[wordToBeReplaced].Add(ssmlString);// +" " + m_ssmlWords[wordToBeReplaced];
                                                //    WordProcessed wordProc = new WordProcessed();
                                                //    wordProc.word = ssmlString;
                                                //    m_ssmlWords[wordToBeReplaced].Add(wordProc);
                                                //}
                                                ////This means the text is not starting with ssml tag
                                                //else
                                                //{
                                                //m_ssmlWords[wordToBeReplaced] = m_ssmlWords[wordToBeReplaced] + " " + ssmlString;

                                                wordProc.word = ssmlString;
                                                m_ssmlWords[wordToBeReplaced].Add(wordProc);
                                                //}

                                            }
                                            //If the word is not present in the ssml string, add it to the end
                                            else
                                            {
                                                //m_ssmlWords[wordToBeReplaced] = ssmlString + " " + words[numWords];
                                                wordProc.word = ssmlString + " " + words[numWords];
                                                m_ssmlWords[wordToBeReplaced].Add(wordProc);
                                            }
                                        }
                                        //Same as above except in this case, the dictionary is being added to rather than replaced
                                        else
                                        {
                                            List<WordProcessed> replacementWords = new List<WordProcessed>();
                                            WordProcessed wordProc = new WordProcessed();
                                            replacementWords.Add(wordProc);
                                            if (ssmlString.Contains(words[numWords]))
                                            {
                                                //replacementWords.Add(ssmlString);
                                                wordProc.word = ssmlString;
                                                m_ssmlWords.Add(wordToBeReplaced, replacementWords);
                                                //m_ssmlWords.Add(wordToBeReplaced, ssmlString);
                                            }
                                            else
                                            {
                                                //replacementWords.Add(ssmlString + " " + words[numWords]);
                                                wordProc.word = ssmlString + " " + words[numWords];
                                                m_ssmlWords.Add(wordToBeReplaced, replacementWords);
                                                //m_ssmlWords.Add(wordToBeReplaced, ssmlString + " " + words[numWords]);
                                            }
                                        }
                                    }
                                }
                                else
                                {

                                    //assming it contains the ssml tag without space
                                    if (m_ssmlWords.ContainsKey(words[numWords]))
                                    {
                                        WordProcessed wordProc = new WordProcessed();
                                        wordProc.word = ssmlString;
                                        //m_ssmlWords[words[numWords]] = ssmlString;
                                        m_ssmlWords[words[numWords]].Add(wordProc);
                                    }
                                    else
                                    {
                                        //m_ssmlWords.Add(words[numWords], ssmlString);
                                        List<WordProcessed> replacementWords = new List<WordProcessed>();
                                        WordProcessed wordProc = new WordProcessed();
                                        wordProc.word = ssmlString;
                                        replacementWords.Add(wordProc);
                                        m_ssmlWords.Add(words[numWords], replacementWords);
                                    }
                                }
                            }

                            ++numWords;
                            ++numSSMLWords;
                        }
                        //This means there is a tag at the end
                        else if (numSSMLWords < ssmlWords.Length)
                        {
                            //first let us find the part before the word
                            if (ssmlWords[numSSMLWords].Contains("<"))
                            {
                                if (!string.IsNullOrEmpty(words[words.Length - 1]))
                                {
                                    string ssmlString = ssmlWords[numSSMLWords];
                                    for (++numSSMLWords; numSSMLWords < ssmlWords.Length; ++numSSMLWords)
                                    {
                                        ssmlString += " " + ssmlWords[numSSMLWords];
                                    }

                                    string wordToBeReplaced = words[words.Length - 1];
                                    wordToBeReplaced = wordToBeReplaced.Replace(".", "");
                                    wordToBeReplaced = wordToBeReplaced.Replace("!", "");
                                    wordToBeReplaced = wordToBeReplaced.Replace(",", "");
                                    wordToBeReplaced = wordToBeReplaced.Replace("?", "");

                                    //Check if hashtable already contains the word 
                                    if (m_ssmlWords.ContainsKey(wordToBeReplaced) && m_ssmlWords[wordToBeReplaced].Count > 0)
                                    {
                                        //m_ssmlWords[wordToBeReplaced] = m_ssmlWords[wordToBeReplaced] + " " + ssmlString;
                                        //string previousReplacementValue = m_ssmlWords[wordToBeReplaced][m_ssmlWords[wordToBeReplaced].Count - 1];
                                        m_ssmlWords[wordToBeReplaced][m_ssmlWords.Count - 1].word += " " + ssmlString;
                                    }
                                    else
                                    {
                                        List<WordProcessed> replacementWords = new List<WordProcessed>();
                                        WordProcessed wordProc = new WordProcessed();
                                        wordProc.word = words[words.Length - 1] + " " + ssmlString;
                                        replacementWords.Add(wordProc);
                                        //m_ssmlWords.Add(wordToBeReplaced, words[words.Length - 1] + " " + ssmlString);
                                        m_ssmlWords.Add(wordToBeReplaced, replacementWords);
                                    }
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                ////Refactor for performance and fix bugs. I do not know why these characters need to be replaced.
                //sentence = Regex.Replace(sentence, "[\"\\t\\r\\n\\0]", "");
                sentence = SanitizeForParser(sentence);

                //var matches = Regex.Matches(sentence, @"\S.*?(\S*(?=\s*?$)|!(?=\s)|\?(?=\s)|\.(?=\s))");
                //foreach (var match in matches.Cast<Match>()) {
                //    GetParseTree(match.Value);
                //}
                foreach (string part in SplitIntoParserSentences(sentence))
                    GetParseTree(part);

                m_totalTimeMarkers = CreatePositionTags(i);
                CacheParseTree();
                m_processedSentences.Clear();
                m_parseTreeBuffer.Clear();
            }

            if (m_fmlBml)
                CreateFMLBMLTimeMarks();

            // add face expression if needed
            if (m_faceExpress && m_useExpressionsDatabase)
                await AddFaceExpressionAsync();

            if (await _context.Switch.GetAllBehaviorAsync()) {
                await AttachRuleTagsAsync();
            }

            XmlNode actTag = m_inputDoc.GetElementsByTagName("act")[0];
            while (m_inputDoc.ContainsAnyElementNamedAs("marked_sentence"))
            {
                actTag.RemoveChild(m_inputDoc.GetElementsByTagName("marked_sentence")[0]);
            }



            //Add a gaze command to make the speaker look at the addressee
            if ((m_inputDoc.GetElementsByTagName("gaze").Count == 0) &&
                (await _context.Switch.GetAllBehaviorAsync()) &&
                (await _context.Switch.GetSpeakerGazeAsync()))
            {
                XmlNode gazeTag = m_inputDoc.CreateElement("gaze");
                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, gazeTag, "participant", await _context.AgentInfo.GetCharacterIdAsync());
                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, gazeTag, "target", _currentMessage.TargetId);
                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, gazeTag, "direction", "POLAR 0");
                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, gazeTag, "angle", "0");
                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, gazeTag, "start", "sp1:T0");
                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, gazeTag, "sbm:joint-range", "HEAD EYES");
                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, gazeTag, "xmlns:sbm", "http://ict.usc.edu");
                m_inputDoc.GetElementsByTagName("bml")[0].AppendChild(gazeTag);

                await _context.GazeInfo.SetGazeAsync(_currentMessage.TargetId, "look", "1");
            }
        }

        private static string SanitizeForParser(string s)
        {
            // this version only creates the StringBuilder if it has to replace something, for perf

            if (string.IsNullOrEmpty(s))
                return s;

            StringBuilder sb = null;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                bool remove =
                    c == '"' ||
                    c == '\t' ||
                    c == '\r' ||
                    c == '\n' ||
                    c == '\0';

                if (remove)
                {
                    if (sb == null)
                    {
                        sb = new StringBuilder(s.Length);
                        if (i > 0)
                            sb.Append(s, 0, i);
                    }

                    // Skip removed char.
                    continue;
                }

                // Keep char.
                sb?.Append(c);
            }

            return sb == null ? s : sb.ToString();
        }

        private static IEnumerable<string> SplitIntoParserSentences(string s)
        {
            // attempts to split the input into sentences.
            // doesn't do well for things like 'Dr.' or '...' or other types of punctuation

            if (string.IsNullOrWhiteSpace(s))
                yield break;

            int start = -1;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (start < 0)
                {
                    if (!char.IsWhiteSpace(c))
                        start = i;

                    continue;
                }

                bool isTerminator = c == '.' || c == '!' || c == '?';
                if (!isTerminator)
                    continue;

                int j = i + 1;
                while (j < s.Length && char.IsWhiteSpace(s[j]))
                    j++;

                // Match the old regex intent: terminator followed by whitespace or end.
                bool isBoundary = j >= s.Length || j > i + 1;
                if (!isBoundary)
                    continue;

                string chunk = s.Substring(start, (i + 1) - start).Trim();
                if (chunk.Length > 0)
                    yield return chunk;

                start = -1;
                i = j - 1; // continue scanning after whitespace
            }

            if (start >= 0)
            {
                string tail = s.Substring(start).Trim();
                if (tail.Length > 0)
                    yield return tail;
            }
        }

        /// <summary>
        /// Get the parse tree for the current sentence if it is not cached
        /// </summary>
        /// <param name="_currentSentence"></param>
        private void GetParseTree(string _currentSentence)
        {
            var sentence = _currentSentence.Trim();
            if (_context.ParseTreeCache != null && _context.ParseTreeCache.TryGetValue(sentence, out var cachedValue))
            {
                _logger?.LogInformation("String present in cache table. Skipping message to parser and using cached parse tree instead.");
                m_parseTreeBuffer.Add(cachedValue);
                return;
            }
            var request = new ParseRequest(sentence);
            var response = parser.ParseAsync(request).Result;
            var result = response.Candidates.FirstOrDefault()?.Tree?.ToString()?.Trim();
            if (result == null)
            {
                _logger?.LogWarning("No valid value returned from parser.");
                result = $"(NONE {sentence} )";
            }
            m_parseTreeBuffer.Add(result);
        }

        /// <summary>
        /// Cache the obtained parse tree for further use
        /// </summary>
        private void CacheParseTree()
        {
            if (_context.ParseTreeCache is null)
            {
                return;
            }
            if (m_processedSentences.Count != m_parseTreeBuffer.Count)
            {
                _logger?.LogError("Error: processed sentences count not equal to parsetree buffer count.");
                return;
            }
            else
            {
                for (int p = 0; p < m_processedSentences.Count; ++p)
                {
                    if (m_parseTreeBuffer[p].StartsWith("(NONE"))
                    {
                        continue;
                    }
                    var sentence = m_processedSentences[p].Trim();
                    if (!_context.ParseTreeCache.ContainsKey(sentence))
                    {
                        _context.ParseTreeCache[sentence] = m_parseTreeBuffer[p];
                    }
                }
            }
        }

        /// <summary>
        /// Generate timemark tags which specify where exactly the fml-bml tag occurs 
        /// in the input sentence. i.e. after which word node.       
        /// </summary>
        private void CreateFMLBMLTimeMarks()
        {
            string speechText;

            for (int i = 0; i < m_inputDoc.GetElementsByTagName("speech").Count; ++i)
            {
                speechText = m_inputDoc.GetElementsByTagName("speech")[i].InnerXml;
                speechText.Trim();

                string[] splitWords = Regex.Split(speechText, "[*<*>*]");
                if (splitWords.Length > 1)
                {
                    for (int j = 0; j < splitWords.Length; ++j)
                    {
                        string currentSplit = splitWords[j];
                        if (currentSplit.Contains("fml-bml"))
                        {
                            char[] delimiters = new char[] { ' ' };
                            if (splitWords.Length > j + 1)
                            {
                                string[] words = splitWords[j + 1].Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                                if (words.Length != 0)
                                {
                                    string wordBeforeFml = words[0];
                                    SearchTimeMarkers(wordBeforeFml);
                                }
                            }
                        }
                    }
                }
                m_fmlBmlTagCount = 0;
            }
        }

        // add face expressions
        private async Task AddFaceExpressionAsync()
        {
            string speechText;

            for (int i = 0; i < m_inputDoc.GetElementsByTagName("face-express").Count; ++i)
            {
                string type = m_inputDoc.GetElementsByTagName("face-express")[i].Attributes["type"].Value;

                speechText = m_inputDoc.GetElementsByTagName("face-express")[i].InnerXml;
                speechText.Trim();
                char[] delimiters = new char[] { ' ' };
                string[] words = speechText.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length != 0)
                {
                    string wordBeforeFaceExpress = words[0];
                    string wordAfterFaceExpress = words[words.Length - 1];
                    string readyMarker = "";
                    string relaxMarker = "";
                    // create face commands for the tagged part of sentence
                    if (GetTimeMarker(wordBeforeFaceExpress, ref readyMarker)
                        && GetTimeMarker(wordAfterFaceExpress, ref relaxMarker))
                    {
                        for (int j = 0; j < m_faceExpressDatabase[type].Count; ++j)
                        {
                            await CreateFaceCommandAsync(m_faceExpressDatabase[type][j].Key, m_faceExpressDatabase[type][j].Value, 1, readyMarker, relaxMarker);
                        }
                    }
                }
            }
        }

        // create and attach a face command
        private async Task CreateFaceCommandAsync(int _au, float _amount, int _priority, string _ready, string _relax)
        {
            XmlNode eventTag = m_inputDoc.CreateElement("face");
            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, eventTag, "au", _au.ToString());
            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, eventTag, "participant", await _context.AgentInfo.GetCharacterIdAsync());
            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, eventTag, "amount", _amount.ToString());
            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, eventTag, "priority", _priority.ToString());
            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, eventTag, "type", "facs");
            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, eventTag, "ready", "sp1:" + _ready);
            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, eventTag, "relax", "sp1:" + _relax);
            m_bmlNode.AppendChild(eventTag);
        }

        /// <summary>
        /// Look for the word-node that matches the input word and get it's timemark
        /// </summary>
        /// <param name="_currentWord"></param>
        private void SearchTimeMarkers(string _currentWord)
        {
            for (int i = 0; i < m_inputDoc.GetElementsByTagName("text").Count; ++i)
            {
                XmlNode textNode = m_inputDoc.GetElementsByTagName("text")[i];
                if (textNode.Attributes["content"].Value.Equals(_currentWord))
                {
                    XmlAttribute timeAttribute = m_inputDoc.CreateAttribute("timemark");
                    timeAttribute.Value = textNode.Attributes["timemark"].Value;
                    m_inputDoc.GetElementsByTagName("fml-bml")[m_fmlBmlTagCount].Attributes.Append(timeAttribute);
                    m_fmlBmlTagCount++;
                }
            }
        }

        // get time marker for that word happen in the sentence
        private bool GetTimeMarker(string _currentWord, ref string _timeMaker)
        {
            for (int i = 0; i < m_inputDoc.GetElementsByTagName("text").Count; ++i)
            {
                XmlNode textNode = m_inputDoc.GetElementsByTagName("text")[i];
                if (textNode.Attributes["content"].Value.Equals(_currentWord))
                {
                    // there will be bug if the same word is mentioned twice
                    _timeMaker = textNode.Attributes["timemark"].Value;
                    return true;
                }
            }
            return false;
        }


        private readonly struct RuleInfo
        {
            public readonly string Type;
            public readonly string Priority;

            public RuleInfo(string type, string priority)
            {
                Type = type;
                Priority = priority;
            }
        }

        private readonly struct PawnTriggerInfo
        {
            public readonly string PawnName;
            public readonly string Priority;

            public PawnTriggerInfo(string pawnName, string priority)
            {
                PawnName = pawnName;
                Priority = priority;
            }
        }

        private static string NormalizeTokenForRuleMatch(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            // Keep behavior consistent with the legacy code: remove a small set of punctuation marks.
            const string Strip = ".?!,";
            bool needsStrip = false;

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (Strip.IndexOf(c) >= 0)
                {
                    needsStrip = true;
                    break;
                }
            }

            if (!needsStrip)
                return raw;

            char[] buffer = new char[raw.Length];
            int w = 0;

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (Strip.IndexOf(c) >= 0)
                    continue;

                buffer[w++] = c;
            }

            return w == 0 ? string.Empty : new string(buffer, 0, w);
        }

        /// <summary>
        /// Generate rules based on the parser's result and attach them to the bml
        /// </summary>
        private async Task AttachRuleTagsAsync()
        {
            try
            {
                XmlDocument ruleDocument = await _context.GetBehaviorXmlDocumentAsync();

                // Cache switches and context values (avoid awaits inside tight loops).
                bool doPoseRules = await _context.Switch.GetPoseRulesAsync();
                bool doGestures = await _context.Switch.GetSpeakerGesturesAsync();
                bool doGaze = await _context.Switch.GetSpeakerGazeAsync();

                string speakerId = await _context.AgentInfo.GetCharacterIdAsync();
                string postureId = await _context.AgentInfo.GetPostureIdAsync();
                string emotion = await _context.AgentInfo.GetEmotionAsync();
                string listenerId = await _context.CurrentDialogue.GetListenerAsync();

                // Build lookup tables once.
                XmlNodeList patternNodes = ruleDocument.GetElementsByTagName("pattern");
                Dictionary<string, RuleInfo> poseRuleMap = new Dictionary<string, RuleInfo>(StringComparer.Ordinal);
                Dictionary<string, RuleInfo> wordRuleMap = new Dictionary<string, RuleInfo>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < patternNodes.Count; i++)
                {
                    XmlNode pattern = patternNodes[i];
                    XmlNode ruleNode = pattern.ParentNode;
                    if (ruleNode == null || ruleNode.Attributes == null)
                        continue;

                    XmlAttribute keywordAttr = ruleNode.Attributes["keyword"];
                    XmlAttribute priorityAttr = ruleNode.Attributes["priority"];
                    if (keywordAttr == null || priorityAttr == null)
                        continue;

                    string key = pattern.InnerText;
                    RuleInfo info = new RuleInfo(keywordAttr.Value, priorityAttr.Value);

                    // Preserve "first match wins" behavior.
                    if (!poseRuleMap.ContainsKey(key))
                        poseRuleMap.Add(key, info);

                    if (!wordRuleMap.ContainsKey(key))
                        wordRuleMap.Add(key, info);
                }

                XmlNodeList pawnTriggerNodes = ruleDocument.GetElementsByTagName("pawn_trigger");
                Dictionary<string, PawnTriggerInfo> pawnTriggerMap = new Dictionary<string, PawnTriggerInfo>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < pawnTriggerNodes.Count; i++)
                {
                    XmlNode trigger = pawnTriggerNodes[i];
                    XmlNode triggerParent = trigger.ParentNode;
                    if (triggerParent == null || triggerParent.Attributes == null)
                        continue;

                    XmlAttribute pawnNameAttr = triggerParent.Attributes["pawn_name"];
                    XmlAttribute priorityAttr = triggerParent.Attributes["priority"];
                    if (pawnNameAttr == null || priorityAttr == null)
                        continue;

                    string key = trigger.InnerText;
                    if (!pawnTriggerMap.ContainsKey(key))
                        pawnTriggerMap.Add(key, new PawnTriggerInfo(pawnNameAttr.Value, priorityAttr.Value));
                }

                // Materialize XmlNodeLists to avoid expensive XmlNodeList indexing in loops.
                XmlNodeList posNodesList = m_inputDoc.GetElementsByTagName("POS");
                XmlNode[] posNodes = new XmlNode[posNodesList.Count];
                for (int i = 0; i < posNodesList.Count; i++)
                    posNodes[i] = posNodesList[i];

                XmlNodeList markListNodeList = m_inputDoc.GetElementsByTagName("mark");
                XmlNode[] markNodes = new XmlNode[markListNodeList.Count];
                for (int i = 0; i < markListNodeList.Count; i++)
                    markNodes[i] = markListNodeList[i];

                // Rules applied to the parse tree result.
                if (doPoseRules)
                {
                    for (int i = 0; i < posNodes.Length; i++)
                    {
                        XmlNode currentNode = posNodes[i];
                        XmlNode parentNode = currentNode.ParentNode;
                        if (parentNode == null)
                            continue;

                        XmlAttribute tagAttr = currentNode.Attributes?["tag"];
                        if (tagAttr == null)
                            continue;

                        string positionTag = tagAttr.Value;

                        if (!poseRuleMap.TryGetValue(positionTag, out RuleInfo info))
                            continue;

                        XmlNode docRuleNode = m_inputDoc.CreateElement("rule");
                        XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "participant", speakerId);
                        XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "type", info.Type);
                        XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "pose", postureId);
                        XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "emotion", emotion);
                        XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "priority", info.Priority);
                        XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "target", listenerId);
                        parentNode.InsertBefore(docRuleNode, currentNode);
                    }
                }

                // Rules applied to certain dialogue elements.
                for (int i = 0; i < markNodes.Length; i += 2)
                {
                    XmlNode currentMark = markNodes[i];
                    if (currentMark == null)
                        continue;

                    XmlNode wordNode = currentMark.NextSibling;
                    XmlNode parentNode = currentMark.ParentNode;
                    if (wordNode == null || parentNode == null)
                        continue;

                    string rawText;

                    // Prefer XmlText.Value over InnerText to avoid unnecessary traversal/concatenation work.
                    if (wordNode is XmlText xmlText)
                        rawText = xmlText.Value;
                    else
                        rawText = wordNode.InnerText;

                    if (string.IsNullOrEmpty(rawText))
                        continue;

                    string token = NormalizeTokenForRuleMatch(rawText);

                    if (doGestures)
                    {
                        if (wordRuleMap.TryGetValue(token, out RuleInfo info))
                        {
                            bool isQuestionWord =
                                string.Equals(token, "why", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(token, "what", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(token, "where", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(token, "who", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(token, "how", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(token, "when", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(token, "do", StringComparison.OrdinalIgnoreCase);

                            if (!(isQuestionWord && i != 0))
                            {
                                XmlNode docRuleNode = m_inputDoc.CreateElement("rule");
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "participant", speakerId);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "type", info.Type);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "pose", postureId);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "emotion", emotion);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "priority", info.Priority);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "target", listenerId);
                                parentNode.InsertBefore(docRuleNode, currentMark);
                            }
                        }
                    }

                    if (doGaze && pawnTriggerMap.Count > 0)
                    {
                        if (pawnTriggerMap.TryGetValue(token, out PawnTriggerInfo gazeInfo))
                        {
                            XmlNode docRuleNode = m_inputDoc.CreateElement("rule");
                            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "participant", speakerId);
                            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "type", "fmlbml_gaze");
                            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "pose", postureId);
                            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "emotion", emotion);
                            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "priority", gazeInfo.Priority);
                            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "prev_target", listenerId);
                            XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "target", gazeInfo.PawnName);
                            parentNode.InsertBefore(docRuleNode, currentMark);
                        }
                    }
                }

                // Check to see if multiple words/phrases occur and if so apply the rule.
                if (doGestures)
                {
                    await CheckForPhrasesAsync();
                }

                // Rules to attach gaze shift when there is an fml-bml tag.
                if (m_fmlBml && doGaze)
                {
                    XmlNodeList fmlBmlNodesList = m_inputDoc.GetElementsByTagName("fml-bml");
                    if (fmlBmlNodesList != null && fmlBmlNodesList.Count > 0)
                    {
                        // Map timemark -> list of (annotate,value)
                        Dictionary<string, List<KeyValuePair<string, string>>> fmlBmlMap =
                            new Dictionary<string, List<KeyValuePair<string, string>>>(StringComparer.Ordinal);

                        for (int i = 0; i < fmlBmlNodesList.Count; i++)
                        {
                            XmlNode node = fmlBmlNodesList[i];
                            if (node == null || node.Attributes == null)
                                continue;

                            XmlAttribute annotateAttr = node.Attributes["annotate"];
                            XmlAttribute valueAttr = node.Attributes["value"];
                            XmlAttribute timemarkAttr = node.Attributes["timemark"];
                            if (annotateAttr == null || valueAttr == null || timemarkAttr == null)
                                continue;

                            string timemark = timemarkAttr.Value;
                            if (!fmlBmlMap.TryGetValue(timemark, out List<KeyValuePair<string, string>> list))
                            {
                                list = new List<KeyValuePair<string, string>>(1);
                                fmlBmlMap.Add(timemark, list);
                            }

                            list.Add(new KeyValuePair<string, string>(annotateAttr.Value, valueAttr.Value));
                        }

                        for (int i = 0; i < markNodes.Length; i += 2)
                        {
                            XmlNode currentMark = markNodes[i];
                            XmlNode parentNode = currentMark?.ParentNode;
                            if (currentMark == null || parentNode == null)
                                continue;

                            string markTag = "T" + i;

                            if (!fmlBmlMap.TryGetValue(markTag, out List<KeyValuePair<string, string>> entries))
                                continue;

                            for (int j = 0; j < entries.Count; j++)
                            {
                                string annotateValue = entries[j].Key;
                                string targetValue = entries[j].Value;

                                if (string.Equals(annotateValue, "addressee", StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(targetValue, listenerId, StringComparison.Ordinal))
                                {
                                    XmlNode docRuleNode = m_inputDoc.CreateElement("rule");
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "participant", speakerId);
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "type", "fmlbml_gaze");
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "pose", postureId);
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "emotion", emotion);
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "priority", "0");
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "prev_target", listenerId);
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "target", targetValue);
                                    parentNode.InsertBefore(docRuleNode, currentMark);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogInformation("ERROR while attaching rule nodes to bml" + e.ToString());
            }
        }

        /// <summary>
        /// check to see if multiple words/phrases occur and if so apply the rule
        /// </summary>
        public async Task CheckForPhrasesAsync()
        {
            var ruleDocument = await _context.GetBehaviorXmlDocumentAsync();
            //XmlNodeList ruleNodes = ruleDocument.GetElementsByTagName("rule");
            XmlNodeList patterns = ruleDocument.GetElementsByTagName("pattern_multiple");


            for (int j = 0; j < patterns.Count; ++j)
            {
                string currentPattern = patterns[j].InnerText;

                if (m_completeUtterance.Contains(currentPattern.Trim()))
                {

                    // rules applied to certain dialogue elements
                    XmlNodeList markList = m_inputDoc.GetElementsByTagName("mark");
                    string text;


                    for (int i = 0; i < markList.Count; i += 2)
                    {
                        XmlNode currentMark = markList[i];
                        XmlNode wordNode = currentMark.NextSibling;
                        XmlNode parentNode = currentMark.ParentNode;
                        text = wordNode.InnerText;

                        if ((text[text.Length - 1].Equals('.')) ||
                        (text[text.Length - 1].Equals('!')) ||
                        (text[text.Length - 1].Equals('?')) ||
                        (text[text.Length - 1].Equals(',')))
                        {
                            text.Insert(text.Length - 1, "");
                        }


                        text = text.ToLower();
                        text = text.Replace(".", "");
                        text = text.Replace("?", "");
                        text = text.Replace("!", "");
                        text = text.Replace(",", "");

                        char[] delimiters = new char[] { ' ' };
                        string[] wordsInPattern = currentPattern.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                        bool match = false;
                        //XmlNode currentWord = wordNode;

                        if (text.Trim().Equals(wordsInPattern[0].Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            for (int counter = i + 2, wordCounter = 1; wordCounter < wordsInPattern.Length; counter += 2, ++wordCounter)
                            {
                                XmlNode nextWord = markList[counter].NextSibling;
                                string nextText = nextWord.InnerText;

                                if ((nextText[nextText.Length - 1].Equals('.')) ||
                                (nextText[nextText.Length - 1].Equals('!')) ||
                                (nextText[nextText.Length - 1].Equals('?')) ||
                                (nextText[nextText.Length - 1].Equals(',')))
                                {
                                    nextText.Insert(nextText.Length - 1, "");
                                }

                                nextText = nextText.ToLower();
                                nextText = nextText.Replace(".", "");
                                nextText = nextText.Replace("?", "");
                                nextText = nextText.Replace("!", "");
                                nextText = nextText.Replace(",", "");

                                if (!nextText.Trim().Equals(wordsInPattern[wordCounter], StringComparison.OrdinalIgnoreCase))
                                {
                                    match = false;
                                }
                                //currentWord = nextWord;
                            }

                            if (match)
                            {
                                string typeName = patterns[j].ParentNode.Attributes["keyword"].Value;
                                string priorityValue = patterns[j].ParentNode.Attributes["priority"].Value;

                                XmlNode docRuleNode = m_inputDoc.CreateElement("rule");
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "participant", await _context.AgentInfo.GetCharacterIdAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "type", typeName);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "pose", await _context.AgentInfo.GetPostureIdAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "emotion", await _context.AgentInfo.GetEmotionAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "priority", priorityValue);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "target", await _context.CurrentDialogue.GetListenerAsync());
                                parentNode.InsertBefore(docRuleNode, currentMark);
                                break;
                            }

                        }
                    }
                }
            }

            m_completeUtterance = "";
        }

        /// <summary>
        /// Convert the parser returned string to XML format.
        /// 
        /// THIS IS GENUINELY HORRIBLE CODE WHICH WAS PORTED FROM C++ NVBG. At the time noone knew what it did. SORRY.
        /// 
        /// Notes:
        /// - This function is intentionally "DOM heavy" because downstream NVBG logic expects:
        ///   * A nested <POS> tree mirroring the parse structure
        ///   * <mark name="T#"/> nodes inserted before/after terminal words
        ///   * <marked_sentence><text content="..." timemark="T#"/></marked_sentence>
        /// - However, the original port performed excessive XmlNodeList traversal and repeated lookups.
        ///   This version preserves the same XML structure while caching nodes and minimizing repeated work.
        /// </summary>
        private int CreatePositionTags(int _currentSentenceIndex)
        {
            XmlNode parent = null;
            bool firstNP = false;
            bool firstVP = false;

            string currentSentence = "";

            try
            {
                // Cache frequently used nodes once (avoid GetElementsByTagName in hot loops).
                XmlNode bmlNode = m_inputDoc.GetElementsByTagName("bml")[0];
                XmlNode actNode = m_inputDoc.GetElementsByTagName("act")[0];

                // Ensure/Cache <marked_sentence>.
                XmlNode markText;
                XmlNodeList markedSentenceNodes = m_inputDoc.GetElementsByTagName("marked_sentence");
                if (markedSentenceNodes.Count > 0)
                {
                    markText = markedSentenceNodes[0];
                }
                else
                {
                    markText = m_inputDoc.CreateElement("marked_sentence");
                    actNode.AppendChild(markText);
                }

                // Ensure/Cache <parsed_result>.
                XmlNode parsedText;
                XmlNodeList parsedResultNodes = m_inputDoc.GetElementsByTagName("parsed_result");
                if (parsedResultNodes.Count == 0)
                {
                    parsedText = m_inputDoc.CreateElement("parsed_result");

                    XmlNode speechNode = m_inputDoc.GetElementsByTagName("speech")[_currentSentenceIndex];

                    XmlAttribute idAttribute = m_inputDoc.CreateAttribute("id");
                    idAttribute.Value = speechNode.Attributes["id"].Value;
                    parsedText.Attributes.Append(idAttribute);

                    XmlNode refNode = speechNode.Attributes["ref"];
                    if (refNode != null)
                    {
                        XmlAttribute refAttribute = m_inputDoc.CreateAttribute("ref");
                        refAttribute.Value = speechNode.Attributes["ref"].Value;
                        parsedText.Attributes.Append(refAttribute);
                    }

                    XmlAttribute typeAttribute = m_inputDoc.CreateAttribute("type");
                    typeAttribute.Value = speechNode.Attributes["type"].Value;
                    parsedText.Attributes.Append(typeAttribute);

                    bmlNode.AppendChild(parsedText);
                }
                else
                {
                    parsedText = parsedResultNodes[0];
                }

                // Local helper: try to resolve SSML-processed tokens while preserving the port's behavior
                // (first unprocessed entry "wins").
                bool TryConsumeSsmlWord(string key, out string resolvedWord)
                {
                    resolvedWord = null;

                    if (string.IsNullOrEmpty(key))
                        return false;

                    if (m_ssmlWords.TryGetValue(key, out List<WordProcessed> list))
                    {
                        for (int n = 0; n < list.Count; n++)
                        {
                            WordProcessed wp = list[n];
                            if (!wp.processed)
                            {
                                wp.processed = true;
                                resolvedWord = wp.word;
                                return true;
                            }
                        }
                    }

                    return false;
                }

                // Local helper: the original code tried multiple punctuation-stripped keys.
                string ResolveText(string text)
                {
                    if (string.IsNullOrEmpty(text) || m_ssmlWords.Count == 0)
                        return text;

                    if (TryConsumeSsmlWord(text, out string resolved))
                        return resolved;

                    // Preserve original order: ".", "?", "!", ","
                    // Only do Replace() if needed to avoid excess allocations.
                    if (text.IndexOf('.') >= 0)
                    {
                        string k = text.Replace(".", "");
                        if (TryConsumeSsmlWord(k, out resolved))
                            return resolved;
                    }
                    if (text.IndexOf('?') >= 0)
                    {
                        string k = text.Replace("?", "");
                        if (TryConsumeSsmlWord(k, out resolved))
                            return resolved;
                    }
                    if (text.IndexOf('!') >= 0)
                    {
                        string k = text.Replace("!", "");
                        if (TryConsumeSsmlWord(k, out resolved))
                            return resolved;
                    }
                    if (text.IndexOf(',') >= 0)
                    {
                        string k = text.Replace(",", "");
                        if (TryConsumeSsmlWord(k, out resolved))
                            return resolved;
                    }

                    return text;
                }

                int createdPosNodes = 0;
                int createdTokenNodes = 0;
                int createdMarks = 0;

                for (int j = 0; j < m_parseTreeBuffer.Count; ++j)
                {
                    parent = null;
                    firstNP = false;
                    firstVP = false;

                    string sentence = m_parseTreeBuffer[j];

                    for (int i = 0; i < sentence.Length; ++i)
                    {
                        char ch = sentence[i];

                        if (ch == ' ')
                            continue;

                        if (ch == ')')
                        {
                            // Guard against malformed parses (parent can be null if the string is unexpected).
                            parent = parent?.ParentNode;
                            continue;
                        }

                        if (ch == '(')
                        {
                            // Parse tag: read until the next space.
                            int tagStart = i + 1;
                            int spaceIdx = sentence.IndexOf(' ', tagStart);
                            if (spaceIdx < 0)
                                break;

                            string tagName = sentence.Substring(tagStart, spaceIdx - tagStart);

                            // The original port attempted to map '$' to '1' but used char-vs-string Equals().
                            // Doing it correctly is low-risk and avoids odd tag names.
                            if (tagName.IndexOf('$') >= 0)
                                tagName = tagName.Replace('$', '1');

                            if (tagName == "S1")
                                tagName = "PE";

                            if (tagName == "NP")
                            {
                                if (!firstNP)
                                {
                                    firstNP = true;
                                    tagName = "first_NP";
                                }
                            }
                            else if (tagName == "VP")
                            {
                                if (!firstVP)
                                {
                                    firstVP = true;
                                    tagName = "first_VP";
                                }
                            }
                            else if (tagName == "SBAR")
                            {
                                // legacy: no-op
                            }

                            XmlNode current = m_inputDoc.CreateElement("POS");
                            XmlAttribute tagAttribute = m_inputDoc.CreateAttribute("tag");
                            tagAttribute.Value = tagName;
                            current.Attributes.Append(tagAttribute);
                            createdPosNodes++;

                            if (parent == null)
                            {
                                parsedText.AppendChild(current);
                            }
                            else
                            {
                                parent.AppendChild(current);
                            }

                            parent = current;

                            // Continue scanning after the space following the tag.
                            i = spaceIdx;
                            continue;
                        }

                        // Terminal word: substring until ')'
                        int closeIdx = sentence.IndexOf(')', i);
                        if (closeIdx < 0)
                            break;

                        string text = sentence.Substring(i, closeIdx - i);
                        text = ResolveText(text);

                        // Advance i to the close paren we consumed.
                        i = closeIdx;

                        // Create marks.
                        XmlNode readyMarkNode = m_inputDoc.CreateElement("mark");
                        XmlNode relaxMarkNode = m_inputDoc.CreateElement("mark");

                        string readyName = "T" + m_totalTimeMarkers;
                        XmlAttribute readyAttr = m_inputDoc.CreateAttribute("name");
                        readyAttr.Value = readyName;
                        readyMarkNode.Attributes.Append(readyAttr);
                        m_totalTimeMarkers++;
                        createdMarks++;

                        string relaxName = "T" + m_totalTimeMarkers;
                        XmlAttribute relaxAttr = m_inputDoc.CreateAttribute("name");
                        relaxAttr.Value = relaxName;
                        relaxMarkNode.Attributes.Append(relaxAttr);
                        m_totalTimeMarkers++;
                        createdMarks++;

                        // Assign word to current POS node.
                        parent.InnerText = text;
                        createdTokenNodes++;

                        // Add marked_sentence text element (timemark points at the "ready" mark).
                        XmlNode currentMarkedTextContent = m_inputDoc.CreateElement("text");
                        XmlAttribute content = m_inputDoc.CreateAttribute("content");
                        content.Value = text;
                        currentMarkedTextContent.Attributes.Append(content);

                        XmlAttribute timemark = m_inputDoc.CreateAttribute("timemark");
                        timemark.Value = readyName;
                        currentMarkedTextContent.Attributes.Append(timemark);

                        markText.AppendChild(currentMarkedTextContent);

                        currentSentence += text + " ";

                        // Insert marks around the terminal POS node.
                        XmlNode tempNode = parent;
                        parent = parent.ParentNode;

                        // parent can be null if the parse is malformed; in that case, append to parsedText.
                        XmlNode insertParent = parent ?? parsedText;
                        insertParent.InsertBefore(readyMarkNode, tempNode);
                        insertParent.AppendChild(relaxMarkNode);
                    }

                    m_processedSentences.Add(currentSentence);
                    m_completeUtterance += currentSentence;
                    currentSentence = "";
                }

                //_logger?.LogInformation($"CreatePositionTags: posNodes={createdPosNodes}, tokenNodes={createdTokenNodes}, marks={createdMarks}, totalTimeMarkers={m_totalTimeMarkers}, sentences={m_parseTreeBuffer.Count}");
            }
            catch (Exception e)
            {
                _logger?.LogInformation("ERROR while creating position tags" + e);
            }

            // Clearing the SSML hashtable after processing.
            m_ssmlWords.Clear();

            // IMPORTANT: the caller expects this to be the current timemark counter (not 0).
            return m_totalTimeMarkers;
        }
    }
}
