#!/usr/bin/env bash
# Apple trees & healing: generate the Unity assets and run the tests with an installed, activated Unity editor.
#
#   UNITY=/path/to/Unity scripts/verify-apples.sh
#
# macOS example:  UNITY="/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app/Contents/MacOS/Unity"
# Linux example:  UNITY="$HOME/Unity/Hub/Editor/6000.3.27f1/Editor/Unity"
#
# Results go to The-Physic-Thief-Project/TestResults/ (ignored by git):
#   AppleTreeSetup-report.txt   setup run twice + duplicate checks
#   setup.log, tests.log        full editor logs
#   editmode-results.xml        NUnit XML of every EditMode test (includes the Play Mode integration test)
#   Captures/                   screenshots and frames (only when Unity has a graphics device)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/The-Physic-Thief-Project"
OUT="$PROJECT/TestResults"
UNITY="${UNITY:?Set UNITY to the Unity 6000.3.27f1 editor executable}"
mkdir -p "$OUT"

if head -c 40 "$PROJECT/Assets/_Project/Scenes/VillageScene.unity" | grep -q "git-lfs"; then
  echo "VillageScene.unity is a Git LFS pointer: run 'git lfs install && git lfs pull' first." >&2
  exit 1
fi

echo "== 1/2 Setup (assets, HUD prompt, VillageScene placement), run twice =="
"$UNITY" -batchmode -quit -projectPath "$PROJECT" \
  -executeMethod PhysicsThief.EditorTools.AppleTreeSetup.RunFromCommandLine \
  -appleSetupReport "$OUT/AppleTreeSetup-report.txt" -logFile "$OUT/setup.log"
cat "$OUT/AppleTreeSetup-report.txt"

echo "== 2/2 EditMode tests (unit + Play Mode integration) =="
set +e
"$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform EditMode \
  -testResults "$OUT/editmode-results.xml" -logFile "$OUT/tests.log"
status=$?
set -e

python3 - "$OUT/editmode-results.xml" <<'PY'
import sys, xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
print(f"total={root.get('total')} passed={root.get('passed')} failed={root.get('failed')} skipped={root.get('skipped')}")
for case in root.iter('test-case'):
    if case.get('result') != 'Passed':
        msg = case.find('./failure/message')
        print(f"  {case.get('result')}: {case.get('fullname')}\n    {(msg.text or '').strip() if msg is not None else ''}")
PY

if [ -d "$OUT/Captures/03_eat_apple" ] && command -v ffmpeg >/dev/null; then
  ffmpeg -loglevel error -y -framerate 20 -i "$OUT/Captures/03_eat_apple/frame_%03d.png" \
    -pix_fmt yuv420p "$OUT/Captures/03_eat_apple.mp4" && echo "Recording: $OUT/Captures/03_eat_apple.mp4"
fi
exit $status
