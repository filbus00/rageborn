# Helpers for drawing a character's rows from centred strings (2026-11-16, the Burnt Library): c centres a string in the
# cell's width, fit puts blank rows on top to a height (a character stands on the cell's bottom), variant copies rows with
# some replaced (a side or a back view from the front).
def c(s, w=16, off=0):
    s = s.replace(" ", ".")
    pad = (w - len(s)) // 2 + off
    row = ("." * max(0, pad) + s.replace(" ", ".")).ljust(w, ".")
    assert len(row) == w, (s, len(row))
    return row


def fit(rows, h=16, w=16):
    rows = [r.ljust(w, ".") for r in rows]
    assert len(rows) <= h, (len(rows), h)
    return ["." * w] * (h - len(rows)) + rows


def variant(rows, changes):
    out = list(rows)
    for i, r in changes.items():
        out[i] = r
    return out
