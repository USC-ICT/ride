using NonverbalBehaviorGenerator.Models;
using System;
using System.Xml;

namespace NonverbalBehaviorGenerator
{
    ///<remarks>Refactor of NVBGCharacter</remarks>
    internal sealed class Character
    {
        public Character(string characterId, string ruleXml, string idlePostureId, string saliencyMapXml, string storyPoint)
        {
            #region Refactor of NVBGCharacter.ctor()
            _agnetInfo = new CharacterInfo(characterId) { 
                PostureId = idlePostureId,
            }; 
            #endregion

            #region Refactor of NVBGCharacter.LoadXML()
            _ruleXmlDocument.LoadXml(ruleXml); 
            #endregion
            /*
            #region Refactor of SaliencyMap.init()
            //Original Comment: saliency map is for generating subconscious gazes, when character is in idle state or certain keyword is mentioned
            throw new NotImplementedException();
            #endregion

            #region These 2 operations are executed together each time a saliency map is set
            LoadSaliencyMap(saliencyMapXml);
            UpdateOneStoryPoint(storyPoint);
            #endregion
            */
        }

        public void ReloadRuleXml(string xml)
        {
            _ruleXmlDocument.LoadXml(xml);
        }

        #region Refactor of NVBGCharacter
        private readonly CharacterInfo _agnetInfo;

        private readonly Switches _switch = new Switches();

        private readonly GazeInfo _gazeInfo = new GazeInfo();

        /// <remarks>Refactor of NVBGCharacter.m_ruleInputXmlDocument</remarks>
        private readonly XmlDocument _ruleXmlDocument = new XmlDocument();

        private readonly Dialogue _currentDialogue = new Dialogue();

        private readonly ConversationInfo _conversationInfo = new ConversationInfo();

        ///<remarks>Refactor of NVBGCharacter.m_saliencyMapInitialized</remarks>
        private bool hasSaliencyMap;

        private readonly XmlDocument _saliencyMapXmlDocument = new XmlDocument();

        public CharacterInfo AgentInfo => _agnetInfo;

        public Switches Switch => _switch;

        public GazeInfo GazeInfo => _gazeInfo;

        /// <summary>
        /// The character rule XML file.
        /// </summary>
        public XmlDocument BehaviorFile => _ruleXmlDocument;

        public Dialogue CurrentDialogue => _currentDialogue;

        public ConversationInfo ConversationInfo => _conversationInfo;

        ///<remarks>Refactor of NVBGCharacter.HasSaliencyMap</remarks>
        public bool HasSaliencyMap => hasSaliencyMap;

        ///<remarks>Refactor of NVBGCharacter.LoadSaliencyMap()</remarks>
        private void LoadSaliencyMap(string saliencyMapXml)
        {
            _saliencyMapXmlDocument.LoadXml(saliencyMapXml);
            hasSaliencyMap = true;
        }

        ///<summary>Original Comment: given one story point, update to new initial saliency map, keyword to object map, and emotion</summary>
        ///<remarks>Legacy NVBGCharacter.UpdateOneStoryPoint()</remarks>
        private void UpdateOneStoryPoint(string storyPoint)
        {
            throw new NotImplementedException();
        } 
        #endregion

        #region Refactor of SaliencyMap
        /// <remarks>Refactor of SaliencyMap.GenerateGazeCommand()</remarks>
        public void GenerateGazeCommand(Character data, XmlDocument document)
        {
            throw new NotImplementedException();
        }

        /// <remarks>Refactor of SaliencyMap.UpdateGazeRange()</remarks>
        public void UpdateGazeRange(XmlDocument document)
        {
            throw new NotImplementedException();
        }

        /// <remarks>Refactor of SaliencyMap.TrackGazeEvent()</remarks>
        public void TrackGazeEvent(XmlDocument document, Character data)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
