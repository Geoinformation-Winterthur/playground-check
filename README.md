# Spielplatzkontrolle

Python-Neuimplementierung der bereitgestellten Projekte
`playground-check-client` (Angular) und `playground-check-service` (ASP.NET Core).
Frontend und Webservice befinden sich in einem Repository. Die Anwendung greift
direkt auf die bestehende PostgreSQL-/PostGIS-Datenbank und die Datenbankobjekte
der Originalanwendung zu. Sie erzeugt kein eigenes Ersatzschema und keine
Demodatenbank.

Zur Laufzeit werden weder Node.js noch .NET benötigt.

## Abgedeckte Funktionen

- responsive Oberfläche mit Toolbar, Seitennavigation und Winterthur-Auftritt
- Anmeldung mit PBKDF2-HMAC-SHA256 und zweitägigem JWT
- Rollen `administrator` und `inspector`
- Spielplatzauswahl, Geräte-Cards, Gerätefotos, Kartenausschnitt und Dokumente
- visuelle, operative und Hauptinspektionen einschliesslich Prüfkriterien
- letzte und vorletzte Kontrollberichte
- Mängelerfassung, Priorität, Zuständigkeiten, Status, Fotos und EML-Infomail
- Benutzerverwaltung mit Freischaltung, Rollen, Passwortwechsel und Deaktivierung
- öffentliche GeoJSON-ähnliche Collection-Endpunkte
- Push-Subscription-API (im Frontend wie in der Vorlage standardmässig deaktiviert)
- ursprüngliche API-Pfade und JSON-Feldnamen
- ursprüngliche PostgreSQL-Tabellen, Views, Spalten und PostGIS-Geometrien

## Voraussetzungen unter Linux

### DEV-Umgebung

- Linux mit Python 3.11 oder neuer
- Erreichbarkeit einer PostgreSQL-Datenbank mit PostGIS
- das vorhandene Datenbankschema der Originalanwendung
- Datenbankzugang mit den erforderlichen Lese- und Schreibrechten
- der ursprüngliche PBKDF2-Salt aus `SaltBase64String`

### Produktive Umgebung

- Linux mit Docker Engine und Docker-Compose-Plugin
- erreichbare produktive PostgreSQL-/PostGIS-Datenbank mit Originalschema
- TLS-Reverse-Proxy wie Nginx oder Apache
- produktive Datenbank-, JWT- und Salt-Konfiguration

## Direkter Start in der DEV-Umgebung

Repository entpacken, in das Projektverzeichnis wechseln und die Python-Umgebung
installieren:

```sh
cd playground-check-python
python3 -m venv .venv
.venv/bin/python -m pip install -e .
```

Danach die Beispielkonfiguration kopieren und anpassen:

```sh
cp .env.example .env
chmod 600 .env
nano .env
```

Eine PostgreSQL-URI ist für `PLAYGROUND_POSTGRES_DSN` die empfohlene
Schreibweise. Die Anwendung liest `.env` beim direkten Start und beim
Datenbank-Check automatisch aus dem Projektverzeichnis ein. Bereits in der
Shell gesetzte Umgebungsvariablen haben Vorrang vor den Werten aus `.env`.

Wichtig: `PLAYGROUND_SALT_BASE64` muss dem `SaltBase64String` der bisherigen
ASP.NET-Anwendung entsprechen. Mit einem neuen Salt können die vorhandenen
Passwort-Hashes in `wgr_sp_kontrolleur` nicht mehr geprüft werden.

Verbindung testen:

```sh
.venv/bin/python -m playground_check --check-database
```

Anwendung starten:

```sh
.venv/bin/python -m playground_check --host 127.0.0.1 --port 8010
```

Danach ist die Anwendung unter <http://127.0.0.1:8010> erreichbar. Sie wird mit
`Ctrl+C` beendet. Die Anmeldung erfolgt mit den vorhandenen Benutzerkonten aus
`wgr_sp_kontrolleur`; es werden keine Demo-Benutzer angelegt.

Alternativ kann nach dem Setzen der Umgebungsvariablen das Linux-Startskript
verwendet werden:

```sh
chmod +x start.sh
./start.sh
```

`start.sh` legt bei Bedarf `.venv` an und installiert die benötigten
Python-Pakete. Die `.env`-Datei wird automatisch eingelesen.

## Konfiguration

