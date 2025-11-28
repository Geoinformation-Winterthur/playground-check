# Playground Check (Spielplatzkontrolle)

Die **Spielplatzkontrolle-App** ist eine spezialisierte Fachanwendung für Stadtgrün Winterthur. Sie unterstützt die systematische Überwachung und Instandhaltung von öffentlichen Spielplätzen und gewährleistet eine effiziente digitale Prozesskette von der Inspektion bis zur Mängelbehebung.

## Funktionen

Die Anwendung bietet eine moderne, responsive Benutzeroberfläche für akkreditierte Kontrolleure:

- **Inspektionsmanagement:** Durchführung und Protokollierung von regelmäßigen Sicherheitsüberprüfungen an Spielgeräten.
- **Mängelverwaltung:** Erfassung, Kategorisierung und Nachverfolgung von Defekten und notwendigen Reparaturen.
- **Geräteinventar:** Detaillierte Übersicht und Verwaltung aller Spielgeräte auf den zugeordneten Spielplätzen.
- **Benutzerportal:** Sichere Authentifizierung und rollenbasierter Zugriff auf die verschiedenen Module.
- **Mobile-First Design:** Optimierte Darstellung für mobile Endgeräte (Tablets/Smartphones) für den Einsatz vor Ort.

## Installation

Folgen Sie diesen Schritten, um die Entwicklungsumgebung einzurichten:

1.  **Repository klonen:**
    ```bash
    git clone <repository-url>
    cd playground-check
    ```

2.  **Virtuelle Umgebung erstellen:**
    Es wird empfohlen, die virtuelle Umgebung im Ordner `.venv` anzulegen.
    ```bash
    python -m venv .venv
    ```

3.  **Virtuelle Umgebung aktivieren:**
    *   **Windows:**
        ```bash
        .venv\Scripts\activate
        ```
    *   **macOS/Linux:**
        ```bash
        source .venv/bin/activate
        ```

4.  **Abhängigkeiten installieren:**
    ```bash
    pip install -r requirements.txt
    ```

5.  **Datenbank initialisieren:**
    ```bash
    flask db upgrade
    ```
    *Hinweis: Stellen Sie sicher, dass die `FLASK_APP` Umgebungsvariable gesetzt ist (z.B. `set FLASK_APP=run.py` auf Windows oder `export FLASK_APP=run.py` auf Unix).*

## Nutzung

1.  **Anwendung starten:**
    ```bash
    python run.py
    ```

2.  **Im Browser öffnen:**
    Navigieren Sie zu [http://127.0.0.1:5000](http://127.0.0.1:5000).

## Projektstruktur

Die Anwendung basiert auf dem Flask-Framework und ist modular aufgebaut:

- **`app/`**: Kern der Anwendung.
    - **`static/`**: Statische Assets (Lokale Fonts, CSS, Bilder).
    - **`templates/`**: HTML-Templates für das Frontend.
    - **`routes.py`**: Anwendungslogik und Endpunkte.
    - **`models.py`**: Datenbankschemata.
- **`run.py`**: Startskript für den Entwicklungsserver.
- **`config.py`**: Zentrale Konfiguration.

## Technologie

- **Backend:** Python, Flask
- **Frontend:** HTML5, CSS3 (Material Design), Jinja2 Templating
- **Datenbank:** SQLAlchemy (SQLite/PostgreSQL)

## Testing

Das Projekt verfügt über eine umfassende Unit-Test-Suite, die mit `pytest` implementiert wurde.

### Voraussetzungen

Installieren Sie die Test-Abhängigkeiten (falls noch nicht geschehen):

```bash
pip install pytest pytest-flask pytest-cov
```

### Tests ausführen

Um alle Tests auszuführen:

```bash
python -m pytest
```

### Code Coverage

Um einen Bericht über die Testabdeckung zu generieren:

```bash
python -m pytest --cov=app tests/
```

Die Tests verwenden eine temporäre SQLite-Datenbank (`test.db`), die automatisch erstellt und nach dem Testlauf bereinigt wird. Die Konfiguration hierfür befindet sich in `config.py` (`TestingConfig`) und `tests/conftest.py`.
