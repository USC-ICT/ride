#nullable enable

using Microsoft.Extensions.Logging;
using NonverbalBehaviorGenerator.Legacy;
using NonverbalBehaviorGenerator.LegacyInterop;
using NonverbalBehaviorGenerator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;

namespace NonverbalBehaviorGenerator
{
    /// <summary>
    /// Non-Verbal Behavior Generator service instance.
    /// Each instance is for 1 character only.
    /// This class's APIs are designed to be thread safe, but only have a degree of concurrency of 1.
    /// This is because NVBG (inherited behavior from the old NVBG) is not a stateless service.
    /// </summary>
    public sealed class Nvbg : IDisposable
    {

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();//TODO: remove this lock and use transaction provided by context

        private readonly ILogger? _logger;

        private readonly XslCompiledTransform _transformXsl = new XslCompiledTransform();

        private readonly IContextFactory _contextFactory;

        #region Legacy Instances

        private readonly ListenerBehaviorManager _listenerBehaviorManagerLegacy;

        private readonly GazeBehaviorManager _gazeBehaviorManagerLegacy;

        private readonly SaliencyMapManager _saliencyMapManagerLegacy;
        #endregion

        public Nvbg(INvbgOptions options, ILogger? logger = null)
        {
            _logger = logger;
            _contextFactory = options.ContextFactory;
            LoadTransformXsl(_transformXsl, options.TransformXsl, options.TransformXslResolver);
            SpeakerBehaviorManager.Initialize(options.ParserModelDirectory, options.Streams);
            _listenerBehaviorManagerLegacy = new ListenerBehaviorManager(logger);
            _gazeBehaviorManagerLegacy = new GazeBehaviorManager(logger);
            _saliencyMapManagerLegacy = new SaliencyMapManager(logger);
        }

        #region APIs
        #region Exposed States
        public async Task<string> GetLastMyExpressIdAsync() {
            using var context = await _contextFactory.CreateContextAsync();
            return await context.ConversationInfo.GetLastMyExpressIdAsync();
        }
        #endregion

        #region Exposed Settings
        ///<summary>For estimating how busy the service is.</summary>
        ///<remarks>Simulate NVBGManager.m_Processing</remarks>
        public bool Busy => _lock.IsWriteLockHeld;

