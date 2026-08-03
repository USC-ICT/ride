import base64
import math
import os
import tempfile
import threading
import time
from importlib.metadata import PackageNotFoundError, version
from pathlib import Path


OPENFACE3_EMOTIONS = (
    "Neutral",
    "Happy",
    "Sad",
    "Surprise",
    "Fear",
    "Disgust",
    "Anger",
    "Contempt",
)

# OpenFace 3 trains these outputs from the selected DISFA labels in this order.
OPENFACE3_ACTION_UNITS = (
    "AU01_c",
    "AU06_c",
    "AU17_c",
    "AU25_c",
    "AU26_c",
    "AU02_c",
    "AU12_c",
    "AU15_c",
)

REQUIRED_WEIGHT_FILES = (
    "Alignment_RetinaFace.pth",
    "Landmark_98.pkl",
    "MTL_backbone.pth",
)


def capabilities():
    return {
        "provider": "OpenFace 3.0",
        "capabilities": [
            "FaceBounds",
            "FaceLandmarks",
            "Gaze",
            "Emotions",
            "ActionUnits",
        ],
    }


class OpenFace3Runtime:
    """Owns the persistent OpenFace 3 models used by all HTTP requests."""

    def __init__(self):
        self._lock = threading.Lock()
        self._state = "not_initialized"
        self._error = None
        self._device = None
        self._weights_dir = Path(os.environ.get("OPENFACE_WEIGHTS_DIR", "/models/weights"))
        self._face_detector = None
        self._landmark_detector = None
        self._multitask_model = None
        self._cv2 = None
        self._np = None
        self._torch = None

    def initialize(self):
        """Loads all OpenFace 3 models once and records failures for the health endpoint."""
        with self._lock:
            if self._state in ("ready", "loading"):
                return

            self._state = "loading"
            self._error = None

            try:
                self._validate_weights()

                import cv2
                import numpy as np
                import torch
                from openface.face_detection import FaceDetector
                from openface.landmark_detection import LandmarkDetector
                from openface.multitask_model import MultitaskPredictor
                from openface.Pytorch_Retinaface.data import cfg_mnet

                self._cv2 = cv2
                self._np = np
                self._torch = torch
                self._device = _resolve_device(torch)
                cuda_device = _cuda_device_index()

                # The package loads a training-only relative backbone checkpoint when this
                # remains enabled. The final RetinaFace checkpoint below already contains it.
                cfg_mnet["pretrain"] = False
                self._face_detector = FaceDetector(
                    model_path=str(self._weights_dir / "Alignment_RetinaFace.pth"),
                    device=self._device,
                )
                self._landmark_detector = LandmarkDetector(
                    model_path=str(self._weights_dir / "Landmark_98.pkl"),
                    device="cuda" if self._device.startswith("cuda") else "cpu",
                    device_ids=[cuda_device] if self._device.startswith("cuda") else [-1],
                )
                self._multitask_model = MultitaskPredictor(
                    model_path=str(self._weights_dir / "MTL_backbone.pth"),
                    device=self._device,
                )
                self._state = "ready"
            except Exception as exc:
                self._state = "error"
                self._error = str(exc)

    def status(self):
        """Returns model readiness and runtime details without triggering model loading."""
        try:
            package_version = version("openface-test")
        except PackageNotFoundError:
            package_version = "not installed"

        result = {
            "ok": self._state == "ready",
            "provider": "OpenFace 3.0",
            "state": self._state,
            "package_version": package_version,
            "device": self._device,
            "weights_dir": str(self._weights_dir),
        }
        if self._error:
            result["message"] = self._error
        return result

    def analyze(self, image_bytes, request):
        """Runs one frame through the resident models and returns normalized RIDE JSON data."""
        if self._state != "ready":
            raise RuntimeError(self._error or "OpenFace 3.0 models are not ready.")

        with self._lock:
            with tempfile.TemporaryDirectory(prefix="ride_openface3_") as temp_dir:
                image_path = Path(temp_dir) / "frame.jpg"
                image_path.write_bytes(image_bytes)
                faces = self._analyze_image_path(image_path, request)

        return {
            "provider": "OpenFace 3.0",
            "timestamp": time.time(),
            "coordinate_space": "pixels",
            "faces": faces,
        }

    def _validate_weights(self):
        missing = [
            name for name in REQUIRED_WEIGHT_FILES
            if not (self._weights_dir / name).is_file()
        ]
        if missing:
            raise RuntimeError(
                f"Missing OpenFace 3.0 model weights in {self._weights_dir}: "
                + ", ".join(missing)
            )

    def _analyze_image_path(self, image_path, request):
        if self._cv2.imread(str(image_path), self._cv2.IMREAD_COLOR) is None:
            raise ValueError("image_base64 does not contain a supported image.")

        detections, image = self._face_detector.detect_faces(str(image_path))
        if detections is None or image is None or len(detections) == 0:
            return []

        image_height, image_width = image.shape[:2]
        max_faces = _max_faces()
        results = []

        for detection in detections:
            confidence = float(detection[4])
            if confidence < self._face_detector.vis_threshold:
                continue

            left, top, right, bottom = _clamped_box(
                detection[:4], image_width, image_height
            )
            if right <= left or bottom <= top:
                continue

            cropped_face = image[top:bottom, left:right]
            landmarks = []
            if request.include_landmarks:
                detected_landmarks = self._landmark_detector.detect_landmarks(
                    image,
                    self._np.asarray([detection]),
                )
                if detected_landmarks:
                    landmarks = _landmark_points(detected_landmarks[0])

            gaze = None
            emotions = {}
            action_units = {}
            if request.include_gaze or request.include_emotions or request.include_action_units:
                emotion_output, gaze_output, au_output = self._multitask_model.predict(
                    cropped_face
                )

                if request.include_gaze:
                    gaze_values = _tensor_values(gaze_output)
                    gaze = {
                        "yaw": _angle_degrees(gaze_values[0]),
                        "pitch": _angle_degrees(gaze_values[1]),
                    }

                if request.include_emotions:
                    probabilities = self._torch.softmax(emotion_output, dim=1)
                    emotions = _named_scores(
                        OPENFACE3_EMOTIONS,
                        _tensor_values(probabilities),
                    )

                if request.include_action_units:
                    action_units = _named_scores(
                        OPENFACE3_ACTION_UNITS,
                        _tensor_values(au_output),
                        clamp=True,
                    )

            results.append({
                "has_face": True,
                "confidence": max(0.0, min(1.0, confidence)),
                "face_rectangle": {
                    "top": float(top),
                    "left": float(left),
                    "width": float(right - left),
                    "height": float(bottom - top),
                },
                "landmarks": landmarks,
                "gaze": gaze,
                "emotions": emotions,
                "action_units": action_units,
            })

            if len(results) >= max_faces:
                break

        return results


