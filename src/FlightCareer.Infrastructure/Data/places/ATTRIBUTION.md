# Fontes dos bancos de locais

Gerados por `tools/places/build_places.py` (2026-11-22). Não edite à mão: rode o script de novo.

| Arquivo | Fonte | Licença |
|---|---|---|
| `tourist_pois.json` | Wikidata (itens com coordenada, por classe; popularidade = nº de sitelinks) | CC0 |
| `training_aerodromes.json` | `hubs_data.json` (curado) + nomes de aeródromos do OurAirports + escolas de voo e aeroclubes do OpenStreetMap a ≤ 3 NM | OurAirports: domínio público; OSM: ODbL |
| `dropzones.json` | OpenStreetMap (`sport=parachuting`) a ≤ 3 NM de um aeródromo + nomes de aeródromos do OurAirports | OSM: ODbL; OurAirports: domínio público |

Contém dados do © OpenStreetMap contributors, disponíveis sob a Open Database License (https://www.openstreetmap.org/copyright).

A altitude de salto **não** vem das fontes: é regra do gerador por classe de aeronave.
