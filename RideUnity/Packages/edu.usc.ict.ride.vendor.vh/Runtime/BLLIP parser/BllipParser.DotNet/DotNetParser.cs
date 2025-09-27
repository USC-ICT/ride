using BllipParser.DotNet.Vanilla;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BllipParser.DotNet
{
    public sealed class DotNetParser : IParser
    {
        private const uint BufferLengthFactor = 10;

        private static ReaderWriterLockSlim @lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static ConcurrentBag<int> tokens = new ConcurrentBag<int>(Enumerable.Range(0, Feature_global.MAXNUMTHREADS));
        private static uint numConfigurationVersions = 0;

        /// <summary>
        /// Initialize the BLLIP parser.
        /// </summary>
        public static void Initialize(ParserConfiguration configuration, Dictionary<string, Stream> streams)
        {
            @lock.EnterWriteLock();
            try
            {
                Debug.Assert(tokens.Count == BllipParser.DotNet.Vanilla.Feature_global.MAXNUMTHREADS);
                var (argc, argv) = configuration.ConvertToNativeArgs();
                export.initialize(argc, argv, streams);
                numConfigurationVersions++;
            }
            finally
            {
                @lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Parse a sentence.
        /// </summary>W
        public static ParseResponse Parse(ParseRequest request)
        {
            @lock.EnterReadLock();
            try
            {
                if (numConfigurationVersions == 0)
                {
                    throw new InvalidOperationException($"BLLIP parser not initialized. Call {nameof(DotNetParser)}.{nameof(Initialize)} to initialize.");
                }
                if (!tokens.TryTake(out var threadId))//get a token
                {
                    throw new InvalidOperationException($"Too many concurrent requests. Underlying native BLLIP parser supports upto {Feature_global.MAXNUMTHREADS} threads.");
                }
                try
                {
                    var text = ParserHelpers.CreateNativeRequestString(request);
                    var bufferCapacity = text.Length * BufferLengthFactor;
                    var stringBuilder = new StringBuilder((int)bufferCapacity);
                    var resultLength = export.parse_and_format_to_buffer(threadId, text, (uint)bufferCapacity, stringBuilder);
                    if (resultLength + 1 > bufferCapacity)
                    {
                        throw new InvalidOperationException("BLLIP parser parse result too long");
                    }
                    var responseString = stringBuilder.ToString();
                    var response = ParserHelpers.CreateResponseFromNativeResponseString(request, responseString);
                    return response;
                }
                finally
                {
                    tokens.Add(threadId);//realease token
                }
            }
            finally
            {
                @lock.ExitReadLock();
            }
        }

        #region IParser
        public Task InitializeAsync(ParserConfiguration configuration, Dictionary<string, Stream> streams = default, CancellationToken cancellationToken = default)
        {
            Initialize(configuration, streams);
            return Task.CompletedTask;
        }

        public Task<ParseResponse> ParseAsync(ParseRequest request, CancellationToken cancellationToken = default)
        {
            var result = Parse(request);
            return Task.FromResult(result);
        }
        #endregion
    }
}
