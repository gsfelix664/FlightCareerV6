"""Gera Data/places/country_borders.json a partir do Natural Earth (domínio público).

Uso: python -I build_country_borders.py <ne_10m_admin_0_countries.geojson> <saida.json>

Fonte: https://github.com/nvkelso/natural-earth-vector (geojson/ne_10m_admin_0_countries.geojson).
Saída compacta: [{"iso": "BR", "bbox": [minLon, minLat, maxLon, maxLat],
                  "polys": [[anelExterno, buraco1, ...], ...]}]
cada anel = lista plana [lon0, lat0, lon1, lat1, ...] com 2 casas decimais (~1 km) e pontos
mais próximos que TOL graus do anterior removidos (simplificação radial).
"""
import json, sys

TOL = 0.01  # graus (~1 km): suficiente para decidir o país de um aeródromo


# áreas sem ISO no Natural Earth: país usado na aviação (igual ao OurAirports)
NO_ISO = {'Somaliland': 'SO', 'N. Cyprus': 'CY', 'Dhekelia': 'CY', 'Akrotiri': 'CY',
          'Cyprus U.N. Buffer Zone': 'CY', 'USNB Guantanamo Bay': 'CU', 'Siachen Glacier': 'IN'}


def iso_of(props):
    if props.get('NAME') in NO_ISO:
        return NO_ISO[props['NAME']]
    # ISO_A2 vem -99 em alguns países (França, Noruega, Kosovo...): usar ISO_A2_EH / WB_A2
    for k in ('ISO_A2', 'ISO_A2_EH', 'WB_A2'):
        v = props.get(k)
        if isinstance(v, str) and len(v) == 2 and v.isalpha() and v != '-9':
            return v.upper()
    return None


def ring(coords):
    out, last = [], None
    for lon, lat in ((c[0], c[1]) for c in coords):
        if last is None or abs(lon - last[0]) >= TOL or abs(lat - last[1]) >= TOL:
            out += [round(lon, 2), round(lat, 2)]
            last = (lon, lat)
    return out if len(out) >= 8 else None  # pelo menos 4 pontos


def main(src, dst):
    data = json.load(open(src, encoding='utf-8'))
    result, skipped = [], []
    for f in data['features']:
        p = f['properties']
        iso = iso_of(p)
        if not iso:
            skipped.append(p.get('NAME'))
            continue
        g = f['geometry']
        polys = g['coordinates'] if g['type'] == 'MultiPolygon' else [g['coordinates']]
        outp = []
        for poly in polys:
            rings = [r for r in (ring(rr) for rr in poly) if r]
            if rings:
                outp.append(rings)
        if not outp:
            continue
        xs = [v for poly in outp for v in poly[0][0::2]]
        ys = [v for poly in outp for v in poly[0][1::2]]
        result.append({'iso': iso, 'bbox': [min(xs), min(ys), max(xs), max(ys)], 'polys': outp})
    json.dump({'source': 'Natural Earth 1:10m Admin 0 Countries (domínio público)', 'tolDeg': TOL,
               'countries': result}, open(dst, 'w', encoding='utf-8'), separators=(',', ':'))
    print(f'{len(result)} países gravados em {dst}; sem ISO: {skipped}')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
