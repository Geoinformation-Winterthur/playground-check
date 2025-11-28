from app.models import User, Playground, PlayDevice

def test_user_password_hashing():
    u = User(nachname='User', vorname='Test', e_mail='test@example.com')
    u.set_password('cat')
    assert u.check_password('cat')
    assert not u.check_password('dog')

def test_playground_creation():
    p = Playground(name='Central Park', fid=100)
    assert p.name == 'Central Park'
    assert p.fid == 100

def test_playdevice_creation():
    d = PlayDevice(fid=200, bemerkungen='Swing', fid_spielplatz=100)
    assert d.bemerkungen == 'Swing'
    assert d.fid_spielplatz == 100
