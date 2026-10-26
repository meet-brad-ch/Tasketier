"""Writes the alarm sound: three short 880 Hz beeps with soft edges, then a pause, 16-bit mono WAV.
The app loops it until the alarm card is answered, so the pause sets the rhythm.

    python tools/make_alarm_sound.py src/Tasketier.App/alarm.wav

No dependencies. The file is committed; rerun only when the sound changes.
"""

from __future__ import annotations

import math
import struct
import sys
import wave
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class BeepPattern:
    """What the sound is made of."""

    frequency_hz: float = 880.0
    beep_seconds: float = 0.16
    gap_seconds: float = 0.10
    count: int = 3
    sample_rate: int = 44100
    amplitude: float = 0.5
    edge_seconds: float = 0.012
    pause_seconds: float = 1.2


class AlarmSound:
    """Renders the pattern and writes it as WAV."""

    def __init__(self, pattern: BeepPattern) -> None:
        self._pattern = pattern

    def samples(self) -> list[int]:
        p = self._pattern
        beep = int(p.beep_seconds * p.sample_rate)
        gap = int(p.gap_seconds * p.sample_rate)
        edge = int(p.edge_seconds * p.sample_rate)
        out: list[int] = []
        for i in range(p.count):
            for n in range(beep):
                envelope = min(1.0, n / edge, (beep - n) / edge)
                value = p.amplitude * envelope * math.sin(2 * math.pi * p.frequency_hz * n / p.sample_rate)
                out.append(int(value * 32767))
            if i < p.count - 1:
                out.extend([0] * gap)
        out.extend([0] * int(p.pause_seconds * p.sample_rate))
        return out

    def write(self, target: Path) -> None:
        data = self.samples()
        target.parent.mkdir(parents=True, exist_ok=True)
        with wave.open(str(target), "wb") as f:
            f.setnchannels(1)
            f.setsampwidth(2)
            f.setframerate(self._pattern.sample_rate)
            f.writeframes(struct.pack("<%dh" % len(data), *data))


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print("usage: make_alarm_sound.py <target.wav>", file=sys.stderr)
        return 2
    AlarmSound(BeepPattern()).write(Path(argv[1]))
    print(f"wrote {argv[1]}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
