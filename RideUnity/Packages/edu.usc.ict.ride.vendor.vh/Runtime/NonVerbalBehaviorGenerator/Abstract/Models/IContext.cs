#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    ///<remarks>Refactor of NVBGCharacter. Not thread safe, create a new instance for each thread.</remarks>
    public interface IContext : IDisposable
    {

        //Task<ITransaction> BeginTransactionAsync(bool readOnly = false);

        ///<remarks>Simulate VrExpressHandler.m_spId</remarks>
        Task<string?> GetCurrentSpeechId();

        ///<remarks>Simulate VrExpressHandler.m_spId</remarks>
        Task SetCurrentSpeechId(string? value);

        Task<string> GetStoryPointIdAsync();

        Task SetStoryPointIdAsync(string value);

        Task<string?> GetFacialExpressionXmlAsync();

        Task SetFacialExpressionXmlAsync(string? xml);

        IDictionary<string, string>? ParseTreeCache { get; }

        #region Refactor of NVBGCharacter

        /// <remarks>Refactor of NVBGCharacter.m_ruleInputXmlDocument</remarks>
        Task<string> GetBehaviorRuleXmlAsync();

        /// <remarks>This method is for backward compatability. For new projects, do not use this interface.</remarks>
        Task SetBehaviorRuleXmlAsync(string xml);

        Task<string?> GetSaliencyMapXmlAsync();

        ///<remarks>Refactor of NVBGCharacter.LoadSaliencyMap()</remarks>
        Task SetSaliencyMapXmlAsync(string? xml);

        ICharacterInfo AgentInfo { get; }

        ISwitches Switch { get; }

        IGazeInfo GazeInfo { get; }

        IDialogue CurrentDialogue { get; }

        IConversationInfo ConversationInfo { get; }

        ///<summary>Original Comment: given one story point, update to new initial saliency map, keyword to object map, and emotion</summary>
        ///<remarks>Legacy NVBGCharacter.UpdateOneStoryPoint()</remarks>
        Task UpdateOneStoryPointAsync(string storyPointId);
        #endregion
    }
}
