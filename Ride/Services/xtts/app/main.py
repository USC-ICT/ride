import base64
import io
import os
import tempfile
import wave
from functools import lru_cache
from typing import Optional

import torch
from fastapi import FastAPI, Header, HTTPException
from pydantic import BaseModel
from TTS.api import TTS


API_TOKEN = os.getenv("API_TOKEN", "").strip()
DEFAULT_VOICE = os.getenv("DEFAULT_VOICE", "Ana Florence").strip()
DEFAULT_LANGUAGE = os.getenv("DEFAULT_LANGUAGE", "en").strip()
MODEL_NAME = os.getenv("MODEL_NAME", "tts_models/multilingual/multi-dataset/xtts_v2").strip()


app = FastAPI(title="VHToolkit XTTS v2 TTS")


class HealthResponse(BaseModel):
    status: str
    default_voice: str
    model: str
    device: str


class VoicesResponse(BaseModel):
    voices: list[str]


class SynthesizeRequest(BaseModel):
    text: str
    voice: Optional[str] = None


class SynthesizeResponse(BaseModel):
    audio_base64: str
    audio_format: str
    sample_rate_hz: int
    duration_seconds: float
    voice: str


def require_authorization(authorization: Optional[str]) -> None:
    if not API_TOKEN:
        return

    expected_header = f"Bearer {API_TOKEN}"
    if authorization != expected_header:
        raise HTTPException(status_code=401, detail="Unauthorized")


def get_wav_metadata(audio_bytes: bytes) -> tuple[int, float]:
    with wave.open(io.BytesIO(audio_bytes), "rb") as wav_file:
        frame_count = wav_file.getnframes()
        sample_rate = wav_file.getframerate()
        duration_seconds = frame_count / float(sample_rate) if sample_rate > 0 else 0.0
        return sample_rate, duration_seconds


@lru_cache(maxsize=1)
def get_tts_model() -> TTS:
    model = TTS(MODEL_NAME)
    if torch.cuda.is_available():
        model = model.to("cuda")
    return model


def get_available_voices() -> list[str]:
    model = get_tts_model()
    speakers = getattr(model, "speakers", None)
    if isinstance(speakers, list) and speakers:
        return [str(speaker) for speaker in speakers]
    return [DEFAULT_VOICE]


def resolve_voice(requested_voice: Optional[str]) -> str:
    voices = get_available_voices()

    if requested_voice and requested_voice.strip():
        normalized_voice = requested_voice.strip()
        if normalized_voice in voices:
            return normalized_voice
        raise HTTPException(status_code=400, detail=f"Unsupported voice '{normalized_voice}'.")

    if DEFAULT_VOICE in voices:
        return DEFAULT_VOICE

    return voices[0]


def synthesize_with_xtts(text: str, voice: str) -> bytes:
    model = get_tts_model()

    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio:
        output_path = temp_audio.name

    try:
        model.tts_to_file(
            text=text,
            file_path=output_path,
            speaker=voice,
            language=DEFAULT_LANGUAGE,
            split_sentences=True,
        )

        with open(output_path, "rb") as audio_file:
            return audio_file.read()
    except Exception as exception:  # pragma: no cover - runtime dependency failure path
        raise HTTPException(status_code=500, detail=f"XTTS synthesis failed: {exception}") from exception
    finally:
        try:
            os.remove(output_path)
        except OSError:
            pass


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    device = "cuda" if torch.cuda.is_available() else "cpu"
    return HealthResponse(status="ok", default_voice=DEFAULT_VOICE, model=MODEL_NAME, device=device)


@app.get("/voices", response_model=VoicesResponse)
def voices() -> VoicesResponse:
    return VoicesResponse(voices=get_available_voices())


@app.post("/synthesize", response_model=SynthesizeResponse)
def synthesize(
    request: SynthesizeRequest,
    authorization: Optional[str] = Header(default=None),
) -> SynthesizeResponse:
    require_authorization(authorization)

    text = request.text.strip() if request.text else ""
    if not text:
        raise HTTPException(status_code=400, detail="Text is required.")

    voice = resolve_voice(request.voice)
    audio_bytes = synthesize_with_xtts(text, voice)
    sample_rate_hz, duration_seconds = get_wav_metadata(audio_bytes)

    return SynthesizeResponse(
        audio_base64=base64.b64encode(audio_bytes).decode("ascii"),
        audio_format="wav",
        sample_rate_hz=sample_rate_hz,
        duration_seconds=duration_seconds,
        voice=voice,
    )
