# Revisão PANORAMICOS-CURADOS — APROVADO (com ajuste do revisor)

Revisor: Claude, 2026-11-22. Lido o diff do gerador (ramo turístico do `CreateScenicTourMissionSync`, importação da 3.5 no tier 1, pesos 70/15/15), `PlacesDataService` (carga de `scenic_aerodromes.json`) e os textos novos. As subcategorias são copiadas por aeródromo (`GetCalibratedCategories`), então a troca de pesos não vaza para outros aeroportos. Auditoria: 0 ações para conferir. Build 0 erros; `dotnet test` 935/935.

Ajuste do revisor:
1. As variantes do 3.5.1 com `{scenic_name}`/`{scenic_note}` podiam sair em aeródromos não turísticos e o título ficava cortado ("Excursão Aérea Guiada: "); uma usava "Curado" no título. Movidas para a chave `3.5.1-turistico` (usada só no ramo turístico), com uma terceira variante e teste.

Observação: a cópia de `SubCategoryConfig` não leva `PreferredDestinations*`, que hoje ninguém preenche. Não testado no MSFS / não visto na tela.
