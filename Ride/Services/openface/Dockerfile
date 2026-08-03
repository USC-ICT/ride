ARG PYTORCH_IMAGE=pytorch/pytorch:2.5.1-cuda12.4-cudnn9-runtime
FROM ${PYTORCH_IMAGE}

ENV PYTHONUNBUFFERED=1
ENV PIP_NO_CACHE_DIR=1
ENV OPENFACE_DEVICE=auto
ENV OPENFACE_CUDA_DEVICE=0
ENV OPENFACE_MAX_FACES=4
ENV OPENFACE_WEIGHTS_DIR=/models/weights
ENV OPENFACE_MODEL_REPOSITORY=nutPace/openface_weights
ENV OPENFACE_MODEL_REVISION=3844412b54706ed0a930b99589508ed3c101f39e

WORKDIR /srv/openface

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgl1 libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt /srv/openface/requirements.txt
RUN python -m pip install --no-cache-dir -r /srv/openface/requirements.txt

COPY app /srv/openface/app
COPY entrypoint.sh /usr/local/bin/openface3-entrypoint
RUN sed -i 's/\r$//' /usr/local/bin/openface3-entrypoint \
    && chmod +x /usr/local/bin/openface3-entrypoint

EXPOSE 5101

ENTRYPOINT ["/usr/local/bin/openface3-entrypoint"]
CMD ["uvicorn", "app.server:app", "--host", "0.0.0.0", "--port", "5101", "--workers", "1"]
