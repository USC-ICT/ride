using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ride.Sensing
{
    /// <summary>
    /// Parses the stable JSON contract returned by the local OpenFace bridge.
    /// </summary>
    public static class OpenFaceSensingResponseParser
    {
        /// <summary>Capabilities supplied by OpenFace 3.0.</summary>
        public const SensingCapability Capabilities = SensingCapability.FaceBounds
            | SensingCapability.FaceLandmarks
            | SensingCapability.Gaze
            | SensingCapability.Emotions
            | SensingCapability.ActionUnits;

        /// <summary>
        /// Parses a local OpenFace bridge response.
        /// </summary>
        /// <param name="responseBody">JSON returned by the bridge.</param>
        /// <returns>A provider-neutral sensing frame.</returns>
        public static SensingFrameResponse Parse(string responseBody)
        {
            var openFaceResponse = JsonConvert.DeserializeObject<OpenFaceAnalyzeResponse>(responseBody);
            var response = new SensingFrameResponse(responseBody)
            {
                provider = string.IsNullOrEmpty(openFaceResponse?.provider) ? "OpenFace" : openFaceResponse.provider,
                timestamp = openFaceResponse != null ? openFaceResponse.timestamp : 0,
                capabilities = Capabilities,
                coordinateSpace = ParseCoordinateSpace(openFaceResponse?.coordinate_space),
                rawJson = responseBody
            };

            if (openFaceResponse?.faces == null || openFaceResponse.faces.Count == 0)
            {
                response.faces = Array.Empty<SensingFaceResult>();
                return response;
            }

            var faces = new SensingFaceResult[openFaceResponse.faces.Count];
            for (int i = 0; i < faces.Length; i++)
                faces[i] = MapFace(openFaceResponse.faces[i]);

            response.faces = faces;
            return response;
        }

        /// <summary>
        /// Creates an empty OpenFace response for unavailable and error states.
        /// </summary>
        /// <param name="message">Error or availability message.</param>
        /// <returns>An empty provider-neutral frame.</returns>
        public static SensingFrameResponse CreateEmpty(string message)
        {
            return new SensingFrameResponse(message)
            {
                provider = "OpenFace",
                error = message,
                capabilities = Capabilities,
                coordinateSpace = SensingCoordinateSpace.Pixels,
                faces = Array.Empty<SensingFaceResult>(),
                rawJson = message
            };
        }

        static SensingCoordinateSpace ParseCoordinateSpace(string coordinateSpace)
        {
            if (string.Equals(coordinateSpace, "normalized", StringComparison.OrdinalIgnoreCase))
                return SensingCoordinateSpace.Normalized;
            if (string.Equals(coordinateSpace, "pixels", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(coordinateSpace))
                return SensingCoordinateSpace.Pixels;

            return SensingCoordinateSpace.Unknown;
        }

        static SensingFaceResult MapFace(OpenFaceFace face)
        {
            if (face == null)
                return new SensingFaceResult();

            var result = new SensingFaceResult
            {
                hasFace = face.has_face || face.hasFace || face.confidence > 0,
                confidence = Math.Max(0, Math.Min(1, face.confidence)),
                landmarks = MapPoints(face.landmarks),
                emotions = MapNamedScores(face.emotions),
                actionUnits = MapActionUnits(face.action_units ?? face.actionUnits)
            };

            var rectangle = face.face_rectangle ?? face.faceRectangle;
            if (rectangle != null)
                result.faceRectangle = new FaceRectangle(rectangle.top, rectangle.left, rectangle.width, rectangle.height);

            var headPose = face.head_pose ?? face.headPose;
            if (headPose != null)
            {
                result.pitch = headPose.pitch;
                result.roll = headPose.roll;
                result.yaw = headPose.yaw;
            }

            if (face.gaze != null)
            {
                result.gazePitch = face.gaze.pitch;
                result.gazeYaw = face.gaze.yaw;
            }

            return result;
        }

        static RideVector2[] MapPoints(List<OpenFacePoint> points)
        {
            if (points == null || points.Count == 0)
                return Array.Empty<RideVector2>();

            var landmarks = new RideVector2[points.Count];
            for (int i = 0; i < points.Count; i++)
                landmarks[i] = new RideVector2(points[i].x, points[i].y);

            return landmarks;
        }

        static SensingNamedScore[] MapActionUnits(Dictionary<string, double> scores)
        {
            if (scores == null || scores.Count == 0)
                return Array.Empty<SensingNamedScore>();

            var results = new List<SensingNamedScore>();
            foreach (var pair in scores)
            {
                double maximum = pair.Key.EndsWith("_r", StringComparison.OrdinalIgnoreCase) ? 5 : 1;
                results.Add(new SensingNamedScore(pair.Key, pair.Value, 0, maximum));
            }

            return results.ToArray();
        }

        static SensingNamedScore[] MapNamedScores(Dictionary<string, double> scores)
        {
            if (scores == null || scores.Count == 0)
                return Array.Empty<SensingNamedScore>();

            var results = new List<SensingNamedScore>();
            foreach (var pair in scores)
                results.Add(new SensingNamedScore(pair.Key, pair.Value));

            return results.ToArray();
        }

        class OpenFaceAnalyzeResponse
        {
            public string provider { get; set; }
            public double timestamp { get; set; }
            public string coordinate_space { get; set; }
            public List<OpenFaceFace> faces { get; set; }
        }

        class OpenFaceFace
        {
            public bool has_face { get; set; }
            public bool hasFace { get; set; }
            public double confidence { get; set; }
            public OpenFaceRectangle face_rectangle { get; set; }
            public OpenFaceRectangle faceRectangle { get; set; }
            public List<OpenFacePoint> landmarks { get; set; }
            public OpenFacePose head_pose { get; set; }
            public OpenFacePose headPose { get; set; }
            public OpenFaceGaze gaze { get; set; }
            public Dictionary<string, double> emotions { get; set; }
            public Dictionary<string, double> action_units { get; set; }
            public Dictionary<string, double> actionUnits { get; set; }
        }

        class OpenFaceRectangle
        {
            public float top { get; set; }
            public float left { get; set; }
            public float width { get; set; }
            public float height { get; set; }
        }

        class OpenFacePoint
        {
            public float x { get; set; }
            public float y { get; set; }
        }

        class OpenFacePose
        {
            public double pitch { get; set; }
            public double roll { get; set; }
            public double yaw { get; set; }
        }

        class OpenFaceGaze
        {
            public double pitch { get; set; }
            public double yaw { get; set; }
        }
    }
}
