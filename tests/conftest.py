import pytest
from app import create_app, db
from app.models import User, Playground, PlayDevice, InspectionType, InspectionCriterion, InspectionReport, Defect
import os
import uuid

@pytest.fixture(scope='function')
def app():
    if os.path.exists('test.db'):
        os.remove('test.db')
    app = create_app('testing')
    app.config['SQLALCHEMY_DATABASE_URI'] = 'sqlite:///test.db'
    print(f"DB URI: {app.config['SQLALCHEMY_DATABASE_URI']}")
    with app.app_context():
        db.create_all()
        yield app
        db.session.remove()
        db.metadata.drop_all(bind=db.engine)
        if os.path.exists('test.db'):
            os.remove('test.db')

@pytest.fixture(scope='function')
def client(app):
    return app.test_client()

@pytest.fixture(scope='function')
def runner(app):
    return app.test_cli_runner()

@pytest.fixture(scope='function')
def init_database(app):
    with app.app_context():
        # Clear data first
        db.session.query(Defect).delete()
        db.session.query(InspectionReport).delete()
        db.session.query(InspectionCriterion).delete()
        db.session.query(InspectionType).delete()
        db.session.query(PlayDevice).delete()
        db.session.query(Playground).delete()
        db.session.query(User).delete()
        db.session.commit()

        try:
            # Create a test user
            email = f'test_{uuid.uuid4()}@example.com'
            user = User(fid=1, nachname='Tester', vorname='Test', e_mail=email, aktiv=True)
            user.set_password('password')
            db.session.add(user)
            db.session.flush()
            print("User added.")
            
            # Create some basic data
            playground = Playground(name='Test Playground', fid=1, nummer=123)
            db.session.add(playground)
            db.session.flush()
            print("Playground added.")
            
            device = PlayDevice(fid=1, fid_spielplatz=1, bemerkungen='Test Device', laufnummer=1)
            db.session.add(device)
            db.session.flush()
            print("PlayDevice added.")
            
            inspection_type = InspectionType(id=1, short_value='HI', value='Hauptinspektion', priority=1)
            db.session.add(inspection_type)
            db.session.flush()
            print("InspectionType added.")

            criterion = InspectionCriterion(fid_spielgeraet=1, id_inspektionsart=1, fid_inspektionskriterium=1, 
                                            pruefung='Check it', inspektionsart='Hauptinspektion')
            db.session.add(criterion)
            
            db.session.commit()
        except Exception as e:
            print(f"Error in init_database: {e}")
            db.session.rollback()
            raise e
        
        yield db
        
        db.session.remove()

@pytest.fixture(scope='function')
def auth_client(client, init_database):
    # Log in the user
    user = User.query.first()
    if user:
        client.post('/login', data={'loginname': user.e_mail, 'password': 'password'}, follow_redirects=True)
    return client