        public async Task<bool> GetAllBehaviorAsync() {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterReadLock();
            try
            {
                return await context.Switch.GetAllBehaviorAsync();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task SetAllBehaviorAsync(bool value) {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (await context.Switch.GetAllBehaviorAsync() == value)
                {
                    return;
                }
                _lock.EnterWriteLock();
                try
                {
                    await context.Switch.SetAllBehaviorAsync(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public async Task<bool> GetSpeakerGazeAsync() {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterReadLock();
            try
            {
                return await context.Switch.GetSpeakerGazeAsync();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task SetSpeakerGazeAsync(bool value) {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (await context.Switch.GetSpeakerGazeAsync() == value)
                {
                    return;
                }
                _lock.EnterWriteLock();
                try
                {
                    await context.Switch.SetSpeakerGazeAsync(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public async Task<bool> GetSpeakerGesturesAsync()
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterReadLock();
            try
            {
                return await context.Switch.GetSpeakerGesturesAsync();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task SetSpeakerGesturesAsync(bool value)
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (await context.Switch.GetSpeakerGesturesAsync() == value)
                {
                    return;
                }
                _lock.EnterWriteLock();
                try
                {
                    await context.Switch.SetSpeakerGesturesAsync(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public async Task<bool> GetListenerGazeAsync()
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterReadLock();
            try
            {
                return await context.Switch.GetListenerGazeAsync();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task SetListenerGazeAsync(bool value)
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (await context.Switch.GetListenerGazeAsync() == value)
                {
                    return;
                }
                _lock.EnterWriteLock();
                try
                {
                    await context.Switch.SetListenerGazeAsync(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public async Task<bool> GetPoseRulesAsync()
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterReadLock();
            try
            {
                return await context.Switch.GetPoseRulesAsync();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task SetPoseRulesAsync(bool value)
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (await context.Switch.GetPoseRulesAsync() == value)
                {
                    return;
                }
                _lock.EnterWriteLock();
                try
                {
                    await context.Switch.SetPoseRulesAsync(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public async Task<string> GetPostureIdAsync()
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterReadLock();
            try
            {
                return await context.AgentInfo.GetPostureIdAsync();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task SetPostureIdAsync(string value)
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (await context.AgentInfo.GetPostureIdAsync() == value)
                {
                    return;
                }
                _lock.EnterWriteLock();
                try
                {
                    await context.AgentInfo.SetPostureIdAsync(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public async Task<bool> GetSaliencyGlanceAsync()
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterReadLock();
            try
            {
                return await context.Switch.GetSaliencyGlanceAsync();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task SetSaliencyGlanceAsync(bool value)
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (await context.Switch.GetSaliencyGlanceAsync() == value)
                {
                    return;
                }
                _lock.EnterWriteLock();
                try
                {
                    await context.Switch.SetSaliencyGlanceAsync(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public async Task<bool> GetSaliencyIdleGazeAsync()
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterReadLock();
            try
            {
                return await context.Switch.GetSaliencyIdleGazeAsync();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task SetSaliencyIdleGazeAsync(bool value)
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (await context.Switch.GetSaliencyIdleGazeAsync() == value)
                {
                    return;
                }
                _lock.EnterWriteLock();
                try
                {
                    await context.Switch.SetSaliencyIdleGazeAsync(value);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }
        #endregion

        /// <summary>
        /// Reload the rule XML for the character.
        /// </summary>
        /// <remarks>This method is for backward compatability. For new projects, do not use this interface.</remarks>
        /// <param name="xml">Character Rule XML text</param>
        [Obsolete("This method is for backward compatability.")]
        public async void ReloadRuleXml(string xml)
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterWriteLock();
            try
            {
                await context.SetBehaviorRuleXmlAsync(xml);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>Main API of NVBG service.</summary>
        /// <remarks>Refactor of NVBG.NVBGManager.ProcessMessage()</remarks>
        public async Task<NvbgResponse> ProcessAsync(NvbgRequest request)
        {
            using var context = await _contextFactory.CreateContextAsync();
            _lock.EnterWriteLock();//Since we do not know how legacy code modifies the state, so we use a lock to protect state integrity.
            try
            {
                //Make a clone of request, becase the request will be modified by legacy code, so that the orignial one get uncontaminated
                var requestClone = new LegacyRequest(request);
                #region Simulate partial NVBGManager.MessageCallback()
                //special handlings for different kinds of request before start processing
                switch (requestClone.Kind)
                {
                    //TODO: other kinds
                    case NvbgRequestKind.None:
                        await context.CurrentDialogue.SetSpeakerIdAsync(requestClone.SourceId);
                        await context.CurrentDialogue.SetListenerIdAsync(requestClone.TargetId);
                        await context.ConversationInfo.SetLastMyExpressIdAsync(requestClone.MessageId);
                        break;
                    case NvbgRequestKind.Listen:
                    case NvbgRequestKind.Backchannel:
                    case NvbgRequestKind.BackChannelFeedback:
                    case NvbgRequestKind.AgentSpeech:
                    case NvbgRequestKind.Speech:
                        //Nothing
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                #endregion
                #region Simulate NVBGManager.ProcessMessage()
                #region STEP 1: Enrich input BML document by adding new nodes to it
                var xml = new XmlDocument();//TODO: set the XmlDeclaration to utf-8, the default value is utf-16, and some software may report errors.
                if (!string.IsNullOrEmpty(requestClone.Xml))
                {
                    xml.LoadXml(requestClone.Xml);//this xml will be enriched
                    #region Simulate VrExpressHandler.ProcessMessage()
#pragma warning disable CS0612 //Obsolete
                    #region Simulate VrExpressHandler.ProcessFMLData()
                    _logger?.LogInformation("Processing fml data");

                    using var affectList = xml.GetElementsByTagName("affect");
                    if (affectList.Count > 0)
                    {
                        await ProcessFMLAffectAsync(context, requestClone, affectList);
                    }
                    using var statusList = xml.GetElementsByTagName("status");
                    if (statusList.Count > 0)
                    {
                        await ProcessFMLStatusAsync(context, requestClone, statusList);
                    }
                    using var requestList = xml.GetElementsByTagName("request");
                    if (requestList.Count > 0)
                    {
                        await ProcessFMLRequestsAsync(context, requestClone, requestList);
                    }
                    using var saliencyList = xml.GetElementsByTagName("saliency");
                    if (saliencyList.Count > 0)
                    {
                        await ProcessFMLSaliencyAsync(context, requestClone, saliencyList);
                    }
                    #endregion
#pragma warning restore CS0612

                    if (xml.ContainsAnyElementNamedAs("speech"))
                    {
                        await ProcessSpeechDataAsync(context, requestClone, xml);
                    }
                    else if (xml.ContainsAnyElementNamedAs("listenerFeedback"))
                    {
                        await ProcessListenerFeedbackAsync(context, requestClone, xml);
                    }
                    else if (requestClone.Kind == NvbgRequestKind.Backchannel)
                    {
                        await ProcessVRBackchannelAsync(context, requestClone, xml);
                    }
                    else if (requestClone.Kind == NvbgRequestKind.BackChannelFeedback)
                    {
                        await ProcessVRBCFeedbackAsync(context, requestClone, xml);
                    }
                    else if (requestClone.Kind == NvbgRequestKind.FeedbackRuleTest)
                    {
                        await ProcessVRNVBGFeedbackRuleTestAsync(context, requestClone, xml);
                    }
                    else if (xml.ContainsAnyElementNamedAs("gaze"))
                    {
                        await ProcessGazeMessageAsync(context, requestClone, xml);
                    }
                    else if (xml.ContainsAnyElementNamedAs("negotiationStance"))
                    {
                        await ProcessNegotiationMessageAsync(context, requestClone);
                    }
                    else if (xml.ContainsAnyElementNamedAs("face"))
                    {
                        requestClone.Kind = NvbgRequestKind.Facs;
                    }
                    else if (xml.ContainsAnyElementNamedAs("body"))
                    {
                        await ProcessBodyMessageAsync(context, requestClone, xml);
                    }
                    #endregion
                }
                if (requestClone.Kind == NvbgRequestKind.IdleBehavior)
                {
                    await CreateIdleBehaviorAsync(context, requestClone, xml);
                }
                #endregion

                #region STEP 2: Transform the input BML to an output BML
                //Original Comment: Applying the transform to the processed message
                var resultBml = await ApplyTransformAsync(context, xml);

                //Original Comment: Filter message and remove overlapping behavior
                if (resultBml.GetElementsByTagName("rule").Count > 1)
                {
                    FilterGestures(resultBml);
                }

                //Original Comment: clean up the bml and remove unwanted nodes in the xml
                CleanUpBml(resultBml);
                
                Trace.Assert(requestClone.SourceId == await context.AgentInfo.GetCharacterIdAsync(), "NVBG refactor simplification assumption broken");
                if (await context.Switch.GetSaliencyGlanceAsync())
                {
                    await _saliencyMapManagerLegacy.UpdateGazeRangeAsync(resultBml);
                    await _saliencyMapManagerLegacy.TrackGazeEventAsync(resultBml, context);
                }

                //Original Comment: Attach the vrAgentPartial messages which will notify us of when the character finishes speaking each word in the sentence
                if (requestClone.Kind == NvbgRequestKind.Dialogue)
                {
                    AttachVRAgentPartialMessage(resultBml, requestClone.MessageId);
                }
                #endregion
                #endregion
                var result = new NvbgResponse(request, requestClone, resultBml);
                return result;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        #endregion

        #region VrExpressHandler Methods
        /// <remarks>
        /// Simulate VrExpressHandler.ProcessFMLAffect()
        /// </remarks>
        [Obsolete]
        private async Task ProcessFMLAffectAsync(IContext context, LegacyRequest request, XmlNodeList affectList)
        {
            if (request.SourceId != await context.AgentInfo.GetCharacterIdAsync())
            {
                return;
            }
            try
            {
                await context.AgentInfo.SetEmotionAsycn(affectList[0].Attributes["type"].Value);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "No type attribute in fml affect tag");
            }
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessFMLStatus()
        /// </remarks>
        [Obsolete]
        private async Task ProcessFMLStatusAsync(IContext context, LegacyRequest request, XmlNodeList statusList)
        {
            if (request.SourceId != await context.AgentInfo.GetCharacterIdAsync())
            {
                return;
            }

            string? statusStr = null;
            try
            {
                
                statusStr = statusList[0].Attributes["type"].Value;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "No type attribute in fml status tag");
            }
            if (statusStr is not null && Enum.TryParse<CharacterStatus>(statusStr, out var status))
            {
                await context.AgentInfo.SetStatusAsync(status);
            }

            switch (await context.AgentInfo.GetStatusAsync())
            {
                case CharacterStatus.Present:
                    await context.Switch.SetAllBehaviorAsync(true);
                    break;
                case CharacterStatus.Absent:
                case CharacterStatus.Incapacitated:
                    await context.Switch.SetAllBehaviorAsync(false);
                    break;
            }
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessFMLRequests()
        /// </remarks>
        [Obsolete]
        private async Task ProcessFMLRequestsAsync(IContext context, LegacyRequest request, XmlNodeList requestList)
        {
            if (request.SourceId != await context.AgentInfo.GetCharacterIdAsync())
            {
                return;
            }
            for (var i = 0; i < requestList.Count; i++)
            {
                try
                {
                    var type = requestList[i].Attributes["type"].Value;
                    var value = requestList[i].Attributes["value"].Value;
                    switch (type)
                    {
                        case "idlebehavior":
                            switch (value)
                            {
                                case "on":
                                    await context.Switch.SetSaliencyIdleGazeAsync(true);
                                    break;
                                case "off":
                                    await context.Switch.SetSaliencyIdleGazeAsync(false);
                                    break;
                            }
                            break;
                        case "behavior":
                            switch (value)
                            {
                                case "on":
                                    await context.Switch.SetAllBehaviorAsync(true);
                                    break;
                                case "off":
                                    await context.Switch.SetAllBehaviorAsync(false);
                                    break;
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Error while processing fml request tag");
                }
            }
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessFMLSaliency()
        /// </remarks>
        private async Task ProcessFMLSaliencyAsync(IContext context, LegacyRequest request, XmlNodeList saliencyList)
        {
            if (request.SourceId != await context.AgentInfo.GetCharacterIdAsync())
            {
                return;
            }
            try
            {
                for (int i = 0; i < saliencyList.Count; ++i)
                {
                    var type = saliencyList[i].Attributes["type"].Value;
                    var value = saliencyList[i].Attributes["value"].Value;
                    switch (type)
                    {
                        case "story-point":
                            NVBGSaliencyMap.m_storyPoint = value;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error while processing fml request tag");
            }
        }

        /// <remarks>
        /// Simulate VrExpressHandler.GetBmlNode()
        /// </remarks>
        private static XmlNode GetOrCreateBmlNode(XmlDocument document)
        {
            var bmlNodes = document.GetElementsByTagName("bml");
            if (bmlNodes.Count > 0)
            {
                return bmlNodes[0];
            }
            var actNode = document.GetElementsByTagName("act")[0];
            var result = document.CreateElement("bml");
            actNode.AppendChild(result);
            return result;
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessSpeechData()
        /// </remarks>
        private async Task ProcessSpeechDataAsync(IContext context, LegacyRequest request, XmlDocument document)
        {
            var bmlNode = GetOrCreateBmlNode(document);
            var eventNode = document.CreateElement("sbm:event");

            //Original Comment: Add the vrSpoke even to the bml This is necessary to receive feedback when the character finishes speaking
            if (bmlNode.ChildNodes.Count > 0 && bmlNode.ChildNodes[0].Name == "speech")
            {
                var speechNode = bmlNode.ChildNodes[0];
                var messageAttrVal = $"vrSpoke {request.SourceId} {request.TargetId} {request.MessageId} {speechNode.InnerText}";
                eventNode.AttachAttribute("message", messageAttrVal);
                eventNode.AttachAttribute("xmlns:sbm", "http://ict.usc.edu");

                var speechId = speechNode.Attributes["id"].Value;
                if (speechId != "")
                {
                    await context.SetCurrentSpeechId(speechId);
                }
            }

            async void addStrokeAttr() => eventNode.AttachAttribute("stroke", $"{await context.GetCurrentSpeechId()}:relax");
            if (request.Kind != NvbgRequestKind.Listen)
            {
                //Original Comment: If message type is not listen, then it is speaker type so the SpeakerBehavior object will process the message
                _logger?.LogInformation("Processing dialog messge");
                request.Kind = NvbgRequestKind.Dialogue;
                var speakerBehaviorManagerLegacy = new SpeakerBehaviorManager(_logger, context);
                await speakerBehaviorManagerLegacy.ProcessDialogMessageAsync(document, context, request, bmlNode);
                await context.AgentInfo.SetHasSpokenAsync(true);
                addStrokeAttr();
                bmlNode.AppendChild(eventNode);
            }
            else
            {
                //Original Comment: If message type is not speak, then it is listen type so the ListenerBehavior object will process the message
                if (await context.Switch.GetAllBehaviorAsync() && await context.Switch.GetListenerGazeAsync())
                {
                    _logger?.LogInformation("Generating listener behavior");
                    await _listenerBehaviorManagerLegacy.ProcessListenMessageAsync(document, request, context);
                    request.SourceId = await context.AgentInfo.GetCharacterIdAsync();
                    request.TargetId = await context.CurrentDialogue.GetSpeakerAsync();
                    addStrokeAttr();
                    //Why eventNode not added to bml node?
                }
            }
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessListenerFeedback()
        /// </remarks>
        private async Task ProcessListenerFeedbackAsync(IContext context, LegacyRequest request, XmlDocument document)
        {
            if (!await context.Switch.GetAllBehaviorAsync())
            {
                return;
            }
            request.Kind = NvbgRequestKind.Listen;
            _listenerBehaviorManagerLegacy.ProcessListenerFeedback(document, request);
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessVRBackchannel()
        /// </remarks>
        private async Task ProcessVRBackchannelAsync(IContext context, LegacyRequest request, XmlDocument document)
        {
            if (!await context.Switch.GetAllBehaviorAsync())
            {
                return;
            }
            await _listenerBehaviorManagerLegacy.ProcessVRBackChannelAsync(document, request, context);
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessVRBCFeedback()
        /// </remarks>
        private async Task ProcessVRBCFeedbackAsync(IContext context, LegacyRequest request, XmlDocument document)
        {
            if (!await context.Switch.GetAllBehaviorAsync())
            {
                return;
            }
            _listenerBehaviorManagerLegacy.ProcessVRBCFeedback(document, request);
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessVRNVBGFeedbackRuleTest()
        /// </remarks>
        private async Task ProcessVRNVBGFeedbackRuleTestAsync(IContext context, LegacyRequest request, XmlDocument document)
        {
            if (!await context.Switch.GetAllBehaviorAsync())
            {
                return;
            }
            _listenerBehaviorManagerLegacy.ProcessVRNVBGFeedbackRuleTest(document, request);
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessGazeMessage()
        /// </remarks>
        private async Task ProcessGazeMessageAsync(IContext context, LegacyRequest request, XmlDocument document)
        {
            if (!await context.Switch.GetAllBehaviorAsync())
            {
                return;
            }
            await _gazeBehaviorManagerLegacy.ProcessGazeMessageAsync(document, context, request);
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessNegotiationMessage()
        /// </remarks>
        private async Task ProcessNegotiationMessageAsync(IContext context, LegacyRequest request)
        {
            if (await context.Switch.GetAllBehaviorAsync())
            {
                request.Kind = NvbgRequestKind.Negotiation;
                #region Simulate VrExpressHandler.ChangePosture()
                //Nothing
                #endregion
            }
        }

        /// <remarks>
        /// Simulate VrExpressHandler.ProcessBodyMessage()
        /// </remarks>
        private async Task ProcessBodyMessageAsync(IContext context, LegacyRequest request, XmlDocument document)
        {
            if (!await context.Switch.GetAllBehaviorAsync())
            {
                return;
            }
            request.Kind = NvbgRequestKind.Posture;
            if (request.SourceId == await context.AgentInfo.GetCharacterIdAsync())
            {
                var bodyNode = document.GetElementsByTagName("body")[0];
                await context.AgentInfo.SetPostureIdAsync(bodyNode.Attributes["posture"].Value);
            }
        }
        #endregion

        #region NVBGManager Methods
        /// <summary>
        /// Generates idle behavior rules to be inserted into the output bml
        /// This method randomlygenerates an idle animation which is specified in the
        /// rule input file for that character, OR, it generates a gaze from the saliency map.
        /// The gazes are controlled by the saliency map and the pawns are specified in 
        /// the saliency map's xml file.
        /// </summary>
        /// <remarks>Simulate NVBGManager.CreateIdleBehavior()</remarks>
        private async Task CreateIdleBehaviorAsync(IContext context, LegacyRequest request, XmlDocument document)
        {
            //assume source ID was not changed by other handlers before this method get called
            Trace.Assert(request.SourceId == await context.AgentInfo.GetCharacterIdAsync(), "NVBG refactor simplification assumption broken");
            var bmlNode = GetOrCreateBmlNode(document);
            var ruleDocument = await context.GetBehaviorXmlDocumentAsync();
            var ruleNodeList = ruleDocument.GetElementsByTagName("rule");
            var randomizer = new Random();
            var randomNumber = randomizer.Next() % 2;
            //Original Comment: int randomNumber = 1; //Original Comment:  currently only generate idle gaze

            foreach (var node in ruleNodeList.Cast<XmlNode>())
            {
                try
                {
                    var keyword = node.Attributes["keyword"].Value;
                    switch (randomNumber)
                    {
                        case 0:
                            if (keyword == "idle_animation")
                            {
                                var ruleTag = document.CreateElement("rule");
                                ruleTag.AttachAttribute("participant", await context.AgentInfo.GetCharacterIdAsync());
                                ruleTag.AttachAttribute("type", "idle_animation");
                                ruleTag.AttachAttribute("priority", node.Attributes["priority"].Value);
                                ruleTag.AttachAttribute("pose", await context.AgentInfo.GetPostureIdAsync());
                                bmlNode.AppendChild(ruleTag);
                                goto loopend;
                            }
                            break;
                        case 1:
                            if (await context.Switch.GetSaliencyGlanceAsync() && await context.GetHasSaliencyMapXmlAsync())
                            {
                                await _saliencyMapManagerLegacy.GenerateGazeCommandAsync(context, document);
                            }
                            goto loopend;
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Error while creating Idle behavior");
                }
            }
            loopend:;
        }

        /// <summary>
        /// Transforms the xml document and generates another xml document
        /// The transform rules perform the mapping to behavior
        /// </summary>
        private async Task<XmlDocument> ApplyTransformAsync(IContext context, XmlDocument inputDocument)
        {
            _logger?.LogInformation("Mapping rules to output behavior");
            var temp = new XmlDocument();
            temp.LoadXml(inputDocument.InnerXml);
            temp.Normalize();
            var characterId = await context.AgentInfo.GetCharacterIdAsync();
            var ruleDocument = await context.GetBehaviorXmlDocumentAsync();
            var xsltObjectMethods = new XsltObjectMethods(_logger, characterId, ruleDocument);
            var xsltArgList = new XsltArgumentList();
            xsltArgList.AddExtensionObject("http://ExternalFunction.xslt.isi.edu", xsltObjectMethods);
            using var stringWriter = new StringWriter();
            _transformXsl.Transform(temp.CreateNavigator(), xsltArgList, stringWriter);
            var result = new XmlDocument();
            result.LoadXml(stringWriter.ToString());
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Removes overlapping rules in the final generated bml based on their priorities
        /// Basically checks to see if the time-marks start/end overlap each other and then 
        /// checks to see if it's an animation/gaze. It prunes if there are conflicts e.g. 2 animations at same time
        /// </summary>
        /// <remarks>Legacy Code</remarks>
        private void FilterGestures(XmlDocument document)
        {
            try
            {
                _logger?.LogInformation("Filtering behaviors based on priority");

                XmlNode bmlNode = document.GetElementsByTagName("bml")[0];
                XmlNodeList rules = document.GetElementsByTagName("rule");
                bool[] rulesToBeDeleted = new bool[rules.Count];
                for (int i = 0; i < rules.Count; ++i)
                {
                    rulesToBeDeleted[i] = false;
                }

                if (rules.Count > 2)
                {
                    for (int i = 0; i < rules.Count; ++i)
                    {
                        if (rulesToBeDeleted[i])
                            continue;

                        XmlNode currentRule = rules[i];
                        //string currentName = currentRule.Attributes["type"].Value;
                        string currentPriority = currentRule.Attributes["priority"].Value;
                        string currentStart = currentRule.Attributes["ready"].Value;
                        string currentEnd = currentRule.Attributes["relax"].Value;

                        string ruleType = "";
                        for (int k = 0; k < currentRule.ChildNodes.Count; ++k)
                        {
                            ruleType = currentRule.ChildNodes[k].Name;
                            if (!ruleType.Equals("#comment"))
                                break;
                        }


                        currentStart = currentStart.Replace("T", "");
                        currentEnd = currentEnd.Replace("T", "");


                        for (int j = i + 1; j < rules.Count; ++j)
                        {
                            if (rulesToBeDeleted[j])
                                continue;

                            XmlNode nextRule = rules[j];
                            //string nextName = nextRule.Attributes["type"].Value;
                            string nextPriority = nextRule.Attributes["priority"].Value;
                            string nextStart = nextRule.Attributes["ready"].Value;
                            string nextEnd = nextRule.Attributes["relax"].Value;
                            string nextRuleType = "";
                            for (int k = 0; k < nextRule.ChildNodes.Count; ++k)
                            {
                                nextRuleType = nextRule.ChildNodes[k].Name;
                                if (!nextRuleType.Equals("#comment"))
                                    break;
                            }


                            nextStart = nextStart.Replace("T", "");
                            nextEnd = nextEnd.Replace("T", "");

                            if (((Convert.ToInt32(currentStart) <= Convert.ToInt32(nextStart)) &&
                                (Convert.ToInt32(currentEnd) > Convert.ToInt32(nextStart))) ||
                                ((Convert.ToInt32(currentStart) > Convert.ToInt32(nextStart)) &&
                                (Convert.ToInt32(currentStart) <= Convert.ToInt32(nextEnd))))
                            {

                                //remove the animation only if it conflicts with another animation. Animations
                                // and gazes are fine as they do not conflict
                                if (!(nextRuleType.Equals("animation") && ruleType.Equals("animation")))
                                    return;


                                if (Convert.ToInt32(currentPriority) > Convert.ToInt32(nextPriority))
                                    rulesToBeDeleted[i] = true;
                                else if (Convert.ToInt32(currentPriority) < Convert.ToInt32(nextPriority))
                                    rulesToBeDeleted[j] = true;
                                else if (Convert.ToInt32(currentPriority) == Convert.ToInt32(nextPriority))
                                {
                                    Random randomGenerator = new Random();
                                    int remainder = randomGenerator.Next() % 2;

                                    if (remainder == 0)
                                        rulesToBeDeleted[i] = true;
                                    else
                                        rulesToBeDeleted[j] = true;
                                }
                            }
                        }
                    }
                }

                for (int i = 0, j = 0; i < rules.Count; ++i, ++j)
                {
                    XmlNode nodeToBeDeleted = rules[i];
                    if (rulesToBeDeleted[j])
                    {
                        bmlNode.RemoveChild(nodeToBeDeleted);
                        --i;
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error while filtering gestures based on priorities");
            }
        }

        /// <summary>
        /// Removes unwanted xml nodes generated during processing
        /// </summary>
        /// <remarks>Legacy Code</remarks>
        private void CleanUpBml(XmlDocument document)
        {
            XmlNode bml = document.GetElementsByTagName("bml")[0];
            XmlNodeList rules = document.GetElementsByTagName("rule");
            List<XmlNode> rulesCopy;

            rulesCopy = new List<XmlNode>();

            for (int i = 0; i < rules.Count; ++i)
            {
                XmlNode rule = rules[i];
                XmlNode copyRule = rule.Clone();
                rulesCopy.Add(copyRule);
            }

            while (document.ContainsAnyElementNamedAs("rule"))
            {
                bml.RemoveChild(document.GetElementsByTagName("rule")[0]);
            }

            for (int i = 0; i < rulesCopy.Count; ++i)
            {
                XmlNodeList childNodes = rulesCopy[i].ChildNodes;

                for (int j = 0; j < childNodes.Count; ++j)
                {
                    XmlNode nodeToAdd = childNodes[j].Clone();

                    if (nodeToAdd.Name.Equals("animation"))
                    {
                        if (!nodeToAdd.Attributes["name"].Value.Equals("none"))
                            bml.AppendChild(nodeToAdd);
                    }
                    else
                        bml.AppendChild(nodeToAdd);
                }
            }

            rulesCopy.Clear();

            XmlNodeList feedbacks = document.GetElementsByTagName("feedbacks");
            List<XmlNode> feedbacksCopy;

            feedbacksCopy = new List<XmlNode>();

            for (int i = 0; i < feedbacks.Count; ++i)
            {
                XmlNode feedback = feedbacks[i];
                XmlNode copyFeedback = feedback.Clone();
                feedbacksCopy.Add(copyFeedback);
            }

            while (document.ContainsAnyElementNamedAs("feedback"))
            {
                bml.RemoveChild(document.GetElementsByTagName("feedback")[0]);
            }


            for (int i = 0; i < feedbacksCopy.Count; ++i)
            {
                XmlNodeList childNodes = feedbacksCopy[i].ChildNodes;

                for (int j = 0; j < childNodes.Count; ++j)
                {
                    XmlNode nodeToAdd = childNodes[j].Clone();

                    if (nodeToAdd.Name.Equals("animation"))
                    {
                        if (!nodeToAdd.Attributes["name"].Value.Equals("none"))
                            bml.AppendChild(nodeToAdd);
                    }
                    else
                        bml.AppendChild(nodeToAdd);
                }
            }

            feedbacksCopy.Clear();

            //Removing nods for first few words as this conflicts with gazes and causes popping
            //SB needs to handle this better
            XmlNodeList nods = document.GetElementsByTagName("head");
            List<XmlNode> toBeRemoved = new List<XmlNode>();

            for (int i = 0; i < nods.Count; ++i)
            {
                XmlAttribute type = nods[i].Attributes["type"];
                if (type.Value.Equals("NOD"))
                {
                    try
                    {
                        XmlAttribute relaxTime = nods[i].Attributes["relax"];
                        string time = relaxTime.Value;
                        time = time.Replace("sp1:T", "");

                        if (time.Contains("+"))
                        {
                            time = time.Substring(0, time.Length - time.IndexOf("+") - 2);
                        }

                        if (time.Contains("-"))
                        {
                            time = time.Substring(0, time.Length - time.IndexOf("-") - 2);
                        }

                        if (Convert.ToInt32(time) < 5)
                        {
                            toBeRemoved.Add(nods[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error while trying clean bml for nods");
                    }
                }
            }

            for (int i = 0; i < toBeRemoved.Count; ++i)
            {
                bml.RemoveChild(toBeRemoved[i]);
            }

            document.Normalize();

        }

        /// <summary>
        /// Attach vrAgent partial messages to the final bml which will notify us of when the character finishes speaking each word in the sentence
        /// </summary>
        /// <remarks>Legacy Code</remarks>
        private void AttachVRAgentPartialMessage(XmlDocument document, string messageId)
        {
            _logger?.LogInformation("Attaching vrAgentPartial messages");

            XmlNode speechTag = document.GetElementsByTagName("speech")[0];
            XmlNode bmlTag = document.GetElementsByTagName("bml")[0];

            XmlNodeList eventTagList = document.GetElementsByTagName("sbm:event", "http://ict.usc.edu");
            XmlNode lastEvent;
            try
            {
                if (eventTagList.Count != 0)
                    lastEvent = eventTagList.Item(eventTagList.Count - 1);
                else
                    lastEvent = speechTag.NextSibling;


                XmlNodeList timeMarkers = document.GetElementsByTagName("mark");
                List<string> prefixWordBuffer = new List<string>();
                string prefixWord = "";

                string spId = speechTag.Attributes["id"].Value;
                spId += ":";


                if (timeMarkers.Count > 0)
                {
                    for (int i = 0; i < timeMarkers.Count; i += 2)
                    {
                        prefixWord = "";
                        XmlNode wordNode = timeMarkers[i].NextSibling;
                        string wordText = wordNode.InnerText;
                        wordText.Trim();
                        prefixWordBuffer.Add(wordText);

                        for (int j = 0; j < prefixWordBuffer.Count; ++j)
                        {
                            string thisWord = prefixWordBuffer[j];
                            thisWord += " ";
                            thisWord = thisWord.Replace("\n", "");
                            thisWord = thisWord.Replace("\t", "");
                            thisWord = thisWord.Replace("\r", "");
                            prefixWord += thisWord;
                        }

                        string endingTimeMarker = $"T{i + 1}";

                        XmlNode eventTag = document.CreateElement("sbm:event");
                        var messageAttribute = $"vrAgentSpeech partial {messageId} {endingTimeMarker} {prefixWord}";
                        eventTag.AttachAttribute("message", messageAttribute);
                        eventTag.AttachAttribute("stroke", spId + endingTimeMarker);

                        bmlTag.InsertBefore(eventTag, lastEvent);

                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error while attaching vrAgentPartial Message");
            }

        }
        #endregion

        #region Helpers
        private static void LoadTransformXsl(XslCompiledTransform xslTransform, string transformXslText, XmlResolver? xmlResolver)
        {
            var xsltSettings = new XsltSettings();
            using var stringReader = new StringReader(transformXslText);
            var xmlReader = new XmlTextReader(stringReader);
            var resolver = xmlResolver ?? new XmlUrlResolver();
            xslTransform.Load(xmlReader, xsltSettings, resolver);
        }
        #endregion

        #region IDisposable
        private bool _disposedValue;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _lock.Dispose();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Nvbg()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}


