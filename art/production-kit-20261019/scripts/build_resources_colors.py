"""Produce colour picked from catalog params (shared by resources / crops builds)."""


def main_color(p):
    for k in ('color', 'head', 'kernel', 'cap', 'flesh', 'core', 'body', 'liquid'):
        if isinstance(p.get(k), str) and p[k].startswith('#'): return p[k]
    return next((v for v in p.values() if isinstance(v, str) and v.startswith('#')), '#a0a0a0')
