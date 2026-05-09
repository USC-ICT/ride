// AzureAsr.jslib
// Self-contained WebGL bridge with dynamic script loading from StreamingAssets.
//
// Requires one-time setup to copy files to:
//   Assets/StreamingAssets/RideCognitionAzureAsr/
//     - speech-sdk.bundle.js
//     - AzureAsrBridge.js   (defines globalThis.__AzureASR)
//     - MicLevel.js         (defines globalThis.__MicLevel)

mergeInto(LibraryManager.library, {
    $RideAzureAsr: {
        _basePath: "StreamingAssets/RideCognitionAzureAsr/",
        _loaded: false,
        _loading: false,
        _waiters: [],

        _getGlobal: function () {
            if (typeof globalThis !== "undefined") return globalThis;
            if (typeof window !== "undefined") return window;
            if (typeof self !== "undefined") return self;
            return null;
        },

        _getSendMessage: function () {
            if (typeof SendMessage === "function") return SendMessage;
            var g = RideAzureAsr._getGlobal();
            if (g && typeof g.SendMessage === "function") return g.SendMessage;
            return null;
        },

        _sendError: function (goName, msg) {
            try {
                var sm = RideAzureAsr._getSendMessage();
                if (sm) sm(goName, "OnError", msg);
                else console.error("[Ride Azure ASR] SendMessage missing. Error: " + msg);
            } catch (e) {
                console.error("[Ride Azure ASR] Failed to SendMessage error:", e, msg);
            }
        },

        _loadScript: function (url, onDone, onError) {
            // Probe the URL first so we can report HTTP status (404, 403, etc.).
            fetch(url, { cache: "no-cache" })
                .then(function (resp) {
                    if (!resp.ok) {
                        onError("HTTP " + resp.status + " loading script: " + url);
                        return null;
                    }
                    return resp.text(); // consume so some servers/CDNs don't keep the request open
                })
                .then(function (text) {
                    if (text === null) return;

                    var s = document.createElement("script");
                    s.src = url;
                    s.async = false;

                    s.onload = function () { onDone(); };
                    s.onerror = function () { onError("Script tag failed to load: " + url); };

                    document.head.appendChild(s);
                })
                .catch(function (e) {
                    onError("Fetch exception loading script: " + url + " (" + e.toString() + ")");
                });
        },

        ensureLoaded: function (goName, onReady) {
            if (RideAzureAsr._loaded) {
                onReady();
                return;
            }

            RideAzureAsr._waiters.push(onReady);

            if (RideAzureAsr._loading)
                return;

            RideAzureAsr._loading = true;

            // Expose SendMessage globally so scripts loaded from StreamingAssets can call back into Unity.
            // In some Unity WebGL templates, SendMessage exists for .jslib code but is not on globalThis/window.
            (function () {
                var g = RideAzureAsr._getGlobal();
                if (!g) return;

                // If already present, don't overwrite.
                if (typeof g.SendMessage === "function") return;

                // If the symbol exists in this scope (jslib), publish it globally.
                if (typeof SendMessage === "function") {
                    g.SendMessage = function (go, method, arg) { SendMessage(go, method, arg); };
                }
            })();

            var base = RideAzureAsr._basePath;
            var scripts = [
                "speech-sdk.bundle.js",
                "AzureAsrBridge.js",
                "MicLevel.js"
            ];

            var index = 0;

            var onError = function (err) {
                var msg =
                    err + "\n" +
                    "Expected files under: " + RideAzureAsr._basePath + "\n" +
                    "Required: speech-sdk.bundle.js, AzureAsrBridge.js, MicLevel.js\n" +
                    "Fix: run the WebGL setup step that copies files into StreamingAssets.";

                console.error("[Ride Azure ASR] " + msg);

                RideAzureAsr._waiters.length = 0;
                RideAzureAsr._loading = false;
                RideAzureAsr._sendError(goName, msg);
            };

            var loadNext = function () {
                if (index >= scripts.length) {
                    RideAzureAsr._loaded = true;
                    RideAzureAsr._loading = false;

                    // Run queued callbacks
                    var i;
                    for (i = 0; i < RideAzureAsr._waiters.length; i++) {
                        try { RideAzureAsr._waiters[i](); }
                        catch (e) { console.error("[Ride Azure ASR] waiter failed:", e); }
                    }
                    RideAzureAsr._waiters.length = 0;
                    return;
                }

                var url = base + scripts[index];
                index++;

                RideAzureAsr._loadScript(url, loadNext, onError);
            };

            loadNext();
        },

        getAzureAsr: function () {
            var g = RideAzureAsr._getGlobal();
            if (!g) return null;
            return g.__AzureASR || (g.window ? g.window.__AzureASR : null);
        },

        getMicLevel: function () {
            var g = RideAzureAsr._getGlobal();
            if (!g) return null;
            return g.__MicLevel || (g.window ? g.window.__MicLevel : null);
        }
    },

    AzureAsr_Start: function (goNamePtr, keyPtr, regionPtr, langPtr) {
        var goName = UTF8ToString(goNamePtr);
        var key = UTF8ToString(keyPtr);
        var region = UTF8ToString(regionPtr);
        var lang = UTF8ToString(langPtr);

        RideAzureAsr.ensureLoaded(goName, function () {
            try {
                var asr = RideAzureAsr.getAzureAsr();
                if (!asr) {
                    RideAzureAsr._sendError(goName, "AzureAsrBridge.js loaded but __AzureASR is missing.");
                    return;
                }

                asr.start({ goName: goName, key: key, region: region, lang: lang });
            } catch (e) {
                RideAzureAsr._sendError(goName, e.toString());
            }
        });
    },

    AzureAsr_Stop: function () {
        try {
            var asr = RideAzureAsr.getAzureAsr();
            if (asr) asr.stop();
        } catch (e) {
            console.error("[Ride Azure ASR] stop failed:", e);
        }
    },

    AzureAsr_Ping: function (goNamePtr) {
        var goName = UTF8ToString(goNamePtr);

        RideAzureAsr.ensureLoaded(goName, function () {
            try {
                var asr = RideAzureAsr.getAzureAsr();
                if (!asr) {
                    RideAzureAsr._sendError(goName, "AzureAsrBridge.js loaded but __AzureASR is missing.");
                    return;
                }

                if (typeof asr.ping === "function")
                    asr.ping(goName);
            } catch (e) {
                RideAzureAsr._sendError(goName, e.toString());
            }
        });
    },

    MicLevel_Start: function (goNamePtr) {
        var goName = UTF8ToString(goNamePtr);

        RideAzureAsr.ensureLoaded(goName, function () {
            try {
                var mic = RideAzureAsr.getMicLevel();
                if (!mic) {
                    RideAzureAsr._sendError(goName, "MicLevel.js loaded but __MicLevel is missing.");
                    return;
                }

                mic.startMeter({ goName: goName });
            } catch (e) {
                RideAzureAsr._sendError(goName, e.toString());
            }
        });
    },

    MicLevel_Stop: function () {
        try {
            var mic = RideAzureAsr.getMicLevel();
            if (mic) mic.stopMeter();
        } catch (e) {
            console.error("[Ride Azure ASR] stopMeter failed:", e);
        }
    }
});

autoAddDeps(LibraryManager.library, "$RideAzureAsr");
