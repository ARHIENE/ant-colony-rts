"""Shared actions for the kemono rig. Motions are authored as eased key poses in motions.py
(2026-10-18: replaced the adapted old-resident recipes, which moved every joint only a little)."""
import importlib
import motions


def create(rig):
    importlib.reload(motions)
    return motions.create(rig)
