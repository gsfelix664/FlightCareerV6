import json
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
INFRA_DATA = os.path.join(REPO_ROOT, "src", "FlightCareer.Infrastructure", "Data")

CATALOG_PATH = os.path.join(INFRA_DATA, "mission_catalog.json")
TEXTS_PATH = os.path.join(INFRA_DATA, "mission_texts.json")

# 17 desabilitados conforme spec
DISABLED_IDS = {
    "instrucao_ifr", "adaptacao_noturna", "pinga_pinga", "demo_flight",
    "pulverizacao", "reboque_planador", "publicidade_aerea", "patrulha_vigilancia",
    "inspecao_linhas", "monitoramento_trafego", "sar", "calibracao_auxilios",
    "ferry_permit", "check_flight", "instrucao_stol", "evacuacao_por_do_sol",
    "saude_familia"
}

# civilAtMilitaryBase = False conforme § 9.2
MILITARY_BASE_EXCEPTIONS = {
    "sar", "militar_can", "autoridades", "diplomatico", "humanitario", "translado_manutencao"
}

LEGACY_MAP = {
    "troncal_domestico": ["linha_domestica_hub", "regional_direto"],
    "ponte_aerea": ["ponte_aerea_troncal"],
    "regional_internacional": ["linha_regional_internacional"],
    "longo_curso": ["linha_long_haul", "long_haul_internacional"],
    "feeder_inbound": ["alimentacao_hub"],
    "carga_paletizada": ["carga_heavy_nacional", "carga_heavy_internacional", "carga_heavy_long_haul", "fretamento_global_carga_alto_valor"],
    "carga_overnight": ["cargueiro_noturno"],
    "malotes": ["malotes_documentos"],
    "ecommerce": ["ecommerce_varejo"],
    "carga_jit": ["aog_agro_suporte", "aog_maquina_campo"],
    "carga_dgr": ["carga_fragil", "carga_fragil_regional", "carga_fragil_municipal"],
    "insumos_agro": ["agro_pecas_insumos", "agro_insumos_biotecnologia", "agro_suprimento_campanha", "insumos_urgentes_agro"],
    "suprimentos_remotos": ["abastecimento_rancho"],
    "executivo_corporativo": ["executiva_business", "executivo_regional", "fretamento_executivo", "fretamento_executivo_regional"],
    "vip_internacional": ["vip_internacional_curto", "vip_internacional_regional"],
    "agro_executivo": ["agro_legacy", "agro_tech", "agro_investidores"],
    "familia_alto_padrao": ["familia_alto_padrao"],
    "equipes_urgentes": ["equipes_urgentes"],
    "translado_piloto_agricola": ["translado_piloto_agricola"],
    "instrucao_local": ["instrucao_local"],
    "circuito_tgl": ["circuito_tgl"],
    "aerolevantamento": ["aerolevantamento"],
    "paraquedismo": ["paraquedismo"],
    "panoramico": ["voo_panoramico"],
    "navegacao_vfr": ["navegacao_cross_country"],
    "cheque": ["voo_de_cheque"],
    "uti_inter_hospitalar": ["remocao_urgencia", "transferencia_eletiva"],
    "orgaos": ["transporte_orgaos"],
    "medevac_remoto": ["medivac_bush", "medevac_dinamico", "medevac_regional", "medevac_municipal"],
    "crew_parts": ["logistica_apoio_crew_parts"],
    "ferry_posicionamento": ["translado_posicionamento_ferry", "translado_basico"],
    "translado_manutencao": ["manutencao_regional"],
    "pessoas_remotas": ["transporte_patrao", "troca_equipe", "apoio_batedor"],
    "autoridades": ["governo_local_estadual"],
    "diplomatico": ["diplomatico_militar"]
}

