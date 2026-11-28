# Fontes dos bancos de locais

Gerados por `tools/places/build_places.py --lnm <little_navmap_msfs24.sqlite>` (2026-11-22). Não edite à mão: rode o script de novo. Com `--lnm` cada aeródromo usa o código que existe no MSFS 2024 (ex.: L65, E60, X26, OMDU).

| Arquivo | Fonte | Licença |
|---|---|---|
| `tourist_pois.json` | Wikidata (itens com coordenada, por classe; popularidade = nº de sitelinks) | CC0 |
| `training_aerodromes.json` | `hubs_data.json` (curado) + nomes de aeródromos do OurAirports + escolas de voo e aeroclubes do OpenStreetMap a ≤ 3 NM | OurAirports: domínio público; OSM: ODbL |
| `dropzones.json` | Lista curada pelo usuário (`tools/places/dropzones_curated.csv`, 32 zonas com nome e descrição) + OpenStreetMap (`sport=parachuting`) a ≤ 3 NM de um aeródromo + nomes de aeródromos do OurAirports | OSM: ODbL; OurAirports: domínio público |
| `scenic_aerodromes.json` | Lista curada pelo usuário (`tools/places/scenic_aerodromes_curated.csv`, 16 aeródromos de voo panorâmico); coordenadas das atrações do Wikidata a ≤ 75 NM | Wikidata: CC0 |
| `training_areas.json` | DECEA — AIP Brasil, WFS GeoAISWEB `ICA:airspace`, extração 2026-11-25 | Dado público oficial, sem licença explícita; uso citando a fonte |
| `airline_routes.json` | OpenFlights routes.dat (última atualização 2014-06) + códigos do OurAirports | ODbL (OpenFlights); OurAirports: domínio público |

Contém dados do © OpenStreetMap contributors, disponíveis sob a Open Database License (https://www.openstreetmap.org/copyright).
Contém dados do OpenFlights (https://openflights.org/data), disponíveis sob a Open Database License.

A altitude de salto **não** vem das fontes: é regra do gerador por classe de aeronave.
