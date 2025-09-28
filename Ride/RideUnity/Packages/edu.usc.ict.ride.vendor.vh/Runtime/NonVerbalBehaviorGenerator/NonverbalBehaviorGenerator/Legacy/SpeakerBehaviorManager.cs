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

                //Refactor for performance and fix bugs. I do not know why these characters need to be replaced.
                sentence = Regex.Replace(sentence, "[\"\\t\\r\\n\\0]", "");
                var matches = Regex.Matches(sentence, @"\S.*?(\S*(?=\s*?$)|!(?=\s)|\?(?=\s)|\.(?=\s))");
                foreach (var match in matches.Cast<Match>()) {
                    GetParseTree(match.Value);
                }

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

        /// <summary>
        /// Generate rules based on the parser's result and attach them to the bml
        /// </summary>
        private async Task AttachRuleTagsAsync()
        {
            try
            {
                //XmlNode parsedResult = m_inputDoc.GetElementsByTagName("parsed_result")[0];

                var ruleDocument = await _context.GetBehaviorXmlDocumentAsync();
                //XmlNodeList ruleNodes = ruleDocument.GetElementsByTagName("rule");
                XmlNodeList patterns = ruleDocument.GetElementsByTagName("pattern");

                XmlNodeList pawnTriggers = ruleDocument.GetElementsByTagName("pawn_trigger");


                XmlNode currentNode;


                XmlNodeList posNodes = m_inputDoc.GetElementsByTagName("POS");


                // rules applied to the parse tree result
                if (await _context.Switch.GetPoseRulesAsync())
                {
                    for (int i = 0; i < posNodes.Count; ++i)
                    {
                        currentNode = posNodes[i];
                        XmlNode parentNode = currentNode.ParentNode;
                        string priorityValue;

                        string positionTag = currentNode.Attributes["tag"].Value;

                        for (int j = 0; j < patterns.Count; ++j)
                        {
                            string currentPattern = patterns[j].InnerText;

                            if (currentPattern.Equals(positionTag))
                            {
                                string typeName = patterns[j].ParentNode.Attributes["keyword"].Value;
                                priorityValue = patterns[j].ParentNode.Attributes["priority"].Value;

                                XmlNode docRuleNode = m_inputDoc.CreateElement("rule");
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "participant", await _context.AgentInfo.GetCharacterIdAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "type", typeName);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "pose", await _context.AgentInfo.GetPostureIdAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "emotion", await _context.AgentInfo.GetEmotionAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "priority", priorityValue);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "target", await _context.CurrentDialogue.GetListenerAsync());
                                parentNode.InsertBefore(docRuleNode, currentNode);
                                break;
                            }
                        }
                    }
                }


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

                    if (await _context.Switch.GetSpeakerGesturesAsync())
                    {
                        for (int j = 0; j < patterns.Count; ++j)
                        {
                            string currentPattern = patterns[j].InnerText;

                            if (currentPattern.Equals(text, StringComparison.OrdinalIgnoreCase))
                            {
                                if ((text.Equals("why") ||
                                    text.Equals("what") ||
                                    text.Equals("where") ||
                                    text.Equals("who") ||
                                    text.Equals("how") ||
                                    text.Equals("when") ||
                                    text.Equals("do")) &&
                                    (i != 0))
                                {
                                    continue;
                                }

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


                    if (await _context.Switch.GetSpeakerGazeAsync())
                    {
                        for (int j = 0; j < pawnTriggers.Count; ++j)
                        {
                            string currentTrigger = pawnTriggers[j].InnerText;

                            if (currentTrigger.Equals(text))
                            {
                                string pawnName = pawnTriggers[j].ParentNode.Attributes["pawn_name"].Value;
                                string priorityValue = pawnTriggers[j].ParentNode.Attributes["priority"].Value;

                                XmlNode docRuleNode = m_inputDoc.CreateElement("rule");
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "participant", await _context.AgentInfo.GetCharacterIdAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "type", "fmlbml_gaze");
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "pose", await _context.AgentInfo.GetPostureIdAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "emotion", await _context.AgentInfo.GetEmotionAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "priority", priorityValue);
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "prev_target", await _context.CurrentDialogue.GetListenerAsync());
                                XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "target", pawnName);
                                parentNode.InsertBefore(docRuleNode, currentMark);
                                break;
                            }
                        }
                    }

                }


                // check to see if multiple words/phrases occur and if so apply the rule
                if (await _context.Switch.GetSpeakerGesturesAsync()) {
                    await CheckForPhrasesAsync();
                }

                // rules to attach gaze shift when there is an fml-bml tag
                if (m_fmlBml && await _context.Switch.GetSpeakerGazeAsync())
                {
                    for (int i = 0; i < markList.Count; i += 2)
                    {
                        XmlNode currentMark = markList[i];
                        //XmlNode wordNode = currentMark.NextSibling;
                        XmlNode parentNode = currentMark.ParentNode;

                        string targetValue = "", annotateValue = "", timemarkValue = "";
                        //int test = patterns.Count;
                        string markTag = "T" + i;

                        for (int j = 0; j < m_inputDoc.GetElementsByTagName("fml-bml").Count; ++j)
                        {
                            XmlNode fmlBmlElement = m_inputDoc.GetElementsByTagName("fml-bml")[j];
                            annotateValue = fmlBmlElement.Attributes["annotate"].Value;
                            targetValue = fmlBmlElement.Attributes["value"].Value;
                            timemarkValue = fmlBmlElement.Attributes["timemark"].Value;

                            if (timemarkValue.Equals(markTag))
                            {
                                if (annotateValue.Equals("addressee") && !targetValue.Equals(await _context.CurrentDialogue.GetListenerAsync()))
                                {
                                    XmlNode docRuleNode = m_inputDoc.CreateElement("rule");
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "participant", await _context.AgentInfo.GetCharacterIdAsync());
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "type", "fmlbml_gaze");
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "pose", await _context.AgentInfo.GetPostureIdAsync());
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "emotion", await _context.AgentInfo.GetEmotionAsync());
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "priority", "0");
                                    XMLHelperMethods.AttachAttributeToNode(m_inputDoc, docRuleNode, "prev_target", await _context.CurrentDialogue.GetListenerAsync());
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
        /// Convert the parser returned string to XML format. \
        /// THIS IS GENUINELY HORRIBLE CODE WHICH WAS PORTED FROM C++ NVBG. At the time noone knew what it did. SORRY.
        /// </summary>
        /// <param name="_currentSentenceIndex"></param>
        /// <returns></returns>
        private int CreatePositionTags(int _currentSentenceIndex)
        {

            XmlNode parent;
            bool firstNP = false;
            bool firstVP = false;
            int markCounter = 0;
            string currentSentence = "";
            XmlNode markText;


            try
            {

                for (int j = 0; j < m_parseTreeBuffer.Count; ++j)
                {
                    parent = null;
                    firstNP = false;
                    firstVP = false;
                    string sentence = m_parseTreeBuffer[j];


                    for (int i = 0; i < sentence.Length; ++i)
                    {
                        if (sentence[i].Equals(' '))
                        {
                            continue;
                        }
                        else if (sentence[i].Equals(')'))
                        {
                            parent = parent.ParentNode;
                            continue;
                        }
                        else if (sentence[i].Equals('('))
                        {
                            string tagName = "";
                            char[] tagNameChar = new char[256];
                            int index = 0;
                            i++;
                            while (!sentence[i].Equals(' '))
                            {

                                tagNameChar[index] = sentence[i];//.Insert(index, sentence[i].ToString());

                                if (sentence[i].Equals("$"))
                                {

                                    tagNameChar[index] = '1';
                                }
                                else if (Char.IsLetter(sentence[i]))
                                {
                                    tagName = "PER";
                                }
                                i++;
                                index++;
                            }
                            tagNameChar[index] = '\0';

                            tagName = new string(tagNameChar);
                            tagName = tagName.Replace("\0", "");

                            if (tagName.Equals("S1"))
                            {
                                tagName = "PE";
                            }


                            if (tagName.Equals("NP"))
                            {
                                if (firstNP == false)
                                {
                                    firstNP = true;
                                    tagName = "first_NP";
                                }
                            }
                            else if (tagName.Equals("VP"))
                            {
                                if (firstVP == false)
                                {
                                    firstVP = true;
                                    tagName = "first_VP";
                                }
                            }
                            else if (tagName.Equals("SBAR"))
                            {
                                //numAni = 0;
                            }


                            XmlNode current = m_inputDoc.CreateElement("POS");
                            XmlAttribute tagAttribute = m_inputDoc.CreateAttribute("tag");
                            tagAttribute.Value = tagName;
                            current.Attributes.Append(tagAttribute);


                            if (parent == null)
                            {
                                if (m_inputDoc.GetElementsByTagName("parsed_result").Count == 0)
                                {
                                    XmlNode parsedText = m_inputDoc.CreateElement("parsed_result");
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

                                    XmlNode bmlNode = m_inputDoc.GetElementsByTagName("bml")[0];
                                    bmlNode.AppendChild(parsedText);
                                    parsedText.AppendChild(current);
                                }
                                else
                                {
                                    XmlNode parsedText = m_inputDoc.GetElementsByTagName("parsed_result")[0];
                                    parsedText.AppendChild(current);
                                }
                            }
                            else
                            {
                                parent.AppendChild(current);
                            }

                            string nodeName = current.Name;

                            if (nodeName.Equals("S1"))
                            {
                                firstNP = false;
                            }

                            parent = current;
                            continue;
                        }
                        else
                        {
                            string text = "";
                            text = sentence.Substring(i, sentence.IndexOf(")", i) - i);

                            //This is added for words which may have ssml tags
                            if (!String.IsNullOrEmpty(text))
                            {
                                if (m_ssmlWords.ContainsKey(text))
                                {
                                    //text = m_ssmlWords[text][0].ToString();
                                    foreach (WordProcessed wordProc in m_ssmlWords[text])
                                    {
                                        if (!wordProc.processed)
                                        {
                                            wordProc.processed = true;
                                            text = wordProc.word.ToString();
                                            break;
                                        }
                                    }
                                }
                                else if (m_ssmlWords.ContainsKey(text.Replace(".", "")))
                                {
                                    //text = m_ssmlWords[text.Replace(".", "")][0].ToString();
                                    foreach (WordProcessed wordProc in m_ssmlWords[text.Replace(".", "")])
                                    {
                                        if (!wordProc.processed)
                                        {
                                            wordProc.processed = true;
                                            text = wordProc.word.ToString();
                                            break;
                                        }
                                    }
                                }
                                else if (m_ssmlWords.ContainsKey(text.Replace("?", "")))
                                {
                                    //text = m_ssmlWords[text.Replace("?", "")][0].ToString();
                                    foreach (WordProcessed wordProc in m_ssmlWords[text.Replace("?", "")])
                                    {
                                        if (!wordProc.processed)
                                        {
                                            wordProc.processed = true;
                                            text = wordProc.word.ToString();
                                            break;
                                        }
                                    }
                                }
                                else if (m_ssmlWords.ContainsKey(text.Replace("!", "")))
                                {
                                    //text = m_ssmlWords[text.Replace("!", "")][0].ToString();
                                    foreach (WordProcessed wordProc in m_ssmlWords[text.Replace("!", "")])
                                    {
                                        if (!wordProc.processed)
                                        {
                                            wordProc.processed = true;
                                            text = wordProc.word.ToString();
                                            break;
                                        }
                                    }
                                }
                                else if (m_ssmlWords.ContainsKey(text.Replace(",", "")))
                                {
                                    //text = m_ssmlWords[text.Replace(",", "")][0].ToString();
                                    foreach (WordProcessed wordProc in m_ssmlWords[text.Replace(",", "")])
                                    {
                                        if (!wordProc.processed)
                                        {
                                            wordProc.processed = true;
                                            text = wordProc.word.ToString();
                                            break;
                                        }
                                    }
                                }
                            }
                            i += sentence.IndexOf(")", i) - i;
                            XmlNode readyMarkNode = m_inputDoc.CreateElement("mark");
                            XmlNode relaxMarkNode = m_inputDoc.CreateElement("mark");

                            string value;
                            value = "T";
                            value += m_totalTimeMarkers;

                            XmlAttribute nameAttribute = m_inputDoc.CreateAttribute("name");
                            nameAttribute.Value = value;
                            readyMarkNode.Attributes.Append(nameAttribute);

                            m_totalTimeMarkers++;

                            string value1;
                            value1 = "T";
                            value1 += m_totalTimeMarkers;
                            XmlAttribute nameAttribute1 = m_inputDoc.CreateAttribute("name");
                            nameAttribute1.Value = value1;
                            relaxMarkNode.Attributes.Append(nameAttribute1);

                            m_totalTimeMarkers++;

                            parent.InnerText = text;


                            XmlNode currentMarkedTextContent = m_inputDoc.CreateElement("text");
                            XmlAttribute content = m_inputDoc.CreateAttribute("content");
                            content.Value = text;
                            currentMarkedTextContent.Attributes.Append(content);

                            XmlAttribute timemark = m_inputDoc.CreateAttribute("timemark");
                            timemark.Value = value;
                            currentMarkedTextContent.Attributes.Append(timemark);


                            if (m_inputDoc.ContainsAnyElementNamedAs("marked_sentence"))
                            {
                                markText = m_inputDoc.GetElementsByTagName("marked_sentence")[0];
                            }
                            else
                            {
                                markText = m_inputDoc.CreateElement("marked_sentence");
                                m_inputDoc.GetElementsByTagName("act")[0].AppendChild(markText);
                            }

                            markText.AppendChild(currentMarkedTextContent);
                            currentSentence += text + " ";

                            XmlNode tempNode = parent;
                            parent = parent.ParentNode;

                            parent.InsertBefore(readyMarkNode, tempNode);
                            parent.AppendChild(relaxMarkNode);
                        }

                    }

                    m_processedSentences.Add(currentSentence);
                    m_completeUtterance += currentSentence;
                    currentSentence = "";

                }
            }
            catch (Exception e)
            {
                _logger?.LogInformation("ERROR while creating position tags" + e.ToString());
            }

            //clearing the ssml hashtable after its done processing.
            m_ssmlWords.Clear();
            return markCounter;
        }

    }

}
