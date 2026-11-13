using FlightCareer.Domain.Entities;
using FlightCareer.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightCareer.UI.Services
{
    public enum FailureType
    {
        EngineIFSD,            // Falha crítica de motor / apagamento em voo (In-Flight Shutdown)
        AlternatorFailure,     // Falha do alternador / perda de geração elétrica principal
        VacuumFailure,         // Falha da bomba mecânica de vácuo (instrumentos giroscópicos)
        PitotBlockage,         // Bloqueio do tubo de pitot (gelo / insetos / contaminação)
        StaticPortBlockage,    // Bloqueio da tomada estática (altímetro / velocímetro / VSI)
        TireBlowout,           // Estouro de pneu (pouso ou rolagem de alta velocidade / aquaplanagem)
        FlapLock,              // Travamento dos hipersustentadores / flaps
        CarbIcing,             // Congelamento de carburador por ar úmido sem Carb Heat
        SparkPlugFouling,      // Encharcamento de velas por táxi prolongado com mistura rica
        CompressorStall,       // Stall de compressor por avanço brusco de potência com vento cruzado
        GearCollapse,          // Colapso estrutural de perna/tesoura de trem por carga lateral no toque
        CabinDepressurization, // Falha de pressurização / outflow valve em altitude
        MagnetoFailure         // Falha de ignição / pane em magneto (Esquerdo ou Direito)
    }

    public class FailureRecord
    {
        public FailureType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCritical { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SimEventName { get; set; } = string.Empty;
        /// <summary>Código do DefectCatalog quando o tipo sozinho não basta (ex.: lado do magneto).</summary>
        public string? DefectCode { get; set; }
    }

    public class WearAndFailureService
    {
        private readonly SimConnectService _simConnect;
        private readonly Random _random = new();

        private readonly HashSet<FailureType> _activeFailures = new();
        private readonly List<FailureRecord> _failureHistory = new();

        // Acumuladores de estresse ambiental e operacional
        private double _carbIceAccumulator = 0;
        private double _foulingAccumulator = 0;
        private double _lastThrottle = 0;
        private DateTime _lastThrottleCheck = DateTime.MinValue;
        private double _breakCheckTimerSeconds = 0;

        /// <summary>Desgaste extra (%/h) de um motor com pane crítica que segue funcionando: 15% -> 0% em ~15 min.</summary>
        public const double DamagedEngineWearRatePerHour = 60.0;

        public event Action<FailureRecord>? OnFailureTriggered;

        public IReadOnlyList<FailureRecord> ActiveFailures => _failureHistory.AsReadOnly();
        public bool HasFailures => _failureHistory.Count > 0;
        public string LatestFailureSummary => _failureHistory.LastOrDefault()?.Title ?? string.Empty;

        public WearAndFailureService(SimConnectService simConnect)
        {
            _simConnect = simConnect;
        }

        /// <summary>
        /// Marca uma pane persistente já aberta como ativa, sem disparar de novo.
        /// Evita sortear outra vez a mesma pane e reenviar um TOGGLE_* que a desligaria no simulador.
        /// </summary>
        public void MarkAlreadyActive(FailureType type)
        {
            _activeFailures.Add(type);
        }

        public void Reset()
        {
            _activeFailures.Clear();
            _failureHistory.Clear();
            _carbIceAccumulator = 0;
            _foulingAccumulator = 0;
            _lastThrottle = 0;
            _lastThrottleCheck = DateTime.MinValue;
            _breakCheckTimerSeconds = 0;
        }

        /// <summary>
        /// Calcula o multiplicador de probabilidade baseado na curva da banheira (Bathtub Curve):
        /// - Mortalidade infantil logo após intervenção/revisão (primeiras horas de operação)
        /// - Taxa base constante durante a vida útil normal (70% - 100%)
        /// - Degradação exponencial acelerada na fase de fim de vida (< 30%)
        /// </summary>
        public double CalculateDegradationMultiplier(double condition, PilotAircraft? aircraft = null)
        {
            double wearMultiplier;

            if (condition >= 70.0)
            {
                // Vida útil normal: Multiplicador suave de 1.0x a 1.5x
                wearMultiplier = 1.0 + ((100.0 - condition) / 30.0) * 0.5;
            }
            else if (condition >= 30.0)
            {
                // Desgaste moderado: Escala quadrática progressiva de 1.5x a 10.0x
                double factor = (70.0 - condition) / 40.0;
                wearMultiplier = 1.5 + (factor * factor) * 8.5;
            }
            else
            {
                // Zona crítica de fim de vida (Fadiga severa): Escala exponencial de 10.0x a 100.0x
                double factor = (30.0 - Math.Max(0, condition)) / 30.0;
                wearMultiplier = 10.0 + (factor * factor) * 90.0;
            }

            // Mortalidade Infantil (Bathtub Curve - Infant Mortality):
            // Em aeronaves recém-saídas de inspeção de 50h/100h ou overhaul geral recente,
            // peças recém-instaladas, abraçadeiras, anéis de vedação e relés novos
            // apresentam risco estatístico transitório de vícios de montagem/usinagem.
            double infantMultiplier = 1.0;
            if (aircraft != null)
            {
                bool recentlyMaintained50h = aircraft.HoursUntilNext50hInspection >= 48.0;
                bool recentlyMaintained100h = aircraft.HoursUntilNext100hInspection >= 96.0;
                if (recentlyMaintained50h || recentlyMaintained100h)
                {
                    infantMultiplier = 1.35; // +35% de sobretaxa por mortalidade infantil
                }
            }

            return wearMultiplier * infantMultiplier;
        }

        public void ProcessWear(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            // Proteção contra saltos temporais anormais (clamp entre 0 e ~18 segundos)
            deltaTimeHours = Math.Clamp(deltaTimeHours, 0.0, 0.005);
            if (deltaTimeHours <= 0) return;

            // 1. Desgaste do MOTOR (Taxa Base Green Arc + Fatores de Sobrecarga MSFS 2024)
            bool isJetOrTurbo = data.EngineType == 1.0 || data.EngineType == 5.0;
            double normalLifeHours = isJetOrTurbo ? 3500.0 : 1800.0;
            double engineWearRate = 100.0 / normalLifeHours;

            if (isJetOrTurbo)
            {
                if (data.TurboITT > 800.0)
                {
                    double ittExcess = data.TurboITT - 800.0;
                    engineWearRate += ittExcess * 2.0;
                }

                if (data.EngineN1 > 100.0)
                {
                    engineWearRate += (data.EngineN1 - 100.0) * 10.0;
                }
            }
            else
            {
                // Over-RPM (Sobretaxa quadrática por excesso de giro)
                if (data.MaxRPM > 0 && data.EngineRPM > data.MaxRPM)
                {
                    double rpmExcess = data.EngineRPM - data.MaxRPM;
                    double rpmStress = 50.0 * Math.Pow(rpmExcess / 100.0, 2);
                    engineWearRate += rpmStress;
                }

                // CHT Overheat (Até 230°C: normal | 230-260°C: 2.5x | >260°C: severo 75 + 10x)
                if (data.EngineCHT > 260.0)
                {
                    engineWearRate += 75.0 + (data.EngineCHT - 260.0) * 10.0;
                }
                else if (data.EngineCHT > 230.0)
                {
                    engineWearRate += (data.EngineCHT - 230.0) * 2.5;
                }

                // Baixa Pressão de Óleo (Atrito seco severo quando motor ligado e pressão < 20 PSI)
                if (data.IsEngineRunning && data.EngineOilPressure < 20.0)
                {
                    double dryFrictionStress = 300.0 * ((20.0 - Math.Max(0, data.EngineOilPressure)) / 20.0);
                    engineWearRate += dryFrictionStress;
                }

                // Alta Temperatura de Óleo (> 110°C)
                if (data.EngineOilTemp > 110.0)
                {
                    engineWearRate += (data.EngineOilTemp - 110.0) * 5.0;
                }
            }

            // Motor com pane crítica que continua girando (pane ainda não derrubou o motor, ou o piloto o religou):
            // a saúde não salta para um valor fixo, ela cai rápido e de forma contínua enquanto ele funcionar.
            if (_activeFailures.Contains(FailureType.EngineIFSD) && data.IsEngineRunning)
            {
                engineWearRate += DamagedEngineWearRatePerHour;
            }

            aircraft.EngineCondition = Math.Max(0, aircraft.EngineCondition - (engineWearRate * deltaTimeHours));

            // Desgaste de Magnetos e Ignição (apenas para motores a pistão)
            if (!isJetOrTurbo)
            {
                double magnetoWearRate = 100.0 / 500.0; // 0.20% por hora nominal
                aircraft.HoursUntilNext500hInspection = Math.Max(-100.0, aircraft.HoursUntilNext500hInspection - deltaTimeHours);

                if (aircraft.HoursUntilNext500hInspection <= 0)
                {
                    magnetoWearRate += 1.5; // Sobretaxa por operar com magneto vencido
                }

                // Abuso de táxi (Spark Plug Fouling): mistura rica com baixo giro no solo
                if (data.IsOnGround && data.GroundSpeed < 25.0 && data.IsEngineRunning && data.EngineRPM < 1200.0 && data.MixturePercent > 80.0)
                {
                    magnetoWearRate += 7.5; // Penaliza táxi longo com mistura rica
                }

                aircraft.MagnetosCondition = Math.Max(0, aircraft.MagnetosCondition - (magnetoWearRate * deltaTimeHours));

                // Degradação do Óleo e Sistema de Lubrificação (Inspeção de 50h)
                double oilWearRate = 100.0 / 50.0; // 2.0%/hora nominal

                if (aircraft.HoursUntilNext50hInspection <= 0)
                {
                    oilWearRate += 5.0; // Óleo vencido acelera o envelhecimento
                }

                if (data.EngineOilTemp > 105.0)
                {
                    oilWearRate += (data.EngineOilTemp - 105.0) * 0.5; // Oxidação por calor
                }

                if (data.EngineCHT > 230.0)
                {
                    oilWearRate += (data.EngineCHT - 230.0) * 0.2; // Contaminação por blow-by
                }

                aircraft.OilCondition = Math.Max(0, aircraft.OilCondition - (oilWearRate * deltaTimeHours));
            }

            // 2. Desgaste da FUSELAGEM
            double airframeWearRate = 100.0 / 400.0;

            double gForce = Math.Abs(data.GForce);
            if (data.GForce > 3.8 || data.GForce < -1.5)
            {
                airframeWearRate += (gForce * 50.0);
            }

            if (data.OverspeedWarning > 0.5)
            {
                airframeWearRate += 300.0;
            }

            // Gelo Estrutural acelera desgaste da célula
            if (data.StructuralIcePct > 10.0)
            {
                airframeWearRate += (data.StructuralIcePct * 0.5);
            }

            // CRASH: Detectado via evento de sistema "Crashed" do SimConnect com impacto real
            bool isRealCrash = _simConnect.IsCrashed && (!data.IsOnGround || data.GroundSpeed > 45.0 || Math.Abs(data.GForce) > 4.5 || Math.Abs(data.VerticalSpeed) > 2500.0);

            if (isRealCrash)
            {
                aircraft.AirframeCondition = 0;
                aircraft.EngineCondition = 0;
                aircraft.TiresCondition = 0;
                aircraft.AvionicsCondition = 0;
                airframeWearRate = 0;
            }
            else
            {
                aircraft.AirframeCondition = Math.Max(0, aircraft.AirframeCondition - (airframeWearRate * deltaTimeHours));
            }

            // 3. Desgaste dos PNEUS
            if (data.OnGround > 0.5)
            {
                double tireWearRate = 0.1;
                if (data.GroundSpeed > 25) tireWearRate += (data.GroundSpeed - 25) * 0.5;
                double avgBrake = (data.BrakeLeftPosition + data.BrakeRightPosition) / 2.0;
                double normalizedBrake = avgBrake > 1.0 ? (avgBrake / 32767.0) : avgBrake;
                if (normalizedBrake > 0.05) tireWearRate += normalizedBrake * 50.0;

                // Pista com água/gelo aumenta atrito dinâmico irregular em frenagens
                if (data.IsWetOrContaminatedSurface && normalizedBrake > 0.1)
                {
                    tireWearRate *= 1.8;
                }

                aircraft.TiresCondition = Math.Max(0, aircraft.TiresCondition - (tireWearRate * deltaTimeHours));
            }

            // 4. Desgaste dos AVIÔNICOS
            double avionicsWearRate = 0.2; // Base de ~0.2% de desgaste por hora de voo
            if (_activeFailures.Contains(FailureType.AlternatorFailure))
            {
                avionicsWearRate += 4.5; // Sobretensão transitória e estresse no barramento elétrico
            }
            if (data.GForce > 3.0 || data.GForce < -1.0)
            {
                avionicsWearRate += 1.0; // Vibrações severas sobre suportes e giroscópios
            }
            aircraft.AvionicsCondition = Math.Max(0, aircraft.AvionicsCondition - (avionicsWearRate * deltaTimeHours));

            // 5. Checagens probabilísticas contínuas e contextuais de falhas
            CheckEngineFailures(aircraft, data, deltaTimeHours);
            CheckSystemFailures(aircraft, data, deltaTimeHours);
            CheckTireRollFailures(aircraft, data, deltaTimeHours);

            // Atualizar rastro de manete para detecção de slam throttle
            _lastThrottle = data.ThrottlePercent;
            _lastThrottleCheck = DateTime.Now;
        }

        public void ProcessLanding(PilotAircraft aircraft, double touchdownFPM, double lateralSpeedFPS = 0)
        {
            aircraft.LandingsCount++;
            double safeFpm = Math.Abs(touchdownFPM);
            double lateralStress = Math.Abs(lateralSpeedFPS);
            double tireWear = 100.0 / 30.0;

            if (safeFpm > 400)
            {
                double extraTireDamage = ((safeFpm - 400) / 100.0) * 5.0;
                tireWear += extraTireDamage;
                double airframeDamage = ((safeFpm - 400) / 100.0) * 2.0;
                aircraft.AirframeCondition = Math.Max(0, aircraft.AirframeCondition - airframeDamage);
            }

            // Estresse de carga lateral (toque caranguejado com vento cruzado)
            if (lateralStress > 5.0)
            {
                double sideDamage = (lateralStress - 5.0) * 1.5;
                tireWear += sideDamage;
                aircraft.AirframeCondition = Math.Max(0, aircraft.AirframeCondition - (sideDamage * 0.8));
            }

            aircraft.TiresCondition = Math.Max(0, aircraft.TiresCondition - tireWear);

            // Avaliar risco de estouro de pneu no pouso e colapso de perna de trem
            CheckTireLandingFailure(aircraft, safeFpm, lateralStress);
            CheckGearCollapseFailure(aircraft, safeFpm, lateralStress);
        }

        public void ProcessLanding(PilotAircraft aircraft, SimTelemetry data)
        {
            double touchdownFPM = Math.Abs(data.VelocityBodyY) * 60.0;
            ProcessLanding(aircraft, touchdownFPM, data.VelocityBodyX);
        }

        /// <summary>
        /// Avalia falhas de propulsão:
        /// - IFSD Geral (Bathtub Curve)
        /// - Congelamento de Carburador (Carb Icing em Pistão)
        /// - Encharcamento de Velas (Spark Plug Fouling em Pistão)
        /// - Stall de Compressor (Surge em Jatos/Turboprops)
        /// </summary>
        private void CheckEngineFailures(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            if (!data.IsEngineRunning) return;

            bool isJetOrTurbo = data.EngineType == 1.0 || data.EngineType == 5.0;

            // --- A. LÓGICA ESPECÍFICA PARA MOTORES A PISTÃO ---
            if (!isJetOrTurbo)
            {
                // 1. Congelamento de Carburador (Carb Icing)
                // Ocorre com potência baixa (descida), ar úmido / precipitação ou temp entre 5°C e 22°C, sem Carb Heat
                bool icingVulnerable = data.IsPrecipitating || (data.AmbientTemperature >= 4.0 && data.AmbientTemperature <= 24.0);
                bool lowPower = data.ThrottlePercent < 35.0 && !data.IsOnGround;

                if (icingVulnerable && lowPower && !data.IsCarbHeatOn)
                {
                    _carbIceAccumulator += deltaTimeHours * 4.0; // Em ~15 min de descida desatenta atinge 1.0
                    if (_carbIceAccumulator >= 1.0 && !_activeFailures.Contains(FailureType.CarbIcing))
                    {
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.CarbIcing,
                            Title = "CONGELAMENTO DE CARBURADOR (CARB ICING)",
                            Description = "Queda térmica no venturi formou gelo obstrutivo por descida prolongada sem aquecimento de carburador (Carb Heat). O motor apagou por falta de ar de admissão.",
                            IsCritical = true,
                            SimEventName = "TOGGLE_ENGINE1_FAILURE"
                        });
                    }
                }
                else if (data.IsCarbHeatOn)
                {
                    _carbIceAccumulator = Math.Max(0, _carbIceAccumulator - (deltaTimeHours * 10.0));
                }

                // 2. Encharcamento de Velas (Spark Plug Fouling)
                // Ocorre em táxi no solo com motor frio e mistura 100% rica
                if (data.IsOnGround && data.MixturePercent > 90.0 && data.EngineRPM < 1200 && data.EngineCHT < 130.0)
                {
                    _foulingAccumulator += deltaTimeHours * 5.0;
                    if (_foulingAccumulator >= 1.0 && !_activeFailures.Contains(FailureType.SparkPlugFouling))
                    {
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.SparkPlugFouling,
                            Title = "ENCHARCAMENTO DE VELAS (LEAD FOULING)",
                            Description = "Depósito de chumbo e fuligem nos eletrodos das velas por táxi prolongado com mistura rica e motor frio. Motor apresentará queda brusca no cheque de magnetos.",
                            IsCritical = false,
                            SimEventName = string.Empty // Antes disparava TOGGLE_VACUUM_FAILURE por engano
                        });
                    }
                }
                else if (data.EngineRPM > 1600 && data.MixturePercent < 80.0)
                {
                    // Procedimento de limpeza de velas com mistura empobrecida
                    _foulingAccumulator = Math.Max(0, _foulingAccumulator - (deltaTimeHours * 15.0));
                }
            }
            // --- B. LÓGICA ESPECÍFICA PARA JATOS E TURBOÉLICES ---
            else
            {
                // Stall de Compressor (Engine Surge)
                // Avanço violento de manete na decolagem com vento de través cruzado forte (> 20 kts)
                if (!_activeFailures.Contains(FailureType.CompressorStall) && _lastThrottleCheck != DateTime.MinValue)
                {
                    double dtSeconds = (DateTime.Now - _lastThrottleCheck).TotalSeconds;
                    if (dtSeconds > 0 && dtSeconds < 2.0)
                    {
                        double throttleRate = (data.ThrottlePercent - _lastThrottle) / dtSeconds;
                        if (throttleRate > 45.0 && data.CrosswindComponentKnots > 20.0 && data.IsOnGround && data.GroundSpeed > 20.0)
                        {
                            double surgeChance = 0.35 * CalculateDegradationMultiplier(aircraft.EngineCondition, aircraft);
                            if (_random.NextDouble() < (surgeChance * 0.1))
                            {
                                TriggerFailure(new FailureRecord
                                {
                                    Type = FailureType.CompressorStall,
                                    Title = "STALL DE COMPRESSOR (ENGINE SURGE)",
                                    Description = $"Perda de sustentação aerodinâmica nas palhetas por avanço brusco de manete ({throttleRate:F0}%/s) sob vento cruzado de {data.CrosswindComponentKnots:F0} kts. Reduza a manete imediatamente.",
                                    IsCritical = true,
                                    SimEventName = "TOGGLE_ENGINE1_FAILURE"
                                });
                            }
                        }
                    }
                }
            }

            // --- C. NOVO SISTEMA DE BREAK CHECK (CICLO DE 60 SEGUNDOS) ---
            if (_activeFailures.Contains(FailureType.EngineIFSD)) return;

            // Acumular tempo decorrido enquanto o motor estiver em funcionamento
            _breakCheckTimerSeconds += deltaTimeHours * 3600.0;

            if (_breakCheckTimerSeconds >= 60.0)
            {
                _breakCheckTimerSeconds = 0.0;

                // Regra de Ouro: Risco ZERO absoluto se a saúde estiver acima de 20%
                if (aircraft.EngineCondition <= 20.0)
                {
                    double chance;
                    if (aircraft.EngineCondition <= 0.0)
                    {
                        chance = 1.00; // 100% quebra imediata
                    }
                    else if (aircraft.EngineCondition <= 10.0)
                    {
                        // Em 10.0% -> 0.50 (50%), em 0.0% -> 1.00 (100%)
                        chance = 0.50 + ((10.0 - aircraft.EngineCondition) / 10.0) * 0.50;
                    }
                    else // 10.0 < EngineCondition <= 20.0
                    {
                        // Em 20.0% -> 0.01 (1%), em 10.0% -> 0.50 (50%)
                        chance = 0.01 + ((20.0 - aircraft.EngineCondition) / 10.0) * 0.49;
                    }

                    if (_random.NextDouble() < chance)
                    {
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.EngineIFSD,
                            Title = "PANE MECÂNICA CRÍTICA DE MOTOR (BREAK CHECK)",
                            Description = $"Apagamento catastrófico por degradação mecânica severa ({aircraft.EngineCondition:F1}% de integridade). Motor sofreu colapso estrutural interno por fadiga crítica de componentes. Inicie checklist de emergência e procedimento de pouso forçado.",
                            IsCritical = true,
                            SimEventName = "TOGGLE_ENGINE1_FAILURE"
                        });
                    }
                }

                // Checagem de Falha de Lubrificação / Fusão de Bronzina (apenas para motores a pistão)
                if (!isJetOrTurbo && !_activeFailures.Contains(FailureType.EngineIFSD))
                {
                    if (aircraft.OilCondition <= 20.0)
                    {
                        double oilChance;
                        if (aircraft.OilCondition <= 0.0)
                        {
                            oilChance = 1.00;
                        }
                        else if (aircraft.OilCondition <= 10.0)
                        {
                            // Em 10.0% -> 0.50 (50%), em 0.0% -> 1.00 (100%)
                            oilChance = 0.50 + ((10.0 - aircraft.OilCondition) / 10.0) * 0.50;
                        }
                        else // 10.0 < OilCondition <= 20.0
                        {
                            // Em 20.0% -> 0.01 (1%), em 10.0% -> 0.50 (50%)
                            oilChance = 0.01 + ((20.0 - aircraft.OilCondition) / 10.0) * 0.49;
                        }

                        if (_random.NextDouble() < oilChance)
                        {
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.EngineIFSD,
                                Title = "PANE CRÍTICA DE MOTOR: FUSÃO DE BRONZINA (LUBRIFICAÇÃO)",
                                Description = $"Travamento mecânico por perda de viscosidade e colapso da película de óleo (Saúde do Óleo: {aircraft.OilCondition:F1}%). O motor fundiu bronzinas de biela e virabrequim.",
                                IsCritical = true,
                                SimEventName = "TOGGLE_ENGINE1_FAILURE"
                            });
                        }
                    }
                }

                // Checagem de Falha de Magnetos / Ignição (apenas para motores a pistão)
                if (!isJetOrTurbo && !_activeFailures.Contains(FailureType.MagnetoFailure))
                {
                    if (aircraft.MagnetosCondition <= 20.0)
                    {
                        double magChance;
                        if (aircraft.MagnetosCondition <= 0.0)
                        {
                            magChance = 1.00;
                        }
                        else if (aircraft.MagnetosCondition <= 10.0)
                        {
                            // Em 10.0% -> 0.50 (50%), em 0.0% -> 1.00 (100%)
                            magChance = 0.50 + ((10.0 - aircraft.MagnetosCondition) / 10.0) * 0.50;
                        }
                        else // 10.0 < MagnetosCondition <= 20.0
                        {
                            // Em 20.0% -> 0.01 (1%), em 10.0% -> 0.50 (50%)
                            magChance = 0.01 + ((20.0 - aircraft.MagnetosCondition) / 10.0) * 0.49;
                        }

                        if (_random.NextDouble() < magChance)
                        {
                            bool leftFailed = _random.Next(2) == 0;
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.MagnetoFailure,
                                Title = $"PANE DE IGNIÇÃO: MAGNETO {(leftFailed ? "ESQUERDO" : "DIREITO")}",
                                Description = $"Falha de ignição por desgaste severo (Saúde {aircraft.MagnetosCondition:F1}%). Queda imediata de rotação. Motor operando em contingência no magneto remanescente.",
                                IsCritical = false,
                                // Mantido pela vigilância contínua (PersistentFailureService), não por evento único
                                DefectCode = leftFailed ? FlightCareer.Domain.Services.DefectCatalog.MagnetoLeft : FlightCareer.Domain.Services.DefectCatalog.MagnetoRight
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Avalia falhas em sistemas secundários, aviônica, pressurização e gelo.
        /// </summary>
        private void CheckSystemFailures(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            // 1. Falha de Alternador / Gerador Elétrico Principal
            if (!_activeFailures.Contains(FailureType.AlternatorFailure) && data.IsEngineRunning)
            {
                double lambda = 0.0005 * CalculateDegradationMultiplier(aircraft.AirframeCondition, aircraft);
                if (_random.NextDouble() < (lambda * deltaTimeHours))
                {
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.AlternatorFailure,
                        Title = "FALHA ELÉTRICA (ALTERNADOR #1)",
                        Description = "Perda de geração elétrica por queima do diodo retificador ou sobretensão. Barramento transferido exclusivamente para a bateria.",
                        IsCritical = false,
                        SimEventName = string.Empty // TOGGLE_ALTERNATOR1_FAILURE não existe no MSFS: alternador mantido desligado pela vigilância
                    });
                }
            }

            // 2. Falha da Bomba de Vácuo (GA a pistão)
            if (!_activeFailures.Contains(FailureType.VacuumFailure) && data.IsEngineRunning && data.EngineType != 1.0)
            {
                double lambda = 0.0004 * CalculateDegradationMultiplier(aircraft.EngineCondition, aircraft);
                if (_random.NextDouble() < (lambda * deltaTimeHours))
                {
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.VacuumFailure,
                        Title = "PANE NA BOMBA DE VÁCUO",
                        Description = "Quebra do rotor de grafite da bomba de sucção mecânica. Horizonte artificial e giro direcional perderão gradualmente a confiabilidade.",
                        IsCritical = false,
                        SimEventName = "TOGGLE_VACUUM_FAILURE"
                    });
                }
            }

            // 3. Bloqueio de Tubo Pitot (Ambiental por gelo ou estocástico)
            if (!_activeFailures.Contains(FailureType.PitotBlockage) && !data.IsOnGround && data.Airspeed > 40)
            {
                bool severeIcing = data.PitotIcePct > 25.0;
                double lambda = severeIcing ? 0.5 : (0.0003 * CalculateDegradationMultiplier(aircraft.AirframeCondition, aircraft));

                if (_random.NextDouble() < (lambda * deltaTimeHours))
                {
                    string cause = severeIcing ? $"Acúmulo severo de gelo ({data.PitotIcePct:F0}%)" : "Obstrução da tomada de pressão dinâmica";
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.PitotBlockage,
                        Title = "BLOQUEIO DE TUBO PITOT",
                        Description = $"{cause}. Indicações de velocímetro (IAS) não confiáveis (Unreliable Airspeed).",
                        IsCritical = true,
                        SimEventName = "TOGGLE_PITOT_BLOCKAGE"
                    });
                }
            }

            // 4. Bloqueio da Tomada Estática
            if (!_activeFailures.Contains(FailureType.StaticPortBlockage) && !data.IsOnGround)
            {
                double lambda = 0.0002 * CalculateDegradationMultiplier(aircraft.AirframeCondition, aircraft);
                if (_random.NextDouble() < (lambda * deltaTimeHours))
                {
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.StaticPortBlockage,
                        Title = "BLOQUEIO DE TOMADA ESTÁTICA",
                        Description = "Obstrução da linha estática por condensação ou detritos. Altímetro, Velocímetro e VSI sujeitos a erro de indicação estática.",
                        IsCritical = false,
                        SimEventName = "TOGGLE_STATIC_PORT_BLOCKAGE"
                    });
                }
            }

            // 5. Travamento Mecânico de Flaps
            if (!_activeFailures.Contains(FailureType.FlapLock) && (data.FlapsPercent > 0 || !data.IsOnGround))
            {
                double lambda = 0.0002 * CalculateDegradationMultiplier(aircraft.AirframeCondition, aircraft);
                if (_random.NextDouble() < (lambda * deltaTimeHours))
                {
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.FlapLock,
                        Title = "TRAVAMENTO DE FLAPS",
                        Description = "Falha mecânica de fuso ou disjuntor atuado nos motores de flap. Hipersustentadores travados na posição.",
                        IsCritical = false,
                        SimEventName = string.Empty // Travado na posição atual pela vigilância contínua
                    });
                }
            }

            // 6. Despressurização de Cabine (Altitude > 12.000 ft e cabine subindo)
            if (!_activeFailures.Contains(FailureType.CabinDepressurization) && !data.IsOnGround && data.Altitude > 12000.0 && data.CabinAltitude > 10000.0)
            {
                if (aircraft.AirframeCondition < 65.0)
                {
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.CabinDepressurization,
                        Title = "DESPRESSURIZAÇÃO DA CABINE",
                        Description = $"Altitude de cabine anormal ({data.CabinAltitude:F0} ft) por falha de vedação ou válvula outflow travada. Inicie descida imediata para o FL100.",
                        IsCritical = true,
                        SimEventName = string.Empty // Sem evento nativo de despressurização (antes usava o do alternador por engano)
                    });
                }
            }
        }

        /// <summary>
        /// Avalia risco de estouro de pneu durante a rolagem de pista em alta velocidade (> 40 kts).
        /// Em pista molhada (SURFACE CONDITION >= 1) com pneus gastos (< 60%), a probabilidade de
        /// aquaplanagem e descolamento de banda (reverted rubber blowout) escala significativamente!
        /// </summary>
        private void CheckTireRollFailures(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            if (_activeFailures.Contains(FailureType.TireBlowout)) return;
            if (data.OnGround <= 0.5 || data.GroundSpeed < 40.0) return;

            double lambda = 0.001 * CalculateDegradationMultiplier(aircraft.TiresCondition, aircraft);

            // Frenagem do piloto
            double avgBrake = (data.BrakeLeftPosition + data.BrakeRightPosition) / 2.0;
            double normalizedBrake = avgBrake > 1.0 ? (avgBrake / 32767.0) : avgBrake;
            if (normalizedBrake > 0.5) lambda *= 3.0;

            // FATOR DE PISTA MOLHADA / AQUAPLANAGEM
            if (data.IsWetOrContaminatedSurface)
            {
                // Se pneu estiver abaixo de 60% de banda, a profundidade dos sulcos é incapaz de evacuar água
                if (aircraft.TiresCondition < 60.0)
                {
                    double wetStressFactor = 1.0 + ((60.0 - aircraft.TiresCondition) / 20.0) * 4.0; // Até 9x mais risco
                    if (normalizedBrake > 0.2) wetStressFactor *= 2.5; // Fricção extrema sobre lâmina d'água
                    lambda *= wetStressFactor;
                }
            }

            if (_random.NextDouble() < (lambda * deltaTimeHours))
            {
                string cause = data.IsWetOrContaminatedSurface && aircraft.TiresCondition < 60
                    ? "Aquaplanagem dinâmica e deslizamento em pista molhada com sulcos gastos (Reverted Rubber)"
                    : "Desprendimento de banda de rodagem ou fusível térmico rompido por superaquecimento na corrida";

                TriggerFailure(new FailureRecord
                {
                    Type = FailureType.TireBlowout,
                    Title = "ESTOURO DE PNEU EM ROLAGEM",
                    Description = $"{cause}. Manobra de pista comprometida; mantenha o controle direcional com leme.",
                    IsCritical = true,
                    SimEventName = "TOGGLE_TIRE_FAILURE"
                });
            }
        }

        /// <summary>
        /// Avalia estouro de pneu no pouso (ciclo operacional).
        /// </summary>
        private void CheckTireLandingFailure(PilotAircraft aircraft, double touchdownFPM, double lateralStress)
        {
            if (_activeFailures.Contains(FailureType.TireBlowout)) return;

            double baseLandingChance = 0.0002; // 0.02% em 100%

            double impactFactor = 1.0;
            if (touchdownFPM > 350)
            {
                impactFactor = 1.0 + ((touchdownFPM - 350) / 100.0) * 1.8;
            }

            // Sobrecarga de desalinhamento lateral com a pista
            if (lateralStress > 5.0)
            {
                impactFactor *= (1.0 + (lateralStress - 5.0) * 0.35);
            }

            double conditionMultiplier = CalculateDegradationMultiplier(aircraft.TiresCondition, aircraft);
            double finalChance = Math.Min(0.85, baseLandingChance * conditionMultiplier * impactFactor);

            if (_random.NextDouble() < finalChance)
            {
                TriggerFailure(new FailureRecord
                {
                    Type = FailureType.TireBlowout,
                    Title = "ESTOURO DE PNEU NO POUSO (BLOWOUT)",
                    Description = $"Deslaminação de carcaça / rompimento de câmara de ar durante o toque com a pista ({touchdownFPM:F0} FPM). Mantenha o eixo e controle direcional.",
                    IsCritical = true,
                    SimEventName = "TOGGLE_TIRE_FAILURE"
                });
            }
        }

        /// <summary>
        /// Avalia colapso estrutural de perna / tesoura de amortecedor por carga lateral extrema no toque.
        /// </summary>
        private void CheckGearCollapseFailure(PilotAircraft aircraft, double touchdownFPM, double lateralStress)
        {
            if (_activeFailures.Contains(FailureType.GearCollapse)) return;

            // Só ocorre se houver carga lateral significativa (toque caranguejado desalinhado)
            if (lateralStress > 7.0 && (touchdownFPM > 300 || aircraft.AirframeCondition < 50))
            {
                double collapseChance = ((lateralStress - 7.0) / 10.0) * 0.15;
                if (aircraft.AirframeCondition < 40) collapseChance *= 3.0;

                if (_random.NextDouble() < Math.Min(0.70, collapseChance))
                {
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.GearCollapse,
                        Title = "COLAPSO ESTRUTURAL DE TREM DE POUSO",
                        Description = $"Fratura de pino mestre / tesoura de amortecedor por estresse lateral violento ({lateralStress:F1} fps laterais) no pouso com vento cruzado. A perna de trem recolheu no impacto.",
                        IsCritical = true,
                        SimEventName = "TOGGLE_TIRE_FAILURE"
                    });
                }
            }
        }

        public void TriggerFailure(FailureRecord record)
        {
            if (_activeFailures.Contains(record.Type)) return;

            _activeFailures.Add(record.Type);
            _failureHistory.Add(record);

            if (!string.IsNullOrEmpty(record.SimEventName))
            {
                _simConnect.SetFailure(record.SimEventName);
            }

            OnFailureTriggered?.Invoke(record);
        }

        public void TriggerManualFailure(FailureType type)
        {
            switch (type)
            {
                case FailureType.EngineIFSD:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.EngineIFSD,
                        Title = "PANE DE MOTOR #1 (IFSD)",
                        Description = "Falha crítica forçada via painel de testes/controle.",
                        IsCritical = true,
                        SimEventName = "TOGGLE_ENGINE1_FAILURE"
                    });
                    break;
                case FailureType.CarbIcing:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.CarbIcing,
                        Title = "CONGELAMENTO DE CARBURADOR",
                        Description = "Pane forçada de gelo no carburador.",
                        IsCritical = true,
                        SimEventName = "TOGGLE_ENGINE1_FAILURE"
                    });
                    break;
                case FailureType.CompressorStall:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.CompressorStall,
                        Title = "STALL DE COMPRESSOR",
                        Description = "Stall de compressor forçado.",
                        IsCritical = true,
                        SimEventName = "TOGGLE_ENGINE1_FAILURE"
                    });
                    break;
                case FailureType.TireBlowout:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.TireBlowout,
                        Title = "ESTOURO DE PNEU",
                        Description = "Estouro forçado via painel de testes/controle.",
                        IsCritical = true,
                        SimEventName = "TOGGLE_TIRE_FAILURE"
                    });
                    break;
                case FailureType.GearCollapse:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.GearCollapse,
                        Title = "COLAPSO DE TREM DE POUSO",
                        Description = "Colapso mecânico forçado da perna do trem.",
                        IsCritical = true,
                        SimEventName = "TOGGLE_TIRE_FAILURE"
                    });
                    break;
                case FailureType.AlternatorFailure:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.AlternatorFailure,
                        Title = "FALHA DE ALTERNADOR",
                        Description = "Falha elétrica forçada via painel de testes/controle.",
                        IsCritical = false,
                        SimEventName = string.Empty // TOGGLE_ALTERNATOR1_FAILURE não existe no MSFS: alternador mantido desligado pela vigilância
                    });
                    break;
                case FailureType.VacuumFailure:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.VacuumFailure,
                        Title = "PANE DE VÁCUO",
                        Description = "Falha da bomba mecânica forçada via painel de testes/controle.",
                        IsCritical = false,
                        SimEventName = "TOGGLE_VACUUM_FAILURE"
                    });
                    break;
                case FailureType.PitotBlockage:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.PitotBlockage,
                        Title = "BLOQUEIO DE TUBO PITOT",
                        Description = "Bloqueio forçado via painel de testes/controle.",
                        IsCritical = true,
                        SimEventName = "TOGGLE_PITOT_BLOCKAGE"
                    });
                    break;
                case FailureType.StaticPortBlockage:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.StaticPortBlockage,
                        Title = "BLOQUEIO DE TOMADA ESTÁTICA",
                        Description = "Bloqueio forçado via painel de testes/controle.",
                        IsCritical = false,
                        SimEventName = "TOGGLE_STATIC_PORT_BLOCKAGE"
                    });
                    break;
                case FailureType.FlapLock:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.FlapLock,
                        Title = "TRAVAMENTO DE FLAPS",
                        Description = "Falha mecânica de flap forçada via painel de testes/controle.",
                        IsCritical = false,
                        SimEventName = string.Empty // Travado na posição atual pela vigilância contínua
                    });
                    break;
                case FailureType.CabinDepressurization:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.CabinDepressurization,
                        Title = "DESPRESSURIZAÇÃO DA CABINE",
                        Description = "Despressurização forçada via painel de testes/controle.",
                        IsCritical = true,
                        SimEventName = string.Empty // Sem evento nativo de despressurização (antes usava o do alternador por engano)
                    });
                    break;
                case FailureType.MagnetoFailure:
                    bool leftFailed = _random.Next(2) == 0;
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.MagnetoFailure,
                        Title = $"PANE DE IGNIÇÃO: MAGNETO {(leftFailed ? "ESQUERDO" : "DIREITO")}",
                        Description = "Falha forçada de magneto/ignição via painel de testes/controle.",
                        IsCritical = false,
                        // Mantido pela vigilância contínua (PersistentFailureService), não por evento único
                                DefectCode = leftFailed ? FlightCareer.Domain.Services.DefectCatalog.MagnetoLeft : FlightCareer.Domain.Services.DefectCatalog.MagnetoRight
                    });
                    break;
            }
        }
    }
}
