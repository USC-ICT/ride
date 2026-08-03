import os
from pathlib import Path

from huggingface_hub import snapshot_download

from .openface_adapter import REQUIRED_WEIGHT_FILES


def main():
    weights_dir = Path(os.environ.get("OPENFACE_WEIGHTS_DIR", "/models/weights"))
    repository = os.environ.get(
        "OPENFACE_MODEL_REPOSITORY",
        "nutPace/openface_weights",
    )
    revision = os.environ.get(
        "OPENFACE_MODEL_REVISION",
        "3844412b54706ed0a930b99589508ed3c101f39e",
    )

    weights_dir.mkdir(parents=True, exist_ok=True)
    for name in REQUIRED_WEIGHT_FILES:
        path = weights_dir / name
        if path.is_symlink():
            path.unlink()

    missing = [name for name in REQUIRED_WEIGHT_FILES if not (weights_dir / name).is_file()]
    if not missing:
        print(f"OpenFace 3.0 model weights already exist in {weights_dir}.")
        return

    print(f"Downloading OpenFace 3.0 model weights to {weights_dir}...")
    snapshot_download(
        repo_id=repository,
        revision=revision,
        local_dir=str(weights_dir),
        local_dir_use_symlinks=False,
        allow_patterns=list(REQUIRED_WEIGHT_FILES),
    )

    missing = [name for name in REQUIRED_WEIGHT_FILES if not (weights_dir / name).is_file()]
    if missing:
        raise RuntimeError("Model download did not produce: " + ", ".join(missing))


if __name__ == "__main__":
    main()
