"""Classificação dos aeroportos do jogo em tier + perfis (análise, passo 1 do catálogo de missões).

Uso: python -I airport_profiles.py <raiz do repo> <saida_dir> [<lnm_msfs24.sqlite> [<airport_profiles.json>]]
"""
import csv, json, os, re, sqlite3, sys, collections
import numpy as np

root, out = sys.argv[1], sys.argv[2]
lnm = sys.argv[3] if len(sys.argv) > 3 else None
infra = os.path.join(root, 'src', 'FlightCareer.Infrastructure')
os.makedirs(out, exist_ok=True)

HARD = ("ASPH", "CON", "CONC", "HARD", "BIT", "PEM", "ASP")  # igual a RunwaySurface.HardSurfaces

# --- pistas ---------------------------------------------------------------
rw = collections.defaultdict(list)
for r in csv.DictReader(open(os.path.join(infra, 'CSV', 'runways.csv'), encoding='utf-8')):
    if r.get('closed') == '1':
        continue
    surf = (r.get('surface') or '').upper()
    try:
        ln = int(float(r['length_ft'])) if r['length_ft'] else 0
    except ValueError:
        ln = 0
    rw[r['airport_ident']].append((surf, ln, r.get('lighted') == '1'))

# --- simulador (Little Navmap) ----------------------------------------------
sim = None
if lnm and os.path.exists(lnm):
    con = sqlite3.connect(f"file:{lnm}?mode=ro", uri=True)
    sim = {row[0].upper() for row in con.execute("select ident from airport where ident is not null")}
    # nome, posição, elevação e pista do MSFS: códigos reaproveitados (ex.: SBOK = fazenda no MT no
    # airports.csv antigo, Amontada/CE no MSFS 2024) precisam dos dados do simulador nas heurísticas
    sim_data = {row[0].upper(): row[1:] for row in con.execute(
        "select ident, name, laty, lonx, altitude, longest_runway_length, longest_runway_surface, num_runway_light, "
        "coalesce(num_parking_gate, 0) + coalesce(num_jetway, 0), coalesce(num_parking_cargo, 0) "
        "from airport where ident is not null")}
    con.close()
else:
    sim_data = {}
LNM_HARD = ("A", "C", "B", "T")  # Little Navmap: A asfalto, C concreto, B betume, T macadame/tratado

# --- aeroportos (mesmo filtro do DataSeeder) ---------------------------------
aps = []
seen = set()
for r in csv.DictReader(open(os.path.join(infra, 'CSV', 'airports.csv'), encoding='utf-8')):
    icao = r['icao_code'] or r['ident']
    t = r['type']
    if not icao or len(icao) < 3 or t in ('heliport', 'seaplane_base', 'balloonport', 'closed') or icao in seen:
        continue
    seen.add(icao)
    runs = rw.get(r['ident'], [])
    hard = any(any(h in s for h in HARD) for s, _, _ in runs)
    tier = 1 if t == 'large_airport' else 2 if t == 'medium_airport' else (3 if t == 'small_airport' and hard else 4)
    try:
        lat, lon = float(r['latitude_deg']), float(r['longitude_deg'])
    except ValueError:
        continue
    try:
        elev = int(float(r['elevation_ft'])) if r['elevation_ft'] else 0
    except ValueError:
        elev = 0
    # se o mesmo código no MSFS está a > 10 NM, é outro aeródromo: usa os dados do simulador
    sd = sim_data.get(icao.upper())
    if sd is not None:
        s_name, s_lat, s_lon, s_alt, s_len, s_surf, s_light, _g, _c = sd
        dnm = (((lat - s_lat) ** 2 + ((lon - s_lon) * np.cos(np.radians(lat))) ** 2) ** 0.5) * 60
        if dnm > 10:
            r = dict(r); r['name'] = s_name or r['name']; r['municipality'] = ''; r['scheduled_service'] = 'no'
            lat, lon, elev = float(s_lat), float(s_lon), int(s_alt or 0)
            s_hard = (s_surf or '') in LNM_HARD
            runs = [(('ASPH' if s_hard else (s_surf or '')), int(s_len or 0), bool(s_light))]
            hard = s_hard
            tier = 3 if hard else 4
    in_sim = None
    if sim is not None:
        cands = {c.strip().upper() for c in (r['icao_code'], r['gps_code'], r['ident'], r['local_code']) if c and c.strip()}
        in_sim = bool(cands & sim)
    aps.append(dict(icao=icao, ident=r['ident'], name=r['name'], city=r['municipality'], cc=r['iso_country'],
                    region=r['iso_region'], lat=lat, lon=lon, elev=elev, tier=tier, type=t,
                    sched=r['scheduled_service'] == 'yes',
                    maxlen=max((ln for _, ln, _ in runs), default=0), hard=hard,
                    lighted=any(l for _, _, l in runs), in_sim=in_sim, tags=set(),
                    gates=int(sd[7] or 0) if sd is not None else 0, cargo_park=int(sd[8] or 0) if sd is not None else 0))

