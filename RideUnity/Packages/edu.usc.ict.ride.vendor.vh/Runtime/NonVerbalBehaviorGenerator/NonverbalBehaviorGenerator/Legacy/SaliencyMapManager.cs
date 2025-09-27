#nullable disable

using Microsoft.Extensions.Logging;
using NonverbalBehaviorGenerator.Models;
using System;
using System.Threading.Tasks;
using System.Xml;

namespace NonverbalBehaviorGenerator.Legacy
{
    /// <summary>
    /// Refactor of SaliencyMap
    /// </summary>
    internal sealed class SaliencyMapManager
    {
        private ILogger _logger;

        public SaliencyMapManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <remarks>Refactor of SaliencyMap.GenerateGazeCommand()</remarks>
        public Task GenerateGazeCommandAsync(IContext context, XmlDocument document)
        {
            throw new NotImplementedException();
        }

        /// <remarks>Refactor of SaliencyMap.UpdateGazeRange()</remarks>
        public Task UpdateGazeRangeAsync(XmlDocument document)
        {
            throw new NotImplementedException();
        }

        /// <remarks>Refactor of SaliencyMap.TrackGazeEvent()</remarks>
        public Task TrackGazeEventAsync(XmlDocument document, IContext context)
        {
            throw new NotImplementedException();
        }
    }
}