| Variable | Bedeutung |
|---|---|
| `PLAYGROUND_POSTGRES_DSN` | PostgreSQL-Verbindungs-URI oder Npgsql-Verbindungszeichenfolge |
| `PLAYGROUND_SECURITY_KEY` | Signaturschlüssel für JWTs |
| `PLAYGROUND_SALT_BASE64` | ursprünglicher PBKDF2-Salt der ASP.NET-Anwendung |
| `PLAYGROUND_SERVICE_URL` | öffentliche Basis-URL; wird als JWT-Issuer und -Audience verwendet |
| `PLAYGROUND_HOST` | Bind-Adresse beim direkten Python-Start, Standard `127.0.0.1` |
| `PLAYGROUND_PORT` | Port beim direkten Python-Start, Standard `8010` |
| `PLAYGROUND_WMS_URL` | URL des Winterthur-WMS |
| `PLAYGROUND_TITLE` | Titel der WebApp |
| `PLAYGROUND_COMPATIBILITY_BUGS` | beobachtbare Altfehler beibehalten, Standard `true` |

Beim direkten Start lädt die Anwendung diese Werte automatisch aus der Datei
`.env` im Projektverzeichnis. Werte, die bereits als Linux-Umgebungsvariablen
gesetzt sind, werden dabei nicht überschrieben.

Zusätzlich werden die Bezeichnungen der ursprünglichen .NET-Konfiguration
unterstützt:

- `Postgres__ConnectionString`
- `SecurityKey`
- `SaltBase64String`
- `URL__ServiceDomain` und `URL__ServiceBasePath`
- `WMS__ServiceUrl`

Eine Npgsql-Zeichenfolge wie die folgende kann daher weiterverwendet werden:

```text
Host=dbhost;Port=5432;Database=spielplatz;Username=spielplatz_app;Password=...
```

## Produktives Deployment mit Docker Compose

Das mitgelieferte `compose.yaml` baut das Image lokal und startet die Anwendung
mit Gunicorn. Der Container läuft ohne Root-Rechte, mit schreibgeschütztem
Root-Dateisystem, entfernten Linux-Capabilities, Restart-Policy und Healthcheck.

Der Compose-Stack stellt bewusst **keinen neuen PostgreSQL-Container** bereit:
Er verbindet sich mit der vorhandenen fachlichen PostgreSQL-/PostGIS-Datenbank
der Originalanwendung. Dadurch bleiben Datenmodell, Views, Daten und
PostGIS-Verhalten unverändert.

### 1. Produktive Konfiguration anlegen

```sh
cp .env.production.example .env
chmod 600 .env
```

In `.env` müssen mindestens folgende Werte ersetzt werden:

- `PLAYGROUND_POSTGRES_DSN`: produktive PostgreSQL-Verbindung
- `PLAYGROUND_SECURITY_KEY`: langer, zufälliger JWT-Signaturschlüssel
- `PLAYGROUND_SALT_BASE64`: bestehender Salt der Originalanwendung
- `PLAYGROUND_SERVICE_URL`: öffentliche HTTPS-Adresse der WebApp

Für den JWT-Schlüssel kann ein zufälliger Wert erzeugt werden:

```sh
python3 -c 'import secrets; print(secrets.token_urlsafe(64))'
```

Der Salt darf bei einer bestehenden Datenbank dagegen nicht neu erzeugt werden.
Die produktive `.env` enthält Geheimnisse und darf nicht in Git committet werden.

### 2. PostgreSQL-Erreichbarkeit beachten

Der in `PLAYGROUND_POSTGRES_DSN` angegebene Hostname muss aus dem Container
erreichbar sein. Für eine Datenbank auf einem separaten Server wird dessen
DNS-Name verwendet. Läuft PostgreSQL direkt auf demselben Linux-Host, kann
`host.docker.internal` verwendet werden; `compose.yaml` bildet diesen Namen auf
das Host-Gateway ab.

PostgreSQL muss Verbindungen vom Docker-Netz zulassen. Dazu gehören eine
passende `listen_addresses`-/`pg_hba.conf`-Konfiguration sowie vorzugsweise eine
TLS-gesicherte Verbindung, beispielsweise mit `sslmode=require` in der DSN.

### 3. Konfiguration prüfen und starten

```sh
docker compose config
docker compose up -d --build
```

Status und Protokollausgabe prüfen:

```sh
docker compose ps
docker compose logs -f web
```

Die PostgreSQL-Verbindung aus dem laufenden Container prüfen:

```sh
docker compose exec web python -m playground_check --check-database
```

Standardmässig wird die WebApp nur auf `127.0.0.1:8010` des Linux-Hosts
veröffentlicht. Das ist für den Betrieb hinter einem TLS-Reverse-Proxy
vorgesehen. Eine direkte Netzwerkfreigabe ist mit
`PLAYGROUND_BIND_ADDRESS=0.0.0.0` möglich, sollte produktiv aber nur zusammen
mit geeigneten Firewall- und TLS-Massnahmen erfolgen.

Der HTTP-Healthcheck ist erreichbar unter:

```text
GET http://127.0.0.1:8010/api/health
```

### 4. Aktualisieren oder stoppen

```sh
docker compose up -d --build
docker compose down
```

