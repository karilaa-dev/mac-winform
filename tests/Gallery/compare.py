"""Compare saved public WinForms control state against a native Windows run."""
from pathlib import Path
import argparse
import json
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("reference", type=Path, help="Stock Windows state.json")
    parser.add_argument("candidate", type=Path, help="Mac WinForms state.json")
    args = parser.parse_args()
    reference = json.loads(args.reference.read_text(encoding="utf-8-sig"))
    candidate = json.loads(args.candidate.read_text(encoding="utf-8-sig"))
    reference_bounds = reference.pop("Bounds")
    candidate_bounds = candidate.pop("Bounds")
    matched = 0
    for key in sorted(reference.keys() | candidate.keys()):
        if key in reference and key in candidate and reference[key] == candidate[key]:
            matched += 1
        else:
            print(f"{key}: Windows {reference.get(key)!r}, candidate {candidate.get(key)!r}")
    print(f"{matched}/{len(reference.keys() | candidate.keys())} state values match.")

    rectangles = 0
    for index in range(max(len(reference_bounds), len(candidate_bounds))):
        expected = reference_bounds[index] if index < len(reference_bounds) else None
        actual = candidate_bounds[index] if index < len(candidate_bounds) else None
        if expected == actual:
            rectangles += 1
        else:
            print(f"Rectangle {index + 1}: Windows {expected}, candidate {actual}")
    total = max(len(reference_bounds), len(candidate_bounds))
    print(f"{rectangles}/{total} control rectangles match.")
    return int(reference != candidate or rectangles != total)


if __name__ == "__main__":
    sys.exit(main())
