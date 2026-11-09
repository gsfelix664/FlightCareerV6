using System;
using System.Collections.Generic;

namespace FlightCareer.Application.Services
{
    public class TierConfig
    {
        public List<int> AllowedDestinationTiers { get; set; } = new List<int>();
        public List<CategoryConfig> Categories { get; set; } = new List<CategoryConfig>();
    }

    public class CategoryConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Weight { get; set; }
        public List<SubCategoryConfig> SubCategories { get; set; } = new List<SubCategoryConfig>();
    }

    public class SubCategoryConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Weight { get; set; }
        public List<ProbRange> RangeRules { get; set; } = new List<ProbRange>();
        public List<ProbRange> PaxRules { get; set; } = new List<ProbRange>();
        public List<ProbRange> LbsRules { get; set; } = new List<ProbRange>();
        public int MinExpHours { get; set; }
        public int MaxExpHours { get; set; }
        public double Urgency { get; set; } = 1.0;
        public bool RequiresHardSurface { get; set; }
        public int MinRunwayDest { get; set; }
        public double LuggageWeightKg { get; set; } = 15;
        public List<string> PreferredDestinations { get; set; } = new List<string>();
        public Dictionary<string, string[]> PreferredDestinationsMap { get; set; } = new Dictionary<string, string[]>();
    }

    public class ProbRange
    {
        public double Weight { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }

        public ProbRange(double w, int min, int max)
        {
            Weight = w;
            Min = min;
            Max = max;
        }
    }

    public static class MissionRules
    {
        public static Dictionary<int, TierConfig> Rules = new Dictionary<int, TierConfig>();

        static MissionRules()
        {
            // TIER 1
            var t1 = new TierConfig { AllowedDestinationTiers = new List<int> { 1, 2, 3 } };
            var c1_1 = new CategoryConfig { Id = "1.1", Name = "Linha Aérea PAX", Weight = 60 };
            c1_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.1.1", Name = "Doméstico / Hub Link",
                Description = "Analise este briefing com cuidado. Este voo matinal é vital para a integridade da nossa rede. O manifesto indica uma cabine lotada de executivos e famílias exaustas, recém-chegadas das conexões internacionais da madrugada. A janela de operação é estreita: a pontualidade no calço é inegociável, pois qualquer atraso causará um efeito dominó desastroso na malha do Hub. Execute os procedimentos com agilidade e solicite o pushback no minuto exato.",
                Weight = 60, MinExpHours = 24, MaxExpHours = 48, RequiresHardSurface = true, MinRunwayDest = 4000, LuggageWeightKg = 20,
                RangeRules = { new(35, 200, 450), new(40, 451, 900), new(25, 901, 1600) },
                PaxRules = { new(10, 110, 130), new(60, 131, 180), new(30, 181, 230) }
            });
            c1_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.1.2", Name = "Regional Internacional",
                Description = "Revise os dados de performance para esta etapa internacional. Hoje transportamos um perfil misto de executivos de multinacionais e turistas de alto padrão cruzando fronteiras. O fator crítico da operação será o peso: espere os porões operando na capacidade máxima devido ao volume excessivo de bagagens. Monitore rigorosamente o carregamento e os cálculos de Peso e Balanceamento (W&B). A aeronave estará pesada na decolagem, exigindo precisão total na configuração dos flaps e velocidades. Coordene com o solo para evitar atrasos no despacho de carga.",
                Weight = 25, MinExpHours = 36, MaxExpHours = 48, RequiresHardSurface = true, MinRunwayDest = 5000, LuggageWeightKg = 30,
                RangeRules = { new(45, 500, 1000), new(35, 1001, 1600), new(20, 1601, 2600) },
                PaxRules = { new(45, 130, 175), new(35, 176, 230), new(20, 231, 320) }
            });
            c1_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.1.3", Name = "Intercontinental (Long Haul)",
                Description = "Prepare-se para uma longa travessia. O manifesto de hoje lista um público de elite: turistas globais e expatriados ocupando as cabines Premium com expectativas altíssimas. Sua prioridade é garantir uma navegação suave; desvios de turbulência são essenciais para manter o conforto. Simultaneamente, a gestão de combustível será crítica. Monitore o Fuel on Board e otimize os step climbs para garantir a eficiência máxima durante as horas sobre o oceano ou continentes. A satisfação a bordo depende da sua precisão.",
                Weight = 15, MinExpHours = 48, MaxExpHours = 72, RequiresHardSurface = true, MinRunwayDest = 8000, LuggageWeightKg = 50,
                RangeRules = { new(40, 3000, 4100), new(50, 4200, 5500), new(10, 5501, 7500) },
                PaxRules = { new(40, 210, 290), new(50, 291, 380), new(10, 381, 440) }
            });
            t1.Categories.Add(c1_1);

            var c1_2 = new CategoryConfig { Id = "1.2", Name = "Carga Heavy", Weight = 20 };
            c1_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.2.1", Name = "Carga Heavy Nacional",
                Description = "O manifesto de hoje exige precisão cirúrgica. Estamos transportando carga de altíssimo valor agregado: componentes eletrônicos e suprimentos industriais críticos. Esta operação é a espinha dorsal do varejo e os estoques em terra dependem de uma entrega Just-in-Time. Atenção redobrada na operação de solo e evite impactos bruscos no pouso. Com a fuselagem cheia, o cálculo do MTOW está no limite. Cumpra o horário rigorosamente para não paralisar a cadeia logística.",
                Weight = 50, MinExpHours = 24, MaxExpHours = 48, RequiresHardSurface = true, MinRunwayDest = 5000,
                RangeRules = { new(25, 200, 600), new(25, 601, 1350), new(50, 1351, 1600) },
                LbsRules = { new(30, 33000, 55000), new(50, 56000, 100000), new(20, 100001, 120000) }
            });
            c1_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.2.2", Name = "Carga Regional Internacional",
                Description = "Atenção redobrada ao plano de carregamento. O manifesto de hoje é um desafio técnico: transportamos maquinário industrial de alta densidade e produtos que exigem protocolos rigorosos de segurança. Essa combinação concentra muito peso em áreas específicas do porão, deixando o Centro de Gravidade (CG) crítico. Verifique seus dados de trimagem; a aeronave responderá de forma diferente na decolagem. Cumpra a rota com precisão para garantir a logística internacional de itens sensíveis. Estabilidade é a palavra de ordem.",
                Weight = 20, MinExpHours = 24, MaxExpHours = 36, RequiresHardSurface = true, MinRunwayDest = 5000,
                RangeRules = { new(40, 600, 1000), new(40, 1001, 1800), new(20, 1801, 2600) },
                LbsRules = { new(30, 30000, 50000), new(50, 50001, 95000), new(20, 95001, 118000) }
            });
            c1_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.2.3", Name = "Carga Intercontinental (Heavy)",
                Description = "Comandante, a operação hoje é de 'peso pesado'. O manifesto inclui componentes críticos em regime AOG (Aircraft on Ground); a prioridade é absoluta. O carregamento de itens indivisíveis no Main Deck altera significativamente o momento de inércia da aeronave. Espere respostas mais lentas nos comandos. O planejamento de descida deve ser antecipado: com essa massa, a gestão de energia cinética é vital para garantir uma aproximação estabilizada no Hub de destino. Entregue essa carga com precisão.",
                Weight = 30, MinExpHours = 48, MaxExpHours = 72, RequiresHardSurface = true, MinRunwayDest = 8000,
                RangeRules = { new(40, 3500, 4100), new(50, 4200, 5500), new(10, 5501, 7500) },
                LbsRules = { new(40, 120000, 170000), new(45, 171000, 240000), new(15, 241000, 300000) }
            });
            t1.Categories.Add(c1_2);

            var c1_3 = new CategoryConfig { Id = "1.3", Name = "Operações Auxiliares", Weight = 20 };
            c1_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.3.1", Name = "Aviação Executiva / Executive Business",
                Description = "Transporte de elite, celebridades e autoridades. Exige discrição, pontualidade e tratamento VIP desde o acionamento até o corte dos motores.",
                Weight = 50, MinExpHours = 12, MaxExpHours = 24, RequiresHardSurface = true, MinRunwayDest = 3000,
                RangeRules = { new(60, 150, 450), new(30, 500, 1200), new(10, 1201, 2500) },
                PaxRules = { new(60, 1, 12), new(25, 2, 14), new(15, 3, 25) }
            });
            c1_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.3.2", Name = "Logística de Apoio / Crew & Parts",
                Description = "Suporte operacional para companhias aéreas. Transporte urgente de tripulações para assumir voos ou peças críticas (AOG) para manutenção imediata.",
                Weight = 30, MinExpHours = 6, MaxExpHours = 12, RequiresHardSurface = true, MinRunwayDest = 3000,
                RangeRules = { new(50, 10, 60), new(40, 61, 250), new(10, 251, 500) },
                PaxRules = { new(60, 4, 18), new(40, 0, 0) } // 40% é carga AOG
            });
            c1_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.3.3", Name = "Voos de Translado / Ferry",
                Description = "Reposicionamento técnico de aeronave ou envio para base de manutenção. Voo realizado sem passageiros ou carga comercial.",
                Weight = 15, MinExpHours = 24, MaxExpHours = 48, RequiresHardSurface = true, MinRunwayDest = 3000,
                RangeRules = { new(65, 30, 60), new(30, 180, 250), new(5, 300, 500) },
                PaxRules = { new(100, 0, 0) }
            });
            c1_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "1.3.4", Name = "Diplomático e Militar",
                Description = "Missões especiais de estado. Transporte de comitivas presidenciais, corpo diplomático ou logística militar estratégica.",
                Weight = 5, MinExpHours = 24, MaxExpHours = 48, RequiresHardSurface = true, MinRunwayDest = 4000,
                RangeRules = { new(50, 100, 500), new(40, 501, 1500), new(10, 1501, 3000) },
                PaxRules = { new(30, 15, 40), new(50, 5, 15), new(20, 0, 0) }
            });
            t1.Categories.Add(c1_3);
            Rules.Add(1, t1);

            // TIER 2
            var t2 = new TierConfig { AllowedDestinationTiers = new List<int> { 1, 2, 3, 4 } };
            var c2_1 = new CategoryConfig { Id = "2.1", Name = "Linha Aérea Regional", Weight = 45 };
            c2_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.1.1", Name = "Alimentação de Hub",
                Description = "Assuma esta etapa essencial de alimentação (feeder). Sua missão é conectar o interior à malha global, transportando passageiros com conexões apertadas nos grandes Hubs. A operação em aeroportos regionais exige atenção total a pistas curtas e infraestrutura de solo limitada. No cruzeiro em níveis de voo inferiores, a gestão meteorológica deve ser constante para minimizar o desconforto da turbulência em aeronaves menores. Realize um turnaround expresso no solo; a pontualidade deste voo dita o sucesso das conexões de toda a rede.",
                Weight = 80, MinExpHours = 12, MaxExpHours = 24, RequiresHardSurface = true, MinRunwayDest = 4000, LuggageWeightKg = 20,
                PaxRules = { new(30, 45, 60), new(50, 61, 110), new(20, 111, 160) },
                RangeRules = { new(60, 80, 180), new(30, 181, 350), new(10, 351, 550) }
            });
            c2_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.1.2", Name = "Regional Direto",
                Description = "Operação entre polos regionais de grande relevância econômica. Diferente da alimentação de hub, este voo conecta diretamente cidades de médio porte, atendendo a uma demanda corporativa e de lazer ponto-a-ponto. Exige eficiência no solo e atenção à diversidade de pistas encontradas no interior.",
                Weight = 20, MinExpHours = 12, MaxExpHours = 24, RequiresHardSurface = true, MinRunwayDest = 4000, LuggageWeightKg = 20,
                PaxRules = { new(40, 35, 50), new(50, 51, 85), new(10, 86, 110) },
                RangeRules = { new(50, 150, 300), new(40, 301, 600), new(10, 601, 850) }
            });
            t2.Categories.Add(c2_1);

            var c2_2 = new CategoryConfig { Id = "2.2", Name = "Executiva (Agro/Biz)", Weight = 30 };
            c2_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.2.1", Name = "Agro Legacy",
                Description = "Prepare-se para a operação rural. O proprietário precisa chegar à sede para supervisionar o pico da safra. O bagageiro está no limite com peças urgentes de maquinário parado e insumos agrícolas. O destino é uma pista não pavimentada, curta e sem apoio de solo. Exige-se máxima perícia em performance de campo curto e aproximação visual, pois a superfície pode estar irregular. A continuidade da colheita depende inteiramente da sua habilidade de colocar esta aeronave no chão com segurança e entregar a peça.",
                Weight = 50, MinExpHours = 12, MaxExpHours = 48, RequiresHardSurface = false, MinRunwayDest = 1800, LuggageWeightKg = 18,
                PaxRules = { new(60, 1, 2), new(30, 3, 5), new(10, 6, 9) },
                LbsRules = { new(100, 150, 400) },
                RangeRules = { new(70, 30, 120), new(25, 121, 450), new(5, 451, 700) }
            });
            c2_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.2.2", Name = "Técnicos e Consultores (Agro Tech)",
                Description = "Transporte de equipe técnica especializada. Missão de assistência em campo. Atenção ao manifesto: passageiros transportam ferramentas de precisão e equipamentos de diagnóstico sensíveis.",
                Weight = 30, MinExpHours = 12, MaxExpHours = 36, RequiresHardSurface = false, MinRunwayDest = 1800, LuggageWeightKg = 13.6,
                PaxRules = { new(50, 1, 2), new(40, 3, 5), new(10, 6, 9) },
                LbsRules = { new(100, 100, 300) },
                RangeRules = { new(40, 60, 180), new(50, 181, 500), new(10, 501, 900) }
            });
            c2_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.2.3", Name = "Investidores e Compradores",
                Description = "Transporte de investidores e grupos de negociação de safras. Exige aeronave impecável e pontualidade. Preferência por aeroportos com estrutura de asfalto.",
                Weight = 20, MinExpHours = 12, MaxExpHours = 24, RequiresHardSurface = true, MinRunwayDest = 3000, LuggageWeightKg = 20.4,
                PaxRules = { new(60, 2, 4), new(30, 5, 8), new(10, 9, 12) },
                LbsRules = { new(100, 100, 100) },
                RangeRules = { new(10, 100, 240), new(70, 250, 450), new(20, 451, 800) }
            });
            t2.Categories.Add(c2_2);

            var c2_3 = new CategoryConfig { Id = "2.3", Name = "Carga Expressa (Cargo)", Weight = 15 };
            c2_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.3.1", Name = "E-commerce & Varejo",
                Description = "Distribuição de compras online (Mercado Livre, Amazon, Magalu) que chegam do Hub para serem entregues no interior.",
                Weight = 50, MinExpHours = 12, MaxExpHours = 24, RequiresHardSurface = false, MinRunwayDest = 2000,
                LbsRules = { new(45, 800, 1800), new(40, 1801, 4500), new(15, 4501, 12000) },
                RangeRules = { new(50, 40, 120), new(35, 121, 280), new(15, 281, 500) }
            });
            c2_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.3.2", Name = "Peças & Insumos Agro",
                Description = "Urgência técnica: correias de colheitadeira, sensores de drones ou componentes de irrigação.",
                Weight = 30, MinExpHours = 6, MaxExpHours = 18, RequiresHardSurface = false, MinRunwayDest = 1500,
                LbsRules = { new(40, 50, 400), new(45, 401, 1500), new(15, 1501, 3500) },
                RangeRules = { new(35, 30, 100), new(50, 101, 350), new(15, 351, 700) }
            });
            c2_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.3.3", Name = "Malotes & Documentos",
                Description = "Malotes bancários, documentos notariais e exames laboratoriais que precisam de processamento rápido.",
                Weight = 20, MinExpHours = 4, MaxExpHours = 12, RequiresHardSurface = true, MinRunwayDest = 2000, Urgency = 1.5,
                LbsRules = { new(50, 50, 200), new(35, 201, 500), new(15, 501, 1000) },
                RangeRules = { new(45, 50, 150), new(45, 151, 300), new(10, 301, 600) }
            });
            t2.Categories.Add(c2_3);

            var c2_4 = new CategoryConfig { Id = "2.4", Name = "Apoio / Outros", Weight = 5 };
            c2_4.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.4.1", Name = "Manutenção Regional (Maint)",
                Description = "Aeronaves que precisam de reparos em oficinas especializadas ou envio de mecânicos para socorro (AOG).",
                Weight = 60, MinExpHours = 6, MaxExpHours = 24, RequiresHardSurface = false, MinRunwayDest = 1500,
                PaxRules = { new(70, 0, 0), new(30, 1, 2) },
                LbsRules = { new(60, 100, 300), new(40, 301, 800) },
                RangeRules = { new(65, 20, 70), new(30, 71, 250), new(5, 251, 500) }
            });
            c2_4.SubCategories.Add(new SubCategoryConfig
            {
                Id = "2.4.2", Name = "Governo Local / Estadual (Gov)",
                Description = "Deslocamento de autoridades e comitivas técnicas entre a capital e os polos regionais.",
                Weight = 40, MinExpHours = 12, MaxExpHours = 24, RequiresHardSurface = true, MinRunwayDest = 3000,
                PaxRules = { new(60, 2, 4), new(35, 5, 9), new(5, 10, 15) },
                RangeRules = { new(40, 100, 250), new(50, 251, 450), new(10, 451, 700) }
            });
            t2.Categories.Add(c2_4);

            var c2_5 = new CategoryConfig { Id = "3.4", Name = "Treinamento", Weight = 5 };
            c2_5.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.4.1", Name = "Voo de Instrução Local",
                Description = "Prática de manobras e pousos (TGL) na vizinhança do aeroporto.",
                Weight = 60, MinExpHours = 1, MaxExpHours = 3, RequiresHardSurface = false, MinRunwayDest = 1000,
                PaxRules = { new(90, 1, 1), new(10, 0, 0) },
                RangeRules = { new(100, 5, 15) }
            });
            c2_5.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.4.2", Name = "Navegação (Cross-Country)",
                Description = "Aluno em treinamento de rota entre duas ou três cidades.",
                Weight = 30, MinExpHours = 2, MaxExpHours = 6, RequiresHardSurface = false, MinRunwayDest = 1200,
                PaxRules = { new(60, 1, 1), new(40, 0, 0) },
                RangeRules = { new(60, 50, 100), new(30, 101, 180), new(10, 181, 300) }
            });
            c2_5.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.4.3", Name = "Voo de Cheque (Exame)",
                Description = "Avaliação prática com examinador da ANAC a bordo.",
                Weight = 10, MinExpHours = 2, MaxExpHours = 4, RequiresHardSurface = false, MinRunwayDest = 1200, Urgency = 1.2,
                PaxRules = { new(100, 1, 1) },
                RangeRules = { new(70, 30, 60), new(30, 61, 120) }
            });
            t2.Categories.Add(c2_5);
            Rules.Add(2, t2);

            // TIER 3
            var t3 = new TierConfig { AllowedDestinationTiers = new List<int> { 1, 2, 3, 4 } };
            var c3_1 = new CategoryConfig { Id = "3.1", Name = "Táxi Aéreo / Charter", Weight = 45 };
            c3_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.1.1", Name = "Executivo Regional",
                Description = "Donos de empresas locais ou diretores que precisam ir à capital para uma reunião e voltar no mesmo dia.",
                Weight = 50, MinExpHours = 6, MaxExpHours = 24, RequiresHardSurface = true, MinRunwayDest = 2500, LuggageWeightKg = 15.8,
                PaxRules = { new(60, 1, 1), new(30, 2, 3), new(10, 4, 6) },
                RangeRules = { new(45, 80, 150), new(45, 151, 350), new(10, 351, 600) }
            });
            c3_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.1.2", Name = "Família de Alto Padrão",
                Description = "Moradores da cidade polo indo para o litoral ou para o Hub internacional para iniciar férias.",
                Weight = 30, MinExpHours = 12, MaxExpHours = 48, RequiresHardSurface = true, MinRunwayDest = 3000, LuggageWeightKg = 22.6,
                PaxRules = { new(30, 2, 2), new(55, 3, 5), new(15, 6, 8) },
                RangeRules = { new(30, 100, 200), new(55, 201, 450), new(15, 451, 800) }
            });
            c3_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.1.3", Name = "Equipes Urgentes",
                Description = "Advogados para audiências de última hora ou engenheiros para assinar liberações técnicas.",
                Weight = 20, MinExpHours = 4, MaxExpHours = 12, RequiresHardSurface = true, MinRunwayDest = 2500, Urgency = 1.3,
                PaxRules = { new(65, 1, 2), new(30, 3, 5), new(5, 6, 8) },
                RangeRules = { new(40, 50, 150), new(50, 151, 400), new(10, 401, 700) }
            });
            t3.Categories.Add(c3_1);

            var c3_2 = new CategoryConfig { Id = "3.2", Name = "Agro Suporte", Weight = 40 };
            c3_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.2.1", Name = "Peças de Reposição Críticas (AOG Agro)",
                Description = "Urgência técnica no campo. Envio de peças para reparo imediato de maquinário parado.",
                Weight = 50, MinExpHours = 4, MaxExpHours = 12, RequiresHardSurface = false, MinRunwayDest = 1200, Urgency = 1.4,
                PaxRules = { new(60, 0, 0), new(40, 1, 2) },
                LbsRules = { new(100, 200, 800) },
                RangeRules = { new(35, 40, 120), new(55, 121, 300), new(10, 301, 500) }
            });
            c3_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.2.2", Name = "Insumos de Alta Tecnologia & Genética",
                Description = "Transporte de material sensível, biológico ou eletrônico de alta precisão para fazendas.",
                Weight = 30, MinExpHours = 12, MaxExpHours = 36, RequiresHardSurface = false, MinRunwayDest = 1500,
                PaxRules = { new(70, 1, 1), new(20, 0, 0), new(10, 2, 3) },
                LbsRules = { new(100, 100, 400) },
                RangeRules = { new(60, 100, 250), new(30, 251, 450), new(10, 50, 99) }
            });
            c3_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.2.3", Name = "Suprimento de Campanha (Volume)",
                Description = "Cabine configurada para carga. Abastecimento de insumos para períodos de safra/plantio.",
                Weight = 20, MinExpHours = 24, MaxExpHours = 72, RequiresHardSurface = false, MinRunwayDest = 1800,
                PaxRules = { new(75, 0, 0), new(25, 1, 2) },
                LbsRules = { new(100, 500, 3000) },
                RangeRules = { new(50, 100, 300), new(50, 301, 600) }
            });
            t3.Categories.Add(c3_2);

            var c3_3 = new CategoryConfig { Id = "3.3", Name = "Saúde / Remoção", Weight = 10 };
            c3_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.3.1", Name = "Remoção de Urgência",
                Description = "Paciente em estado crítico sendo levado do hospital municipal para um centro de trauma.",
                Weight = 60, MinExpHours = 2, MaxExpHours = 6, RequiresHardSurface = true, MinRunwayDest = 2500, Urgency = 2.0,
                PaxRules = { new(70, 3, 3), new(30, 4, 4) },
                RangeRules = { new(55, 80, 160), new(40, 161, 320), new(5, 321, 550) }
            });
            c3_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.3.2", Name = "Transferência Eletiva",
                Description = "Paciente estável que precisa de exames ou cirurgias em grandes centros. Exige conforto.",
                Weight = 30, MinExpHours = 12, MaxExpHours = 48, RequiresHardSurface = true, MinRunwayDest = 3000,
                PaxRules = { new(60, 4, 4), new(40, 5, 6) },
                RangeRules = { new(50, 150, 300), new(40, 301, 500), new(10, 501, 800) }
            });
            c3_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.3.3", Name = "Transporte de Órgãos",
                Description = "Missão de altíssima prioridade (Mayday/Medevac). Apenas a equipe médica e o órgão.",
                Weight = 10, MinExpHours = 1, MaxExpHours = 4, RequiresHardSurface = true, MinRunwayDest = 2500, Urgency = 3.0,
                PaxRules = { new(70, 2, 2), new(30, 3, 4) },
                RangeRules = { new(100, 100, 600) }
            });
            t3.Categories.Add(c3_3);

            var c3_4 = new CategoryConfig { Id = "3.4", Name = "Treinamento", Weight = 5 };
            c3_4.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.4.1", Name = "Voo de Instrução Local",
                Description = "Prática de manobras e pousos (TGL) na vizinhança do aeroporto.",
                Weight = 60, MinExpHours = 1, MaxExpHours = 3, RequiresHardSurface = false, MinRunwayDest = 1000,
                PaxRules = { new(90, 1, 1), new(10, 0, 0) },
                RangeRules = { new(100, 5, 15) }
            });
            c3_4.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.4.2", Name = "Navegação (Cross-Country)",
                Description = "Aluno em treinamento de rota entre duas ou três cidades.",
                Weight = 30, MinExpHours = 2, MaxExpHours = 6, RequiresHardSurface = false, MinRunwayDest = 1200,
                PaxRules = { new(60, 1, 1), new(40, 0, 0) },
                RangeRules = { new(60, 50, 100), new(30, 101, 180), new(10, 181, 300) }
            });
            c3_4.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.4.3", Name = "Voo de Cheque (Exame)",
                Description = "Avaliação prática com examinador da ANAC a bordo.",
                Weight = 10, MinExpHours = 2, MaxExpHours = 4, RequiresHardSurface = false, MinRunwayDest = 1200, Urgency = 1.2,
                PaxRules = { new(100, 1, 1) },
                RangeRules = { new(70, 30, 60), new(30, 61, 120) }
            });
            t3.Categories.Add(c3_4);
            Rules.Add(3, t3);

            // TIER 4
            var t4 = new TierConfig { AllowedDestinationTiers = new List<int> { 2, 3, 4 } };
            var c4_1 = new CategoryConfig { Id = "4.1", Name = "Logística de Sede", Weight = 55 };
            c4_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "4.1.1", Name = "Abastecimento e Rancho",
                Description = "Levar o essencial para a sobrevivência da sede: comida, galões de óleo/diesel e água.",
                Weight = 50, MinExpHours = 6, MaxExpHours = 24, RequiresHardSurface = false, MinRunwayDest = 1000,
                PaxRules = { new(65, 0, 0), new(30, 1, 1), new(5, 2, 2) },
                LbsRules = { new(35, 50, 100), new(65, 100, 1500) },
                RangeRules = { new(80, 15, 40), new(15, 41, 80), new(5, 81, 150) }
            });
            c4_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "4.1.2", Name = "Transporte do Patrão",
                Description = "O dono da fazenda ou investidores indo para a sede para reuniões ou fiscalização.",
                Weight = 30, MinExpHours = 12, MaxExpHours = 36, RequiresHardSurface = false, MinRunwayDest = 1500,
                PaxRules = { new(50, 2, 2), new(30, 1, 1), new(20, 3, 5) },
                RangeRules = { new(60, 30, 80), new(30, 81, 200), new(10, 201, 400) }
            });
            c4_1.SubCategories.Add(new SubCategoryConfig
            {
                Id = "4.1.3", Name = "Troca de Equipe",
                Description = "Levar funcionários, gerentes ou cozinheiros para o início da escala de trabalho na fazenda.",
                Weight = 20, MinExpHours = 12, MaxExpHours = 48, RequiresHardSurface = false, MinRunwayDest = 1200,
                PaxRules = { new(65, 3, 5), new(20, 2, 2), new(15, 6, 9) },
                RangeRules = { new(80, 20, 60), new(20, 61, 120) }
            });
            t4.Categories.Add(c4_1);

            var c4_2 = new CategoryConfig { Id = "4.2", Name = "Aviação Agrícola (Ferry)", Weight = 30 };
            c4_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "4.2.1", Name = "Apoio ao Batedor",
                Description = "Levar o batedor (técnico de área), ferramentas e óleo para a pista de operação agrícola.",
                Weight = 50, MinExpHours = 4, MaxExpHours = 12, RequiresHardSurface = false, MinRunwayDest = 1000,
                PaxRules = { new(70, 2, 2), new(25, 1, 1), new(5, 3, 4) },
                LbsRules = { new(100, 0, 100) },
                RangeRules = { new(75, 15, 45), new(20, 46, 90), new(5, 91, 150) }
            });
            c4_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "4.2.2", Name = "Translado de Piloto (Ferry)",
                Description = "Levar o piloto agrícola de volta para a base ou para outra aeronave de trabalho.",
                Weight = 30, MinExpHours = 2, MaxExpHours = 8, RequiresHardSurface = false, MinRunwayDest = 1000,
                PaxRules = { new(85, 1, 1), new(15, 2, 2) },
                RangeRules = { new(75, 30, 80), new(25, 81, 180) }
            });
            c4_2.SubCategories.Add(new SubCategoryConfig
            {
                Id = "4.2.3", Name = "Insumos Urgentes",
                Description = "Peças específicas para pulverização ou defensivos químicos que acabaram no meio do serviço.",
                Weight = 20, MinExpHours = 4, MaxExpHours = 12, RequiresHardSurface = false, MinRunwayDest = 1000, Urgency = 1.5,
                PaxRules = { new(70, 0, 0), new(30, 1, 1) },
                LbsRules = { new(100, 300, 1200) },
                RangeRules = { new(65, 20, 60), new(35, 61, 150) }
            });
            t4.Categories.Add(c4_2);

            var c4_3 = new CategoryConfig { Id = "4.3", Name = "Emergência de Campo", Weight = 10 };
            c4_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "4.3.1", Name = "Resgate Médico (Medivac Bush)",
                Description = "Extração de paciente da fazenda para o hospital regional mais próximo. Urgência total.",
                Weight = 60, MinExpHours = 1, MaxExpHours = 4, RequiresHardSurface = false, MinRunwayDest = 1000, Urgency = 2.5,
                PaxRules = { new(75, 3, 3), new(20, 2, 2), new(5, 4, 4) },
                LbsRules = { new(100, 0, 200) },
                RangeRules = { new(65, 30, 80), new(30, 81, 180), new(5, 181, 300) }
            });
            c4_3.SubCategories.Add(new SubCategoryConfig
            {
                Id = "4.3.2", Name = "AOG Máquina (Peça Bruta)",
                Description = "Colheitadeira ou trator quebrado no auge da colheita. Transporte de peça pesada e mecânico.",
                Weight = 40, MinExpHours = 4, MaxExpHours = 12, RequiresHardSurface = false, MinRunwayDest = 1000, Urgency = 1.6,
                PaxRules = { new(60, 0, 0), new(40, 1, 1) },
                LbsRules = { new(100, 300, 1200) },
                RangeRules = { new(70, 30, 90), new(30, 91, 200) }
            });
            t4.Categories.Add(c4_3);

            var c4_4 = new CategoryConfig { Id = "3.4", Name = "Treinamento", Weight = 5 };
            c4_4.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.4.1", Name = "Voo de Instrução Local",
                Description = "Prática de manobras e pousos (TGL) na vizinhança do aeródromo.",
                Weight = 60, MinExpHours = 1, MaxExpHours = 3, RequiresHardSurface = false, MinRunwayDest = 1000,
                PaxRules = { new(90, 1, 1), new(10, 0, 0) },
                RangeRules = { new(100, 5, 15) }
            });
            c4_4.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.4.2", Name = "Navegação (Cross-Country)",
                Description = "Aluno em treinamento de rota entre duas ou três cidades.",
                Weight = 30, MinExpHours = 2, MaxExpHours = 6, RequiresHardSurface = false, MinRunwayDest = 1000,
                PaxRules = { new(60, 1, 1), new(40, 0, 0) },
                RangeRules = { new(60, 50, 100), new(30, 101, 180), new(10, 181, 300) }
            });
            c4_4.SubCategories.Add(new SubCategoryConfig
            {
                Id = "3.4.3", Name = "Voo de Cheque (Exame)",
                Description = "Avaliação prática com examinador a bordo.",
                Weight = 10, MinExpHours = 2, MaxExpHours = 4, RequiresHardSurface = false, MinRunwayDest = 1000, Urgency = 1.2,
                PaxRules = { new(100, 1, 1) },
                RangeRules = { new(70, 30, 60), new(30, 61, 120) }
            });
            t4.Categories.Add(c4_4);
            Rules.Add(4, t4);
        }
    }
}
