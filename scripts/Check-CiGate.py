#!/usr/bin/env python3
"""Report a required gate only when its real selected validation succeeded."""
import os
import sys


def main() -> int:
    selection = os.environ.get("SELECTION_RESULT", "")
    selected = os.environ.get("VALIDATION_SELECTED", "")
    validation = os.environ.get("VALIDATION_RESULT", "")
    if selection != "success" or selected not in {"true", "false"}:
        print("FAIL CI gate: selection did not complete with an explicit validation decision.", file=sys.stderr)
        return 1
    expected = "success" if selected == "true" else "skipped"
    if validation != expected:
        print(f"FAIL CI gate: selected={selected}, validation={validation!r}; expected {expected!r}.", file=sys.stderr)
        return 1
    print("PASS CI gate: selected validation succeeded." if selected == "true" else "PASS CI gate: selector explicitly found no affected validation.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
