from pathlib import Path
from datetime import datetime, timezone
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parent
OUTPUT = ROOT / 'Igra-FULL-HANDOFF-2026-10-10.zip'
CHECKSUM = OUTPUT.with_suffix('.zip.sha256')
PREFIX = 'Igra-FULL-HANDOFF'
GENERATED = Path('C:/Users/User/.codex/generated_images/01a12619-054a-7b43-af10-11952a2ab009')
ATTACHMENTS = [
    Path('C:/Users/User/AppData/Local/Temp/codex-clipboard-73cf9212-14ee-4a7a-acda-ad27d1c5789d.png'),
    Path('C:/Users/User/AppData/Local/Temp/codex-clipboard-441eeec0-0006-443e-937a-27703b3a822b.png'),
]

def sha256(data):
    return hashlib.sha256(data).hexdigest()

def build():
    excluded = {OUTPUT, CHECKSUM}
    project_files = sorted(p for p in ROOT.rglob('*') if p.is_file() and p not in excluded)
    project_dirs = sorted(p for p in ROOT.rglob('*') if p.is_dir())
    entries = [(p, p.relative_to(ROOT).as_posix(), 'project') for p in project_files]
    unavailable = []
    if GENERATED.is_dir():
        entries.extend((p, 'additional-materials/generated-originals/' + p.relative_to(GENERATED).as_posix(), 'generated-original')
                       for p in sorted(GENERATED.rglob('*')) if p.is_file())
    else:
        unavailable.append(str(GENERATED))
    for p in ATTACHMENTS:
        if p.is_file():
            entries.append((p, 'additional-materials/user-attachments/' + p.name, 'user-attachment'))
        else:
            unavailable.append(str(p))
    records = []
    with zipfile.ZipFile(OUTPUT, 'w', zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as archive:
        for p in project_dirs:
            archive.writestr(PREFIX + '/' + p.relative_to(ROOT).as_posix() + '/', b'')
        for source, relative, kind in entries:
            data = source.read_bytes()
            archive.writestr(PREFIX + '/' + relative, data)
            records.append({'path': relative, 'bytes': len(data), 'sha256': sha256(data),
                            'kind': kind, 'original_path': str(source)})
        manifest = {
            'package': 'Full game project handoff',
            'created_at_utc': datetime.now(timezone.utc).isoformat(),
            'user_acceptance': 'Rejected by user; preserved for another agent',
            'project_file_count': len(project_files),
            'additional_file_count': len(entries) - len(project_files),
            'total_payload_files': len(records),
            'markdown_files': sum(r['path'].lower().endswith('.md') for r in records),
            'payload_bytes': sum(r['bytes'] for r in records),
            'unavailable_additional_paths': unavailable,
            'exclusions': ['This output ZIP and its checksum sidecar only'],
            'manifest_note': 'Manifest does not hash itself. The archive checksum is provided alongside the ZIP.',
            'files': records,
        }
        archive.writestr(PREFIX + '/MANIFEST.json', json.dumps(manifest, ensure_ascii=False, indent=2).encode('utf-8'))
    with zipfile.ZipFile(OUTPUT) as archive:
        assert archive.testzip() is None, 'ZIP CRC verification failed'
        for record in records:
            data = archive.read(PREFIX + '/' + record['path'])
            assert len(data) == record['bytes'] and sha256(data) == record['sha256'], record['path']
        expected = {p.relative_to(ROOT).as_posix() for p in project_files}
        actual = {r['path'] for r in records if r['kind'] == 'project'}
        assert expected == actual, 'Project files missing from archive'
        assert PREFIX + '/sources/' in archive.namelist(), 'Empty sources directory not preserved'
    digest = hashlib.file_digest(OUTPUT.open('rb'), 'sha256').hexdigest()
    CHECKSUM.write_text(digest + '  ' + OUTPUT.name + '\n', encoding='utf-8')
    print(json.dumps({'archive': str(OUTPUT), 'archive_bytes': OUTPUT.stat().st_size,
                      'sha256': digest, 'project_files': len(project_files),
                      'additional_files': len(entries)-len(project_files),
                      'markdown_files': manifest['markdown_files'],
                      'verified_payload_files': len(records), 'missing_additional': unavailable}, ensure_ascii=False))

if __name__ == '__main__':
    build()
