from __future__ import annotations

import os
import unittest

from playground_check.db import REQUIRED_RELATIONS, connect


POSTGRES_TEST_DSN = os.getenv("PLAYGROUND_TEST_POSTGRES_DSN", "")


@unittest.skipUnless(
    POSTGRES_TEST_DSN,
    "PLAYGROUND_TEST_POSTGRES_DSN ist nicht gesetzt; PostgreSQL-Schematest übersprungen.",
)
class PostgreSQLSchemaIntegrationTest(unittest.TestCase):
    def test_postgis_and_original_relations_are_available(self):
        with connect(POSTGRES_TEST_DSN) as connection:
            version = connection.execute("SELECT PostGIS_Version() AS version").fetchone()
            self.assertTrue(version["version"])
            for relation in REQUIRED_RELATIONS:
                found = connection.execute("SELECT to_regclass(%s) AS relation", (relation,)).fetchone()
                self.assertIsNotNone(found["relation"], relation)
