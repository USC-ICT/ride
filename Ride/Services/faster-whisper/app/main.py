import math
import os
import tempfile
import wave
from typing import Optional

from fastapi import FastAPI, Header, HTTPException, Query, Request
from pydantic import BaseModel
from faster_whisper import WhisperModel


API_TOKEN = os.getenv("API_TOKEN", "").strip()
MODEL_SIZE = os.getenv("MODEL_SIZE", "medium")
DEVICE = os.getenv("DEVICE", "cuda")
COMPUTE_TYPE = os.getenv("COMPUTE_TYPE", "float16")
DEFAULT_LANGUAGE = os.getenv("DEFAULT_LANGUAGE", "en").strip()
DEFAULT_VAD_FILTER = os.getenv("DEFAULT_VAD_FILTER", "true").strip().lower() == "true"
BEAM_SIZE = int(os.getenv("BEAM_SIZE", "1"))


app = FastAPI(title="VHToolkit FasterWhisper ASR")
model = WhisperModel(MODEL_SIZE, device=DEVICE, compute_type=COMPUTE_TYPE)


class HealthResponse(BaseModel):
    status: str
    model: str
    device: str
    compute_type: str


class TranscribeResponse(BaseModel):
    text: str
    confidence: float
    language: str
    duration_seconds: float


def require_authorization(authorization: Optional[str]) -> None:
    if not API_TOKEN:
        return

    expected_header = f"Bearer {API_TOKEN}"
    if authorization != expected_header:
        raise HTTPException(status_code=401, detail="Unauthorized")


def get_wav_duration_seconds(file_path: str) -> float:
    with wave.open(file_path, "rb") as wav_file:
        frame_count = wav_file.getnframes()
        frame_rate = wav_file.getframerate()
        if frame_rate <= 0:
            return 0.0
        return frame_count / float(frame_rate)


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(
        status="ok",
        model=MODEL_SIZE,
        device=DEVICE,
        compute_type=COMPUTE_TYPE,
    )


@app.post("/transcribe", response_model=TranscribeResponse)
async def transcribe(
    request: Request,
    authorization: Optional[str] = Header(default=None),
    language: Optional[str] = Query(default=None),
    vad_filter: Optional[bool] = Query(default=None),
) -> TranscribeResponse:
    require_authorization(authorization)

    audio_bytes = await request.body()
    if not audio_bytes:
        raise HTTPException(status_code=400, detail="Request body is empty.")

    transcribe_language = language.strip() if language and language.strip() else DEFAULT_LANGUAGE or None
    use_vad_filter = DEFAULT_VAD_FILTER if vad_filter is None else vad_filter

    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio:
        temp_audio.write(audio_bytes)
        temp_audio_path = temp_audio.name

    try:
        duration_seconds = get_wav_duration_seconds(temp_audio_path)
        segment_iterator, info = model.transcribe(
            temp_audio_path,
            language=transcribe_language,
            vad_filter=use_vad_filter,
            beam_size=BEAM_SIZE,
        )

        segments = list(segment_iterator)
        transcript = " ".join(segment.text.strip() for segment in segments if segment.text).strip()

        if segments:
            weighted_logprob_sum = 0.0
            total_weight = 0.0
            for segment in segments:
                segment_duration = max(segment.end - segment.start, 0.001)
                weighted_logprob_sum += segment.avg_logprob * segment_duration
                total_weight += segment_duration

            mean_logprob = weighted_logprob_sum / total_weight if total_weight > 0 else -10.0
            confidence = max(0.0, min(1.0, math.exp(mean_logprob)))
        else:
            confidence = 0.0

        detected_language = getattr(info, "language", None) or (transcribe_language or "")

        return TranscribeResponse(
            text=transcript,
            confidence=confidence,
            language=detected_language,
            duration_seconds=duration_seconds,
        )
    finally:
        try:
            os.remove(temp_audio_path)
        except OSError:
            pass
