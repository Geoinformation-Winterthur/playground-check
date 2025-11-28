from app import db
import os
from werkzeug.security import generate_password_hash, check_password_hash
from geoalchemy2 import Geometry
from sqlalchemy.dialects.postgresql import BYTEA
import base64

from flask_login import UserMixin

class User(UserMixin, db.Model):
    __tablename__ = 'wgr_sp_kontrolleur'
    fid = db.Column(db.BigInteger, primary_key=True)
    nachname = db.Column(db.String(80))
    vorname = db.Column(db.String(80))
    e_mail = db.Column(db.String(80))
    pwd = db.Column(db.String(80)) # In original it's pwd, likely plain text or simple hash. I'll use it for hash.
    registrierung_uuid = db.Column(db.String(36))
    aktiv = db.Column(db.Boolean)
    letzter_anmeldeversuch = db.Column(db.DateTime)

    def get_id(self):
        return str(self.fid)

    def set_password(self, password):
        self.pwd = generate_password_hash(password)

    def check_password(self, password):
        return check_password_hash(self.pwd, password)

from app import login

@login.user_loader
def load_user(id):
    return User.query.get(int(id))

class Playground(db.Model):
    __tablename__ = 'wgr_sp_spielplatz'
    fid = db.Column(db.BigInteger, primary_key=True)
    fid_anlage = db.Column(db.BigInteger)
    nummer = db.Column(db.BigInteger)
    name = db.Column(db.String(80))
    beschrieb = db.Column(db.String(255))
    erstellungskosten = db.Column(db.Numeric(10, 2))
    oeffentlich = db.Column(db.SmallInteger)
    id_period_visu_insp = db.Column(db.BigInteger)
    dat_letzt_visu_insp = db.Column(db.DateTime)
    dat_naech_visu_insp = db.Column(db.DateTime)
    id_period_oper_insp = db.Column(db.BigInteger)
    dat_letzt_oper_insp = db.Column(db.DateTime)
    dat_naech_oper_insp = db.Column(db.DateTime)
    id_period_haupt_insp = db.Column(db.BigInteger)
    dat_letzt_haupt_insp = db.Column(db.DateTime)
    dat_naech_haupt_insp = db.Column(db.DateTime)
    strassenname = db.Column(db.String(40))
    hausnummer = db.Column(db.String(10))
    uuid = db.Column(db.String(128))

class PlayDevice(db.Model):
    __tablename__ = 'gr_v_spielgeraete'
    fid = db.Column(db.BigInteger, primary_key=True)
    id_pflegestufe = db.Column(db.BigInteger)
    fid_spielplatz = db.Column(db.BigInteger, db.ForeignKey('wgr_sp_spielplatz.fid'))
    laufnummer = db.Column(db.BigInteger)
    id_haupt_fallschutz = db.Column(db.BigInteger)
    id_neben_fallschutz = db.Column(db.BigInteger)
    lebensdauer = db.Column(db.SmallInteger)
    sanierungsjahr = db.Column(db.SmallInteger)
    abbruchjahr = db.Column(db.SmallInteger)
    id_geraeteart = db.Column(db.BigInteger)
    id_lieferant = db.Column(db.BigInteger)
    bemerkungen = db.Column(db.String(100))
    norm = db.Column(db.String(255))
    verbindungen_s_n = db.Column(db.SmallInteger)
    bew_teil_aufhaengung = db.Column(db.SmallInteger)
    bew_teil_drehend = db.Column(db.SmallInteger)
    bew_teil_federn = db.Column(db.SmallInteger)
    bew_teil_ringe = db.Column(db.SmallInteger)
    seile = db.Column(db.SmallInteger)
    stahlseile = db.Column(db.SmallInteger)
    ketten = db.Column(db.SmallInteger)
    kustst_gummi = db.Column(db.SmallInteger)
    holz = db.Column(db.SmallInteger)
    allg_metallteile = db.Column(db.SmallInteger)
    lackierter_stahl = db.Column(db.SmallInteger)
    verzinkung = db.Column(db.SmallInteger)
    aluminium = db.Column(db.SmallInteger)
    fangstellen = db.Column(db.Numeric(1, 0))
    kostenschaetzung = db.Column(db.Numeric(10, 2))
    empfohlenes_sanierungsjahr = db.Column(db.Numeric(4, 0))
    bemerkung_empf_sanierung = db.Column(db.String(255))
    geom = db.Column(db.String() if 'sqlite' in (os.environ.get('DATABASE_URL') or 'sqlite') else Geometry('POINT', srid=2056))
    picture_base64 = db.Column(db.LargeBinary) # bytea in postgres

    device_type = db.relationship('PlayDeviceType', foreign_keys=[id_geraeteart], primaryjoin='PlayDevice.id_geraeteart == PlayDeviceType.id')
    playground = db.relationship('Playground', foreign_keys=[fid_spielplatz], primaryjoin='PlayDevice.fid_spielplatz == Playground.fid')


