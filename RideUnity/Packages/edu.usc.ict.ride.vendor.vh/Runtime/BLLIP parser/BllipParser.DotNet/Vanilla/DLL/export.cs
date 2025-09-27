using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ResultCollection = BllipParser.DotNet.Vanilla.vector<BllipParser.DotNet.Vanilla.SentenceParseResult>;  //using ResultCollection = vector<SentenceParseResult>;
using size_t = System.UInt64;

using static BllipParser.DotNet.Vanilla.extraMain;
using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    class SentenceParseResult
    {
        public int sentenceIndex;
        public SentRep sentence;  //shared_ptr<SentRep> sentence;
        public size_t numCandidates;
        public vector<InputTree> trees = new vector<InputTree>();  //vector<shared_ptr<InputTree>> trees;
        public vector<double> probs = new vector<double>();
        public ECString name;
    }

    //using ResultCollection = vector<SentenceParseResult>;


    public static class export
    {
        static readonly double log600 = Math.Log(600.0, 2);  //static const double log600 = log2(600.0);

        static int sentenceCount = 0;
        static Params params_ = new Params();


        static void addSkipResult(ResultCollection resultCollection, SentenceParseResult result)
        {
            //TODO:
        }


        static bool decodeParses(SentRep sentence, MeChart chart, SentenceParseResult sentenceParseResult)
        {
            var len = sentence.length();
            // compute the outside probabilities on the items so that we can skip doing detailed computations on the really bad ones 
            chart.set_Alphas();
            var bst = chart.findMapParse();
            if (bst.empty())
            {
                WARN("Parse failed: chart.findMapParse().empty()");
                return false;
            }

            if (Feature.isLM)
            {
                double lgram = Math.Log(bst.sum(), 2);  //double lgram = log2(bst.sum());
                lgram -= (len * log600);
                double pgram = Math.Pow(2, lgram);
                double iptri = chart.triGram();
                double ltri = (Math.Log(iptri, 2) - len * log600);  //double ltri = (log2(iptri) - len * log600);
                double ptri = Math.Pow(2.0, ltri);
                double pcomb = (0.667 * pgram) + (0.333 * ptri);
                double lmix = Math.Log(pcomb, 2);  //double lmix = log2(pcomb);
                Console.WriteLine(lgram + "\t" + ltri + "\t" + lmix);//TODO: output lgram, ltri, lmix to caller
            }

            var diffs = new Link(0);
            for (var candidateIndex = 0; ; ++candidateIndex)
            {
                var pos = (short)0;
                var pV = bst.next(candidateIndex);
                if (pV == null)
                {
                    break;
                }

                var prob = pV.prob();
                if (prob == 0 || double.IsNaN(prob) || double.IsInfinity(prob))
                {
                    break;
                }

                var pMapParse = inputTreeFromBsts(pV, ref pos, sentence);  //auto pMapParse = shared_ptr<InputTree>(inputTreeFromBsts(pV, pos, sentence));
                var count = 0;
                diffs.is_unique(pMapParse, out bool isUnique, ref count);
                if (count != len)
                {
                    Console.WriteLine("Bad length parse for: " + sentence);
                    Console.WriteLine(pMapParse);
                    //assert(count == len);//TODO: return error message to caller
                }

                if (isUnique)
                {
                    sentenceParseResult.probs.push_back(pV.prob());
                    sentenceParseResult.trees.push_back(pMapParse);
                    sentenceParseResult.numCandidates++;
                }

                if (sentenceParseResult.numCandidates >= Bchart.Nth)
                {
                    break;
                }

                if (candidateIndex > 20000)
                {
                    break;
                }
            }

            return true;
        }


        static bool parse(in int threadId, SentRep sentence, ExtPos extPos, SentenceParseResult parseResult, in bool useExtPos)
        {
            var pChart = useExtPos ? new MeChart(sentence, extPos, threadId) : new MeChart(sentence, threadId);//too large, put on heap
            var chart = pChart;  //auto& chart = *pChart;

            chart.parse();

            var pTopS = chart.topS();
            if (pTopS == null)
            {
                WARN("Parse failed: !topS");
                return false;
            }

            var decodeSucceed = decodeParses(sentence, chart, parseResult);

            if (parseResult.numCandidates == 0)
            {
                WARN("Parse failed from 0, inf or NaN probabililty");
                return false;
            }

            return true;
        }


        static ResultCollection parse(in int threadId, in ECString text)
        {
            if (threadId < 0 || threadId >= MAXNUMTHREADS)
            {
                string oString = "";  //auto oString = ostringstream();
                oString += "Invalid thread ID " + threadId + ". This BLLIP parser build only supports upto " + MAXNUMTHREADS + "threads.";
                ERROR(oString);
            }

            string inputStream = text;  //auto inputStream = istringstream(text, istringstream::in_);
            string [] inputStreamSplit = inputStream.Split((char [])null, StringSplitOptions.RemoveEmptyEntries);
            int inputStreamIdx = 0;
            var tokenizedStream = new ewDciTokStrm(inputStreamSplit);
            var pResult = new ResultCollection();
            var result = pResult;  //auto& result = *pResult;

            while (true)
            {
                var pSentence = new SentRep(params_.maxSentLen);//put on heap
                var sentence = pSentence;  //auto& sentence = *pSentence;
                if (Bchart.tokenize)
                {
                    SentRep.op(tokenizedStream, sentence);  //tokenizedStream >> sentence;
                }
                else
                {
                    SentRep.op(inputStreamSplit, ref inputStreamIdx, sentence);  //inputStream >> sentence;
                }

                var extPos = new ExtPos();
                if (params_.extPosIfstream != null)
                {
                    extPos.read(params_.extPosIfstream, sentence);
                }

                var sentenceIndex = sentenceCount;
                sentenceCount++;

                var sentenceResult = new SentenceParseResult
                {
                    sentenceIndex = sentenceIndex,
                    name = sentence.getName(),
                    sentence = pSentence,
                    numCandidates = 0
                };

                if (!params_.field().in_(sentenceCount))
                {
                    addSkipResult(result, sentenceResult);
                    continue;
                }

                var len = sentence.length();
                if (len == 0)
                {
                    break;
                }

                if (len >= params_.maxSentLen)
                {
                    var message = new ECString("skipping sentence longer than specified limit of ") + intToString(params_.maxSentLen);
                    WARN(message);
                    addSkipResult(result, sentenceResult);
                    continue;
                }

                //handle input containing reserved word Bchart::HEADWORD_S1; could probably do better (like undo replacement before printing) but this seems sufficient.
                for (var i = 0; i < len; ++i)
                {
                    var w = sentence.op(i).lexeme();
                    if (w == Bchart.HEADWORD_S1)
                    {
                        var message = new ECString("Replacing reserved token \"") + Bchart.HEADWORD_S1 + "\" at index " + intToString(i) + " of input with token \"^^^\"";
                        WARN(message);
                        w = "^^^";
                    }
                }

                var succeed = parse(threadId, sentence, extPos, sentenceResult, true);
                if (!succeed && extPos.hasExtPos())
                {
                    WARN("Parse failed: reparsing with out POS");
                    succeed = parse(threadId, sentence, extPos, sentenceResult, false);
                }

                if (!succeed)
                {
                    addSkipResult(result, sentenceResult);
                    continue;
                }

                result.push_back(sentenceResult);
            }

            return pResult;
        }


        //public static void initialize(in int argc, in string [] argv)
        public static void initialize(in int argc, in string [] argv, Dictionary<string, Stream> streams)
        {
            var argv_modified_vec = new vector<ECString>();
            argv_modified_vec.push_back("dummy.exe");//required by parser code
            for (var i = 0; i < argc; i++)
            {
                argv_modified_vec.push_back(argv[i]);
            }
            var argv_modified = new string [argv_modified_vec.size()];
            for (var i = 0; i < (int)argv_modified_vec.size(); i++)
            {
                argv_modified[i] = argv_modified_vec[i];
            }
            var args = new ECArgs((int)argv_modified_vec.size(), argv_modified);
            //delete[] argv_modified;
            params_.init(args);
            var modelDirectory = new ECString(args.arg(0));
            generalInit(modelDirectory, streams);
            if (Bchart.tokenize)
            {
                ERROR("Tokenized input not supported. Add \"-K\" in argument string");
            }
        }


        public static int parse_and_format_to_buffer(Int32 threadId, string text, uint bufferSize, StringBuilder buffer)
        {
            var textStr = new ECString(text);
            if (!textStr.Contains(" </s>"))  //if (textStr.find(" </s>") == string::npos)
            {
                ERROR("Each sentence should be wrapped by \"<s> \" and \" </s>\"");
            }

            var collection = parse(threadId, textStr);

            string oStream = "";  //auto oStream = ostringstream();
            for (var sentenceIndex = 0; sentenceIndex < (int)collection.size(); sentenceIndex++)
            {
                if (sentenceIndex != 0)
                {
                    oStream += "\n";
                }

                var sentenceResult = collection.at(sentenceIndex);
                oStream += "[" + sentenceResult.name + "]" + sentenceResult.sentence + "\n";
                for (var candidateIndex = 0; candidateIndex < (int)sentenceResult.numCandidates; candidateIndex++)
                {
                    var prob = sentenceResult.probs[candidateIndex];
                    oStream += "<" + prob + ">";
                    var pTree = sentenceResult.trees[candidateIndex];
                    var tree = pTree;
                    if (Bchart.prettyPrint)
                    {
                        oStream += tree;
                    }
                    else
                    {
                        tree.printproper(ref oStream);
                    }
                }
            }

            //auto str = oStream.str();
            //auto result = str.copy(buffer, bufferSize - 1);
            //buffer[result] = '\0';//for safety
            buffer.Clear();
            buffer.Append(oStream);
            return buffer.Length;
        }
    }
}
