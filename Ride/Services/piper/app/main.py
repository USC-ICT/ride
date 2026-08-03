import base64
import io
import os
import subprocess
import tempfile
import wave
from typing import Optional

from fastapi import FastAPI, Header, HTTPException
from pydantic import BaseModel


API_TOKEN = os.getenv("API_TOKEN", "").strip()
DEFAULT_VOICE = os.getenv("DEFAULT_VOICE", "en_US-lessac-medium").strip()
SUPPORTED_VOICES = [
    voice.strip()
    for voice in os.getenv("SUPPORTED_VOICES", DEFAULT_VOICE).split(",")
    if voice.strip()
]
PIPER_DATA_DIR = os.getenv("PIPER_DATA_DIR", "/root/.local/share/piper").strip()


app = FastAPI(title="VHToolkit Piper TTS")


class HealthResponse(BaseModel):
    status: str
    default_voice: str


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


def resolve_voice(requested_voice: Optional[str]) -> str:
    if requested_voice and requested_voice.strip():
        normalized_voice = requested_voice.strip()
        if normalized_voice in SUPPORTED_VOICES:
            return normalized_voice
        raise HTTPException(status_code=400, detail=f"Unsupported voice '{normalized_voice}'.")

    return DEFAULT_VOICE


def get_wav_metadata(audio_bytes: bytes) -> tuple[int, float]:
    with wave.open(io.BytesIO(audio_bytes), "rb") as wav_file:
        frame_count = wav_file.getnframes()
        sample_rate = wav_file.getframerate()
        duration_seconds = frame_count / float(sample_rate) if sample_rate > 0 else 0.0
        return sample_rate, duration_seconds


def ensure_voice_downloaded(voice: str) -> None:
    command = [
        "python",
        "-m",
        "piper.download_voices",
        voice,
        "--data-dir",
        PIPER_DATA_DIR,
    ]

    try:
        subprocess.run(
            command,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=True,
        )
    except subprocess.CalledProcessError as exception:
        stderr = exception.stderr.decode("utf-8", errors="replace")
        raise HTTPException(status_code=500, detail=f"Voice download failed: {stderr}") from exception


def synthesize_with_piper(text: str, voice: str) -> bytes:
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio:
        output_path = temp_audio.name

    command = [
        "python",
        "-m",
        "piper",
        "-m",
        voice,
        "--data-dir",
        PIPER_DATA_DIR,
        "-f",
        output_path,
        "--",
        text,
    ]

    try:
        ensure_voice_downloaded(voice)
        subprocess.run(
            command,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=True,
        )

        with open(output_path, "rb") as audio_file:
            return audio_file.read()
    except subprocess.CalledProcessError as exception:
        stderr = exception.stderr.decode("utf-8", errors="replace")
        raise HTTPException(status_code=500, detail=f"Piper synthesis failed: {stderr}") from exception
    finally:
        try:
            os.remove(output_path)
        except OSError:
            pass


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(status="ok", default_voice=DEFAULT_VOICE)


@app.get("/voices", response_model=VoicesResponse)
def voices() -> VoicesResponse:
    return VoicesResponse(voices=SUPPORTED_VOICES)


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
    audio_bytes = synthesize_with_piper(text, voice)
    sample_rate_hz, duration_seconds = get_wav_metadata(audio_bytes)

    return SynthesizeResponse(
        audio_base64=base64.b64encode(audio_bytes).decode("ascii"),
        audio_format="wav",
        sample_rate_hz=sample_rate_hz,
        duration_seconds=duration_seconds,
        voice=voice,
    )
