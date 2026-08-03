// RAG eval harness, JS side - scores the VHToolkit Studio wizard's stub retriever
// (study-config.js) against a dataset, for comparison with the real package retrieval
// measured by the C# harness (csharp/). Same dataset convention as the C# harness: a
// folder with pairs.json plus corpus documents (corpus/ subfolder, or directly in the
// folder). The stub is slated for retirement once Studio uses the package retrieval;
// this scorer documents the gap until then.
//
// Modes:
//   node run-eval.js                     -> stub scorer on the default dataset
//                                           (datasets/vhtoolkit)
//   node run-eval.js --dataset <folder>  -> same, on any dataset folder
//   node run-eval.js --csv X             -> scores a CSV of "questionId,rank,sourcePage"
//                                           rows produced externally
//
// A hit at k: any of the top-k retrieved chunks comes from an accepted page for the question.
// ASCII only.

var fs = require("fs");
var path = require("path");

var K = 3;
var here = __dirname;

var dsArg = process.argv.indexOf("--dataset");
var dataset = dsArg >= 0 ? path.resolve(process.argv[dsArg + 1])
                         : path.join(here, "datasets", "vhtoolkit");

var pairs = JSON.parse(fs.readFileSync(path.join(dataset, "pairs.json"), "utf8")).pairs;

function loadCorpus() {
  var dir = fs.existsSync(path.join(dataset, "corpus")) ? path.join(dataset, "corpus") : dataset;
  return fs.readdirSync(dir).filter(function (f) { return f.endsWith(".md") || f.endsWith(".txt"); }).map(function (f) {
    return { title: f.replace(/\.(md|txt)$/, ""), text: fs.readFileSync(path.join(dir, f), "utf8") };
  });
}

function report(name, results) {
  var all = results.length, hits = results.filter(function (r) { return r.hit; }).length;
  var hard = results.filter(function (r) { return r.hard; });
  var hardHits = hard.filter(function (r) { return r.hit; }).length;
  var easy = all - hard.length, easyHits = hits - hardHits;
  console.log("");
  console.log("=== " + name + " (hit@" + K + ") ===");
  console.log("overall: " + hits + "/" + all + " = " + (hits / all).toFixed(2));
  console.log("easy:    " + easyHits + "/" + easy + " = " + (easyHits / easy).toFixed(2));
  console.log("hard:    " + hardHits + "/" + hard.length + " = " + (hardHits / hard.length).toFixed(2));
  console.log("misses:");
  results.filter(function (r) { return !r.hit; }).forEach(function (r) {
    console.log("  " + r.id + " " + JSON.stringify(r.question) + " -> got [" + r.got.join(", ") + "] wanted [" + r.accept.join("|") + "]");
  });
}

var csvArg = process.argv.indexOf("--csv");
if (csvArg >= 0) {
  // Score the C# harness output.
  var rows = fs.readFileSync(process.argv[csvArg + 1], "utf8").trim().split(/\r?\n/)
    .map(function (l) { return l.split(","); });
  var byQ = {};
  rows.forEach(function (r) {
    var q = r[0], rank = parseInt(r[1], 10), page = r.slice(2).join(",");
    (byQ[q] = byQ[q] || [])[rank] = page;
  });
  var results = pairs.map(function (p) {
    var got = (byQ[p.id] || []).slice(0, K).filter(Boolean);
    return { id: p.id, question: p.question, accept: p.accept, hard: p.hard, got: got,
             hit: got.some(function (g) { return p.accept.indexOf(g) >= 0; }) };
  });
  report("C# KnowledgeIndex", results);
} else {
  // Run the actual wizard stub scorer.
  var shim = { StudyConfig: null, localStorage: { getItem: function () { return null; }, setItem: function () {}, removeItem: function () {} } };
  global.window = shim;
  global.localStorage = shim.localStorage;
  // The wizard's stub scorer lives in the vh_branch working copy; assumes the standard
  // workspace layout with svn_ride_trunk and svn_vh_branch checked out side by side.
  var src = fs.readFileSync(path.join(here, "..", "..", "..", "svn_vh_branch", "vh-study-wizard", "shared", "study-config.js"), "utf8");
  eval(src);
  var SC = shim.StudyConfig;
  if (!SC || !SC.retrieve) { console.error("FAILED to load StudyConfig from study-config.js"); process.exit(1); }

  var corpus = loadCorpus();
  var chunks = [];
  corpus.forEach(function (doc) { chunks = chunks.concat(SC.chunkText(doc.title, doc.text)); });
  console.log("corpus: " + corpus.length + " pages, " + chunks.length + " chunks");

  var results = pairs.map(function (p) {
    var top = SC.retrieve(p.question, chunks, K);
    var got = top.map(function (c) { return c.source; });
    return { id: p.id, question: p.question, accept: p.accept, hard: p.hard, got: got,
             hit: got.some(function (g) { return p.accept.indexOf(g) >= 0; }) };
  });
  report("JS stub scorer (study-config.js)", results);
}
