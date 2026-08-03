import base64
import os
import tempfile
import unittest
from types import SimpleNamespace
from unittest.mock import patch

from app import openface_adapter


class OpenFaceAdapterTests(unittest.TestCase):
    def test_capabilities_match_openface3_output(self):
        capability_names = openface_adapter.capabilities()["capabilities"]

        self.assertIn("FaceLandmarks", capability_names)
        self.assertIn("ActionUnits", capability_names)
        self.assertIn("Gaze", capability_names)
        self.assertIn("Emotions", capability_names)
        self.assertNotIn("HeadPose", capability_names)

    def test_initialize_reports_missing_model_weights(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            with patch.dict(
                os.environ,
                {"OPENFACE_WEIGHTS_DIR": temp_dir},
                clear=False,
            ):
                runtime = openface_adapter.OpenFace3Runtime()
                runtime.initialize()

        status = runtime.status()
        self.assertFalse(status["ok"])
        self.assertEqual("error", status["state"])
        self.assertIn("Missing OpenFace 3.0 model weights", status["message"])

    def test_analyze_image_base64_decodes_data_url_and_uses_runtime(self):
        expected_bytes = b"image bytes"
        request = SimpleNamespace(
            image_base64="data:image/jpeg;base64,"
            + base64.b64encode(expected_bytes).decode("ascii")
        )

        class FakeRuntime:
            def analyze(self, image_bytes, actual_request):
                self.image_bytes = image_bytes
                self.request = actual_request
                return {"faces": []}

        runtime = FakeRuntime()
        with patch.object(openface_adapter, "_runtime", runtime):
            result = openface_adapter.analyze_image_base64(request)

        self.assertEqual({"faces": []}, result)
        self.assertEqual(expected_bytes, runtime.image_bytes)
        self.assertIs(request, runtime.request)

    def test_decode_image_rejects_invalid_base64(self):
        with self.assertRaisesRegex(ValueError, "not valid base64"):
            openface_adapter._decode_image("not base64")

    def test_clamped_box_stays_inside_image(self):
        box = openface_adapter._clamped_box((-2.2, 10.1, 105.8, 80.9), 100, 60)

        self.assertEqual((0, 10, 100, 60), box)

    def test_named_action_unit_scores_are_clamped(self):
        scores = openface_adapter._named_scores(
            ("AU01_c", "AU02_c", "AU03_c"),
            (-0.2, 0.6, 1.4),
            clamp=True,
        )

        self.assertEqual(
            {"AU01_c": 0.0, "AU02_c": 0.6, "AU03_c": 1.0},
            scores,
        )

    def test_gaze_angles_are_converted_from_radians_to_degrees(self):
        self.assertAlmostEqual(90.0, openface_adapter._angle_degrees(1.57079632679))


if __name__ == "__main__":
    unittest.main()
