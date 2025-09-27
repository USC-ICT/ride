#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    internal sealed class InMemoryContext : IContext
    {
        private readonly InMemoryCharacterInfo _characterInfo;

        private readonly InMemorySwitches _switch;

        private readonly InMemoryGazeInfo _gazeInfo = new InMemoryGazeInfo();

        private readonly InMemoryDialogue _currentDialogue = new InMemoryDialogue();

        private readonly InMemoryConversationInfo _conversationInfo = new InMemoryConversationInfo();

        private readonly IDictionary<string, string>? _parseTreeCache;

        public InMemoryContext(string characterId, string ruleXml, string? facialExpressionXml, string idlePostureId, string storyPointId, IDictionary<string, string>? parseTreeCache, bool allBehavior, bool saliencyGlance, bool saliencyIdleGaze, bool speakerGaze, bool speakerGestures, bool listenerGaze, bool poseRules)
        {
            this.facialExpressionXml = facialExpressionXml;
            this.storyPointId = storyPointId;
            _parseTreeCache = parseTreeCache;
            _switch = new InMemorySwitches(allBehavior, saliencyGlance, saliencyIdleGaze, speakerGaze, speakerGestures, listenerGaze, poseRules);

            #region Refactor of NVBGCharacter.ctor()
            _characterInfo = new InMemoryCharacterInfo(characterId, idlePostureId);
            #endregion

            #region Refactor of NVBGCharacter.LoadXML()
            this.ruleXml = ruleXml;
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



        public IDictionary<string, string>? ParseTreeCache => _parseTreeCache;

        private string? facialExpressionXml;

        public Task<string?> GetFacialExpressionXmlAsync() => Task.FromResult(facialExpressionXml);

        public Task SetFacialExpressionXmlAsync(string? xml) { 
            facialExpressionXml = xml;
            return Task.CompletedTask;
        }

        private string ruleXml;

        public Task<string> GetBehaviorRuleXmlAsync() => Task.FromResult(ruleXml);

        public Task SetBehaviorRuleXmlAsync(string xml) { 
            ruleXml = xml;
            return Task.CompletedTask;
        }

        private string? saliencyMapXml = null;

        public Task<string?> GetSaliencyMapXmlAsync() => Task.FromResult(saliencyMapXml);

        public Task SetSaliencyMapXmlAsync(string? xml) { 
            saliencyMapXml = xml;
            return Task.CompletedTask;
        }

        private string? currentSpeechId = null;

        public Task<string?> GetCurrentSpeechId() => Task.FromResult(currentSpeechId);

        public Task SetCurrentSpeechId(string? value) { 
            currentSpeechId = value;
            return Task.CompletedTask;
        }

        private string storyPointId;

        public Task<string> GetStoryPointIdAsync() => Task.FromResult(storyPointId);

        public Task SetStoryPointIdAsync(string value)
        {
            storyPointId = value;
            return Task.CompletedTask;
        }

        public ICharacterInfo AgentInfo => _characterInfo;

        public ISwitches Switch => _switch;

        public IGazeInfo GazeInfo => _gazeInfo;

        public IDialogue CurrentDialogue => _currentDialogue;

        public IConversationInfo ConversationInfo => _conversationInfo;

        public Task UpdateOneStoryPointAsync(string storyPointId) {
            throw new NotImplementedException();
        }

        #region Transactions
        //private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        //private sealed class InMemoryContextTransaction : ITransaction
        //{
        //    private readonly ReaderWriterLockSlim _lock;
        //    private readonly bool _readOnly;

        //    public InMemoryContextTransaction(ReaderWriterLockSlim @lock, bool readOnly)
        //    {
        //        _lock = @lock;
        //        _readOnly = readOnly;
        //        if (readOnly)
        //        {
        //            @lock.EnterReadLock();
        //        }
        //        else 
        //        {
        //            @lock.EnterWriteLock();
        //        }
        //    }

        //    public ValueTask DisposeAsync()
        //    {
        //        if (_readOnly) 
        //        {
        //            _lock.ExitReadLock();
        //        }
        //        else
        //        {
        //            _lock.ExitWriteLock();
        //        }
        //        return default;//completed
        //    }
        //}

        //public Task<ITransaction> BeginTransactionAsync(bool readOnly = false) => Task.FromResult<ITransaction>(new InMemoryContextTransaction(_lock, readOnly));
        #endregion

        #region IDisposable
        public void Dispose() { 
            //_lock.Dispose();
        }
        #endregion
    }
}
