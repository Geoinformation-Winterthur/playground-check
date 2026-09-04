from __future__ import annotations

import io
import json
import os
import tempfile
import logging
import unittest
from decimal import Decimal
from dataclasses import replace
from pathlib import Path
from unittest.mock import patch

from playground_check import server as server_module
from playground_check import service
from playground_check.auth import decode_token, issue_token
from playground_check.config import _npgsql_to_libpq, load_environment
from playground_check.http import Response
from playground_check.logging_ext import ElkLogHandler
from playground_check.server import Application


class ApplicationTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.app = Application()

    def request(self, method, path, body=None, token=None):
        if "?" in path:
            path, query = path.split("?", 1)
        else:
            query = ""
        data = json.dumps(body).encode() if body is not None else b""
        environ = {
            "REQUEST_METHOD": method, "PATH_INFO": path, "QUERY_STRING": query,
            "CONTENT_LENGTH": str(len(data)), "CONTENT_TYPE": "application/json",
            "wsgi.input": io.BytesIO(data), "SERVER_NAME": "test", "SERVER_PORT": "80",
            "wsgi.url_scheme": "http",
        }
        if token:
            environ["HTTP_AUTHORIZATION"] = f"Bearer {token}"
        captured = {}

        def start(status, headers):
            captured.update(status=status, headers=dict(headers))

        payload = b"".join(self.app(environ, start))
        content_type = captured["headers"].get("Content-Type", "")
        parsed = json.loads(payload) if payload and "json" in content_type else payload
        return int(captured["status"].split()[0]), parsed, captured

    @staticmethod
    def token():
        return issue_token({
            "mailAddress": "test@winterthur.ch", "firstName": "Test",
            "lastName": "User", "role": "administrator",
        })

    def test_spa_static_files_and_health_without_database(self):
        status, body, _ = self.request("GET", "/")
        self.assertEqual(status, 200)
        self.assertIn(b"Spielplatzkontrolle", body)
        status, body, _ = self.request("GET", "/api/health")
        self.assertEqual(body["status"], "ok")
        status, manifest, _ = self.request("GET", "/static/manifest.webmanifest")
        self.assertEqual(manifest["short_name"], "SPK")

    def test_swagger_openapi_endpoints_are_available_without_database(self):
        status, document, _ = self.request("GET", "/swagger/v1/swagger.json")
        self.assertEqual(status, 200)
        self.assertEqual(document["openapi"], "3.0.1")
        self.assertEqual(document["info"]["title"], "Winterthur Playground Regular Inspection API - V1")
        self.assertIn("/Playground/byname", document["paths"])
        self.assertIn("/Defect", document["paths"])

        status, body, _ = self.request("GET", "/swagger/index.html")
        self.assertEqual(status, 200)
        self.assertIn(b"Swagger / OpenAPI V1", body)

        status, _, captured = self.request("GET", "/swagger")
        self.assertEqual(status, 302)
        self.assertEqual(captured["headers"].get("Location"), "/swagger/index.html")


    def test_elk_payload_matches_legacy_shape(self):
        handler = ElkLogHandler(
            url="http://127.0.0.1:9/elk", verify_ssl=True, environment="test",
            directory="/srv/playground", service="playground-check-service", hostname="wsstadt573",
        )
        try:
            record = logging.LogRecord(
                "playground_check", logging.INFO, __file__, 1,
                "HTTP %s responded %s", ("GET /api/health", 200), None,
            )
            record.elk_details = ["RequestMethod=GET", "StatusCode=200"]
            payload = handler.build_payload(record)
            self.assertEqual(payload["host"], "wsstadt573")
            self.assertEqual(payload["environment"], "test")
            self.assertEqual(payload["level"], "INFO")
            self.assertEqual(payload["service"], "playground-check-service")
            self.assertEqual(payload["details"], ["RequestMethod=GET", "StatusCode=200"])
            self.assertTrue(payload["@timestamp"].endswith("Z"))
        finally:
            handler.close()

    def test_configured_path_base_is_stripped_for_routing_and_exposed_to_frontend(self):
        configured = replace(server_module.settings, base_path="/stadtgruen/spielplatzkontrolle")
        with patch.object(server_module, "settings", configured):
            status, body, _ = self.request("GET", "/stadtgruen/spielplatzkontrolle/")
            self.assertEqual(status, 200)
            self.assertIn(b'"basePath":"/stadtgruen/spielplatzkontrolle"', body)

            status, result, _ = self.request("GET", "/stadtgruen/spielplatzkontrolle/api/health")
            self.assertEqual(status, 200)
            self.assertEqual(result["status"], "ok")

            status, _, _ = self.request("GET", "/")
            self.assertEqual(status, 404)

    def test_unknown_api_paths_do_not_fall_back_to_spa(self):
        status, body, _ = self.request("GET", "/playground/not-a-route")
        self.assertEqual(status, 404)
        self.assertEqual(body, b"Not found")

        status, body, _ = self.request("GET", "/defects-client-route")
        self.assertEqual(status, 200)
        self.assertIn(b"Spielplatzkontrolle", body)

    def test_known_api_path_with_wrong_method_returns_405(self):
        status, body, captured = self.request("GET", "/account/login")
        self.assertEqual(status, 405)
        self.assertEqual(body, b"Method not allowed")
        self.assertEqual(captured["headers"].get("Allow"), "POST")

    def test_login_rejects_mail_addresses_rejected_by_legacy_mailaddress(self):
        invalid_addresses = (
            "edgar@@win.ch",
            "edgar@",
            "@win.ch",
            "edgar..butwilowski@win.ch",
        )
        with patch.object(service, "connect", side_effect=AssertionError("database must not be touched")):
            for email in invalid_addresses:
                with self.subTest(email=email):
                    status, body, _ = self.request(
                        "POST",
                        "/account/login",
                        {"mailAddress": email, "passPhrase": "test"},
                    )
                    self.assertEqual(status, 400)
                    self.assertEqual(body, b"Keine oder falsche Login-Daten.")

    def test_jwt_round_trip_and_role_protection(self):
        token = self.token()
        self.assertEqual(
            decode_token(token)["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"],
            "administrator",
        )
        status, _, _ = self.request("GET", "/account/users/")
        self.assertEqual(status, 401)

    def test_preserved_playground_id_bug_does_not_touch_database(self):
        status, message, _ = self.request("GET", "/playground/1", token=self.token())
        self.assertEqual(status, 500)
        self.assertIn(b"Index was outside", message)

    def test_malformed_legacy_playground_id_url_keeps_aspnet_model_binding_error(self):
        status, result, _ = self.request(
            "GET",
            "/playground/1&inspectiontype=Hauptinspektion%20(HI)",
            token=self.token(),
        )
        self.assertEqual(status, 400)
        self.assertEqual(result["status"], 400)
        self.assertIn("1&inspectiontype=", result["errors"]["id"][0])

    def test_map_image_zero_coordinates(self):
        status, image, _ = self.request("GET", "/playground/mapimage?x=0&y=0")
        self.assertEqual(status, 200)
        self.assertEqual(image, "")

    def test_public_collection_keeps_original_error_payload(self):
        with patch.object(service, "connect", side_effect=RuntimeError("database unavailable")):
            status, result, _ = self.request("GET", "/collections/playgrounds/items/")
        self.assertEqual(status, 200)
        self.assertEqual(result[0]["errorMessage"], "Unknown critical error.")

    def test_json_response_serializes_postgresql_decimals_as_numbers(self):
        response = Response.json({"integer": Decimal("27"), "fraction": Decimal("12.5")})
        self.assertEqual(json.loads(response.body), {"integer": 27, "fraction": 12.5})

    def test_invalid_json_body_returns_api_controller_style_400(self):
        data = b'{"mailAddress":'
        environ = {
            "REQUEST_METHOD": "POST", "PATH_INFO": "/account/login", "QUERY_STRING": "",
            "CONTENT_LENGTH": str(len(data)), "CONTENT_TYPE": "application/json",
            "wsgi.input": io.BytesIO(data), "SERVER_NAME": "test", "SERVER_PORT": "80",
            "wsgi.url_scheme": "http",
        }
        captured = {}

        def start(status, headers):
            captured.update(status=status, headers=dict(headers))

        payload = b"".join(self.app(environ, start))
        result = json.loads(payload)
        self.assertEqual(int(captured["status"].split()[0]), 400)
        self.assertEqual(result["status"], 400)
        self.assertIn("$", result["errors"])

    def test_json_property_names_bind_case_insensitively_like_aspnet(self):
        with patch.object(service, "connect", side_effect=RuntimeError("database unavailable")):
            status, body, _ = self.request("POST", "/account/login", {
                "MAILADDRESS": "test@winterthur.ch",
                "PASSPHRASE": "abcdefgh",
                "LASTNAME": "Test",
                "FIRSTNAME": "User",
            })
        # The body reached the normal login/database branch. Before the binding
        # compatibility fix the differently-cased property names were rejected early.
        self.assertEqual(status, 400)
        self.assertEqual(body, b"Ein kritischer Fehler ist aufgetreten. Bitte kontaktieren Sie den Administrator.")

    def test_unhandled_exception_is_hidden_outside_development(self):
        with patch.object(self.app.router, "dispatch", side_effect=RuntimeError("sensitive database detail")):
            with patch.dict(os.environ, {"ASPNETCORE_ENVIRONMENT": "Production"}, clear=False):
                status, body, captured = self.request("GET", "/api/health")
        self.assertEqual(status, 500)
        self.assertEqual(body, b"")
        self.assertEqual(captured["headers"].get("Content-Type"), "text/plain; charset=utf-8")

    def test_unhandled_exception_is_detailed_in_development(self):
        with patch.object(self.app.router, "dispatch", side_effect=RuntimeError("development detail")):
            with patch.dict(os.environ, {"ASPNETCORE_ENVIRONMENT": "Development"}, clear=False):
                status, body, captured = self.request("GET", "/api/health")
        self.assertEqual(status, 500)
        self.assertEqual(captured["headers"].get("Content-Type"), "text/html; charset=utf-8")
        self.assertIn(b"RuntimeError", body)
        self.assertIn(b"development detail", body)

    def test_image_decoder_accepts_raw_and_data_url_images(self):
        png = b"\x89PNG\r\n\x1a\ncontent"
        self.assertEqual(service._decode_image(png), (png, "image/png"))
        encoded = "data:image/png;base64," + __import__("base64").b64encode(png).decode()
        self.assertEqual(service._decode_image(encoded), (png, "image/png"))

    def test_original_postgresql_contract_is_present(self):
        source = Path(service.__file__).read_text(encoding="utf-8")
        required = {
            "wgr_sp_kontrolleur", "wgr_sp_spielplatz", "gr_v_spielgeraete",
            "wgr_sp_inspektion", "wgr_sp_insp_bericht", "wgr_sp_insp_mangel",
            "wgr_sp_insp_mangel_foto", "wgr_v_sp_ger_insp_krit",
            "wgr_v_sp_hfall_insp_krit", "wgr_v_sp_nfall_insp_krit",
            "wgr_sp_push_subscription", "ST_X", "ST_Y",
        }
        self.assertTrue(all(name in source for name in required))
        self.assertNotIn("sqlite3", source)
        self.assertNotIn("CREATE TABLE", source.upper())

    def test_original_npgsql_connection_string_can_be_reused(self):
        converted = _npgsql_to_libpq(
            "Host=db.internal;Port=5432;Database=spielplatz;Username=app;Password=secret"
        )
        self.assertIn("host='db.internal'", converted)
        self.assertIn("dbname='spielplatz'", converted)
        self.assertIn("user='app'", converted)

    def test_dotenv_is_loaded_without_overriding_shell_values(self):
        with tempfile.TemporaryDirectory() as directory:
            dotenv_path = Path(directory) / ".env"
            dotenv_path.write_text(
                "PLAYGROUND_TITLE=Aus Datei\nPLAYGROUND_PORT=9000\n",
                encoding="utf-8",
            )
            previous_directory = Path.cwd()
            try:
                os.chdir(directory)
                with patch.dict(os.environ, {"PLAYGROUND_TITLE": "Aus Shell"}, clear=False):
                    os.environ.pop("PLAYGROUND_PORT", None)
                    load_environment()
                    self.assertEqual(os.environ["PLAYGROUND_TITLE"], "Aus Shell")
                    self.assertEqual(os.environ["PLAYGROUND_PORT"], "9000")
                    os.environ.pop("PLAYGROUND_PORT", None)
            finally:
                os.chdir(previous_directory)


if __name__ == "__main__":
    unittest.main()
