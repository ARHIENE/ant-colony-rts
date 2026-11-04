"""Deterministic appearance recipes; no Blender dependency."""
import random
FIELDS={'shape':['round','soft_oval','wide_cheek'], 'ears':['round','short','long','point','upright'], 'crown':['none','tuft','leaves'], 'pattern':['none','blaze_a','blaze_b','blaze_c'], 'expression':['calm','confident','happy','wink'], 'palette':list(range(8)), 'clothing':['none','sage','blue','rust']}
def generate(seed,count):
    if not 1<=count<=48:raise ValueError('Count must be 1..48')
    rng=random.Random(seed);out=[];seen=set()
    while len(out)<count:
        recipe={key:rng.choice(values) for key,values in FIELDS.items()}
        # Tall ears and leaves collide at their bases; keep a short tuft or bare crown.
        if recipe['ears'] in ('point','upright') and recipe['crown']=='leaves':recipe['crown']='tuft'
        signature=tuple(recipe.items())
        if signature not in seen:seen.add(signature);out.append(recipe)
    return out
def validate(items):
    if not isinstance(items,list) or not 1<=len(items)<=48:raise ValueError('Expected 1..48 recipes')
    for recipe in items:
        if not isinstance(recipe,dict) or set(recipe)!=set(FIELDS):raise ValueError('Invalid recipe fields')
        for key,values in FIELDS.items():
            if recipe[key] not in values:raise ValueError('Invalid '+key)
        if recipe['ears'] in ('point','upright') and recipe['crown']=='leaves':raise ValueError('Incompatible ears and crown')
    return items