# --- perfis vindos de dados existentes ----------------------------------------
def ids(path, key):
    d = json.load(open(path, encoding='utf-8'))
    return {x['icao'].upper() for x in d[key]}

places = os.path.join(infra, 'Data', 'places')
dz = ids(os.path.join(places, 'dropzones.json'), 'dropzones')
tr = ids(os.path.join(places, 'training_aerodromes.json'), 'aerodromes')
sc = ids(os.path.join(places, 'scenic_aerodromes.json'), 'aerodromes')
# bases de voo panorâmico (role != destino) ganham o subtipo turistico_curado:base
sc_base = {x['icao'].upper() for x in json.load(open(os.path.join(places, 'scenic_aerodromes.json'), encoding='utf-8'))['aerodromes'] if (x.get('role') or 'panoramico') != 'destino'}
hubs = json.load(open(os.path.join(infra, 'Data', 'hubs_data.json'), encoding='utf-8'))['HubProfiles']
hubmap = collections.defaultdict(set)
for cc, prof in hubs.items():
    for k, v in prof.items():
        if isinstance(v, list):
            for i in v:
                hubmap[i.upper()].add(k.replace('Hubs', ''))

# POIs turísticos (popularidade p) para um perfil "turístico" global aproximado
pois = json.load(open(os.path.join(places, 'tourist_pois.json'), encoding='utf-8'))
NATURAL = {'ilha','lago','parque_nacional','cachoeira','pico','praia','caverna','cabo','montanha','vulcao','reserva_natural','patrimonio_unesco','farol','castelo'}
poi_list = [p for p in (pois['pois'] if isinstance(pois, dict) and 'pois' in pois else pois) if p.get('k') in NATURAL]
poi_ll = np.array([[p['lat'], p['lon']] for p in poi_list], dtype=float) if poi_list else np.zeros((0, 2))
poi_pop = np.array([float(p.get('p', 0) or 0) for p in poi_list], dtype=float) if poi_list else np.zeros(0)

FARM = re.compile(r"\b(fazenda|usina|s[ií]tio|agropecu[aá]ria|agr[ií]cola|est[aâ]ncia|estancia|hacienda|finca|granja|"
                  r"farm|farms|ranch|plantation|station|ag strip|agricultural|estate)\b", re.I)
NOT_FARM = re.compile(r"\b(air station|naval|research station|space|coast guard|police|fire station|railway station|gas station|power station)\b", re.I)
MIL = re.compile(r"\b(air base|airbase|air force|afb|base a[ée]rea|base aeronaval|naval air|army air ?field|aaf|"
                 r"military|militar|fliegerhorst|heliport militar|rnas|raf |ala \d+|base de avia[cç][aã]o)\b|\bRAF\b", re.I)

