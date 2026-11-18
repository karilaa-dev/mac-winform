"""Run the portable fixture and exercise its own native AppKit views in-process."""
import os
from pathlib import Path
import subprocess
import tempfile

fixture = Path(__file__).resolve().parent
with tempfile.TemporaryDirectory(prefix="mac-winform-widgets-") as directory:
    probe = Path(directory) / "WidgetProbe.dylib"
    subprocess.run(["xcrun", "clang", "-dynamiclib", "-fobjc-arc", "-framework",
                    "AppKit", str(fixture / "Probe.m"), "-o", str(probe)], check=True)
    env = dict(os.environ, MAC_WINFORM_WIDGET_PROBE=str(probe))
    subprocess.run([str(fixture.parents[1] / "bin" / "mac-winform"), "run", str(fixture)],
                   env=env, timeout=90, check=True)
