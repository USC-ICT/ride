// Ride WebGL audio bridge used by provider-specific runtime code that needs to
// turn in-memory audio bytes into a browser-playable Blob URL for Unity WebGL.
// This currently supports the ElevenLabs TTS implementation in
// Runtime/Tts/ElevenLabsTextToSpeech.cs, but the bridge is intentionally
// provider-agnostic so other TTS providers can reuse the same path later.
// The file lives under the ASR WebGL folder for convenience alongside the other
// browser interop plugins, even though its primary current use is TTS playback.

mergeInto(LibraryManager.library, {
    $RideWebGLAudio: {
        ptrToOffset: function (ptr) {
            return typeof ptr === "bigint" ? Number(ptr) : ptr;
        },

        utf8ToString: function (ptr) {
            return UTF8ToString(RideWebGLAudio.ptrToOffset(ptr));
        },

        ptrToAbi: function (ptr, samplePtr) {
            return typeof samplePtr === "bigint" && typeof ptr !== "bigint" ? BigInt(ptr) : ptr;
        }
    },

    RideWebGLAudio_CreateAudioBlobUrl: function (mimeTypePtr, audioBase64Ptr) {
        try {
            var mimeType = RideWebGLAudio.utf8ToString(mimeTypePtr);
            var audioBase64 = RideWebGLAudio.utf8ToString(audioBase64Ptr);
            var binary = atob(audioBase64);
            var length = binary.length;
            var bytes = new Uint8Array(length);

            for (var i = 0; i < length; i++) {
                bytes[i] = binary.charCodeAt(i);
            }

            var blob = new Blob([bytes], { type: mimeType });
            var url = URL.createObjectURL(blob);
            var urlLength = lengthBytesUTF8(url) + 1;
            var urlPtr = _malloc(urlLength);
            stringToUTF8(url, RideWebGLAudio.ptrToOffset(urlPtr), urlLength);
            return RideWebGLAudio.ptrToAbi(urlPtr, mimeTypePtr);
        } catch (error) {
            console.error("[Ride WebGL Audio] Failed to create audio blob URL.", error);
            return RideWebGLAudio.ptrToAbi(0, mimeTypePtr);
        }
    }
});

autoAddDeps(LibraryManager.library, "$RideWebGLAudio");
