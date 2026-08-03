from contextlib import asynccontextmanager
import logging

from fastapi import FastAPI, HTTPException, Response
from pydantic import BaseModel

from .openface_adapter import (
    analyze_image_base64,
    capabilities,
    initialize_runtime,
    runtime_status,
)


logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(_app):
    initialize_runtime()
    yield


app = FastAPI(title="RIDE OpenFace 3 Local Sensing", lifespan=lifespan)


class AnalyzeRequest(BaseModel):
    image_base64: str
    include_landmarks: bool = True
    include_gaze: bool = True
    include_emotions: bool = True
    include_action_units: bool = True


@app.get("/health")
def health(response: Response):
    status = runtime_status()
    if not status["ok"]:
        response.status_code = 503
    return status


@app.get("/capabilities")
def get_capabilities():
    return capabilities()


@app.post("/analyze")
def analyze(request: AnalyzeRequest):
    try:
        return analyze_image_base64(request)
    except RuntimeError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    except Exception as exc:
        logger.exception("OpenFace 3.0 frame analysis failed")
        raise HTTPException(status_code=500, detail="OpenFace 3.0 frame analysis failed.") from exc
