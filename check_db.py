from app import create_app, db
from app.models import InspectionType, Playground, PlayDevice, User

app = create_app()
with app.app_context():
    print("Users:")
    users = User.query.all()
    for u in users:
        print(f"- ID: {u.fid}, Name: '{u.vorname}' '{u.nachname}', Email: {u.e_mail}")

    print("\nInspection Types:")
    types = InspectionType.query.all()
    for t in types:
        print(f"- {t.short_value}: {t.value}")
    
    print("\nPlaygrounds:")
    playgrounds = Playground.query.all()
    for p in playgrounds:
        print(f"- {p.name} (ID: {p.fid})")
        devices = PlayDevice.query.filter_by(fid_spielplatz=p.fid).count()
        print(f"  - Devices: {devices}")
