"""Read-only, explicit date-range archive inspection. Does not modify SQLite or user files."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path

def records(root, start, end):
    manifest_path = root / 'manifest.json'
    if manifest_path.stat().st_size > 65536:
        raise ValueError('Manifest exceeds 64 KiB')
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    if manifest.get('Format') != 1 or manifest.get('Layout') != 'yyyy-MM-dd/sha256(id).json':
        raise ValueError('Unsupported manifest')
    for day in root.iterdir():
        if day.is_symlink() or not day.is_dir():
            continue
        try:
            date = datetime.date.fromisoformat(day.name)
        except ValueError:
            continue
        if not start <= date <= end:
            continue
        for file in day.glob('*.json'):
            if file.is_symlink() or file.stat().st_size > 1024 * 1024:
                raise ValueError('Unsupported file: ' + str(file))
            record = json.loads(file.read_text(encoding='utf-8'))
            if record.get('Format') != 1:
                raise ValueError('Unsupported transaction format')
            purchase = record['Purchase']
            expected = hashlib.sha256(purchase['Id'].encode('utf-8')).hexdigest()
            if file.stem != expected:
                raise ValueError('Transaction identity mismatch')
            yield purchase

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--from-date', required=True, type=datetime.date.fromisoformat)
    parser.add_argument('--through-date', required=True, type=datetime.date.fromisoformat)
    args = parser.parse_args()
    if args.from_date > args.through_date:
        parser.error('Date range is reversed')
    for purchase in records(args.directory, args.from_date, args.through_date):
        print(json.dumps(purchase, ensure_ascii=False))
