# OpenFace 3 Local Sensing

Local Docker service that exposes OpenFace 3.0 through RIDE's provider-neutral sensing API.

The service loads the OpenFace models once at startup and keeps them resident. Unity sends individual webcam frames over HTTP; the service returns face bounds, facial landmarks, gaze, categorical emotion probabilities, and facial action-unit scores.

OpenFace 2.x is not supported by this service.

## Start

CPU or automatic device selection:

```powershell
docker compose up -d --build
```

NVIDIA GPU:

```powershell
docker compose -f compose.yaml -f compose.gpu.yaml up -d --build
```

The first startup downloads approximately 280 MB of model weights into the persistent `openface3_models` Docker volume. Later starts reuse that volume. Check progress with:

```powershell
docker compose logs -f openface3
```

The service listens on `http://127.0.0.1:5101` by default. Useful commands are also available as `load.bat`, `load-gpu.bat`, `test.bat`, and `unload.bat`.

## Endpoints

- `GET /health`
- `GET /capabilities`
- `POST /analyze`

`POST /analyze` expects:

```json
{
  "image_base64": "...",
  "include_landmarks": true,
  "include_gaze": true,
  "include_emotions": true,
  "include_action_units": true
}
```

The response contains all detected faces in pixel coordinate space. OpenFace 3.0 does not expose head-pose output in its documented model API, so this service does not advertise `HeadPose`. RIDE can still derive coarse head information from face bounds and landmarks.

## RIDE Configuration

Configure the Unity client through the user-side RIDE configuration:

```json
"openFace": {
  "endpoint": "http://127.0.0.1:5101/analyze",
  "timeoutSeconds": 10
}
```

Environment variables are listed in `.env.example`. `OPENFACE_DEVICE` accepts `auto`, `cpu`, or `cuda`. Keep one Uvicorn worker: each worker would load another full copy of every model.

## Version Pinning

The image pins:

- `openface-test==0.1.26`
- Hugging Face model repository `nutPace/openface_weights`
- Model revision `3844412b54706ed0a930b99589508ed3c101f39e`

The required models are `Alignment_RetinaFace.pth`, `Landmark_98.pkl`, and `MTL_backbone.pth`. Although the upstream README describes 68 landmarks, package `0.1.26` configures its STAR detector for WFLW and loads the 98-point checkpoint in its own demo. RIDE therefore uses the package's working 98-point configuration.

## Tests

Bridge tests do not require PyTorch or downloaded model weights:

```powershell
python -m unittest discover -s tests -v
```

After starting the container:

```powershell
Invoke-RestMethod http://127.0.0.1:5101/health
Invoke-RestMethod http://127.0.0.1:5101/capabilities
```

## License Constraint

OpenFace 3.0 is licensed for noncommercial internal research use and restricts redistribution and third-party access. This repository contains only the RIDE bridge and a local-build Docker recipe. Do not publish or distribute the resulting image or model volume without obtaining appropriate permission from CMU.
