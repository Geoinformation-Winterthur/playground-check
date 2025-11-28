from app import create_app, db
from app.models import User, Playground, PlayDevice, InspectionType, DefectPriority, PlayDeviceType, InspectionCriterion, InspectionReport, Defect
from datetime import datetime

app = create_app()

def seed():
    with app.app_context():
        # Create User
        if not User.query.filter_by(e_mail='admin@winterthur.ch').first():
            user = User(
                fid=1,
                nachname='Admin',
                vorname='Super',
                e_mail='admin@winterthur.ch',
                aktiv=True,
                registrierung_uuid='uuid-123'
            )
            user.set_password('admin')
            db.session.add(user)
            print("Created admin user.")

        # Create Lookup Data
        if not InspectionType.query.first():
            types = [
                InspectionType(id=1, short_value='Visuell', value='Visuelle Routine-Inspektion', priority=1),
                InspectionType(id=2, short_value='Operativ', value='Operative Inspektion', priority=2),
                InspectionType(id=3, short_value='Haupt', value='Jahreshauptinspektion', priority=3)
            ]
            db.session.add_all(types)
            print("Created inspection types.")

        if not DefectPriority.query.first():
            priorities = [
                DefectPriority(id=1, short_value='Niedrig', value='Niedrige Priorität'),
                DefectPriority(id=2, short_value='Mittel', value='Mittlere Priorität'),
                DefectPriority(id=3, short_value='Hoch', value='Hohe Priorität')
            ]
            db.session.add_all(priorities)
            print("Created defect priorities.")

        if not PlayDeviceType.query.first():
            dt = PlayDeviceType(id=1, short_value='Rutschbahn', value='Rutschbahn', active=1)
            db.session.add(dt)
            print("Created play device type.")

        # Create Playground
        if not Playground.query.first():
            pg = Playground(
                fid=1,
                name='Stadtgarten',
                beschrieb='Schöner Spielplatz im Stadtgarten',
                strassenname='Stadtgartenstrasse',
                hausnummer='1',
                oeffentlich=1,
                dat_letzt_visu_insp=datetime.now(),
                uuid='pg-uuid-1'
            )
            db.session.add(pg)
            print("Created playground.")

            # Create Device
            device = PlayDevice(
                fid=1,
                fid_spielplatz=1,
                laufnummer=1,
                bemerkungen='Rutschbahn',
                kostenschaetzung=5000.00,
                empfohlenes_sanierungsjahr=2030,
                id_geraeteart=1,
                geom='POINT(2697000 1262000)' # Mock coordinates
            )
            db.session.add(device)
            print("Created play device.")

            # Create Inspection Criteria (Mocking the view)
            crit1 = InspectionCriterion(
                fid_spielgeraet=1,
                id_inspektionsart=1, # Visuelle Routine-Inspektion (assuming id 1)
                fid_inspektionskriterium=1,
                inspektionsart='Visuelle Routine-Inspektion',
                bereich='Allgemein',
                pruefung='Zustand der Rutschfläche',
                pruefung_kurztext='Rutschfläche prüfen',
                wartung='Reinigen'
            )
            crit2 = InspectionCriterion(
                fid_spielgeraet=1,
                id_inspektionsart=1,
                fid_inspektionskriterium=2,
                inspektionsart='Visuelle Routine-Inspektion',
                bereich='Allgemein',
                pruefung='Stabilität der Verankerung',
                pruefung_kurztext='Verankerung prüfen',
                wartung='Nachziehen'
            )
            db.session.add(crit1)
            db.session.add(crit2)
            print("Created inspection criteria.")
            
            # Create Dummy Inspection Report for Defect
            report = InspectionReport(
                fid_spielgeraet=1,
                inspektionsart='Mangelmeldung',
                datum_inspektion=datetime.now(),
                kontrolleur='Admin',
                pruefung_text='Mangelmeldung',
                pruefung_erledigt=0,
                fid_geraet_detail=0
            )
            db.session.add(report)
            db.session.flush()

            # Create Dummy Defect
            defect = Defect(
                tid_insp_bericht=report.tid,
                id_dringlichkeit=2,
                beschrieb='Riss im Holz',
                datum_erledigung=None,
                fid_erledigung=None,
                bemerkunng='Bitte reparieren'
            )
            db.session.add(defect)
            print("Created dummy defect.")


        db.session.commit()
        print("Seeding complete.")

if __name__ == '__main__':
    seed()
