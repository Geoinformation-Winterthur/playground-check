from flask import Blueprint, render_template, jsonify, request, redirect, url_for, flash
from flask_login import current_user, login_user, logout_user, login_required
from app.models import User, Playground, PlayDevice, InspectionType, InspectionCriterion, InspectionReport, Defect
from app import db
from sqlalchemy import func
from datetime import datetime

bp = Blueprint('main', __name__)

@bp.route('/')
@login_required
def index():
    return render_template('index.html')

@bp.route('/choosedevice')
@login_required
def choose_device():
    playground_name = request.args.get('playground_name')
    playgrounds = Playground.query.with_entities(Playground.name).order_by(Playground.name).all()
    playgrounds = [p.name for p in playgrounds]
    
    selected_playground = None
    devices = []
    
    if playground_name:
        selected_playground = Playground.query.filter_by(name=playground_name).first()
        if selected_playground:
            devices = PlayDevice.query.filter_by(fid_spielplatz=selected_playground.fid).all()

    return render_template('choose_device.html', 
                           playgrounds=playgrounds, 
                           selected_playground=selected_playground,
                           devices=devices)

@bp.route('/inspections')
@login_required
def inspections():
    inspection_type = request.args.get('inspection_type')
    playground_name = request.args.get('playground_name')
    
    inspection_types = InspectionType.query.order_by(InspectionType.priority).all()
    playgrounds = Playground.query.with_entities(Playground.name).order_by(Playground.name).all()
    playgrounds = [p.name for p in playgrounds]
    
    selected_playground = None
    devices = []
    
    if playground_name and inspection_type:
        selected_playground = Playground.query.filter_by(name=playground_name).first()
        if selected_playground:
            devices = PlayDevice.query.filter_by(fid_spielplatz=selected_playground.fid).all()

    return render_template('inspections.html',
                           inspection_types=inspection_types,
                           playgrounds=playgrounds,
                           selected_inspection_type=inspection_type,
                           selected_playground=selected_playground,
                           devices=devices)

@bp.route('/deviceattributes/playdevice/<int:fid>', methods=['GET', 'POST'])
@login_required
def device_inspection(fid):
    inspection_type = request.args.get('inspection_type')
    device = PlayDevice.query.get_or_404(fid)
    
    # Fetch criteria based on device type and inspection type
    criteria = InspectionCriterion.query.filter_by(fid_spielgeraet=fid).all()
    
    if request.method == 'POST':
        # Create a new Inspection record (simplified)
        # In reality, we should check if an inspection already exists for this day/type
        
        # Save reports for each criterion
        for criterion in criteria:
            check_val = request.form.get(f'check_{criterion.fid_inspektionskriterium}')
            comment_val = request.form.get(f'comment_{criterion.fid_inspektionskriterium}')
            
            if check_val:
                # Create InspectionReport
                report = InspectionReport(
                    fid_spielgeraet=fid,
                    inspektionsart=inspection_type,
                    datum_inspektion=datetime.now().date(),
                    kontrolleur=current_user.nachname, # Simplified
                    pruefung_text=criterion.pruefung,
                    pruefung_erledigt=1 if check_val == 'ok' else 0,
                    pruefung_kommentar=comment_val,
                    fid_geraet_detail=0 # Default
                )
                db.session.add(report)
        
        db.session.commit()
        flash('Inspektion gespeichert.', 'success')
        return redirect(url_for('main.inspections', inspection_type=inspection_type, playground_name=device.playground.name))
    
    return render_template('device_inspection.html',
                           device=device,
                           criteria=criteria,
                           inspection_type=inspection_type)

@bp.route('/defects')
@login_required
def defects():
    playground_name = request.args.get('playground_name')
    playgrounds = Playground.query.with_entities(Playground.name).order_by(Playground.name).all()
    playgrounds = [p.name for p in playgrounds]
    
    selected_playground = None
    devices = []
    
    if playground_name:
        selected_playground = Playground.query.filter_by(name=playground_name).first()
        if selected_playground:
            devices = PlayDevice.query.filter_by(fid_spielplatz=selected_playground.fid).all()
            
            # Eager load defects for each device (simplified approach)
            for device in devices:
                device.defects = db.session.query(Defect).join(InspectionReport, Defect.tid_insp_bericht == InspectionReport.tid)\
                    .filter(InspectionReport.fid_spielgeraet == device.fid).all()
                device.has_open_defects = any(d.datum_erledigung is None for d in device.defects)

    return render_template('defects.html', 
                           playgrounds=playgrounds, 
                           selected_playground=selected_playground,
                           devices=devices)

@bp.route('/defects/<int:device_fid>')
@login_required
def defects_list(device_fid):
    device = PlayDevice.query.get_or_404(device_fid)
    # Fetch defects linked to this device via inspection reports
    # This is a bit complex due to the schema.
    # Defects are linked to InspectionReport (tid_insp_bericht)
    # InspectionReport is linked to PlayDevice (fid_spielgeraet)
    
    defects = db.session.query(Defect).join(InspectionReport, Defect.tid_insp_bericht == InspectionReport.tid)\
        .filter(InspectionReport.fid_spielgeraet == device_fid).all()
        
    return render_template('defects_list.html', device=device, defects=defects)

@bp.route('/defect/<int:device_fid>/<int:defect_tid>', methods=['GET', 'POST'])
@login_required
def defect_edit(device_fid, defect_tid):
    device = PlayDevice.query.get_or_404(device_fid)
    defect = None
    
    if defect_tid > 0:
        defect = Defect.query.get_or_404(defect_tid)
    else:
        defect = Defect() # New defect
        
    if request.method == 'POST':
        description = request.form.get('description')
        priority = request.form.get('priority')
        photo = request.files.get('photo')
        
        if defect_tid == 0:
            # Create a dummy inspection report to link the defect to
            # In a real app, this might be handled differently
            report = InspectionReport(
                fid_spielgeraet=device_fid,
                inspektionsart='Mangelmeldung',
                datum_inspektion=func.now(),
                kontrolleur=current_user.nachname,
                pruefung_text='Mangelmeldung',
                pruefung_erledigt=0,
                fid_geraet_detail=0
            )
            db.session.add(report)
            db.session.flush() # Get the ID
            
            defect.tid_insp_bericht = report.tid
            defect.datum_erledigung = None
            db.session.add(defect)
        
        defect.beschrieb = description
        defect.id_dringlichkeit = priority
        
        if photo:
            # Simple base64 encoding for now
            import base64
            file_content = photo.read()
            defect.picture1_base64 = base64.b64encode(file_content).decode('utf-8')
            
        db.session.commit()
        flash('Mangel gespeichert.', 'success')
        return redirect(url_for('main.defects_list', device_fid=device_fid))

    return render_template('defect_edit.html', device=device, defect=defect)


@bp.route('/login', methods=['GET', 'POST'])
def login():
    if current_user.is_authenticated:
        return redirect(url_for('main.index'))
    
    error = None
    if request.method == 'POST':
        username = request.form['loginname']
        password = request.form['password']
        user = User.query.filter_by(e_mail=username).first()
        if user is None or not user.check_password(password):
            error = 'Benutzername oder Passphrase ungültig.'
        else:
            login_user(user)
            return redirect(url_for('main.index'))
    
    return render_template('login.html', error=error)

@bp.route('/logout')
def logout():
    logout_user()
    return redirect(url_for('main.login'))

