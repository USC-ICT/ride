using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BllipParser
{
    public interface IParser
    {
        Task InitializeAsync(ParserConfiguration configuration, Dictionary<string, Stream> streams = default, CancellationToken cancellationToken = default);

        Task<ParseResponse> ParseAsync(ParseRequest request, CancellationToken cancellationToken = default);
    }
}