RAW_MISSIONS_DATA = [
    # A. Instrução e formação
    {
        "id": "instrucao_local", "name": "Instrução Local", "description": "Voo de instrução inicial com manobras em área e toques na origem.",
        "board": "Instrucao", "vip": False, "phase": 1, "payFamily": "instrucao_basica", "xpType": "instrucao_basica",
        "generator": "local_instruction",
        "baseByTier": {"1": 1, "2": 3, "3": 10, "4": 6},
        "originProfiles": {"instrucao": 4, "aeroclube": 4},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [20, 40], "pax": [1, 1], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [2, 6], "crew": {"count": 1, "luggageKg": [2, 6]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "circuito_tgl", "name": "Circuito de Tráfego (TGL)", "description": "Treinamento intensivo de aproximação, toques e arremetidas no circuito de tráfego.",
        "board": "Instrucao", "vip": False, "phase": 1, "payFamily": "instrucao_basica", "xpType": "instrucao_basica",
        "generator": "tgl_circuit",
        "baseByTier": {"1": 1, "2": 3, "3": 10, "4": 6},
        "originProfiles": {"instrucao": 4, "aeroclube": 4},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [
            {"weight": 80, "rangeNm": [10, 25], "pax": [1, 1], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 20, "rangeNm": [10, 25], "pax": [0, 0], "cargoLbs": [0, 0], "bigAirport": False}
        ],
        "luggagePerPersonKg": [2, 6], "crew": {"count": 1, "luggageKg": [2, 6]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "navegacao_vfr", "name": "Navegação VFR", "description": "Navegação visual entre aeródromos homologados com reporte e pousos completos.",
        "board": "Instrucao", "vip": False, "phase": 1, "payFamily": "instrucao", "xpType": "instrucao",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 10, "4": 6},
        "originProfiles": {"instrucao": 3, "aeroclube": 3},
        "destination": {"rule": "standard", "tiers": [3], "country": "same", "runway": "any", "maxNm": 80, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [80, 250], "pax": [1, 1], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [2, 6], "crew": {"count": 1, "luggageKg": [2, 6]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "instrucao_ifr", "name": "Instrução IFR", "description": "Procedimentos de aproximação por instrumentos e esperas em rota.",
        "board": "Instrucao", "vip": False, "phase": 1, "payFamily": "instrucao", "xpType": "instrucao",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 6, "3": 6, "4": 0},
        "originProfiles": {"instrucao:ifr": 5, "instrucao:academia_linha": 3, "instrucao": 2},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [40, 160], "pax": [1, 1], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [2, 6], "crew": {"count": 1, "luggageKg": [2, 6]},
        "requiresHardSurface": True, "minRunwayDest": 3000
    },
    {
        "id": "adaptacao_noturna", "name": "Adaptação Noturna", "description": "Treinamento de voo noturno e operação em aeródromo com balizamento luminoso.",
        "board": "Instrucao", "vip": False, "phase": 1, "payFamily": "instrucao", "xpType": "instrucao",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 3, "3": 6, "4": 0},
        "originProfiles": {"instrucao": 3, "aeroclube": 3},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3], "country": "same", "runway": "lighted", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [10, 30], "pax": [1, 1], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [2, 6], "crew": {"count": 1, "luggageKg": [2, 6]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "cheque", "name": "Voo de Cheque / Revalidação", "description": "Voo de avaliação prática com examinador credenciado para concessão ou revalidação de habilitação.",
        "board": "Instrucao", "vip": False, "phase": 1, "payFamily": "instrucao", "xpType": "instrucao",
        "generator": "checkride",
        "baseByTier": {"1": 1, "2": 3, "3": 6, "4": 1},
        "originProfiles": {"instrucao": 3, "instrucao:academia_linha": 3},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [20, 60], "pax": [1, 1], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [2, 6], "crew": {"count": 1, "luggageKg": [2, 6]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "instrucao_stol", "name": "Instrução STOL / Bush", "description": "Treinamento avançado de pousos curtos e operação em pistas desfavoráveis.",
        "board": "Instrucao", "vip": False, "phase": 2, "payFamily": "instrucao", "xpType": "instrucao",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 1, "3": 3, "4": 6},
        "originProfiles": {"aeroclube": 3, "instrucao:bush": 4, "altitude": 2},
        "destination": {"rule": "standard", "tiers": [4], "country": "same", "runway": "short_or_unpaved", "maxNm": 30, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [10, 40], "pax": [1, 1], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [2, 6], "crew": {"count": 1, "luggageKg": [2, 6]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },

    # B. Linha aérea e regular
    {
        "id": "ponte_aerea", "name": "Ponte Aérea de Alta Frequência", "description": "Ligação rápida e contínua entre dois grandes centros econômicos interligados.",
        "board": "LinhaRegular", "vip": False, "phase": 6, "payFamily": "linha", "xpType": "linha",
        "generator": "standard",
        "baseByTier": {"1": 10, "2": 6, "3": 0, "4": 0},
        "originProfiles": {"linha:ponte_aerea": 8, "linha:alta_frequencia": 4},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": 300, "profiles": {"linha:ponte_aerea": 8, "linha:alta_frequencia": 4}},
        "profiles": [{"weight": 100, "rangeNm": [150, 400], "pax": [120, 195], "cargoLbs": [0, 6000], "bigAirport": True}],
        "luggagePerPersonKg": [5, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 5000
    },
    {
        "id": "troncal_domestico", "name": "Troncal Doméstico", "description": "Rotas troncais regulares interligando grandes capitais e polos metropolitanos nacionais.",
        "board": "LinhaRegular", "vip": False, "phase": 6, "payFamily": "linha", "xpType": "linha",
        "generator": "standard",
        "baseByTier": {"1": 10, "2": 6, "3": 0, "4": 0},
        "originProfiles": {"linha": 3, "linha:domestico": 2},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"linha": 4}},
        "profiles": [
            {"weight": 45, "rangeNm": [250, 700], "pax": [120, 190], "cargoLbs": [0, 8000], "bigAirport": False},
            {"weight": 40, "rangeNm": [700, 1400], "pax": [140, 210], "cargoLbs": [0, 10000], "bigAirport": False},
            {"weight": 15, "rangeNm": [1400, 2400], "pax": [160, 230], "cargoLbs": [0, 10000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [5, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 5000
    },
    {
        "id": "regional_internacional", "name": "Regional Internacional", "description": "Voo regular internacional entre capitais e grandes centros de países vizinhos.",
        "board": "LinhaRegular", "vip": False, "phase": 6, "payFamily": "linha", "xpType": "linha",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 3, "3": 0, "4": 0},
        "originProfiles": {"linha:internacional": 3},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "other", "runway": "paved", "maxNm": None, "profiles": {"linha": 3}},
        "profiles": [
            {"weight": 40, "rangeNm": [400, 1000], "pax": [120, 190], "cargoLbs": [0, 8000], "bigAirport": False},
            {"weight": 45, "rangeNm": [1000, 1900], "pax": [140, 220], "cargoLbs": [0, 10000], "bigAirport": False},
            {"weight": 15, "rangeNm": [1900, 3000], "pax": [160, 220], "cargoLbs": [0, 8000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [15, 32], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 5000
    },
    {
        "id": "longo_curso", "name": "Longo Curso Internacional", "description": "Travessia intercontinental regular de passageiros e carga em aeronaves widebody.",
        "board": "LinhaRegular", "vip": False, "phase": 7, "payFamily": "linha", "xpType": "linha",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 0, "3": 0, "4": 0},
        "originProfiles": {"linha:mega_hub": 4, "linha:internacional": 3},
        "destination": {"rule": "standard", "tiers": [1], "country": "other", "runway": "paved", "maxNm": None, "profiles": {"linha:mega_hub": 5}},
        "profiles": [
            {"weight": 35, "rangeNm": [3000, 4500], "pax": [220, 320], "cargoLbs": [5000, 30000], "bigAirport": False},
            {"weight": 50, "rangeNm": [4500, 6500], "pax": [250, 380], "cargoLbs": [5000, 35000], "bigAirport": False},
            {"weight": 15, "rangeNm": [6500, 7700], "pax": [250, 400], "cargoLbs": [5000, 25000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [15, 32], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 7000
    },
    {
        "id": "feeder_inbound", "name": "Alimentador de Hub (Inbound)", "description": "Voo regional regular conectando polos do interior diretamente ao hub principal da companhia.",
        "board": "LinhaRegional", "vip": False, "phase": 5, "payFamily": "regional", "xpType": "regional",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 10, "3": 6, "4": 0},
        "originProfiles": {"alimentador": 5},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": 500, "profiles": {"linha": 5}},
        "profiles": [
            {"weight": 40, "rangeNm": [80, 250], "pax": [20, 48], "cargoLbs": [0, 2000], "bigAirport": False},
            {"weight": 45, "rangeNm": [150, 400], "pax": [40, 72], "cargoLbs": [0, 3000], "bigAirport": False},
            {"weight": 15, "rangeNm": [250, 550], "pax": [60, 72], "cargoLbs": [0, 3000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [5, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 3500
    },
    {
        "id": "feeder_outbound", "name": "Distribuidor de Hub (Outbound)", "description": "Distribuição de passageiros e conexões a partir de um grande hub para aeroportos regionais.",
        "board": "LinhaRegional", "vip": False, "phase": 5, "payFamily": "regional", "xpType": "regional",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 3, "3": 0, "4": 0},
        "originProfiles": {"linha": 3},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": 500, "profiles": {"alimentador": 5}},
        "profiles": [
            {"weight": 40, "rangeNm": [80, 250], "pax": [20, 48], "cargoLbs": [0, 2000], "bigAirport": False},
            {"weight": 45, "rangeNm": [150, 400], "pax": [40, 72], "cargoLbs": [0, 3000], "bigAirport": False},
            {"weight": 15, "rangeNm": [250, 550], "pax": [60, 72], "cargoLbs": [0, 3000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [5, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 3500
    },
    {
        "id": "sub_regional", "name": "Sub-Regional Leve", "description": "Transporte aéreo regular entre cidades de menor porte e capitais ou polos regionais.",
        "board": "LinhaRegional", "vip": False, "phase": 3, "payFamily": "regional", "xpType": "regional",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 6, "3": 10, "4": 3},
        "originProfiles": {"alimentador": 2, "interior_distante": 2},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "any", "maxNm": None, "profiles": {"alimentador": 4}},
        "profiles": [
            {"weight": 60, "rangeNm": [50, 200], "pax": [4, 12], "cargoLbs": [0, 800], "bigAirport": False},
            {"weight": 40, "rangeNm": [100, 300], "pax": [10, 19], "cargoLbs": [0, 1000], "bigAirport": False}
        ],
        "luggagePerPersonKg": [5, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "pinga_pinga", "name": "Pinga-Pinga Regional", "description": "Voo regular de múltiplas escalas curtas para atendimento de passageiros e encomendas.",
        "board": "LinhaRegional", "vip": False, "phase": 3, "payFamily": "regional", "xpType": "regional",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 6, "3": 6, "4": 3},
        "originProfiles": {"alimentador": 3, "isolado": 2},
        "destination": {"rule": "standard", "tiers": [2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [100, 400], "pax": [2, 14], "cargoLbs": [100, 1500], "bigAirport": False}],
        "luggagePerPersonKg": [5, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "regional_essencial", "name": "Regional Essencial / Táxi Social", "description": "Linha subsidiada ou comunitária conectando localidades isoladas à rede de serviços essenciais.",
        "board": "LinhaRegional", "vip": False, "phase": 3, "payFamily": "regional", "xpType": "regional",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 6, "4": 6},
        "originProfiles": {"isolado": 8, "alimentador:amazonia": 4, "alimentador:remoto": 4},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "any", "maxNm": None, "profiles": {"linha": 4, "alimentador": 4}},
        "profiles": [
            {"weight": 70, "rangeNm": [50, 300], "pax": [4, 14], "cargoLbs": [200, 1500], "bigAirport": False},
            {"weight": 30, "rangeNm": [100, 350], "pax": [20, 48], "cargoLbs": [500, 3000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [5, 25], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },

    # C. Executiva, charter e turismo
    {
        "id": "executivo_corporativo", "name": "Táxi Aéreo Corporativo", "description": "Fretamento executivo corporativo para executivos e empresários entre polos de negócios.",
        "board": "TaxiAereo", "vip": False, "phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 10, "3": 6, "4": 1},
        "originProfiles": {"executivo": 4, "executivo:corporativo": 2},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"executivo": 3}},
        "profiles": [
            {"weight": 45, "rangeNm": [100, 400], "pax": [1, 4], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 40, "rangeNm": [300, 1200], "pax": [2, 7], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 15, "rangeNm": [1000, 2000], "pax": [4, 10], "cargoLbs": [0, 0], "bigAirport": True}
        ],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 3000
    },
    {
        "id": "executivo_intercontinental", "name": "Executivo Intercontinental", "description": "Transporte VIP de longa distância intercontinental em jatos executivos de grande porte.",
        "board": "TaxiAereo", "vip": True, "phase": 4, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 1, "3": 0, "4": 0},
        "originProfiles": {"executivo:dedicado": 4, "executivo": 2},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "other", "runway": "paved", "maxNm": None, "profiles": {"executivo": 4}},
        "profiles": [{"weight": 100, "rangeNm": [1800, 3400], "pax": [2, 12], "cargoLbs": [0, 0], "bigAirport": True}],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 5000
    },
    {
        "id": "vip_internacional", "name": "VIP Internacional", "description": "Fretamento VIP privado internacional de médio alcance para países vizinhos.",
        "board": "TaxiAereo", "vip": True, "phase": 4, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 3, "3": 0, "4": 0},
        "originProfiles": {"executivo": 3},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "other", "runway": "paved", "maxNm": None, "profiles": {"executivo:lazer": 3}},
        "profiles": [{"weight": 100, "rangeNm": [300, 1200], "pax": [2, 8], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 3500
    },
    {
        "id": "agro_executivo", "name": "Táxi Aéreo do Agronegócio", "description": "Deslocamento de produtores, agrônomos e investidores entre centros urbanos e propriedades rurais.",
        "board": "TaxiAereo", "vip": False, "phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 6, "3": 10, "4": 6},
        "originProfiles": {"agro:executivo_rural": 4, "agro": 3, "fazenda": 3},
        "destination": {"rule": "standard", "tiers": [2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"agro": 4, "fazenda": 4}},
        "profiles": [
            {"weight": 60, "rangeNm": [80, 300], "pax": [1, 4], "cargoLbs": [0, 200], "bigAirport": False},
            {"weight": 40, "rangeNm": [250, 600], "pax": [3, 8], "cargoLbs": [0, 200], "bigAirport": False}
        ],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "familia_alto_padrao", "name": "Família de Alto Padrão", "description": "Fretamento VIP privativo para famílias em viagem de lazer ou deslocamento de residência.",
        "board": "TaxiAereo", "vip": True, "phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 6, "3": 6, "4": 3},
        "originProfiles": {"executivo:lazer": 3},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "any", "maxNm": None, "profiles": {"executivo:lazer": 3, "turistico_curado": 3, "turistico_poi": 3}},
        "profiles": [{"weight": 100, "rangeNm": [100, 800], "pax": [2, 7], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "equipes_urgentes", "name": "Equipes Urgentes", "description": "Transporte ágil de técnicos e especialistas com ferramentas e peças para atendimento urgente.",
        "board": "TaxiAereo", "vip": False, "phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 6, "3": 6, "4": 3},
        "originProfiles": {"mro": 2, "carga:industrial": 2},
        "destination": {"rule": "standard", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [50, 500], "pax": [2, 6], "cargoLbs": [50, 400], "bigAirport": False}],
        "luggagePerPersonKg": [15, 30], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "charter_lazer", "name": "Fretamento Turístico / Lazer", "description": "Charter turístico para grupos e turistas com destino a resorts, praias e lodges de pesca.",
        "board": "TaxiAereo", "vip": False, "phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 6, "3": 3, "4": 1},
        "originProfiles": {"linha:turistico": 3, "executivo:lazer": 3},
        "destination": {"rule": "standard", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"turistico_curado": 4, "turistico_poi": 2, "executivo:lazer": 3}},
        "profiles": [
            {"weight": 60, "rangeNm": [100, 800], "pax": [4, 9], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 40, "rangeNm": [40, 300], "pax": [2, 8], "cargoLbs": [0, 0], "bigAirport": False}
        ],
        "luggagePerPersonKg": [15, 30], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "charter_esportivo", "name": "Charter Esportivo / Turnês", "description": "Fretamento dedicado para delegações esportivas, comissões técnicas e bandas em turnê.",
        "board": "LinhaRegional", "vip": False, "phase": 5, "payFamily": "regional", "xpType": "regional",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 6, "3": 0, "4": 0},
        "originProfiles": {"linha": 2},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": None, "profiles": {}},
        "profiles": [
            {"weight": 60, "rangeNm": [200, 1200], "pax": [30, 70], "cargoLbs": [0, 4000], "bigAirport": False},
            {"weight": 40, "rangeNm": [300, 1500], "pax": [60, 150], "cargoLbs": [0, 6000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 4500
    },
    {
        "id": "transfer_metropolitano", "name": "Transfer Metropolitano", "description": "Ligação executiva rápida de curto alcance entre aeroportos executivos e helipontos da mesma metrópole.",
        "board": "TaxiAereo", "vip": True, "phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 6, "3": 3, "4": 0},
        "originProfiles": {"executivo:urbano": 4, "executivo:dedicado": 2},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": 150, "profiles": {"executivo": 4}},
        "profiles": [{"weight": 100, "rangeNm": [50, 250], "pax": [1, 6], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 2500
    },
    {
        "id": "panoramico", "name": "Voo Panorâmico", "description": "Passeio panorâmico local ou safári aéreo sobrevoando atrativos naturais com retorno à origem.",
        "board": "TaxiAereo", "vip": False, "phase": 1, "payFamily": "panoramico", "xpType": "panoramico",
        "generator": "scenic_tour",
        "baseByTier": {"1": 1, "2": 3, "3": 6, "4": 3},
        "originProfiles": {"turistico_curado:base": 8, "turistico_poi": 3},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [
            {"weight": 70, "rangeNm": [15, 60], "pax": [1, 3], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 30, "rangeNm": [30, 80], "pax": [4, 12], "cargoLbs": [0, 0], "bigAirport": False}
        ],
        "luggagePerPersonKg": [0, 5], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "demo_flight", "name": "Voo de Demonstração Comercial", "description": "Demonstração de performance de aeronave para potenciais compradores com retorno à origem.",
        "board": "Geral", "vip": False, "phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 3, "3": 3, "4": 0},
        "originProfiles": {"mro:fabrica": 8, "mro:executivo": 3, "executivo:dedicado": 2},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [20, 60], "pax": [1, 3], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [0, 5], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 3000
    },

    # D. Carga
    {
        "id": "carga_paletizada", "name": "Carga Geral Paletizada", "description": "Transporte de cargas industriais pesadas e contêineres paletizados em aeronaves cargueiras.",
        "board": "Carga", "vip": False, "phase": 6, "payFamily": "linha", "xpType": "linha",
        "generator": "standard",
        "baseByTier": {"1": 10, "2": 6, "3": 0, "4": 0},
        "originProfiles": {"carga": 3, "carga:cargueiros": 3},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "any", "runway": "paved", "maxNm": None, "profiles": {"carga": 4}},
        "profiles": [
            {"weight": 40, "rangeNm": [300, 1600], "pax": [0, 0], "cargoLbs": [20000, 45000], "bigAirport": False},
            {"weight": 40, "rangeNm": [600, 2600], "pax": [0, 0], "cargoLbs": [45000, 100000], "bigAirport": False},
            {"weight": 20, "rangeNm": [3500, 7200], "pax": [0, 0], "cargoLbs": [100000, 148000], "bigAirport": True}
        ],
        "luggagePerPersonKg": None, "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 6000
    },
    {
        "id": "carga_overnight", "name": "Carga Expressa Overnight", "description": "Malha noturna de encomendas prioritárias e encomendas urgentes entre hubs e alimentadores.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 6, "3": 3, "4": 0},
        "originProfiles": {"carga:integrador": 5, "carga:noturno": 4, "carga:expressa": 3, "carga:postal": 3},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"carga": 3, "alimentador": 3}},
        "profiles": [
            {"weight": 45, "rangeNm": [100, 400], "pax": [0, 0], "cargoLbs": [1000, 3300], "bigAirport": False},
            {"weight": 35, "rangeNm": [150, 500], "pax": [0, 0], "cargoLbs": [6000, 16000], "bigAirport": False},
            {"weight": 20, "rangeNm": [300, 1500], "pax": [0, 0], "cargoLbs": [20000, 45000], "bigAirport": True}
        ],
        "luggagePerPersonKg": None, "crew": {"count": 0, "luggageKg": [0, 0]}, "urgency": 1.2,
        "requiresHardSurface": True, "minRunwayDest": 3500
    },
    {
        "id": "malotes", "name": "Malotes Bancários e Documentos", "description": "Transporte urgente de malotes bancários, compensação de cheques e documentação institucional.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 6, "3": 6, "4": 3},
        "originProfiles": {"carga:postal": 2, "interior_distante": 2},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [50, 350], "pax": [0, 1], "cargoLbs": [50, 600], "bigAirport": False}],
        "luggagePerPersonKg": [5, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "carga_valores", "name": "Transporte de Valores", "description": "Transporte seguro de numerário, barras de ouro e pedras preciosas com escolta armada a bordo.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "aog", "xpType": "aog",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 6, "3": 3, "4": 0},
        "originProfiles": {"carga:alto_valor": 4},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"carga": 3, "linha": 3}},
        "profiles": [{"weight": 100, "rangeNm": [100, 600], "pax": [1, 2], "cargoLbs": [200, 2000], "bigAirport": False}],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 3500
    },
    {
        "id": "ecommerce", "name": "E-Commerce e Carga Fracionada", "description": "Distribuição expressa de encomendas e compras online conectando polos industriais ao consumidor.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 6, "3": 3, "4": 0},
        "originProfiles": {"carga:ecommerce": 5, "carga:integrador": 3},
        "destination": {"rule": "standard", "tiers": [2, 3], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"alimentador": 3}},
        "profiles": [
            {"weight": 50, "rangeNm": [40, 300], "pax": [0, 0], "cargoLbs": [500, 3300], "bigAirport": False},
            {"weight": 35, "rangeNm": [100, 500], "pax": [0, 0], "cargoLbs": [4000, 15000], "bigAirport": False},
            {"weight": 15, "rangeNm": [300, 1200], "pax": [0, 0], "cargoLbs": [20000, 45000], "bigAirport": True}
        ],
        "luggagePerPersonKg": None, "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 3000
    },
    {
        "id": "carga_jit", "name": "Carga Industrial Just-In-Time / AOG", "description": "Transporte de emergência de componentes eletrônicos, maquinário industrial parado ou peças agrícolas.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "aog", "xpType": "aog",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 6, "3": 6, "4": 3},
        "originProfiles": {"carga:industrial": 3, "carga:automotivo": 3, "carga:eletronicos": 3, "agro": 2},
        "destination": {"rule": "standard", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"carga:industrial": 3, "agro": 5, "fazenda": 5}},
        "profiles": [
            {"weight": 70, "rangeNm": [50, 800], "pax": [0, 2], "cargoLbs": [50, 1500], "bigAirport": False},
            {"weight": 30, "rangeNm": [20, 150], "pax": [0, 1], "cargoLbs": [50, 500], "bigAirport": False}
        ],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]}, "urgency": 1.2,
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "cadeia_fria", "name": "Cadeia Fria e Perecíveis", "description": "Transporte refrigerado com controle térmico rigoroso de pescados, frutas finas e vacinas.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 6, "3": 3, "4": 0},
        "originProfiles": {"carga:pereciveis": 6, "agro:pereciveis": 5, "agro:pescado": 4},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "any", "runway": "paved", "maxNm": None, "profiles": {"carga": 4}},
        "profiles": [
            {"weight": 40, "rangeNm": [80, 400], "pax": [0, 0], "cargoLbs": [800, 3300], "bigAirport": False},
            {"weight": 40, "rangeNm": [150, 600], "pax": [0, 0], "cargoLbs": [6000, 16000], "bigAirport": False},
            {"weight": 20, "rangeNm": [400, 1500], "pax": [0, 0], "cargoLbs": [20000, 45000], "bigAirport": True}
        ],
        "luggagePerPersonKg": None, "crew": {"count": 0, "luggageKg": [0, 0]}, "urgency": 1.2,
        "requiresHardSurface": True, "minRunwayDest": 3500
    },
    {
        "id": "carga_dgr", "name": "Carga Perigosa / Farmacêutica", "description": "Materiais controlados, farmacêuticos hospitalares ou explosivos de mineração exigindo parâmetros suaves.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "carga_fragil", "xpType": "carga_fragil",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 6, "3": 3, "4": 0},
        "originProfiles": {"carga:farma": 5, "carga:mineracao": 3, "saude": 2},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"carga:farma": 3, "saude": 3, "carga:mineracao": 4}},
        "profiles": [
            {"weight": 70, "rangeNm": [100, 800], "pax": [0, 0], "cargoLbs": [100, 2000], "bigAirport": False},
            {"weight": 30, "rangeNm": [50, 300], "pax": [0, 0], "cargoLbs": [500, 3000], "bigAirport": False}
        ],
        "luggagePerPersonKg": None, "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 3000
    },
    {
        "id": "carga_sensivel", "name": "Carga Sensível e Viva", "description": "Insumos biológicos, botijões de genética animal com veterinário de campo ou fauna silvestre.",
        "board": "Carga", "vip": False, "phase": 2, "payFamily": "carga_fragil", "xpType": "carga_fragil",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 6, "3": 6, "4": 6},
        "originProfiles": {"agro:genetica": 5, "agro:sementes": 4, "agro": 2},
        "destination": {"rule": "standard", "tiers": [2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"fazenda": 4, "agro": 4}},
        "profiles": [{"weight": 100, "rangeNm": [50, 400], "pax": [0, 1], "cargoLbs": [50, 500], "bigAirport": False}],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "carga_combi", "name": "Carga Combi (Passageiros + Suprimentos)", "description": "Configuração mista de cabine transportando passageiros e volumes de carga para regiões isoladas.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "agro_bush", "xpType": "agro_bush",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 6, "4": 6},
        "originProfiles": {"isolado": 6, "alimentador:amazonia": 4},
        "destination": {"rule": "standard", "tiers": [3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"isolado": 5}},
        "profiles": [
            {"weight": 70, "rangeNm": [50, 400], "pax": [2, 9], "cargoLbs": [500, 2000], "bigAirport": False},
            {"weight": 30, "rangeNm": [100, 400], "pax": [15, 30], "cargoLbs": [3000, 6000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [5, 25], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "insumos_agro", "name": "Insumos e Peças Agrícolas", "description": "Abastecimento logístico com peças, defensivos e ferramentas para polos de produção agrícola.",
        "board": "Carga", "vip": False, "phase": 2, "payFamily": "agro_municipal", "xpType": "agro_municipal",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 6, "3": 10, "4": 10},
        "originProfiles": {"agro": 4, "fazenda": 4},
        "destination": {"rule": "standard", "tiers": [3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"fazenda": 5, "agro": 5}},
        "profiles": [{"weight": 100, "rangeNm": [30, 400], "pax": [0, 1], "cargoLbs": [200, 3000], "bigAirport": False}],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "suprimentos_remotos", "name": "Suprimentos para Comunidades Remotas", "description": "Mantimentos, combustíveis e itens essenciais para ranchos, reservas indígenas e vilarejos.",
        "board": "Carga", "vip": False, "phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 6, "4": 10},
        "originProfiles": {"isolado": 4, "fazenda": 3, "interior_distante": 2},
        "destination": {"rule": "standard", "tiers": [3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"fazenda": 5, "isolado": 5}},
        "profiles": [
            {"weight": 60, "rangeNm": [15, 150], "pax": [0, 1], "cargoLbs": [300, 1500], "bigAirport": False},
            {"weight": 40, "rangeNm": [30, 250], "pax": [0, 1], "cargoLbs": [1500, 3300], "bigAirport": False}
        ],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "conexao_pista_curta", "name": "Conexão de Pista Curta", "description": "Salto curto com carga e suprimentos de polo municipal para pista rústica de fazenda.",
        "board": "Carga", "vip": False, "phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 0, "3": 6, "4": 6},
        "originProfiles": {"fazenda": 2, "interior_distante": 2},
        "destination": {"rule": "standard", "tiers": [4], "country": "same", "runway": "short_or_unpaved", "maxNm": 50, "profiles": {"fazenda": 3, "pista_curta": 3}},
        "profiles": [{"weight": 100, "rangeNm": [15, 80], "pax": [0, 5], "cargoLbs": [0, 1200], "bigAirport": False}],
        "luggagePerPersonKg": [5, 25], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },

    # E. Saúde
    {
        "id": "orgaos", "name": "Transporte de Órgãos para Transplante", "description": "Missão crítica com contagem regressiva para entrega rápida de órgãos vitais em centro cirúrgico.",
        "board": "Medica", "vip": False, "phase": 4, "payFamily": "medevac", "xpType": "medevac",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 6, "3": 3, "4": 0},
        "originProfiles": {"saude:orgaos": 6, "saude:hospital_referencia": 3},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"saude:orgaos": 6, "saude:hospital_referencia": 6}},
        "profiles": [{"weight": 100, "rangeNm": [50, 700], "pax": [2, 3], "cargoLbs": [20, 60], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 2, "luggageKg": [3, 8]}, "urgency": 1.5,
        "requiresHardSurface": True, "minRunwayDest": 3500
    },
    {
        "id": "uti_inter_hospitalar", "name": "Remoção Aeromédica UTI Inter-Hospitalar", "description": "Transferência médica especializada de paciente em estado grave entre centros hospitalares.",
        "board": "Medica", "vip": False, "phase": 4, "payFamily": "medevac", "xpType": "medevac",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 6, "3": 6, "4": 3},
        "originProfiles": {"saude": 3},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"saude:hospital_referencia": 5}},
        "profiles": [{"weight": 100, "rangeNm": [80, 500], "pax": [1, 2], "cargoLbs": [150, 400], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 2, "luggageKg": [3, 8]},
        "requiresHardSurface": True, "minRunwayDest": 3000
    },
    {
        "id": "medevac_remoto", "name": "Evacuação Aeromédica Remota (Bush Medevac)", "description": "Resgate de emergência em pistas curtas de terra/grama para transporte de paciente ferido.",
        "board": "Medica", "vip": False, "phase": 4, "payFamily": "medevac", "xpType": "medevac",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 6, "4": 10},
        "originProfiles": {"saude:medevac_remoto": 4, "isolado": 4, "fazenda": 2},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "short_or_unpaved", "maxNm": None, "profiles": {"saude": 3}},
        "profiles": [{"weight": 100, "rangeNm": [30, 300], "pax": [1, 2], "cargoLbs": [100, 300], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 2, "luggageKg": [3, 8]}, "urgency": 1.5,
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "uti_longa_distancia", "name": "UTI Aérea de Longa Distância / Repatriação", "description": "Repatriação médica internacional com acompanhamento de equipe e suporte avançado de vida.",
        "board": "Medica", "vip": False, "phase": 4, "payFamily": "medevac", "xpType": "medevac",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 3, "3": 0, "4": 0},
        "originProfiles": {"saude:repatriacao": 6, "saude:uti_aerea": 3},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "other", "runway": "paved", "maxNm": None, "profiles": {"saude:hospital_referencia": 5}},
        "profiles": [{"weight": 100, "rangeNm": [800, 3000], "pax": [1, 2], "cargoLbs": [200, 400], "bigAirport": True}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 2, "luggageKg": [3, 8]},
        "requiresHardSurface": True, "minRunwayDest": 4500
    },
    {
        "id": "saude_familia", "name": "Ronda Médica e Vacinação", "description": "Deslocamento de profissionais de saúde e insumos para atendimento preventivo em aldeias e vilas.",
        "board": "Medica", "vip": False, "phase": 3, "payFamily": "medevac", "xpType": "medevac",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 6, "3": 6, "4": 3},
        "originProfiles": {"saude": 3, "alimentador": 2},
        "destination": {"rule": "standard", "tiers": [3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"isolado": 4}},
        "profiles": [{"weight": 100, "rangeNm": [50, 250], "pax": [3, 8], "cargoLbs": [100, 600], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },

    # F. Trabalho aéreo (SAE), agro e vigilância
    {
        "id": "aerolevantamento", "name": "Aerolevantamento e Fotogrametria", "description": "Mapeamento aéreo por linhas paralelas com controle rigoroso de altitude, proa e velocidade.",
        "board": "Sae", "vip": False, "phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto",
        "generator": "survey",
        "baseByTier": {"1": 1, "2": 3, "3": 6, "4": 3},
        "originProfiles": {},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [15, 80], "pax": [0, 1], "cargoLbs": [50, 200], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "paraquedismo", "name": "Lançamento de Paraquedistas", "description": "Subida rápida para lançamento de paraquedistas em dropzone e descida controlada para pouso na origem.",
        "board": "Sae", "vip": False, "phase": 2, "payFamily": "paraquedismo", "xpType": "paraquedismo",
        "generator": "skydive",
        "baseByTier": {"1": 1, "2": 1, "3": 3, "4": 1},
        "originProfiles": {"zona_salto": 10},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [
            {"weight": 35, "rangeNm": [5, 15], "pax": [3, 6], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 50, "rangeNm": [5, 15], "pax": [8, 14], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 15, "rangeNm": [5, 15], "pax": [15, 20], "cargoLbs": [0, 0], "bigAirport": True}
        ],
        "luggagePerPersonKg": [12, 16], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "pulverizacao", "name": "Pulverização Agrícola", "description": "Aplicação aérea de defensivos e fertilizantes em baixa altura sobre talhões agrícolas.",
        "board": "Sae", "vip": False, "phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 0, "3": 6, "4": 10},
        "originProfiles": {"agro:pulverizacao": 6, "fazenda": 4, "agro": 3},
        "destination": {"rule": "return_to_origin", "tiers": [3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [5, 15], "pax": [0, 0], "cargoLbs": [600, 1100], "bigAirport": False}],
        "luggagePerPersonKg": None, "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "reboque_planador", "name": "Reboque de Planadores", "description": "Reboque de planador até altitude estipulada para desengate esportivo.",
        "board": "Sae", "vip": False, "phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 1, "3": 3, "4": 3},
        "originProfiles": {"instrucao:planador": 10, "aeroclube": 3},
        "destination": {"rule": "return_to_origin", "tiers": [2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [5, 15], "pax": [0, 0], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": None, "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "publicidade_aerea", "name": "Publicidade Aérea", "description": "Reboque de faixa publicitária ao longo de praias e eventos de grande aglomeração.",
        "board": "Sae", "vip": False, "phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 3, "4": 0},
        "originProfiles": {"turistico_poi": 4, "turistico_curado": 3},
        "destination": {"rule": "return_to_origin", "tiers": [2, 3], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [20, 80], "pax": [0, 0], "cargoLbs": [30, 60], "bigAirport": False}],
        "luggagePerPersonKg": None, "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "patrulha_vigilancia", "name": "Patrulha e Vigilância de Área", "description": "Vigilância ambiental, monitoramento de queimadas e contagem de rebanho em baixa altura.",
        "board": "Sae", "vip": False, "phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 6, "4": 6},
        "originProfiles": {"isolado": 3, "fazenda": 3, "agro": 2, "militar:fronteira": 2},
        "destination": {"rule": "return_to_origin", "tiers": [2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [30, 200], "pax": [1, 2], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "inspecao_linhas", "name": "Inspeção de Linhas e Dutos", "description": "Rastreio e monitoramento aéreo de linhas de transmissão, dutos de combustível ou ferrovias.",
        "board": "Sae", "vip": False, "phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 6, "4": 3},
        "originProfiles": {"interior_distante": 2},
        "destination": {"rule": "return_to_origin", "tiers": [2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [40, 200], "pax": [1, 1], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "monitoramento_trafego", "name": "Monitoramento de Tráfego e Eventos", "description": "Voo de observação e suporte jornalístico/segurança sobre malhas viárias e eventos.",
        "board": "Sae", "vip": False, "phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 6, "3": 3, "4": 0},
        "originProfiles": {"executivo:urbano": 2},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [10, 40], "pax": [1, 2], "cargoLbs": [0, 50], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 2500
    },
    {
        "id": "sar", "name": "Busca e Salvamento (SAR)", "description": "Operação de varredura visual e busca de emergência em padrões de creeping line ou expanding square.",
        "board": "Sae", "vip": False, "phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 3, "3": 3, "4": 3},
        "originProfiles": {"militar:sar": 5, "saude:resgate": 3},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [40, 250], "pax": [1, 3], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "calibracao_auxilios", "name": "Calibração de Auxílios à Navegação", "description": "Aproximações e voos técnicos de aferição de auxílios-rádio, ILS e balizamentos de pista.",
        "board": "Sae", "vip": False, "phase": 4, "payFamily": "aerofoto", "xpType": "aerofoto",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 6, "3": 3, "4": 0},
        "originProfiles": {},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [20, 200], "pax": [1, 2], "cargoLbs": [300, 800], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 4000
    },

    # G. Operações técnicas e translados
    {
        "id": "ferry_posicionamento", "name": "Translado e Posicionamento (Ferry)", "description": "Voo de translado de aeronave da frota para cobrir malha ou atender demanda em outra base.",
        "board": "Geral", "vip": False, "phase": 1, "payFamily": "translado", "xpType": "translado",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 3, "3": 3, "4": 1},
        "originProfiles": {"mro": 2},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "any", "runway": "any", "maxNm": None, "profiles": {"linha": 2, "executivo": 2}},
        "profiles": [
            {"weight": 55, "rangeNm": [20, 150], "pax": [0, 0], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 35, "rangeNm": [150, 600], "pax": [0, 0], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 10, "rangeNm": [600, 1500], "pax": [0, 0], "cargoLbs": [0, 0], "bigAirport": True}
        ],
        "luggagePerPersonKg": None, "crew": {"count": 1, "luggageKg": [3, 10]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "translado_manutencao", "name": "Translado para Manutenção / Revisão", "description": "Voo técnico para oficina homologada especializada para cumprimento de inspeções periódicas ou reparos.",
        "board": "Geral", "vip": False, "phase": 1, "payFamily": "translado", "xpType": "translado",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 6, "3": 3, "4": 1},
        "originProfiles": {},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"mro": 6}},
        "profiles": [{"weight": 100, "rangeNm": [20, 400], "pax": [0, 0], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": None, "crew": {"count": 1, "luggageKg": [3, 10]},
        "requiresHardSurface": True, "minRunwayDest": 3000
    },
    {
        "id": "ferry_permit", "name": "Translado com Restrição Técnica (Special Permit)", "description": "Voo com autorização especial de translado cumprindo limitações operacionais estritas.",
        "board": "Geral", "vip": False, "phase": 2, "payFamily": "translado", "xpType": "translado",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 3, "3": 3, "4": 0},
        "originProfiles": {},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": None, "profiles": {"mro": 6}},
        "profiles": [{"weight": 100, "rangeNm": [20, 300], "pax": [0, 0], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": None, "crew": {"count": 1, "luggageKg": [3, 10]},
        "requiresHardSurface": True, "minRunwayDest": 3000
    },
    {
        "id": "check_flight", "name": "Voo de Teste Pós-Manutenção", "description": "Verificação funcional em voo de sistemas, bússola e aviônicos após intervenção de manutenção.",
        "board": "Geral", "vip": False, "phase": 2, "payFamily": "translado", "xpType": "translado",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 3, "3": 3, "4": 0},
        "originProfiles": {"mro": 6},
        "destination": {"rule": "return_to_origin", "tiers": [1, 2, 3], "country": "same", "runway": "paved", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [20, 60], "pax": [0, 1], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [3, 10], "crew": {"count": 1, "luggageKg": [3, 10]},
        "requiresHardSurface": True, "minRunwayDest": 3000
    },
    {
        "id": "crew_parts", "name": "Logística de Apoio a Aeronaves (Crew & Parts)", "description": "Transporte de mecânicos, ferramentas ou tripulantes substitutos para aeronave em pane (AOG).",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 3, "3": 3, "4": 0},
        "originProfiles": {"mro": 3, "linha": 2},
        "destination": {"rule": "standard", "tiers": [1, 2, 3], "country": "same", "runway": "any", "maxNm": None, "profiles": {"linha": 3, "mro": 3}},
        "profiles": [
            {"weight": 60, "rangeNm": [50, 800], "pax": [1, 6], "cargoLbs": [50, 600], "bigAirport": False},
            {"weight": 40, "rangeNm": [30, 200], "pax": [1, 2], "cargoLbs": [100, 1000], "bigAirport": False}
        ],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "translado_piloto_agricola", "name": "Transporte de Piloto Agrícola", "description": "Translado ágil de piloto agrícola para assunção de aeronave de pulverização na base de safra.",
        "board": "Geral", "vip": False, "phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 6, "4": 6},
        "originProfiles": {"agro:pulverizacao": 4, "fazenda": 3},
        "destination": {"rule": "standard", "tiers": [2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"agro": 4, "fazenda": 4}},
        "profiles": [{"weight": 100, "rangeNm": [30, 200], "pax": [1, 2], "cargoLbs": [0, 0], "bigAirport": False}],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "pessoas_remotas", "name": "Transporte de Pessoal para Locais Remotos", "description": "Rodízio de turnos de trabalho, translado de brigadistas ou gerentes entre sedes e postos remotos.",
        "board": "TaxiAereo", "vip": False, "phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 6, "3": 6, "4": 10},
        "originProfiles": {"fazenda": 4, "isolado": 3, "agro": 2},
        "destination": {"rule": "standard", "tiers": [3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"fazenda": 4, "isolado": 4}},
        "profiles": [
            {"weight": 60, "rangeNm": [15, 150], "pax": [1, 6], "cargoLbs": [0, 300], "bigAirport": False},
            {"weight": 40, "rangeNm": [30, 300], "pax": [6, 19], "cargoLbs": [0, 1000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [15, 30], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },

    # H. Governo, defesa e humanitário
    {
        "id": "autoridades", "name": "Autoridades e Comitivas Oficiais", "description": "Transporte oficial de autoridades governamentais, urnas eleitorais ou documentos sigilosos de estado.",
        "board": "TaxiAereo", "vip": True, "phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 3, "2": 6, "3": 3, "4": 1},
        "originProfiles": {"militar:vip": 4, "executivo:governo": 3},
        "destination": {"rule": "standard", "tiers": [1, 2], "country": "same", "runway": "paved", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [80, 600], "pax": [2, 8], "cargoLbs": [0, 600], "bigAirport": False}],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 3500
    },
    {
        "id": "diplomatico", "name": "Missão Diplomática e Comitivas de Estado", "description": "Voo internacional de comitiva diplomática e representantes governamentais em protocolo oficial.",
        "board": "TaxiAereo", "vip": True, "phase": 4, "payFamily": "taxi_aereo", "xpType": "taxi_aereo",
        "generator": "standard",
        "baseByTier": {"1": 6, "2": 3, "3": 0, "4": 0},
        "originProfiles": {"militar:vip": 4, "executivo:governo": 3},
        "destination": {"rule": "standard", "tiers": [1], "country": "other", "runway": "paved", "maxNm": None, "profiles": {}},
        "profiles": [
            {"weight": 60, "rangeNm": [300, 3000], "pax": [4, 12], "cargoLbs": [0, 0], "bigAirport": False},
            {"weight": 40, "rangeNm": [1000, 6000], "pax": [20, 60], "cargoLbs": [0, 3000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": True, "minRunwayDest": 5000
    },
    {
        "id": "militar_can", "name": "Logística Militar e Fronteiras (CAN)", "description": "Apoio aerologístico a pelotões de fronteira, bases militares e correio militar avançado.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 6, "3": 6, "4": 3},
        "originProfiles": {"militar": 6, "militar_misto": 3, "militar:fronteira": 4},
        "destination": {"rule": "standard", "tiers": [2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"militar:fronteira": 5, "isolado": 3}},
        "profiles": [
            {"weight": 40, "rangeNm": [100, 600], "pax": [0, 9], "cargoLbs": [500, 3000], "bigAirport": False},
            {"weight": 40, "rangeNm": [200, 1200], "pax": [5, 30], "cargoLbs": [3000, 15000], "bigAirport": False},
            {"weight": 20, "rangeNm": [300, 2000], "pax": [10, 60], "cargoLbs": [15000, 60000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "humanitario", "name": "Apoio Humanitário e Defesa Civil", "description": "Distribuição de água potável, lonas, remédios e socorro a populações atingidas por catástrofes.",
        "board": "Carga", "vip": False, "phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa",
        "generator": "standard",
        "baseByTier": {"1": 1, "2": 3, "3": 6, "4": 6},
        "originProfiles": {"saude:humanitario": 4, "militar:transporte": 2},
        "destination": {"rule": "standard", "tiers": [2, 3, 4], "country": "same", "runway": "any", "maxNm": None, "profiles": {"isolado": 4}},
        "profiles": [
            {"weight": 70, "rangeNm": [50, 400], "pax": [0, 4], "cargoLbs": [1000, 4500], "bigAirport": False},
            {"weight": 30, "rangeNm": [200, 1500], "pax": [0, 20], "cargoLbs": [10000, 60000], "bigAirport": True}
        ],
        "luggagePerPersonKg": [8, 23], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },

    # I. Remotas, bush e campo
    {
        "id": "combustivel_tambor", "name": "Transporte de Combustível em Tambores", "description": "Transporte de combustíveis em tambores de 200 L para abastecimento de geradores e postos remotos.",
        "board": "Carga", "vip": False, "phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 6, "4": 10},
        "originProfiles": {"isolado": 3, "alimentador:remoto": 3},
        "destination": {"rule": "standard", "tiers": [4], "country": "same", "runway": "short_or_unpaved", "maxNm": None, "profiles": {"isolado": 4, "fazenda": 4}},
        "profiles": [{"weight": 100, "rangeNm": [20, 200], "pax": [0, 0], "cargoLbs": [720, 3600], "bigAirport": False}],
        "luggagePerPersonKg": None, "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "apoio_acampamento", "name": "Apoio a Acampamentos e Expedições", "description": "Suprimento de acampamentos de mineração, equipes científicas ou pesquisadores em campo.",
        "board": "Carga", "vip": False, "phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 3, "3": 6, "4": 10},
        "originProfiles": {"carga:mineracao": 3, "isolado": 4},
        "destination": {"rule": "standard", "tiers": [4], "country": "same", "runway": "short_or_unpaved", "maxNm": None, "profiles": {"isolado": 5}},
        "profiles": [{"weight": 100, "rangeNm": [30, 300], "pax": [0, 4], "cargoLbs": [200, 2000], "bigAirport": False}],
        "luggagePerPersonKg": [15, 30], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    },
    {
        "id": "evacuacao_por_do_sol", "name": "Evacuação Antes do Pôr do Sol", "description": "Decolagem rápida de pista sem balizamento antes do pôr do sol rumo a aeródromo com balizamento noturno.",
        "board": "TaxiAereo", "vip": False, "phase": 3, "payFamily": "agro_bush", "xpType": "agro_bush",
        "generator": "standard",
        "baseByTier": {"1": 0, "2": 0, "3": 3, "4": 6},
        "originProfiles": {"isolado": 3},
        "destination": {"rule": "standard", "tiers": [2, 3], "country": "same", "runway": "lighted", "maxNm": None, "profiles": {}},
        "profiles": [{"weight": 100, "rangeNm": [30, 250], "pax": [1, 6], "cargoLbs": [0, 300], "bigAirport": False}],
        "luggagePerPersonKg": [5, 25], "crew": {"count": 0, "luggageKg": [0, 0]},
        "requiresHardSurface": False, "minRunwayDest": 0
    }
]

def build_catalog():
    missions = []
    for item in RAW_MISSIONS_DATA:
        m_id = item["id"]
        enabled = m_id not in DISABLED_IDS
        civil_at_mil = m_id not in MILITARY_BASE_EXCEPTIONS
        legacy_ids = LEGACY_MAP.get(m_id, [])

        # Default properties
        m = {
            "id": m_id,
            "name": item["name"],
            "description": item["description"],
            "enabled": enabled,
            "board": item["board"],
            "vip": item.get("vip", False),
            "phase": item["phase"],
            "payFamily": item["payFamily"],
            "xpType": item["xpType"],
            "generator": item.get("generator", "standard"),
            "baseByTier": item["baseByTier"],
            "originProfiles": item.get("originProfiles", {}),
            "civilAtMilitaryBase": civil_at_mil,
            "destination": item["destination"],
            "profiles": item["profiles"],
            "luggagePerPersonKg": item.get("luggagePerPersonKg"),
            "crew": item.get("crew", {"count": 0, "luggageKg": [0, 0]}),
            "fixedTexts": item.get("fixedTexts", None),
            "minExpHours": item.get("minExpHours", 4),
            "maxExpHours": item.get("maxExpHours", 24),
            "urgency": item.get("urgency", 1.0),
            "requiresHardSurface": item.get("requiresHardSurface", False),
            "minRunwayDest": item.get("minRunwayDest", 0),
            "legacyIds": legacy_ids
        }
        missions.append(m)

    assert len(missions) == 69, f"Esperado 69 missões, mas obteve {len(missions)}"
    disabled_count = sum(1 for m in missions if not m["enabled"])
    assert disabled_count == 17, f"Esperado 17 desabilitadas, mas obteve {disabled_count}"

    catalog_data = {
        "version": 2,
        "missions": missions
    }
    return catalog_data

def update_texts():
    with open(TEXTS_PATH, "r", encoding="utf-8") as f:
        old_texts = json.load(f)

    new_texts = {}

    # 1. Para cada novo tipo, se veio de legados, unir todas as variantes dos legados
    for m in RAW_MISSIONS_DATA:
        m_id = m["id"]
        legacy_ids = LEGACY_MAP.get(m_id, [])
        variants = []
        for leg_id in legacy_ids:
            if leg_id in old_texts:
                variants.extend(old_texts[leg_id])

        if variants:
            new_texts[m_id] = variants
        else:
            # Tipo novo sem variante antiga: criar 1 variante simples com os marcadores
            desc_text = f"{m['name']}: voo de {{origin}} para {{dest}}"
            if m.get("profiles") and m["profiles"][0]["pax"][1] > 0:
                desc_text += " com {pax} passageiros"
            if m.get("profiles") and m["profiles"][0]["cargoLbs"][1] > 0:
                desc_text += " transportando {cargo_lbs} lbs de carga"
            desc_text += "."

            new_texts[m_id] = [
                {
                    "title": f"{m['name']} ({{dest}})",
                    "description": desc_text
                }
            ]

    # Manter chave especial de voo panorâmico turístico e alias de voo_panoramico
    if "voo_panoramico-turistico" in old_texts:
        new_texts["panoramico-turistico"] = old_texts["voo_panoramico-turistico"]
        new_texts["voo_panoramico-turistico"] = old_texts["voo_panoramico-turistico"]

    if "panoramico" in new_texts:
        new_texts["voo_panoramico"] = new_texts["panoramico"]

    return new_texts

def main():
    cat = build_catalog()
    with open(CATALOG_PATH, "w", encoding="utf-8") as f:
        json.dump(cat, f, indent=2, ensure_ascii=False)
    print(f"Catálogo salvo em {CATALOG_PATH} com {len(cat['missions'])} tipos.")

    texts = update_texts()
    with open(TEXTS_PATH, "w", encoding="utf-8") as f:
        json.dump(texts, f, indent=2, ensure_ascii=False)
    print(f"Textos salvos em {TEXTS_PATH} com {len(texts)} chaves.")

if __name__ == "__main__":
    main()
