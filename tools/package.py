"""Package only reviewed plugin payloads; never include user data or host binaries."""
from pathlib import Path
import hashlib
import json
import zipfile

root = Path(__file__).resolve().parents[1]
release = root / 'MarketLedger/bin/Release'
artifacts = root / 'artifacts'
artifacts.mkdir(exist_ok=True)
manifest = json.loads((release / 'MarketLedger.json').read_text(encoding='utf-8-sig'))
assert manifest['AssemblyVersion'] == '1.0.0.0'
assert manifest['Author'] == 'Roxyz0501' and manifest['DalamudApiLevel'] == 15
assert manifest['RepoUrl'] == 'https://github.com/Roxyz0501/MarketLedger'
assert manifest['IconUrl'].endswith('/MarketLedger/main/MarketLedger/images/icon.png')
payload = {name: release / name for name in ['MarketLedger.dll', 'MarketLedger.json', 'MarketLedger.deps.json', 'LICENSE', 'NOTICE']}
payload.update({'images/icon.png': root / 'MarketLedger/images/icon.png', 'README.md': root / 'README.md', 'VALIDATION.md': root / 'VALIDATION.md'})
target = artifacts / 'MarketLedger-1.0.0.0.zip'
with zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for name, source in payload.items():
        assert source.is_file(), source
        z.write(source, name)
with zipfile.ZipFile(target) as z:
    assert set(z.namelist()) == set(payload)
    assert z.testzip() is None
    assert json.loads(z.read('MarketLedger.json')) == manifest
digest = hashlib.sha256(target.read_bytes()).hexdigest()
(artifacts / 'SHA256SUMS.txt').write_text(f'{digest}  {target.name}\n', encoding='utf-8')
entry = dict(manifest)
url = f'https://github.com/Roxyz0501/MarketLedger/releases/download/v1.0.0.0/{target.name}'
entry.update(DownloadLinkInstall=url, DownloadLinkUpdate=url, DownloadLinkTesting=url)
(artifacts / 'repo-entry.PREPARED-NOT-PUBLISHED.json').write_text(json.dumps(entry, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(f'Package verified: {len(payload)} files, {target.stat().st_size} bytes')
print(f'SHA256: {digest}')
