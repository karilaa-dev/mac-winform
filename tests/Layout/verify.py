"""Run the portable fixture and exercise its own native AppKit views in-process."""
import os
from pathlib import Path
import subprocess
import tempfile

fixture = Path(__file__).resolve().parent
with tempfile.TemporaryDirectory(prefix="mac-winform-layout-") as directory:
    probe = Path(directory) / "LayoutProbe.dylib"
    subprocess.run(["xcrun", "clang", "-dynamiclib", "-fobjc-arc", "-framework",
                    "AppKit", str(fixture / "Probe.m"), "-o", str(probe)], check=True)
    env = dict(os.environ, MAC_WINFORM_LAYOUT_PROBE=str(probe))
    subprocess.run(["dotnet", "winforms", "run", str(fixture)],
                   env=env, timeout=90, check=True)
