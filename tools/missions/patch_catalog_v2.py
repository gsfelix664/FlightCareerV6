import json
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
CATALOG_PATH = os.path.join(REPO_ROOT, "src", "FlightCareer.Infrastructure", "Data", "mission_catalog.json")
TEXTS_PATH = os.path.join(REPO_ROOT, "src", "FlightCareer.Infrastructure", "Data", "mission_texts.json")

# Tabela "Atributos" da especificação E2A:
# fase · payFamily · xpType · board · vip · urgency (quando especificado)
ATTRIBUTES = {
    # A
    "instrucao_local": {"phase": 1, "payFamily": "instrucao_basica", "xpType": "instrucao_basica", "board": "Instrucao", "vip": False, "minExpHours": 1, "maxExpHours": 3, "urgency": 1.0},
    "circuito_tgl": {"phase": 1, "payFamily": "instrucao_basica", "xpType": "instrucao_basica", "board": "Instrucao", "vip": False, "minExpHours": 1, "maxExpHours": 3, "urgency": 1.0},
    "navegacao_vfr": {"phase": 1, "payFamily": "instrucao", "xpType": "instrucao", "board": "Instrucao", "vip": False, "minExpHours": 2, "maxExpHours": 6, "urgency": 1.0},
    "instrucao_ifr": {"phase": 1, "payFamily": "instrucao", "xpType": "instrucao", "board": "Instrucao", "vip": False},
    "adaptacao_noturna": {"phase": 1, "payFamily": "instrucao", "xpType": "instrucao", "board": "Instrucao", "vip": False},
    "cheque": {"phase": 1, "payFamily": "instrucao", "xpType": "instrucao", "board": "Instrucao", "vip": False, "minExpHours": 2, "maxExpHours": 4, "urgency": 1.2},
    # B
    "ponte_aerea": {"phase": 6, "payFamily": "linha", "xpType": "linha", "board": "LinhaRegular", "vip": False, "minExpHours": 12, "maxExpHours": 24, "urgency": 1.1},
    "troncal_domestico": {"phase": 6, "payFamily": "linha", "xpType": "linha", "board": "LinhaRegular", "vip": False, "minExpHours": 24, "maxExpHours": 48, "urgency": 1.0},
    "regional_internacional": {"phase": 6, "payFamily": "linha", "xpType": "linha", "board": "LinhaRegular", "vip": False, "minExpHours": 36, "maxExpHours": 48, "urgency": 1.0},
    "longo_curso": {"phase": 7, "payFamily": "linha", "xpType": "linha", "board": "LinhaRegular", "vip": False, "minExpHours": 48, "maxExpHours": 72, "urgency": 1.0},
    "feeder_inbound": {"phase": 5, "payFamily": "regional", "xpType": "regional", "board": "LinhaRegional", "vip": False, "minExpHours": 12, "maxExpHours": 24, "urgency": 1.0},
    "feeder_outbound": {"phase": 5, "payFamily": "regional", "xpType": "regional", "board": "LinhaRegional", "vip": False, "minExpHours": 12, "maxExpHours": 24, "urgency": 1.0},
    "sub_regional": {"phase": 3, "payFamily": "regional", "xpType": "regional", "board": "LinhaRegional", "vip": False},
    "pinga_pinga": {"phase": 3, "payFamily": "regional", "xpType": "regional", "board": "LinhaRegional", "vip": False},
    "regional_essencial": {"phase": 3, "payFamily": "regional", "xpType": "regional", "board": "LinhaRegional", "vip": False},
    # C
    "executivo_corporativo": {"phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": False, "minExpHours": 6, "maxExpHours": 24, "urgency": 1.0},
    "executivo_intercontinental": {"phase": 4, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": True},
    "vip_internacional": {"phase": 4, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": True, "minExpHours": 12, "maxExpHours": 36, "urgency": 1.2},
    "agro_executivo": {"phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": False, "minExpHours": 12, "maxExpHours": 36, "urgency": 1.0},
    "familia_alto_padrao": {"phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": True, "minExpHours": 12, "maxExpHours": 48, "urgency": 1.0},
    "equipes_urgentes": {"phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": False, "minExpHours": 4, "maxExpHours": 12, "urgency": 1.3},
    "charter_lazer": {"phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": False},
    "charter_esportivo": {"phase": 5, "payFamily": "regional", "xpType": "regional", "board": "LinhaRegional", "vip": False},
    "transfer_metropolitano": {"phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": True},
    "panoramico": {"phase": 1, "payFamily": "panoramico", "xpType": "panoramico", "board": "TaxiAereo", "vip": False, "minExpHours": 2, "maxExpHours": 6, "urgency": 1.0},
    "demo_flight": {"phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "Geral", "vip": False},
    # D
    "carga_paletizada": {"phase": 6, "payFamily": "linha", "xpType": "linha", "board": "Carga", "vip": False, "minExpHours": 24, "maxExpHours": 48, "urgency": 1.0},
    "carga_overnight": {"phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa", "board": "Carga", "vip": False, "minExpHours": 12, "maxExpHours": 36, "urgency": 1.2},
    "malotes": {"phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa", "board": "Carga", "vip": False, "minExpHours": 4, "maxExpHours": 12, "urgency": 1.5},
    "carga_valores": {"phase": 3, "payFamily": "aog", "xpType": "aog", "board": "Carga", "vip": False},
    "ecommerce": {"phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa", "board": "Carga", "vip": False, "minExpHours": 12, "maxExpHours": 24, "urgency": 1.0},
    "carga_jit": {"phase": 3, "payFamily": "aog", "xpType": "aog", "board": "Carga", "vip": False, "minExpHours": 4, "maxExpHours": 12, "urgency": 1.2},
    "cadeia_fria": {"phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa", "board": "Carga", "vip": False, "urgency": 1.2},
    "carga_dgr": {"phase": 3, "payFamily": "carga_fragil", "xpType": "carga_fragil", "board": "Carga", "vip": False, "minExpHours": 1, "maxExpHours": 3, "urgency": 1.3},
    "carga_sensivel": {"phase": 2, "payFamily": "carga_fragil", "xpType": "carga_fragil", "board": "Carga", "vip": False, "urgency": 1.0},
    "carga_combi": {"phase": 3, "payFamily": "agro_bush", "xpType": "agro_bush", "board": "Carga", "vip": False},
    "insumos_agro": {"phase": 2, "payFamily": "agro_municipal", "xpType": "agro_municipal", "board": "Carga", "vip": False, "minExpHours": 4, "maxExpHours": 12, "urgency": 1.0},
    "suprimentos_remotos": {"phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush", "board": "Carga", "vip": False, "minExpHours": 6, "maxExpHours": 24, "urgency": 1.0},
    "conexao_pista_curta": {"phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush", "board": "Carga", "vip": False},
    # E
    "orgaos": {"phase": 4, "payFamily": "medevac", "xpType": "medevac", "board": "Medica", "vip": False, "minExpHours": 1, "maxExpHours": 4, "urgency": 1.5},
    "uti_inter_hospitalar": {"phase": 4, "payFamily": "medevac", "xpType": "medevac", "board": "Medica", "vip": False, "minExpHours": 2, "maxExpHours": 12, "urgency": 1.0},
    "medevac_remoto": {"phase": 4, "payFamily": "medevac", "xpType": "medevac", "board": "Medica", "vip": False, "minExpHours": 1, "maxExpHours": 2, "urgency": 1.5},
    "uti_longa_distancia": {"phase": 4, "payFamily": "medevac", "xpType": "medevac", "board": "Medica", "vip": False, "minExpHours": 4, "maxExpHours": 24, "urgency": 1.0},
    "saude_familia": {"phase": 3, "payFamily": "medevac", "xpType": "medevac", "board": "Medica", "vip": False, "minExpHours": 4, "maxExpHours": 24, "urgency": 1.0},
    # F
    "aerolevantamento": {"phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto", "board": "Sae", "vip": False, "minExpHours": 2, "maxExpHours": 6, "urgency": 1.0},
    "paraquedismo": {"phase": 2, "payFamily": "paraquedismo", "xpType": "paraquedismo", "board": "Sae", "vip": False, "minExpHours": 2, "maxExpHours": 6, "urgency": 1.0},
    "pulverizacao": {"phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto", "board": "Sae", "vip": False},
    "reboque_planador": {"phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto", "board": "Sae", "vip": False},
    "publicidade_aerea": {"phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto", "board": "Sae", "vip": False},
    "patrulha_vigilancia": {"phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto", "board": "Sae", "vip": False},
    "inspecao_linhas": {"phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto", "board": "Sae", "vip": False},
    "monitoramento_trafego": {"phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto", "board": "Sae", "vip": False},
    "sar": {"phase": 2, "payFamily": "aerofoto", "xpType": "aerofoto", "board": "Sae", "vip": False},
    "calibracao_auxilios": {"phase": 4, "payFamily": "aerofoto", "xpType": "aerofoto", "board": "Sae", "vip": False},
    # G
    "ferry_posicionamento": {"phase": 1, "payFamily": "translado", "xpType": "translado", "board": "Geral", "vip": False, "minExpHours": 2, "maxExpHours": 8, "urgency": 1.0},
    "translado_manutencao": {"phase": 1, "payFamily": "translado", "xpType": "translado", "board": "Geral", "vip": False, "minExpHours": 6, "maxExpHours": 24, "urgency": 1.0},
    "ferry_permit": {"phase": 2, "payFamily": "translado", "xpType": "translado", "board": "Geral", "vip": False},
    "check_flight": {"phase": 2, "payFamily": "translado", "xpType": "translado", "board": "Geral", "vip": False},
    "crew_parts": {"phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "Carga", "vip": False, "minExpHours": 6, "maxExpHours": 12, "urgency": 1.0},
    "translado_piloto_agricola": {"phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush", "board": "Geral", "vip": False, "minExpHours": 2, "maxExpHours": 8, "urgency": 1.0},
    "pessoas_remotas": {"phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush", "board": "TaxiAereo", "vip": False, "minExpHours": 6, "maxExpHours": 24, "urgency": 1.0},
    # H
    "autoridades": {"phase": 3, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": True, "minExpHours": 12, "maxExpHours": 24, "urgency": 1.0},
    "diplomatico": {"phase": 4, "payFamily": "taxi_aereo", "xpType": "taxi_aereo", "board": "TaxiAereo", "vip": True, "minExpHours": 24, "maxExpHours": 48, "urgency": 1.0},
    "militar_can": {"phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa", "board": "Carga", "vip": False},
    "humanitario": {"phase": 3, "payFamily": "carga_expressa", "xpType": "carga_expressa", "board": "Carga", "vip": False},
    # I
    "combustivel_tambor": {"phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush", "board": "Carga", "vip": False},
    "apoio_acampamento": {"phase": 2, "payFamily": "agro_bush", "xpType": "agro_bush", "board": "Carga", "vip": False},
    "instrucao_stol": {"phase": 2, "payFamily": "instrucao", "xpType": "instrucao", "board": "Instrucao", "vip": False},
    "evacuacao_por_do_sol": {"phase": 3, "payFamily": "agro_bush", "xpType": "agro_bush", "board": "TaxiAereo", "vip": False},
}

OLD_NUMERIC = {
    'linha_domestica_hub': '1.1.1', 'linha_regional_internacional': '1.1.2', 'linha_long_haul': '1.1.3',
    'carga_heavy_nacional': '1.2.1', 'carga_heavy_internacional': '1.2.2', 'carga_heavy_long_haul': '1.2.3',
    'executiva_business': '1.3.1', 'logistica_apoio_crew_parts': '1.3.2', 'translado_posicionamento_ferry': '1.3.3',
    'diplomatico_militar': '1.3.4', 'alimentacao_hub': '2.1.1', 'regional_direto': '2.1.2',
    'agro_legacy': '2.2.1', 'agro_tech': '2.2.2', 'agro_investidores': '2.2.3',
    'ecommerce_varejo': '2.3.1', 'agro_pecas_insumos': '2.3.2', 'malotes_documentos': '2.3.3',
    'manutencao_regional': '2.4.1', 'governo_local_estadual': '2.4.2',
    'instrucao_local': '3.4.1', 'navegacao_cross_country': '3.4.2', 'voo_de_cheque': '3.4.3',
    'executivo_regional': '3.1.1', 'familia_alto_padrao': '3.1.2', 'equipes_urgentes': '3.1.3',
    'aog_agro_suporte': '3.2.1', 'agro_insumos_biotecnologia': '3.2.2', 'agro_suprimento_campanha': '3.2.3',
    'remocao_urgencia': '3.3.1', 'transferencia_eletiva': '3.3.2', 'transporte_orgaos': '3.3.3',
    'abastecimento_rancho': '4.1.1', 'transporte_patrao': '4.1.2', 'troca_equipe': '4.1.3',
    'apoio_batedor': '4.2.1', 'translado_piloto_agricola': '4.2.2', 'insumos_urgentes_agro': '4.2.3',
    'medivac_bush': '4.3.1', 'aog_maquina_campo': '4.3.2',
    'aerolevantamento': '3.6.1', 'paraquedismo': '3.6.2',
    'voo_panoramico': '3.5.1', 'circuito_tgl': '3.5.2', 'translado_basico': '3.5.3',
    'fretamento_executivo': '1.7.1', 'carga_fragil': '1.7.2', 'medevac_dinamico': '1.7.3',
    'vip_internacional_curto': '1.7.4', 'ponte_aerea_troncal': '1.7.5', 'cargueiro_noturno': '1.7.6',
    'long_haul_internacional': '1.7.7', 'fretamento_global_carga_alto_valor': '1.7.8',
    'fretamento_executivo_regional': '2.7.1', 'carga_fragil_regional': '2.7.2', 'medevac_regional': '2.7.3',
    'vip_internacional_regional': '2.7.4', 'carga_fragil_municipal': '3.7.1', 'medevac_municipal': '3.7.2'
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
    "diplomatico": ["diplomatico_militar"],
}

def patch_catalog():
    with open(CATALOG_PATH, "r", encoding="utf-8") as f:
        data = json.load(f)

    for m in data["missions"]:
        m_id = m["id"]
        # Apply attributes
        if m_id in ATTRIBUTES:
            attrs = ATTRIBUTES[m_id]
            m["phase"] = attrs["phase"]
            m["payFamily"] = attrs["payFamily"]
            m["xpType"] = attrs["xpType"]
            m["board"] = attrs["board"]
            m["vip"] = attrs["vip"]
            if "urgency" in attrs:
                m["urgency"] = attrs["urgency"]
            if "minExpHours" in attrs:
                m["minExpHours"] = attrs["minExpHours"]
            if "maxExpHours" in attrs:
                m["maxExpHours"] = attrs["maxExpHours"]

        # Build legacyIds including both old text ids and old numeric codes
        legacy = list(LEGACY_MAP.get(m_id, []))
        if m_id in OLD_NUMERIC and OLD_NUMERIC[m_id] not in legacy:
            legacy.append(OLD_NUMERIC[m_id])
        for leg in list(legacy):
            if leg in OLD_NUMERIC and OLD_NUMERIC[leg] not in legacy:
                legacy.append(OLD_NUMERIC[leg])

        m["legacyIds"] = legacy

    with open(CATALOG_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    print("Catálogo atualizado com sucesso!")

if __name__ == "__main__":
    patch_catalog()
