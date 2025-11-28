import base64
from app.models import Defect

def test_image_base64_encoding():
    # Simulate the logic used in the route
    file_content = b'test image content'
    encoded = base64.b64encode(file_content).decode('utf-8')
    
    # Verify it matches what we expect
    assert encoded == 'dGVzdCBpbWFnZSBjb250ZW50'
    
    # Verify we can decode it back
    decoded = base64.b64decode(encoded)
    assert decoded == file_content
