FROM python:3.11-slim

ENV DEBIAN_FRONTEND=noninteractive
ENV COQUI_TOS_AGREED=1

RUN apt-get update && \
    apt-get install -y --no-install-recommends espeak-ng ffmpeg && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app ./app

EXPOSE 9004

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "9004"]
