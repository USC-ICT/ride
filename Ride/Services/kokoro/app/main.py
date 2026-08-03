import io
import os
from functools import lru_cache
from typing import Optional

import numpy as np
import soundfile as sf
from fastapi import FastAPI, Header, HTTPException
from fastapi.responses import Response
from kokoro import KPipeline
from pydantic import BaseModel


API_TOKEN = os.getenv("API_TOKEN", "").strip()
DEFAULT_VOICE = os.getenv("DEFAULT_VOICE", "af_heart").strip()
SUPPORTED_VOICES = [
    voice.strip()
    for voice in os.getenv("SUPPORTED_VOICES", DEFAULT_VOICE).split(",")
    if voice.strip()
]
DEFAULT_SAMPLE_RATE_HZ = int(os.getenv("DEFAULT_SAMPLE_RATE_HZ", "24000"))

VOICE_LANGUAGE_CODES = {
    "a": "a",  # American English
    "b": "b",  # British English
    "e": "e",  # Spanish
    "f": "f",  # French
    "h": "h",  # Hindi
    "i": "i",  # Italian
    "j": "j",  # Japanese
    "k": "k",  # Korean
    "p": "p",  # Brazilian Portuguese
    "z": "z",  # Mandarin Chinese
}


app = FastAPI(title="VHToolkit Kokoro TTS")


class VoicesResponse(BaseModel):
    voices: list[str]


class SpeechRequest(BaseModel):
    model: str = "kokoro"
    input: str
    voice: Optional[str] = None
    response_format: str = "wav"
    speed: float = 1.0


def require_authorization(authorization: Optional[str]) -> None:
    if not API_TOKEN:
        return
    if authorization != f"Bearer {API_TOKEN}":
        raise HTTPException(status_code=401, detail="Unauthorized")


def resolve_voice(requested_voice: Optional[str]) -> str:
    if requested_voice and requested_voice.strip():
        normalized = requested_voice.strip()
        if normalized in SUPPORTED_VOICES:
            return normalized
        raise HTTPException(status_code=400, detail=f"Unsupported voice '{normalized}'.")
    return DEFAULT_VOICE


def get_lang_code_for_voice(voice: str) -> str:
    if not voice:
        return "a"
    return VOICE_LANGUAGE_CODES.get(voice[0].lower(), "a")


@lru_cache(maxsize=8)
def get_pipeline(lang_code: str) -> KPipeline:
    return KPipeline(lang_code=lang_code)


def synthesize_wav(text: str, voice: str, speed: float) -> bytes:
    lang_code = get_lang_code_for_voice(voice)
    pipeline = get_pipeline(lang_code)
    chunks = []

    try:
        for _, _, audio in pipeline(text, voice=voice, speed=speed):
            chunks.append(np.asarray(audio, dtype=np.float32))
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"Kokoro synthesis failed: {exc}") from exc

    if not chunks:
        raise HTTPException(status_code=500, detail="Kokoro synthesis produced no audio.")

    buf = io.BytesIO()
    sf.write(buf, np.concatenate(chunks), DEFAULT_SAMPLE_RATE_HZ, format="WAV")
    return buf.getvalue()


@app.get("/health")
def health():
    return {"status": "ok", "default_voice": DEFAULT_VOICE}


@app.get("/v1/audio/voices", response_model=VoicesResponse)
def voices(authorization: Optional[str] = Header(default=None)) -> VoicesResponse:
    require_authorization(authorization)
    return VoicesResponse(voices=SUPPORTED_VOICES)


@app.post("/v1/audio/speech")
def speech(
    request: SpeechRequest,
    authorization: Optional[str] = Header(default=None),
) -> Response:
    require_authorization(authorization)

    text = (request.input or "").strip()
    if not text:
        raise HTTPException(status_code=400, detail="Input text is required.")

    voice = resolve_voice(request.voice)
    speed = max(0.25, min(4.0, request.speed))
    audio_bytes = synthesize_wav(text, voice, speed)

    return Response(content=audio_bytes, media_type="audio/wav")
