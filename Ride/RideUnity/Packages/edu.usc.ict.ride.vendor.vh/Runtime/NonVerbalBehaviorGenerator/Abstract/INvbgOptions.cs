#nullable enable

using NonverbalBehaviorGenerator.Models;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace NonverbalBehaviorGenerator
{
    public interface INvbgOptions
    {
        string TransformXsl { get; }
        XmlResolver? TransformXslResolver { get; }
        string ParserModelDirectory { get; }
        Dictionary<string, Stream> Streams { get; }

        IContextFactory ContextFactory { get; }
    }
}