for a in aps:
    i = a['icao'].upper()
    if i in dz: a['tags'].add('zona_salto')
    if i in tr: a['tags'].add('aeroclube')
    if i in sc: a['tags'].add('turistico_curado')
    if i in sc_base: a['tags'].add('turistico_curado:base')
    for h in hubmap.get(i, ()): a['tags'].add('hub_' + h.lower())
    if FARM.search(a['name']) and not NOT_FARM.search(a['name']) and a['tier'] >= 3: a['tags'].add('fazenda')
    # etiquetas automáticas (não curadas; não entram na pontuação do tier): servem para dizer se o
    # aeroporto pode receber voo de linha / carga mesmo fora das listas curadas
    if a['sched'] or (a['gates'] >= 2 and a['hard'] and a['maxlen'] >= 4300): a['tags'].add('linha_auto')
    if a['cargo_park'] >= 1 and a['hard']: a['tags'].add('carga_auto')
    if MIL.search(a['name']):
        a['tags'].add('militar_misto' if (a['sched'] or re.search(r'international|internacional|regional|municipal', a['name'], re.I)) else 'militar')
    if a['elev'] >= 5000: a['tags'].add('altitude')

# --- listas curadas pelo usuário (tools/places/*_curated.csv) ---------------------
CURATED = {
    'cargo_hubs_curated.csv': 'carga', 'pax_hubs_curated.csv': 'linha', 'exec_hubs_curated.csv': 'executivo',
    'feeder_hubs_curated.csv': 'alimentador', 'health_hubs_curated.csv': 'saude', 'training_hubs_curated.csv': 'instrucao',
    'mro_hubs_curated.csv': 'mro', 'agro_hubs_curated.csv': 'agro',
}
by_code = {}
for a in aps:
    by_code.setdefault(a['icao'].upper(), a)
    by_code.setdefault(a['ident'].upper(), a)
curated_info = collections.defaultdict(dict)
tools_dir = os.path.join(root, 'tools', 'places')
for fname, role in CURATED.items():
    path = os.path.join(tools_dir, fname)
    if not os.path.exists(path):
        continue
    for r in csv.DictReader(open(path, encoding='utf-8')):
        a = by_code.get(r['icao'].upper())
        if a is None:
            continue
        a['tags'].add(role)
        for t in (r.get('tags') or '').split('|'):
            if t:
                a['tags'].add(f'{role}:{t}')
        curated_info[a['icao']][role] = r['note']
mil_path = os.path.join(tools_dir, 'military_curated.csv')
if os.path.exists(mil_path):
    for r in csv.DictReader(open(mil_path, encoding='utf-8')):
        a = by_code.get(r['icao'].upper())
        if a is None:
            continue
        a['tags'].discard('militar'); a['tags'].discard('militar_misto')
        role = 'militar' if r['role'] == 'militar' else 'militar_misto'
        a['tags'].add(role)
        for t in (r.get('tags') or '').split('|'):
            if t:
                a['tags'].add(f'militar:{t}')
        curated_info[a['icao']][role] = r['note']

# --- isolamento: distância ao tier 1/2 mais próximo -----------------------------
def to_xyz(lat, lon):
    la, lo = np.radians(lat), np.radians(lon)
    return np.stack([np.cos(la) * np.cos(lo), np.cos(la) * np.sin(lo), np.sin(la)], axis=-1)

big = [a for a in aps if a['tier'] <= 2]
t1 = [a for a in aps if a['tier'] == 1]
big_xyz = to_xyz(np.array([a['lat'] for a in big]), np.array([a['lon'] for a in big]))
all_xyz = to_xyz(np.array([a['lat'] for a in aps]), np.array([a['lon'] for a in aps]))
poi_xyz = to_xyz(poi_ll[:, 0], poi_ll[:, 1]) if len(poi_ll) else None
t1_xyz = to_xyz(np.array([a['lat'] for a in t1]), np.array([a['lon'] for a in t1]))
R_NM = 3440.065
for s in range(0, len(aps), 2000):
    blk = all_xyz[s:s + 2000]
    dots = np.clip(blk @ big_xyz.T, -1, 1)
    near = R_NM * np.arccos(dots.max(axis=1))
    near1 = R_NM * np.arccos(np.clip(blk @ t1_xyz.T, -1, 1).max(axis=1))
    part = np.partition(dots, -2, axis=1)
    near_other = R_NM * np.arccos(part[:, -2])  # 2º mais próximo (exclui ele mesmo quando é tier 1/2)
    if poi_xyz is not None:
        pd = R_NM * np.arccos(np.clip(blk @ poi_xyz.T, -1, 1))
        score = ((pd <= 15) * poi_pop).sum(axis=1)
    else:
        score = np.zeros(len(blk))
    for k, a in enumerate(aps[s:s + 2000]):
        a['nm_big'] = float(near[k]) if a['tier'] >= 3 else 0.0
        a['nm_t1'] = float(near1[k])
        a['nm_other_big'] = float(near_other[k])
        a['poi_score'] = float(score[k])

