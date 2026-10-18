"""Assets/_Project altında .meta'sı olmayan klasör ve .cs dosyalarına .meta yazar.

Unity açık değilken (ör. ayrı bir worktree'de) yeni dosya eklendiğinde kullanılır; böylece
GUID'ler commit'le birlikte gelir ve iki kişide farklı GUID oluşmaz. Başka dosya türlerine
dokunmaz, onları Unity içe aktarırken üretsin.

Kullanım: py -3 tools/meta/ensure_meta.py [--check]
--check: yazmaz, eksik varsa listeler ve 1 ile çıkar.
"""
import sys
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Assets" / "_Project"

FOLDER = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

SCRIPT = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def missing():
    for p in sorted(ROOT.rglob("*")):
        if p.name.startswith(".") or p.suffix == ".meta":
            continue
        if any(part.startswith(".") for part in p.relative_to(ROOT).parts):
            continue
        meta = p.with_name(p.name + ".meta")
        if meta.exists():
            continue
        if p.is_dir():
            yield p, FOLDER
        elif p.suffix == ".cs":
            yield p, SCRIPT


def main():
    check = "--check" in sys.argv
    found = list(missing())
    for p, template in found:
        rel = p.relative_to(ROOT.parents[1])
        if check:
            print(f"eksik meta: {rel}")
        else:
            p.with_name(p.name + ".meta").write_text(
                template.format(guid=uuid.uuid4().hex), encoding="utf-8", newline="\n")
            print(f"yazildi: {rel}.meta")
    return 1 if (check and found) else 0


if __name__ == "__main__":
    sys.exit(main())
