from __future__ import annotations

import argparse
import logging
import mimetypes
import os
from pathlib import Path
from wsgiref.simple_server import WSGIRequestHandler, make_server

from . import __version__
from .config import settings
from .db import check_connection
from .http import Request, Response, Router, status_line
from . import service


LOG = logging.getLogger("playground_check")


def create_router() -> Router:
    router = Router()
    router.add("POST", r"/Account/Login/?", service.login)
    router.add("GET", r"/Account/Users/?", service.get_users)
    router.add("GET", r"/Account/Users/Assignable/?", service.get_assignable_users)
    router.add("PUT", r"/Account/Users/?", service.update_user)
    router.add("DELETE", r"/Account/Users/?", service.delete_user)
    router.add("GET", r"/Inspection/types/?", service.inspection_types)
    router.add("GET", r"/Inspection/renovationtypes/?", service.renovation_types)
    router.add("POST", r"/Inspection/?", service.post_inspections)
    router.add("GET", r"/Collections/Playgrounds/Items/?", service.public_playgrounds)
    router.add("GET", r"/Collections/Playgrounds/Items/(?P<uuid>[^/]+)/?", service.public_playground)
    router.add("GET", r"/Playground/onlynames/?", service.only_names)
    router.add("GET", r"/Playground/byname/?", service.get_playground_by_name)
    router.add("GET", r"/Playground/byplaydevice/(?P<fid>\d+)/?", service.get_playground_by_device)
    router.add("GET", r"/Playground/mapimage/?", service.get_map_image)
    router.add("GET", r"/Playground/(?P<id>\d+)/?", service.get_playground_by_id)
    router.add("POST", r"/Playdevice/?", service.save_playdevice)
    router.add("PUT", r"/Playdevice/?", service.exchange_playdevice_picture)
    router.add("GET", r"/Playdevice/(?P<fid>\d+)/Picture/?", service.get_playdevice_picture)
    router.add("PUT", r"/Playdevice/(?P<fid>\d+)/Picture/?", service.put_playdevice_picture)
    router.add("GET", r"/Defect/?", service.get_defect)
    router.add("POST", r"/Defect/?", service.update_defect)
    router.add("PUT", r"/Defect/?", service.create_defect)
    router.add("POST", r"/Defect/(?P<tid>\d+)/infomail-sent/?", service.mark_info_mail)
    router.add("POST", r"/Defect/(?P<tid>\d+)/accept/?", lambda r: service.assignment_status(r, True))
    router.add("POST", r"/Defect/(?P<tid>\d+)/reject/?", lambda r: service.assignment_status(r, False))
    router.add("GET", r"/Defect/Picture/(?P<tid>\d+)/?", service.get_defect_picture)
    router.add("PUT", r"/Defect/Picture/(?P<tid>\d+)/?", service.put_defect_picture)
    router.add("GET", r"/Document/(?P<fid>\d+)/?", service.get_document)
    router.add("POST", r"/PushSubscription/Register/?", service.push_register)
    router.add("DELETE", r"/PushSubscription/Unregister/?", service.push_unregister)
    router.add("GET", r"/PushSubscription/Me/?", service.push_me)
    return router


class Application:
    def __init__(self) -> None:
        self.router = create_router()
        self.static_root = Path(__file__).with_name("static")
        self.index = Path(__file__).with_name("templates") / "index.html"

    def __call__(self, environ, start_response):
        request = Request(environ)
        try:
            response = self.router.dispatch(request)
            if response is None:
                response = self._frontend(request)
        except ValueError as exc:
            response = Response.text(str(exc), 400)
        except Exception as exc:
            LOG.exception("Request failed: %s %s", request.method, request.path)
            response = Response.text(str(exc), 500)
        headers = [("Content-Type", response.content_type), ("Content-Length", str(len(response.body))),
                   ("Access-Control-Allow-Origin", "*"), ("X-Content-Type-Options", "nosniff")]
        headers.extend(response.headers)
        start_response(status_line(response.status), headers)
        return [response.body]

    def _frontend(self, request: Request) -> Response:
        if request.path == "/api/health":
            return Response.json({"status":"ok","version":__version__})
        if request.path.startswith("/static/"):
            relative = request.path[len("/static/"):]
            target = (self.static_root / relative).resolve()
            if self.static_root.resolve() not in target.parents or not target.is_file():
                return Response.text("Not found", 404)
            content_type = mimetypes.guess_type(str(target))[0] or "application/octet-stream"
            return Response.file(target.read_bytes(), content_type)
        if request.method == "GET":
            html = (
                self.index.read_text(encoding="utf-8")
                .replace("{{APP_TITLE}}", settings.title)
                .replace("{{APP_VERSION}}", __version__)
                .replace("{{SERVICE_WORKER_ENABLED}}", "true" if settings.service_worker_enabled else "false")
            )
            return Response.text(html, 200, "text/html; charset=utf-8")
        return Response.text("Not found", 404)


application = Application()


class QuietHandler(WSGIRequestHandler):
    def log_message(self, fmt, *args):
        LOG.info("%s - %s", self.address_string(), fmt % args)


def main() -> None:
    parser = argparse.ArgumentParser(description="Spielplatzkontrolle Python WebApp")
    parser.add_argument("--host", default=settings.host)
    parser.add_argument("--port", type=int, default=settings.port)
    parser.add_argument("--check-database", action="store_true", help="PostgreSQL-Verbindung prüfen und beenden")
    args = parser.parse_args()
    if args.check_database:
        check_connection()
        print("PostgreSQL-Verbindung erfolgreich geprüft.")
        return
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
    print(f"Spielplatzkontrolle {__version__} läuft auf http://{args.host}:{args.port}")
    with make_server(args.host, args.port, application, handler_class=QuietHandler) as httpd:
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\nServer beendet.")


if __name__ == "__main__":
    main()
