#nullable disable

using Microsoft.Extensions.Logging;
using NonverbalBehaviorGenerator.LegacyInterop;
using NonverbalBehaviorGenerator.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace NonverbalBehaviorGenerator.Legacy
{
    /// <summary>
    /// Class that handles messages that were 'listen' messages i.e. the character received the messages
    /// </summary>
    internal sealed class ListenerBehaviorManager
    {
        private readonly ILogger _logger;

        List<BackChannelMessage> m_BackChannelQueue;

        public ListenerBehaviorManager(ILogger logger)
        {
            _logger = logger;

            m_BackChannelQueue = new List<BackChannelMessage>();
        }

        /// <summary>
        /// Not being used now
        /// Please ask STACY about what the purpose of this message was
        /// </summary>
        /// <param name="_inputDoc"></param>
        /// <param name="_currentMessage"></param>
        public void ProcessListenerFeedback(XmlDocument _inputDoc, LegacyRequest _currentMessage)
        {
            try
            {
                BackChannelMessage backChannelMessage = new BackChannelMessage();

                XmlNode feedBack = _inputDoc.GetElementsByTagName("listenerFeedback")[0];
                backChannelMessage.Agreement = feedBack.Attributes["agreement"].Value;
                backChannelMessage.UtteranceID = feedBack.Attributes["uttid"].Value;
                backChannelMessage.MsgID = _currentMessage.MessageId + "-";
                backChannelMessage.MsgID += backChannelMessage.UtteranceID;
                backChannelMessage.Speaker = feedBack.Attributes["speaker"].Value;
                backChannelMessage.Polarity = feedBack.Attributes["polarity"].Value;

                if (backChannelMessage.UtteranceID.StartsWith("gsym"))
                    return;
                if (backChannelMessage.Agreement.Equals("neutral") || backChannelMessage.Equals("unknown"))
                    return;

                m_BackChannelQueue.Add(backChannelMessage);
            }
            catch (Exception e)
            {
                _logger?.LogInformation("ERROR in listener behaviour handler while processing listener feedback message" + e.ToString());
            }
        }


        /// <summary>
        /// This is where the listener behavior is added
        /// The listener basically should look at the speaker
        /// If the character is neither a speaker or a listener, he looks at both the speaker and then the llistener
        /// </summary>
        /// <param name="_inputDoc"></param>
        /// <param name="_currentMessage"></param>
        /// <param name="context"></param>
        public async Task ProcessListenMessageAsync(XmlDocument _inputDoc, LegacyRequest _currentMessage, IContext context)
        {
            try
            {
                _currentMessage.MessageId += "-participant";

                if ((await context.CurrentDialogue.GetListenerAsync()).Equals(await context.AgentInfo.GetCharacterIdAsync()))
                {
                    XmlNode gaze = _inputDoc.CreateElement("gaze");

                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "target", await context.CurrentDialogue.GetSpeakerAsync());
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "start", "0.2");
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "sbm:joint-range", "HEAD EYES");
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "xmlns:sbm", "http://ict.usc.edu");

                    _inputDoc.GetElementsByTagName("bml")[0].AppendChild(gaze);
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, _inputDoc.GetElementsByTagName("bml")[0], "xmlns:sbm", "http://ict.usc.edu");

                    if (!(await context.CurrentDialogue.GetSpeakerAsync()).Equals(await context.GazeInfo.GetGazeTargetAsync()))
                    {
                        await context.GazeInfo.SetGazeAsync(await context.CurrentDialogue.GetSpeakerAsync(), "none", "0");
                    }
                }
                else if (!(await context.CurrentDialogue.GetSpeakerAsync()).Equals((await context.AgentInfo.GetCharacterIdAsync())) &&
                         !(await context.CurrentDialogue.GetListenerAsync()).Equals((await context.AgentInfo.GetCharacterIdAsync())))
                {
                    XmlNode gaze = _inputDoc.CreateElement("gaze");

                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "target", (await context.CurrentDialogue.GetSpeakerAsync()));
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "start", "0.2");
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "end", "1.5");
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "sbm:joint-range", "HEAD EYES");
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "xmlns:sbm", "http://ict.usc.edu");

                    _inputDoc.GetElementsByTagName("bml")[0].AppendChild(gaze);


                    XmlNode gazeListener = _inputDoc.CreateElement("gaze");

                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gazeListener, "target", (await context.CurrentDialogue.GetListenerAsync()));
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gazeListener, "start", "1.8");
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gazeListener, "sbm:joint-range", "HEAD EYES");
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, gazeListener, "xmlns:sbm", "http://ict.usc.edu");

                    _inputDoc.GetElementsByTagName("bml")[0].AppendChild(gazeListener);
                    XMLHelperMethods.AttachAttributeToNode(_inputDoc, _inputDoc.GetElementsByTagName("bml")[0], "xmlns:sbm", "http://ict.usc.edu");

                    if (!(await context.CurrentDialogue.GetListenerAsync()).Equals(await context.GazeInfo.GetGazeTargetAsync()))
                    {
                        await context.GazeInfo.SetGazeAsync(await context.CurrentDialogue.GetListenerAsync(), "none", "0");
                    }

                }
            }
            catch (Exception e)
            {
                _logger?.LogInformation("ERROR in listener behaviour handler while processing listen message" + e.ToString());
            }
        }

        /// <summary>
        /// Not being used now
        /// Please ask STACY about what the purpose of this message was
        /// </summary>
        /// <param name="_inputDoc"></param>
        /// <param name="_currentMessage"></param>
        /// <param name="context"></param>
        public async Task ProcessVRBackChannelAsync(XmlDocument _inputDoc, LegacyRequest _currentMessage, IContext context)
        {
            try
            {
                for (int i = 0; i < m_BackChannelQueue.Count; ++i)
                {
                    BackChannelMessage backChannelMessage = m_BackChannelQueue[i];

                    if (_currentMessage.MessageId.Equals(backChannelMessage.UtteranceID))
                    {
                        _currentMessage.SourceId = (await context.AgentInfo.GetCharacterIdAsync());
                        _currentMessage.MessageId = backChannelMessage.MsgID;
                        _currentMessage.TargetId = backChannelMessage.Speaker;

                        string agent = "other";
                        if ((await context.AgentInfo.GetCharacterIdAsync()).Equals("doctor"))
                            agent = "doctor";
                        else if ((await context.AgentInfo.GetCharacterIdAsync()).Equals("elder"))
                            agent = "elder";

                        if (backChannelMessage.Agreement.Equals("positive"))
                        {
                            XmlNode head = _inputDoc.CreateElement("head");

                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, head, "type", "NOD");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, head, "amount", "0.2");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, head, "repeats", "0.5");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, head, "veocity", "0.5");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, head, "start", "2");

                            _inputDoc.GetElementsByTagName("bml")[0].AppendChild(head);

                            if (agent.Equals("elder"))
                            {
                                XmlNode head1 = _inputDoc.CreateElement("head");

                                XMLHelperMethods.AttachAttributeToNode(_inputDoc, head1, "type", "NOD");
                                XMLHelperMethods.AttachAttributeToNode(_inputDoc, head1, "amount", "0.2");
                                XMLHelperMethods.AttachAttributeToNode(_inputDoc, head1, "repeats", "0.5");
                                XMLHelperMethods.AttachAttributeToNode(_inputDoc, head1, "veocity", "0.2");
                                XMLHelperMethods.AttachAttributeToNode(_inputDoc, head1, "start", "3");

                                _inputDoc.GetElementsByTagName("bml")[0].AppendChild(head1);
                            }
                        }
                        else if (backChannelMessage.Agreement.Equals("negative"))
                        {

                            XmlNode gaze = _inputDoc.CreateElement("gaze");

                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "target", backChannelMessage.Speaker);
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "start", "2");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, gaze, "end", "3");

                            XmlNode chest = _inputDoc.CreateElement("sbm:chest");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, chest, "pitch", "-30");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, chest, "xmlns:sbm", "http://ict.usc.edu");

                            gaze.AppendChild(chest);

                            XmlNode neck = _inputDoc.CreateElement("sbm:neck");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, neck, "pitch", "-50");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, neck, "headling", "30");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, neck, "xmlns:sbm", "http://ict.usc.edu");

                            gaze.AppendChild(neck);

                            XmlNode head = _inputDoc.CreateElement("sbm:head");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, head, "xmlns:sbm", "http://ict.usc.edu");
                            gaze.AppendChild(head);

                            _inputDoc.GetElementsByTagName("bml")[0].AppendChild(gaze);

                            XmlNode innerBrow = _inputDoc.CreateElement("face");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, innerBrow, "type", "facs");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, innerBrow, "au", "1");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, innerBrow, "start", "2");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, innerBrow, "end", "3");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, innerBrow, "amount", "2");
                            _inputDoc.GetElementsByTagName("bml")[0].AppendChild(innerBrow);

                            XmlNode outerBrow = _inputDoc.CreateElement("face");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, outerBrow, "type", "facs");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, outerBrow, "au", "2");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, outerBrow, "start", "2");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, outerBrow, "end", "3");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, outerBrow, "amount", "1");
                            _inputDoc.GetElementsByTagName("bml")[0].AppendChild(outerBrow);

                            XmlNode returnGaze = _inputDoc.CreateElement("gaze");
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, returnGaze, "target", backChannelMessage.Speaker);
                            XMLHelperMethods.AttachAttributeToNode(_inputDoc, returnGaze, "start", "3");
                            _inputDoc.GetElementsByTagName("bml")[0].AppendChild(returnGaze);

                            if (!backChannelMessage.Speaker.Equals(await context.GazeInfo.GetGazeTargetAsync()))
                            {
                                await context.GazeInfo.SetGazeAsync(backChannelMessage.Speaker, "none", "0");
                            }

                        }

                        m_BackChannelQueue.Remove(m_BackChannelQueue[i]);

                    }

                }
            }
            catch (Exception e)
            {
                _logger?.LogInformation("ERROR in listener behaviour handler while processing vrBackChannel message" + e.ToString());
            }

        }


        /// <summary>
        /// Not being used now
        /// Please ask STACY about what the purpose of this message was
        /// </summary>
        /// <param name="_inputDoc"></param>
        /// <param name="_currentMessage"></param>
        public void ProcessVRBCFeedback(XmlDocument _inputDoc, LegacyRequest _currentMessage)
        {
            try
            {
                BackChannelMessage backChannelMessage = new BackChannelMessage();

                XmlNode feedback = _inputDoc.GetElementsByTagName("feedback")[0];
                backChannelMessage.Speaker = feedback.Attributes["main-speaker"].Value;
                _currentMessage.TargetId = backChannelMessage.Speaker;

                backChannelMessage.UtteranceID = feedback.Attributes["utterance"].Value;
                backChannelMessage.Progress = Convert.ToInt32(feedback.Attributes["progress"].Value);
                string tempComplete = feedback.Attributes["complete"].Value;

                if (tempComplete.Equals("yes"))
                    backChannelMessage.IsComplete = true;
                else if (tempComplete.Equals("no"))
                    backChannelMessage.IsComplete = false;

                XmlNodeList partialTextNodeList = _inputDoc.GetElementsByTagName("partial-text");
                if (partialTextNodeList.Count > 0)
                    backChannelMessage.PartialText = partialTextNodeList[0].InnerText;

                m_BackChannelQueue.Add(backChannelMessage);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "ERROR in listener behaviour handler while processing VRBCFeedback message");
            }
        }


        public void ProcessVRNVBGFeedbackRuleTest(XmlDocument _inputDoc, LegacyRequest _currentMessage)
        {
            // to be implemented for test purposes
        }


    }
}