for a in aps:
    if a['tier'] >= 3 and a['nm_big'] >= 150: a['tags'].add('isolado')
    elif a['tier'] >= 3 and a['nm_big'] >= 75: a['tags'].add('interior_distante')
    if a['tier'] == 2 and a['nm_other_big'] >= 150: a['tags'].add('isolado')

# limiar turístico global: top 3 % de pontuação de POIs entre os tier 2–4
sc_vals = sorted(a['poi_score'] for a in aps if a['tier'] >= 2)
thr = sc_vals[int(len(sc_vals) * 0.97)] if sc_vals else 0
for a in aps:
    if a['tier'] >= 2 and a['poi_score'] >= thr and thr > 0 and 'militar' not in a['tags']: a['tags'].add('turistico_poi')

# --- saída ------------------------------------------------------------------
with open(os.path.join(out, 'airport_profiles.csv'), 'w', newline='', encoding='utf-8') as f:
    w = csv.writer(f)
    w.writerow(['icao', 'name', 'city', 'cc', 'tier', 'in_sim', 'maxlen', 'lighted', 'elev', 'nm_big', 'nm_t1', 'poi_score', 'tags'])
    for a in aps:
        w.writerow([a['icao'], a['name'], a['city'], a['cc'], a['tier'], a['in_sim'], a['maxlen'], int(a['lighted']),
                    a['elev'], round(a['nm_big']), round(a['nm_t1']), round(a['poi_score'], 2), ';'.join(sorted(a['tags']))])

profiles_json = {}
for a in aps:
    if a['tags'] and (a['in_sim'] is None or a['in_sim']):
        profiles_json[a['icao']] = {'t': sorted(a['tags'])}
        if a['icao'] in curated_info:
            profiles_json[a['icao']]['n'] = curated_info[a['icao']]
if len(sys.argv) > 4:
    doc = {'version': 1, 'generatedBy': 'tools/places/airport_profiles.py',
           'note': 'Perfis dos aeroportos (tier não muda). Listas curadas: tools/places/*_curated.csv; heurísticas: nome, distância e POIs. t = perfis; n = descrição curada por papel.',
           'airports': profiles_json}
    with open(sys.argv[4], 'w', encoding='utf-8', newline=chr(10)) as f:
        json.dump(doc, f, ensure_ascii=False, separators=(',', ':'))
        f.write(chr(10))

summary = {
    'total': len(aps),
    'in_sim': sum(1 for a in aps if a['in_sim']) if sim is not None else None,
    'tiers': collections.Counter(a['tier'] for a in aps),
    'tiers_in_sim': collections.Counter(a['tier'] for a in aps if a['in_sim']) if sim is not None else None,
    'poi_threshold': thr,
    'tags': collections.Counter(t for a in aps for t in a['tags']),
    'tags_in_sim': collections.Counter(t for a in aps if a['in_sim'] for t in a['tags']) if sim is not None else None,
    'tags_br': collections.Counter(t for a in aps if a['cc'] == 'BR' for t in a['tags']),
    'tags_by_tier': {t: dict(collections.Counter(x for a in aps if a['tier'] == t for x in a['tags'])) for t in (1, 2, 3, 4)},
    'no_tag_by_tier': collections.Counter(a['tier'] for a in aps if not a['tags']),
}
json.dump(summary, open(os.path.join(out, 'summary.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1, default=dict)
print(json.dumps(summary, ensure_ascii=False, indent=1, default=dict))
