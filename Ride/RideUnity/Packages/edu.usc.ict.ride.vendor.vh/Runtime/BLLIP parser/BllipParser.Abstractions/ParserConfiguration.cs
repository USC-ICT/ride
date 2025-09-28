using System.Collections.Generic;

namespace BllipParser
{
    public sealed class ParserConfiguration
    {
        /* Message from BLLIP parser
        static void usage(const char *program) 
        {
          cerr << "\n*** Usage information for " << program << " ***\n";

          cerr << "\nDefault use: " << program << " DATA/ [input file]\n";
          cerr << "If no input file supplied, stdin is assumed.\n";

          cerr << "\nRun mode:\n";
          cerr << "-M: language modeling flag\n";
          cerr << "-N: number of parses to produce in n-best parsing\n"; 

          cerr << "\nPerformance/Quality:\n";
          cerr << "-s: small training corpus flag [off by default]\n";
          cerr << "-t: number of threads [1 -- multithreading may be unstable]\n";
          cerr << "-T: over-parsing level [210]\n";
          cerr << "-p: smooth known part of speech probabilities. Set to a float to enable. [0]\n";

          cerr << "\nInput:\n";
          cerr << "-C: case-insensitive flag\n";
          cerr << "-K: pre-tokenized data flag (implied if -LAr)\n";
          cerr << "-E: use external POS tags file (see first-stage/README.rst for format)\n";
          cerr << "-l: skip sentences exceeding specified length [100]\n";
          cerr << "-L: language selection (En|Ch|Ar) [En]\n";
          cerr << "-n: process every Nth sentence only\n";

          cerr << "\nOutput:\n";
          cerr << "-d: print debug info at specified detail level\n";
          cerr << "-P: pretty-print flag\n";
          cerr << "-S: silent failure flag\n";

          cerr << "\nSee README file for additional information.\n\n";
        }
        */

        public string ModelDirectory { get; set; } = ".";

        public bool? Untokenized { get; set; }

        public int? OverParsingLevel { get; set; }

        //TODO: add other items when needed

        public (int Argc, string[] Argv) ConvertToNativeArgs()
        {
            var argc = 0;
            var argv = new List<string>();

            argc++;
            argv.Add(ModelDirectory);

            if (Untokenized == true)
            {
                argc++;
                argv.Add("-K");
            }

            if (OverParsingLevel.HasValue)
            {
                argc++;
                argv.Add($"-T{OverParsingLevel}");
            }

            return (argc, argv.ToArray());
        }
    }
}