Da die Datenbank ausserhalb dieses Compose-Stacks liegt, löscht
`docker compose down` keine Fachdaten.

## Verwendete Original-Datenbankobjekte

Die Python-Anwendung verwendet unter anderem dieselben Objekte wie der
ASP.NET-Service:

- `wgr_sp_kontrolleur`
- `wgr_sp_spielplatz`
- `gr_v_spielgeraete`
- `wgr_sp_inspart_kontr`
- `wgr_sp_inspektionsart_tbd`
- `wgr_sp_sanierungsart_tbd`
- `wgr_sp_inspektion`
- `wgr_sp_insp_bericht`
- `wgr_sp_insp_mangel`
- `wgr_sp_insp_mangel_foto`
- `wgr_v_sp_ger_insp_krit`
- `wgr_v_sp_hfall_insp_krit`
- `wgr_v_sp_nfall_insp_krit`
- `wgr_sp_dringlichkeit_tbd`
- `wgr_sp_zust_mangelbeheb_tbd`
- `wgr_sp_abnahmen`
- `wgr_sp_zertifikat`
- `wgr_sp_push_subscription`

Punktkoordinaten werden mit den PostGIS-Funktionen `ST_X` und `ST_Y` gelesen.
Schreibvorgänge erfolgen wie im Original direkt auf den dafür verwendeten
Tabellen beziehungsweise aktualisierbaren Views. Die Anwendung führt keine
Migrationen und keine `CREATE TABLE`-Anweisungen aus.

Der Datenbankbenutzer benötigt die gleichen Rechte wie der bisherige
Servicebenutzer: `SELECT` auf den verwendeten Tabellen/Views sowie `INSERT` und
`UPDATE` auf den vom Service beschriebenen Objekten.

## Kompatibilitätsmodus und bewusst erhaltene Eigenheiten

Der Kompatibilitätsmodus ist standardmässig aktiv. Unter anderem bleiben
folgende beobachtbare Eigenheiten erhalten:

- Der Login-JWT enthält keine `fid`-Claim, obwohl der alte Client sie auszulesen
  versucht.
- `dryRun=true` führt bei Schreib-Endpunkten mit der ursprünglichen
  `getAuthorizedUser`-Prüfung weiterhin zu `401`.
- `PUT /Defect/Picture/{tid}?dryRun=true` schreibt weiterhin ein Bild, weil im
  Original nach `Ok()` das `return` fehlt.
- `GET /Playground/{id}` liefert im Kompatibilitätsmodus den ursprünglichen
  Indexfehler.
- Die Sanierungsarten reproduzieren die im C#-Code wiederverwendete
  `Enumeration`-Instanz.
- Benutzerdaten werden beim Passwortwechsel vor der Prüfung der Mindestlänge
  gespeichert.
- `POST /Defect` aktualisiert und `PUT /Defect` legt neu an.
- Der Bild-Upload eines Mangels bleibt ohne Autorisierungsattribut erreichbar.

Mit `PLAYGROUND_COMPATIBILITY_BUGS=false` werden der fehlerhafte Playground-
ID-Abruf und die Sanierungsarten-Referenz korrigiert. Die Datenbankanbindung
bleibt in beiden Modi PostgreSQL/PostGIS.

## Tests

Lokale Tests ohne Datenbankzugriff:

```sh
.venv/bin/python -m unittest discover -s tests -v
```

Diese Tests prüfen Web-Auslieferung, Routing, JWT, Bilddekodierung, bekannte
Kompatibilitätsfehler und das Vorhandensein der originalen SQL-Verträge.

Zusätzlich kann gegen eine vorbereitete PostgreSQL-/PostGIS-Testdatenbank mit
Originalschema geprüft werden:

```sh
export PLAYGROUND_TEST_POSTGRES_DSN='postgresql://USER:PASSWORT@HOST:5432/TESTDB'
.venv/bin/python -m unittest tests.test_postgres_integration -v
```

Ohne `PLAYGROUND_TEST_POSTGRES_DSN` wird dieser Integrationstest ausdrücklich
als übersprungen ausgewiesen. Er verwendet keine lokale Ersatzdatenbank.

## Projektstruktur

```text
playground-check-python/
├── compose.yaml
├── .dockerignore
├── .env.example
├── .env.production.example
├── Dockerfile
├── pyproject.toml
├── src/playground_check/
│   ├── auth.py              # PBKDF2 und JWT
│   ├── config.py            # PostgreSQL- und App-Konfiguration
│   ├── db.py                # Psycopg-Verbindungen und Transaktionen
│   ├── http.py              # WSGI-Request/Response und Router
│   ├── service.py           # API und SQL auf dem Originalschema
│   ├── server.py            # Webserver, statische Dateien, SPA-Fallback
│   ├── templates/index.html
│   └── static/
├── tests/
├── start.sh
└── README.md
```