_runtime = OpenFace3Runtime()


def initialize_runtime():
    _runtime.initialize()


def runtime_status():
    return _runtime.status()


def analyze_image_base64(request):
    image_bytes = _decode_image(request.image_base64)
    return _runtime.analyze(image_bytes, request)


def _decode_image(image_base64):
    if not image_base64:
        raise ValueError("image_base64 is required.")
    if "," in image_base64:
        image_base64 = image_base64.split(",", 1)[1]

    try:
        image_bytes = base64.b64decode(image_base64, validate=True)
    except Exception as exc:
        raise ValueError("image_base64 is not valid base64.") from exc

    if not image_bytes:
        raise ValueError("image_base64 decoded to an empty image.")
    return image_bytes


def _resolve_device(torch_module):
    requested = os.environ.get("OPENFACE_DEVICE", "auto").strip().lower()
    cuda_device = _cuda_device_index()
    if requested == "auto":
        return f"cuda:{cuda_device}" if torch_module.cuda.is_available() else "cpu"
    if requested == "cuda":
        if not torch_module.cuda.is_available():
            raise RuntimeError("OPENFACE_DEVICE=cuda, but CUDA is not available.")
        return f"cuda:{cuda_device}"
    if requested == "cpu":
        return "cpu"
    raise RuntimeError("OPENFACE_DEVICE must be auto, cpu, or cuda.")


def _cuda_device_index():
    try:
        return max(0, int(os.environ.get("OPENFACE_CUDA_DEVICE", "0")))
    except ValueError:
        return 0


def _max_faces():
    try:
        return max(1, min(16, int(os.environ.get("OPENFACE_MAX_FACES", "4"))))
    except ValueError:
        return 4


def _clamped_box(values, image_width, image_height):
    left = max(0, min(image_width, int(math.floor(float(values[0])))))
    top = max(0, min(image_height, int(math.floor(float(values[1])))))
    right = max(0, min(image_width, int(math.ceil(float(values[2])))))
    bottom = max(0, min(image_height, int(math.ceil(float(values[3])))))
    return left, top, right, bottom


def _landmark_points(values):
    points = []
    for value in values:
        points.append({"x": float(value[0]), "y": float(value[1])})
    return points


def _tensor_values(tensor):
    values = tensor.detach().cpu().reshape(-1).tolist()
    return [float(value) for value in values]


def _named_scores(names, values, clamp=False):
    result = {}
    for name, value in zip(names, values):
        result[name] = max(0.0, min(1.0, value)) if clamp else float(value)
    return result


def _angle_degrees(value):
    return math.degrees(float(value))
