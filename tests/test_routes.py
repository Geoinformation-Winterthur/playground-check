import pytest
from io import BytesIO
from app.models import Defect, InspectionReport

def test_index_redirect(client):
    response = client.get('/')
    assert response.status_code == 302
    assert b'/login' in response.data

def test_login(client, init_database):
    response = client.post('/login', data={'loginname': 'test@example.com', 'password': 'password'}, follow_redirects=True)
    assert response.status_code == 200
    assert b'Willkommen' in response.data or b'Stadtgr' in response.data # Check for welcome message or header

def test_login_invalid(client, init_database):
    response = client.post('/login', data={'loginname': 'test@example.com', 'password': 'wrong'}, follow_redirects=True)
    assert response.status_code == 200
    assert b'ung' in response.data # ungültig

def test_index_authenticated(auth_client):
    response = auth_client.get('/')
    assert response.status_code == 200

def test_choose_device(auth_client):
    response = auth_client.get('/choosedevice')
    assert response.status_code == 200
    assert b'Spielplatz' in response.data

def test_inspections(auth_client):
    response = auth_client.get('/inspections')
    assert response.status_code == 200
    assert b'Inspektion' in response.data

def test_device_inspection_get(auth_client):
    # Assuming device with fid 1 exists from init_database
    response = auth_client.get('/deviceattributes/playdevice/1?inspection_type=Hauptinspektion')
    assert response.status_code == 200
    assert b'Check it' in response.data

def test_device_inspection_post(auth_client, app):
    # Post inspection data
    with app.app_context():
        # Form data based on criterion fid 1
        data = {
            'check_1': 'ok',
            'comment_1': 'All good'
        }
        response = auth_client.post('/deviceattributes/playdevice/1?inspection_type=Hauptinspektion', data=data, follow_redirects=True)
        assert response.status_code == 200
        assert b'Inspektion gespeichert' in response.data
        
        # Verify report created
        report = InspectionReport.query.filter_by(fid_spielgeraet=1).first()
        assert report is not None
        assert report.pruefung_erledigt == 1

def test_defects_list(auth_client):
    response = auth_client.get('/defects/1')
    assert response.status_code == 200

def test_defect_edit_new(auth_client):
    response = auth_client.get('/defect/1/0')
    assert response.status_code == 200
    assert b'Mangel' in response.data

def test_defect_create_post(auth_client, app):
    with app.app_context():
        data = {
            'description': 'Broken swing',
            'priority': '1',
            'photo': (BytesIO(b'fakeimage'), 'test.jpg')
        }
        response = auth_client.post('/defect/1/0', data=data, content_type='multipart/form-data', follow_redirects=True)
        assert response.status_code == 200
        assert b'Mangel gespeichert' in response.data
        
        defect = Defect.query.filter_by(beschrieb='Broken swing').first()
        assert defect is not None
        assert defect.picture1_base64 is not None