class Inspection(db.Model):
    __tablename__ = 'wgr_sp_inspektion'
    tid = db.Column(db.Integer, primary_key=True)
    fid_spielplatz = db.Column(db.Integer, db.ForeignKey('wgr_sp_spielplatz.fid'))
    id_inspektionsart = db.Column(db.Integer)
    datum_inspektion = db.Column(db.Date)
    fid_kontrolleur = db.Column(db.Integer, db.ForeignKey('wgr_sp_kontrolleur.fid'))
    bemerkung = db.Column(db.String(255))
    fid = db.Column(db.Integer)

class InspectionReport(db.Model):
    __tablename__ = 'wgr_sp_insp_bericht'
    tid = db.Column(db.Integer, primary_key=True)
    tid_inspektion = db.Column(db.Integer, db.ForeignKey('wgr_sp_inspektion.tid'))
    fid_spielgeraet = db.Column(db.Integer, db.ForeignKey('gr_v_spielgeraete.fid'))
    fid_geraet_detail = db.Column(db.Integer)
    inspektionsart = db.Column(db.String(80))
    datum_inspektion = db.Column(db.Date)
    kontrolleur = db.Column(db.String(80))
    pruefung_text = db.Column(db.String(255))
    pruefung_erledigt = db.Column(db.Numeric(1, 0))
    pruefung_kommentar = db.Column(db.String(255))
    wartung_text = db.Column(db.String(255))
    wartung_erledigung = db.Column(db.Numeric(1, 0))
    wartung_kommentar = db.Column(db.String(255))
    fid = db.Column(db.Integer)
    fallschutz = db.Column(db.String(30))

class Defect(db.Model):
    __tablename__ = 'wgr_sp_insp_mangel'
    tid = db.Column(db.Integer, primary_key=True)
    tid_insp_bericht = db.Column(db.Integer, db.ForeignKey('wgr_sp_insp_bericht.tid'))
    id_dringlichkeit = db.Column(db.Numeric(1, 0))
    beschrieb = db.Column(db.String(255))
    datum_erledigung = db.Column(db.Date)
    fid_erledigung = db.Column(db.String(80))
    bemerkunng = db.Column(db.String(255))
    picture1_base64 = db.Column(db.String)
    picture2_base64 = db.Column(db.String)
    picture3_base64 = db.Column(db.String)
    picture1_base64_thumb = db.Column(db.String)
    picture2_base64_thumb = db.Column(db.String)
    picture3_base64_thumb = db.Column(db.String)
    fid = db.Column(db.Integer)

class InspectionType(db.Model):
    __tablename__ = 'wgr_sp_inspektionsart_tbd'
    id = db.Column(db.BigInteger, primary_key=True)
    short_value = db.Column(db.String(30))
    value = db.Column(db.String(255))
    priority = db.Column(db.BigInteger)

class DefectPriority(db.Model):
    __tablename__ = 'wgr_sp_dringlichkeit_tbd'
    id = db.Column(db.BigInteger, primary_key=True)
    short_value = db.Column(db.String(30))
    value = db.Column(db.String(255))

class PlayDeviceType(db.Model):
    __tablename__ = 'wgr_sp_spielgeraeteart_tbd'
    id = db.Column(db.BigInteger, primary_key=True)
    short_value = db.Column(db.String(30))
    value = db.Column(db.String(255))
    active = db.Column(db.SmallInteger)
    fid_norm = db.Column(db.BigInteger)

class InspectionCriterion(db.Model):
    __tablename__ = 'wgr_v_sp_ger_insp_krit'
    # This is a view, so no primary key in reality, but SQLAlchemy needs one.
    # We'll use a composite key or just pick one if unique enough for read-only.
    # The view has fid_spielgeraet, id_inspektionsart, fid_inspektionskriterium
    fid_spielgeraet = db.Column(db.BigInteger, primary_key=True)
    id_inspektionsart = db.Column(db.BigInteger, primary_key=True)
    fid_inspektionskriterium = db.Column(db.BigInteger, primary_key=True)
    inspektionsart = db.Column(db.String(255))
    bereich = db.Column(db.String(80))
    pruefung = db.Column(db.String(255))
    pruefung_kurztext = db.Column(db.String(255))
    wartung = db.Column(db.String(255))

