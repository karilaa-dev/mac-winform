"""Run the native WinForms acceptance tests on macOS from an interactive login session."""
from pathlib import Path
import os
import subprocess
import sys
import tempfile

TESTS = Path(__file__).resolve().parent
LAUNCHER = TESTS.parent / "bin/mac-winform"


def main():
    if sys.platform != "darwin":
        sys.exit("This verification requires macOS and AppKit.")
    with tempfile.TemporaryDirectory(prefix="mac-winform-verification-") as directory:
        scratch = Path(directory)
        env = dict(os.environ)
        env.setdefault("MAC_WINFORM_CACHE",str(scratch / "cache"))

        def run(*args):
            subprocess.run(args,env=env,check=True,timeout=240)

        run(sys.executable,TESTS / "cli_integration.py")
        suites = sorted(p for p in TESTS.iterdir() if p.is_dir() and (p / (p.name + ".csproj")).is_file())
        for suite in suites:
            print(f"Testing {suite.name}",flush=True)
            if (suite / "verify.py").exists():
                run(sys.executable,suite / "verify.py")
            elif suite.name == "Gallery":
                output = scratch / "gallery"
                output.mkdir()
                run(LAUNCHER,"run",suite,"--","--probe",output)
                run(sys.executable,suite / "compare.py",TESTS.parent / "docs/gallery/windows/state.json",output / "state.json")
            elif suite.name == "Printing":
                proof = scratch / "print.pdf"
                run(LAUNCHER,"run",suite,"--","--pdf",proof)
                if not proof.read_bytes().startswith(b"%PDF-"):
                    sys.exit("PrintDocument did not create a PDF.")
            else:
                run(LAUNCHER,"run",suite)
        print(f"Passed {len(suites)} native fixtures and both launcher tests.",flush=True)


if __name__ == "__main__":
    main()
