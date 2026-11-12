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
        EngineFire,            // Incêndio de motor na partida por erro de procedimento e fadiga crítica
        AlternatorFailure,     // Falha do alternador / perda de geração elétrica principal
        VacuumFailure,         // Falha da bomba mecânica de vácuo (instrumentos giroscópicos)
        PitotBlockage,         // Bloqueio do tubo de pitot (gelo / insetos / contaminação)
        StaticPortBlockage,    // Bloqueio da tomada estática (altímetro / velocímetro / VSI)
        TireBlowout,           // Estouro de pneu (pouso ou rolagem de alta velocidade / aquaplanagem)
        FlapLock,              // Travamento dos hipersustentadores / flaps
        SparkPlugFouling,      // Encharcamento de velas por táxi prolongado com mistura rica
        GearCollapse,          // Colapso estrutural de perna/tesoura de trem por carga lateral no toque
        CabinDepressurization, // Falha de pressurização / outflow valve em altitude
        MagnetoFailure,        // Falha de ignição / pane em magneto (Esquerdo ou Direito)
        CarbIcing,             // Congelamento severo de carburador por negligência de Carb Heat em descida
        OilLeak                // Vazamento progressivo de óleo (GENERAL ENG OIL LEAKED PERCENT:1)
    }

    public class FailureRecord
    {
        public FailureType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCritical { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SimEventName { get; set; } = string.Empty;
    }

    public class WearAndFailureService
    {
        private readonly SimConnectService _simConnect;
        private readonly Random _random = new();

        private readonly HashSet<FailureType> _activeFailures = new();
        private readonly List<FailureRecord> _failureHistory = new();

        // Acumuladores de estresse ambiental e operacional
        private double _foulingAccumulator = 0;
        private double _breakCheckTimerSeconds = 0;
        private double _carbIceTimerSeconds = 0;
        private double _primeTimerSeconds = 0;
        private double _pistonFireTimerSeconds = 0;
        private double _turboFireTimerSeconds = 0;
        private double _starterStuckTimerSeconds = 0;

        // Estado do Ciclo de Incêndio Progressivo (ENG ON FIRE:1)
        private bool _isEngineOnFire = false;
        private double _activeFireBurnSeconds = 0;
        private double _fireCoolingTimerSeconds = 0;

        // Estado do Sistema de Vazamento Progressivo de Óleo (GENERAL ENG OIL LEAKED PERCENT:1)
        private double _oilLeakedPercent = 0;
        private double _lastTransmittedOilLeak = 0;
        private double _activeOilLeakRatePctPerMin = 0;
        private double _oilOverheatTimerSeconds = 0;
        private double _postMaintFlightMinutes = 0;
        private bool _flightStartOilChecksDone = false;

        // Estado de falhas sem evento nativo no SimConnect (mantidas por enforcement a cada tick)
        private bool _engineDestroyedByFire = false;
        private bool _failedMagnetoIsLeft = false;
        private double _lockedFlapsHandle = 0.0;
        private bool _observedPressurization = false;
        private DateTime _lastEnforcementEvent = DateTime.MinValue;
        private SimTelemetry _lastData;

        public event Action<FailureRecord>? OnFailureTriggered;

        public IReadOnlyList<FailureRecord> ActiveFailures => _failureHistory.AsReadOnly();
        public bool HasFailures => _failureHistory.Count > 0;
        public string LatestFailureSummary => _failureHistory.LastOrDefault()?.Title ?? string.Empty;
        public bool IsEngineOnFire => _isEngineOnFire;
        public double OilLeakedPercent => _oilLeakedPercent;

        public WearAndFailureService(SimConnectService simConnect)
        {
            _simConnect = simConnect;
        }

        /// <summary>
        /// Zera o estado interno de falhas. Com clearSimState = true também desfaz no simulador
        /// as falhas aplicadas (toggles, fogo e vazamento de óleo), deixando app e MSFS consistentes.
        /// </summary>
        public void Reset(bool clearSimState = false)
        {
            if (clearSimState)
            {
                _simConnect.ClearSimFailures();
                if (_isEngineOnFire || _simConnect.CurrentData.IsEngineOnFire) _simConnect.SetEngineFire(false);
                if (_oilLeakedPercent > 0 || _simConnect.CurrentData.OilLeakedPercent > 0) _simConnect.SetOilLeakedPercent(0.0);
            }

            _engineDestroyedByFire = false;
            _failedMagnetoIsLeft = false;
            _lockedFlapsHandle = 0.0;
            _observedPressurization = false;
            _lastEnforcementEvent = DateTime.MinValue;

            _activeFailures.Clear();
            _failureHistory.Clear();
            _foulingAccumulator = 0;
            _breakCheckTimerSeconds = 0;
            _carbIceTimerSeconds = 0;
            _primeTimerSeconds = 0;
            _pistonFireTimerSeconds = 0;
            _turboFireTimerSeconds = 0;
            _starterStuckTimerSeconds = 0;
            _isEngineOnFire = false;
            _activeFireBurnSeconds = 0;
            _fireCoolingTimerSeconds = 0;
            _oilLeakedPercent = 0;
            _lastTransmittedOilLeak = 0;
            _activeOilLeakRatePctPerMin = 0;
            _oilOverheatTimerSeconds = 0;
            _postMaintFlightMinutes = 0;
            _flightStartOilChecksDone = false;
        }

        /// <summary>
        /// Converte uma chance por segundo de exposição na chance acumulada no intervalo dt,
        /// tornando o sorteio independente da frequência de pacotes de telemetria.
        /// </summary>
        private static double ChanceOverInterval(double chancePerSecond, double dtSeconds)
        {
            if (chancePerSecond <= 0.0 || dtSeconds <= 0.0) return 0.0;
            return 1.0 - Math.Pow(1.0 - Math.Min(chancePerSecond, 1.0), dtSeconds);
        }

        private static double NormalizeFlapsHandle(double handle) => handle > 1.0 ? handle / 100.0 : handle;

        private double CalculateBreakCheckChance(double condition)
        {
            if (condition > 20.0) return 0.0;
            if (condition <= 0.0) return 1.00;
            if (condition <= 10.0) return 0.50 + ((10.0 - condition) / 10.0) * 0.50;
            return 0.01 + ((20.0 - condition) / 10.0) * 0.49;
        }

        /// <summary>
        /// Calcula a probabilidade estocástica de incêndio baseada em estatísticas reais da FAA/NTSB
        /// (apenas ~3% a 5% das falhas críticas de motor chegam a produzir chamas ativas).
        /// Risco ZERO se a condição estiver acima de 20%.
        /// Em 20%: ~0.05%
        /// Em 10%: ~2.5%
        /// Em 0%: ~5.0% (teto máx de 10% em abusos extremos com óleo/combustível).
        /// </summary>
        private double CalculateFireChance(double condition, double riskMultiplier = 1.0)
        {
            if (condition > 20.0) return 0.0;
            double baseBreakChance = CalculateBreakCheckChance(condition);
            double fireProbability = baseBreakChance * 0.05 * riskMultiplier;
            return Math.Min(0.10, fireProbability);
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
            _lastData = data;

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

                // Sobretorque mecânico (Estresse na caixa de redução e palhetas por torque acima de 100%)
                if (data.TurboTorque > 100.0)
                {
                    double torqueExcess = data.TurboTorque - 100.0;
                    engineWearRate += 25.0 * Math.Pow(torqueExcess / 5.0, 2);
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

            aircraft.EngineCondition = Math.Max(0, aircraft.EngineCondition - (engineWearRate * deltaTimeHours));

            // Desgaste de Magnetos e Ignição (apenas para motores a pistão)
            if (!isJetOrTurbo)
            {
                double magnetoWearRate = 100.0 / 500.0; // 0.20% por hora nominal
                // As horas da inspeção de 500h são descontadas apenas no fechamento do voo (FlightService),
                // junto com 50h/100h/TBO; descontar aqui também contava as horas em dobro.

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

            // Cenário C (Sub-gatilho 1): Travamento de Relé / Solenoide do Starter Colado com Motor Ligado
            bool isEngineSpunUp = data.EngineRPM > 800.0 || data.EngineN1 > 40.0;
            if (data.IsEngineRunning && data.StarterActive > 0.5 && isEngineSpunUp)
            {
                _starterStuckTimerSeconds += deltaTimeHours * 3600.0;
            }
            else
            {
                _starterStuckTimerSeconds = Math.Max(0, _starterStuckTimerSeconds - (deltaTimeHours * 3600.0 * 0.5));
            }

            if (_starterStuckTimerSeconds >= 8.0)
            {
                if (_random.NextDouble() < CalculateFireChance(aircraft.AvionicsCondition, 2.0) && !_activeFailures.Contains(FailureType.EngineFire))
                {
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.EngineFire,
                        Title = "INCÊNDIO ELÉTRICO: TRAVAMENTO DE RELÉ DO STARTER",
                        Description = "Motor de arranque permaneceu acoplado ao motor ligado. Sobreaquecimento extremo do induzido e cabos de alta amperagem incendiou a fiação do cofre. Corte combustível e bomba imediatamente!",
                        IsCritical = true,
                        SimEventName = ""
                    });
                }
                _starterStuckTimerSeconds = 0;
            }

            // 5. Ciclo de Incêndio Progressivo (ENG ON FIRE:1) e Vazamento de Óleo (GENERAL ENG OIL LEAKED PERCENT:1)
            ProcessActiveEngineFire(aircraft, data, deltaTimeHours);
            ProcessOilLeakSystem(aircraft, data, deltaTimeHours);

            // 6. Checagens probabilísticas contínuas e contextuais de falhas
            if (data.IsOnGround)
            {
                CheckStartupFailures(aircraft, data, deltaTimeHours);
            }
            if (!data.IsOnGround && data.Altitude > 8000.0 && data.CabinAltitude < data.Altitude - 3000.0)
            {
                _observedPressurization = true; // Cabine bem abaixo da altitude real: aeronave pressurizada
            }
            CheckEngineFailures(aircraft, data, deltaTimeHours);
            CheckSystemFailures(aircraft, data, deltaTimeHours);
            CheckTireRollFailures(aircraft, data, deltaTimeHours);
            CheckBirdStrikeFailure(aircraft, data, deltaTimeHours);

            // 7. Mantém no simulador as falhas que não têm evento nativo de SimConnect
            EnforceActiveFailures(data);
        }

        /// <summary>
        /// Falhas sem evento nativo de falha no SimConnect são mantidas forçando o estado das chaves/comandos:
        /// - Magneto: o piloto não pode "consertar" girando a chave de volta para BOTH.
        /// - Alternador: a chave ALT é mantida desligada.
        /// - Flaps: a alavanca é mantida na posição em que travou.
        /// </summary>
        private void EnforceActiveFailures(SimTelemetry data)
        {
            if ((DateTime.Now - _lastEnforcementEvent).TotalSeconds < 2.0) return; // Aguarda o simulador refletir o último comando

            if (_activeFailures.Contains(FailureType.MagnetoFailure) && data.IsEngineRunning && data.StarterActive <= 0.5)
            {
                bool failedOn = _failedMagnetoIsLeft ? data.LeftMagneto > 0.5 : data.RightMagneto > 0.5;
                bool goodOn = _failedMagnetoIsLeft ? data.RightMagneto > 0.5 : data.LeftMagneto > 0.5;
                if (failedOn && goodOn)
                {
                    _simConnect.SendEvent(_failedMagnetoIsLeft ? "MAGNETO1_RIGHT" : "MAGNETO1_LEFT");
                    _lastEnforcementEvent = DateTime.Now;
                    return;
                }
                if (failedOn)
                {
                    _simConnect.SendEvent("MAGNETO1_OFF"); // Só o magneto em pane selecionado: motor sem ignição
                    _lastEnforcementEvent = DateTime.Now;
                    return;
                }
            }

            if (_activeFailures.Contains(FailureType.AlternatorFailure) && data.MasterAlternator > 0.5)
            {
                _simConnect.SendEvent("TOGGLE_ALTERNATOR1");
                _lastEnforcementEvent = DateTime.Now;
                return;
            }

            if (_activeFailures.Contains(FailureType.FlapLock))
            {
                double current = NormalizeFlapsHandle(data.FlapsHandlePercent);
                if (Math.Abs(current - _lockedFlapsHandle) > 0.02)
                {
                    _simConnect.SendEvent("FLAPS_SET", (uint)Math.Round(Math.Clamp(_lockedFlapsHandle, 0.0, 1.0) * 16383.0));
                    _lastEnforcementEvent = DateTime.Now;
                }
            }
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

            // Avaliar risco de estouro de pneu no pouso, colapso de perna de trem e trinca de cárter/retentor (pouso duro)
            CheckTireLandingFailure(aircraft, safeFpm, lateralStress);
            CheckGearCollapseFailure(aircraft, safeFpm, lateralStress);
            CheckHardLandingOilLeakFailure(aircraft, safeFpm);
        }

        public void ProcessLanding(PilotAircraft aircraft, SimTelemetry data)
        {
            double touchdownFPM = Math.Abs(data.VelocityBodyY) * 60.0;
            ProcessLanding(aircraft, touchdownFPM, data.VelocityBodyX);
        }

        /// <summary>
        /// Gerencia o ciclo de vida realista do fogo no motor (ENG ON FIRE:1 no MSFS 2024):
        /// - Fogo != Motor Apagado Automaticamente: O motor continua girando até o piloto cortar combustível
        ///   ou até o fogo derreter o bloco após 90 segundos (janela de 60 a 120s).
        /// - Se o piloto cortar combustível (Mistura/Condition Lever < 5% e Fuel Pump desligada),
        ///   após 25 segundos de resfriamento pelo vento relativo, seta ENG ON FIRE:1 = 0 (fogo extinto),
        ///   salvando a aeronave de perda total.
        /// </summary>
        private void ProcessActiveEngineFire(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            if (_engineDestroyedByFire) return;
            if (!_isEngineOnFire && !_activeFailures.Contains(FailureType.EngineFire)) return;

            double dtSeconds = deltaTimeHours * 3600.0;
            bool isJetOrTurbo = data.EngineType == 1.0 || data.EngineType == 5.0;

            // Verifica se o piloto executou o Memory Item de Corte de Combustível e Bomba
            bool fuelCutOff = (isJetOrTurbo ? data.ConditionLever < 5.0 : data.MixturePercent < 5.0) && data.FuelPumpSwitch <= 0.5;

            if (fuelCutOff)
            {
                _fireCoolingTimerSeconds += dtSeconds;
                // Dano residual reduzido enquanto as chamas perdem alimentação
                aircraft.EngineCondition = Math.Max(1.0, aircraft.EngineCondition - (0.15 * dtSeconds));

                // Após ~25 segundos de resfriamento pelo vento relativo sem combustível, o fogo se extingue
                if (_fireCoolingTimerSeconds >= 25.0)
                {
                    _isEngineOnFire = false;
                    _activeFireBurnSeconds = 0;
                    _fireCoolingTimerSeconds = 0;
                    _activeFailures.Remove(FailureType.EngineFire);
                    _simConnect.SetEngineFire(false); // ENG ON FIRE:1 = 0

                    var resolvedRecord = new FailureRecord
                    {
                        Type = FailureType.EngineFire,
                        Title = "INCÊNDIO EXTINTO COM SUCESSO (CORTE DE COMBUSTÍVEL)",
                        Description = $"O corte de combustível e o resfriamento pelo vento relativo extinguiram as chamas no motor #1! Aeronave salva de perda total (Integridade preservada: {aircraft.EngineCondition:F1}%).",
                        IsCritical = false,
                        SimEventName = ""
                    };
                    _failureHistory.Add(resolvedRecord);
                    OnFailureTriggered?.Invoke(resolvedRecord);
                }
            }
            else
            {
                // Piloto não cortou o combustível: o fogo consome o motor rapidamente
                _fireCoolingTimerSeconds = Math.Max(0, _fireCoolingTimerSeconds - (dtSeconds * 0.5));
                _activeFireBurnSeconds += dtSeconds;
                aircraft.EngineCondition = Math.Max(0, aircraft.EngineCondition - (0.80 * dtSeconds));

                // Garante que a SimVar ENG ON FIRE:1 permaneça ativa no simulador
                if (!data.IsEngineOnFire)
                {
                    _simConnect.SetEngineFire(true);
                }

                // Se passar de 90 segundos (janela de 60-120s) sem corte de combustível: destruição total por derretimento
                if (_activeFireBurnSeconds >= 90.0)
                {
                    // O fogo consome o motor e se extingue; o ciclo de fogo é encerrado de vez
                    // (antes continuava forçando ENG ON FIRE e podia anunciar "fogo extinto com sucesso").
                    _isEngineOnFire = false;
                    _engineDestroyedByFire = true;
                    _simConnect.SetEngineFire(false);
                    aircraft.EngineCondition = 0.0; // Penalidade máxima de reparo no SQLite

                    if (!_activeFailures.Contains(FailureType.EngineIFSD))
                    {
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.EngineIFSD,
                            Title = "COLAPSO CATASTRÓFICO: MOTOR DESTRUÍDO PELO FOGO",
                            Description = "O incêndio no motor #1 não foi contido dentro de 90 segundos (combustível não cortado). O fogo derreteu o bloco/seção quente causando perda total do motor (0%) e se extinguiu após consumir o compartimento.",
                            IsCritical = true,
                            SimEventName = "TOGGLE_ENGINE1_FAILURE"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Gerencia o sistema progressivo de vazamento de óleo via SimVar GENERAL ENG OIL LEAKED PERCENT:1 (0 a 100%):
        /// - Gatilhos Operacionais (por tick): Acelerar com Óleo Frio (< 38°C), Superaquecimento Crítico Prolongado,
        ///   Sobrevelocidade (Over-RPM) / Excesso de Torque.
        /// - Gatilhos de Manutenção/Ambientais: Tampa/Vareta Solta no Walkaround, Detritos na Pista (FOD),
        ///   Filtro Mal Apertado Pós-Revisão (primeiros 30 min), Fadiga de Mangueiras Aeroquip e Válvula Reguladora.
        /// </summary>
        private void ProcessOilLeakSystem(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            double dtSeconds = deltaTimeHours * 3600.0;
            double dtMinutes = dtSeconds / 60.0;
            bool isJetOrTurbo = data.EngineType == 1.0 || data.EngineType == 5.0;

            // Sincroniza se o próprio MSFS já tiver reportado um percentual maior de vazamento
            if (data.OilLeakedPercent > _oilLeakedPercent)
            {
                _oilLeakedPercent = data.OilLeakedPercent;
            }

            if (data.IsEngineRunning)
            {
                _postMaintFlightMinutes += dtMinutes;

                // 1. Acelerar com Óleo Frio (Cold Oil Overpressure):
                // Aplicar potência alta (> 1500 RPM ou decolar) enquanto GENERAL ENG OIL TEMPERATURE:1 < 38°C (100°F)
                bool isColdOilHighPower = data.EngineOilTemp < 38.0 && (data.EngineRPM > 1500.0 || !data.IsOnGround || data.ThrottlePercent > 65.0);
                if (isColdOilHighPower)
                {
                    // Incrementa gradualmente por tick devido aos picos de pressão do óleo viscoso nas vedações
                    _oilLeakedPercent += 1.8 * dtMinutes;

                    // ATIVA O BREAKCHECK SE A SAÚDE DO ÓLEO ESTIVER ABAIXO DE 20%
                    if (aircraft.OilCondition <= 20.0 && !_activeFailures.Contains(FailureType.OilLeak))
                    {
                        double coldBreakChance = CalculateBreakCheckChance(aircraft.OilCondition);
                        if (_random.NextDouble() < coldBreakChance)
                        {
                            _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, 4.5);
                            _oilLeakedPercent = Math.Max(_oilLeakedPercent, 12.0);
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.OilLeak,
                                Title = "VAZAMENTO CRÍTICO DE ÓLEO: RUPTURA POR SOBREPRESSÃO A FRIO",
                                Description = $"Potência elevada aplicada com óleo frio ({data.EngineOilTemp:F1}°C / {data.EngineOilTempF:F0}°F) e saúde do óleo crítica ({aircraft.OilCondition:F1}%). O pico de pressão rompeu o radiador/vedação do filtro!",
                                IsCritical = true,
                                SimEventName = ""
                            });
                        }
                    }
                }

                // 2. Superaquecimento Crítico Prolongado (CHT > 245°C ou Oil Temp > 115°C):
                // O calor excessivo resseca e deforma juntas de borracha/silicone das tampas de válvulas e cárter
                if (data.EngineCHT > 245.0 || data.EngineOilTemp > 115.0)
                {
                    _oilOverheatTimerSeconds += dtSeconds;
                    if (_oilOverheatTimerSeconds >= 60.0)
                    {
                        _oilLeakedPercent += 1.2 * dtMinutes;
                        if (_oilLeakedPercent >= 8.0 && !_activeFailures.Contains(FailureType.OilLeak))
                        {
                            _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, 1.5);
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.OilLeak,
                                Title = "VAZAMENTO PROGRESSIVO DE ÓLEO (DEFORMAÇÃO TÉRMICA DE JUNTAS)",
                                Description = $"Superaquecimento prolongado (CHT: {data.EngineCHT:F0}°C / Óleo: {data.EngineOilTemp:F0}°C) ressecou e deformou as juntas de silicone do cárter. Pressão de óleo em queda progressiva.",
                                IsCritical = false,
                                SimEventName = ""
                            });
                        }
                    }
                }
                else
                {
                    _oilOverheatTimerSeconds = Math.Max(0, _oilOverheatTimerSeconds - (dtSeconds * 0.5));
                }

                // 3. Sobrevelocidade (Over-RPM) ou Excesso de Torque:
                // Aumenta drasticamente a pressão da bomba de óleo e vibração mecânica nas mangueiras de alta pressão
                bool isOverSpeedOrTorque = (data.MaxRPM > 0 && data.EngineRPM > data.MaxRPM * 1.02) || (isJetOrTurbo && data.TurboTorque > 102.0);
                if (isOverSpeedOrTorque)
                {
                    _oilLeakedPercent += 1.6 * dtMinutes;
                    if (_oilLeakedPercent >= 8.0 && !_activeFailures.Contains(FailureType.OilLeak))
                    {
                        _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, 1.8);
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.OilLeak,
                            Title = "VAZAMENTO DE ÓLEO: FISSURA POR EXCESSO DE ROTAÇÃO / TORQUE",
                            Description = "Excesso de RPM/Torque elevou a pressão da bomba de óleo e a vibração mecânica, fissurando mangueira flexível de alta pressão.",
                            IsCritical = false,
                            SimEventName = ""
                        });
                    }
                }

                // 4. Vareta de Óleo ou Tampa de Abastecimento Solta (Oil Cap Loose no Walkaround):
                // Checado uma única vez na decolagem; sopra óleo para fora a +0.5% a +1.0% por minuto
                if (!data.IsOnGround && !_flightStartOilChecksDone)
                {
                    _flightStartOilChecksDone = true;
                    // Se o óleo estiver desgastado/negligenciado (<= 20%), chance de falha humana na inspeção pré-voo
                    if (aircraft.OilCondition <= 20.0 && !_activeFailures.Contains(FailureType.OilLeak))
                    {
                        double capLooseChance = CalculateBreakCheckChance(aircraft.OilCondition) * 0.20;
                        if (_random.NextDouble() < capLooseChance)
                        {
                            double leakRate = 0.5 + (_random.NextDouble() * 0.5); // +0.5% a +1.0% por minuto
                            _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, leakRate);
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.OilLeak,
                                Title = "VAZAMENTO DE ÓLEO: TAMPA DE ABASTECIMENTO / VARETA SOLTA",
                                Description = $"Com a pressão interna do cárter em voo, o óleo está sendo soprado pelo bocal de abastecimento (+{leakRate:F1}%/min). Monitore a pressão e temperatura do óleo!",
                                IsCritical = false,
                                SimEventName = ""
                            });
                        }
                    }
                }

                // 5. Ingestão de Objeto Estranho / Detritos na Pista (FOD):
                // Pedras lançadas pela hélice/trem de pouso em rolagem de alta potência perfurando o radiador de óleo inferior
                if (data.IsOnGround && data.GroundSpeed > 25.0 && data.ThrottlePercent > 65.0 && !_activeFailures.Contains(FailureType.OilLeak))
                {
                    double fodLambda = data.IsWetOrContaminatedSurface ? 0.006 : 0.002;
                    if (_random.NextDouble() < (fodLambda * deltaTimeHours))
                    {
                        _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, 2.2);
                        _oilLeakedPercent = Math.Max(_oilLeakedPercent, 5.0);
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.OilLeak,
                            Title = "VAZAMENTO DE ÓLEO: RADIADOR PERFURADO POR DETRITO (FOD)",
                            Description = "Detrito/pedra lançada durante a corrida de pista perfurou a colmeia do radiador de óleo (Oil Cooler) na parte inferior do capô. Vazamento contínuo ativo!",
                            IsCritical = true,
                            SimEventName = ""
                        });
                    }
                }

                // Aplica a taxa de vazamento contínuo ativo (se houver furo/tampa solta/mangueira rompida)
                if (_activeOilLeakRatePctPerMin > 0)
                {
                    _oilLeakedPercent += _activeOilLeakRatePctPerMin * dtMinutes;
                }
            }

            // Clamp entre 0 e 100% e transmite ao MSFS 2024 via SimConnect
            _oilLeakedPercent = Math.Clamp(_oilLeakedPercent, 0.0, 100.0);
            if (_oilLeakedPercent > 0 && (Math.Abs(_oilLeakedPercent - _lastTransmittedOilLeak) >= 0.20 || _oilLeakedPercent >= 100.0))
            {
                _simConnect.SetOilLeakedPercent(_oilLeakedPercent);
                _lastTransmittedOilLeak = _oilLeakedPercent;
            }

            // À medida que o óleo vaza no simulador, a saúde/nível do sistema de lubrificação cai proporcionalmente,
            // ativando naturalmente o Break Check de 60s quando atingir <= 20% ( >= 80% de óleo vazado )
            if (_oilLeakedPercent > 0)
            {
                aircraft.OilCondition = Math.Min(aircraft.OilCondition, Math.Max(0.0, 100.0 - _oilLeakedPercent));
            }
        }

        /// <summary>
        /// Avalia falhas de propulsão:
        /// - Encharcamento de Velas (Spark Plug Fouling em Pistão)
        /// - Congelamento de Carburador (Carb Icing em Pistão)
        /// - Incêndio de Motor a Pistão em Voo por Superaquecimento e Óleo Quente
        /// - Incêndio de Turbina em Voo por Abuso de Torque/ITT ou Gelo sem Inertial Separator
        /// - Ciclo de Break Check de 60 Segundos (Motor IFSD, Óleo/Bronzina, Magnetos)
        /// </summary>
        private void CheckEngineFailures(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            if (!data.IsEngineRunning) return;

            bool isJetOrTurbo = data.EngineType == 1.0 || data.EngineType == 5.0;

            // --- A. LÓGICA ESPECÍFICA PARA MOTORES A PISTÃO ---
            if (!isJetOrTurbo)
            {
                // Encharcamento de Velas (Spark Plug Fouling)
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
                            SimEventName = ""
                        });
                    }
                }
                else if (data.EngineRPM > 1600 && data.MixturePercent < 80.0)
                {
                    // Procedimento de limpeza de velas com mistura empobrecida
                    _foulingAccumulator = Math.Max(0, _foulingAccumulator - (deltaTimeHours * 15.0));
                }

                // Congelamento Severo de Carburador (apenas para aeronaves carburadas com Carb Heat disponível no MSFS)
                if (data.CarbHeatAvailable > 0.5 && !data.IsOnGround)
                {
                    // Condição de gelo ativo reportada pelo MSFS: venturi <= 0°C, baixa potência (< 45%) e ar úmido
                    bool isIcingActive = data.CarbTemperature <= 0.0 && data.ThrottlePercent < 45.0 && 
                                         (data.IsPrecipitating || (data.AmbientTemperature >= -5.0 && data.AmbientTemperature <= 25.0));

                    if (isIcingActive && !data.IsCarbHeatOn)
                    {
                        _carbIceTimerSeconds += deltaTimeHours * 3600.0;

                        // Se o piloto ignorar o gelo por 90 segundos contínuos sem acionar o Carb Heat
                        if (_carbIceTimerSeconds >= 90.0 && !_activeFailures.Contains(FailureType.CarbIcing))
                        {
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.CarbIcing,
                                Title = "CONGELAMENTO SEVERO DE CARBURADOR",
                                Description = $"Apagamento por asfixia de ar: o carburador permaneceu congelado ({data.CarbTemperature:F1}°C) por mais de 90 segundos sem acionamento do Carb Heat durante descida. O motor apagou.",
                                IsCritical = true,
                                SimEventName = "TOGGLE_ENGINE1_FAILURE"
                            });
                        }
                    }
                    else if (data.IsCarbHeatOn || data.CarbTemperature > 2.0)
                    {
                        // Degelo acelerado ao aplicar Carb Heat ou restaurar potência
                        _carbIceTimerSeconds = Math.Max(0, _carbIceTimerSeconds - (deltaTimeHours * 3600.0 * 2.0));
                    }
                }

                // Superaquecimento Extremo + Óleo Quente em Voo (Incêndio no Motor a Pistão)
                if (!data.IsOnGround)
                {
                    if (data.EngineCHT > 245.0 && data.EngineOilTemp > 115.0)
                    {
                        _pistonFireTimerSeconds += deltaTimeHours * 3600.0;
                    }
                    else
                    {
                        _pistonFireTimerSeconds = Math.Max(0, _pistonFireTimerSeconds - (deltaTimeHours * 3600.0 * 0.5));
                    }

                    if (_pistonFireTimerSeconds >= 45.0)
                    {
                        double chance = CalculateFireChance(aircraft.EngineCondition, 1.2);
                        if (chance > 0.0 && _random.NextDouble() < chance && !_activeFailures.Contains(FailureType.EngineFire))
                        {
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.EngineFire,
                                Title = "INCÊNDIO NO MOTOR EM VOO (RUPTURA DE LINHA DE ÓLEO)",
                                Description = $"Superaquecimento severo continuado (CHT: {data.EngineCHT:F0}°C, Óleo: {data.EngineOilTemp:F0}°C). O estresse térmico rompeu mangueiras e retentores de óleo sobre o escapamento incandescente, gerando fogo em voo. Corte combustível imediatamente!",
                                IsCritical = true,
                                SimEventName = ""
                            });
                        }
                        _pistonFireTimerSeconds = 0;
                    }
                }
                else
                {
                    _pistonFireTimerSeconds = Math.Max(0, _pistonFireTimerSeconds - (deltaTimeHours * 3600.0 * 0.5));
                }
            }
            // --- B. LÓGICA ESPECÍFICA PARA TURBOÉLICES ---
            else if (data.EngineType == 5.0)
            {
                // Excesso de Torque/ITT em Subida ou Operação sem Inertial Separator no Gelo
                if (!data.IsOnGround)
                {
                    bool isClimbAbuse = data.VerticalSpeed > 500.0 && (data.TurboTorque > 102.0 || data.TurboITT > 820.0);
                    bool isInertSepAbuse = (data.IsPrecipitating || data.StructuralIcePct > 10.0) && !data.IsCarbHeatOn && (data.TurboTorque > 95.0 || data.TurboITT > 800.0);

                    if (isClimbAbuse || isInertSepAbuse)
                    {
                        _turboFireTimerSeconds += deltaTimeHours * 3600.0;
                    }
                    else
                    {
                        _turboFireTimerSeconds = Math.Max(0, _turboFireTimerSeconds - (deltaTimeHours * 3600.0 * 0.5));
                    }

                    if (_turboFireTimerSeconds >= 45.0)
                    {
                        double chance = CalculateFireChance(aircraft.EngineCondition, 1.2);
                        if (chance > 0.0 && _random.NextDouble() < chance && !_activeFailures.Contains(FailureType.EngineFire))
                        {
                            string cause = isClimbAbuse
                                ? $"Subida com sobretorque ({data.TurboTorque:F0}%) e ITT excessiva ({data.TurboITT:F0}°C)"
                                : "Operação em gelo sem acionamento do Inertial Separator";

                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.EngineFire,
                                Title = "INCÊNDIO DE TURBINA EM VOO (FALHA ESTRUTURAL DE NACELE)",
                                Description = $"{cause}. Colapso térmico-estrutural da câmara de combustão com vazamento e ignição de combustível Jet-A1 na nacele. Corte a Condition Lever imediatamente!",
                                IsCritical = true,
                                SimEventName = ""
                            });
                        }
                        _turboFireTimerSeconds = 0;
                    }
                }
                else
                {
                    _turboFireTimerSeconds = Math.Max(0, _turboFireTimerSeconds - (deltaTimeHours * 3600.0 * 0.5));
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
                double engineChance = CalculateBreakCheckChance(aircraft.EngineCondition);
                if (engineChance > 0.0 && _random.NextDouble() < engineChance)
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

                // Checagem de Falha de Lubrificação / Fusão de Bronzina (apenas para motores a pistão)
                if (!isJetOrTurbo && !_activeFailures.Contains(FailureType.EngineIFSD))
                {
                    double oilChance = CalculateBreakCheckChance(aircraft.OilCondition);
                    if (oilChance > 0.0 && _random.NextDouble() < oilChance)
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

                // Checagem de Falha de Magnetos / Ignição (apenas para motores a pistão)
                if (!isJetOrTurbo && !_activeFailures.Contains(FailureType.MagnetoFailure))
                {
                    double magChance = CalculateBreakCheckChance(aircraft.MagnetosCondition);
                    if (magChance > 0.0 && _random.NextDouble() < magChance)
                    {
                        bool leftFailed = _random.Next(2) == 0;
                        _failedMagnetoIsLeft = leftFailed;
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.MagnetoFailure,
                            Title = $"PANE DE IGNIÇÃO: MAGNETO {(leftFailed ? "ESQUERDO" : "DIREITO")}",
                            Description = $"Falha de ignição por desgaste severo (Saúde {aircraft.MagnetosCondition:F1}%). Queda imediata de rotação. Motor operando em contingência no magneto remanescente.",
                            IsCritical = false,
                            SimEventName = "" // Mantido por EnforceActiveFailures (chave não volta para BOTH)
                        });
                    }
                }

                // Gatilhos de Manutenção e Fadiga de Óleo (GENERAL ENG OIL LEAKED PERCENT:1):
                if (!_activeFailures.Contains(FailureType.OilLeak))
                {
                    // 1. Erro na Troca de Óleo / Filtro Mal Apertado (Pós-Manutenção - Primeiros 30 minutos após revisão de 50h/100h)
                    bool isJustInspected = aircraft.HoursUntilNext50hInspection >= 49.5 || aircraft.HoursUntilNext100hInspection >= 99.5;
                    if (isJustInspected && _postMaintFlightMinutes <= 30.0)
                    {
                        if (_random.NextDouble() < 0.004) // ~11% de chance cumulativa na janela de 30 minutos pós-revisão
                        {
                            _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, 2.0);
                            _oilLeakedPercent = Math.Max(_oilLeakedPercent, 5.0);
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.OilLeak,
                                Title = "VAZAMENTO PÓS-MANUTENÇÃO: FILTRO / ANEL O-RING MAL APERTADO",
                                Description = "Torque incorreto no filtro de óleo ou anel de vedação duplo após revisão recente. O óleo está vazando sob pressão (+2.0%/min). Alterne para pouso preventivo!",
                                IsCritical = true,
                                SimEventName = ""
                            });
                        }
                    }

                    // 2. Fadiga de Mangueiras Flexíveis (Aeroquip Hoses) e Juntas (Saúde do Motor <= 20%)
                    if (aircraft.EngineCondition <= 20.0 && !_activeFailures.Contains(FailureType.OilLeak))
                    {
                        double hoseChance = CalculateBreakCheckChance(aircraft.EngineCondition) * 0.25;
                        if (hoseChance > 0.0 && _random.NextDouble() < hoseChance)
                        {
                            _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, 2.5);
                            _oilLeakedPercent = Math.Max(_oilLeakedPercent, 8.0);
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.OilLeak,
                                Title = "VAZAMENTO DE ÓLEO: FADIGA DE MANGUEIRA FLEXÍVEL (AEROQUIP)",
                                Description = $"Fissura por vibração crônica na linha Aeroquip entre o bloco e o radiador de óleo (Motor: {aircraft.EngineCondition:F1}%). Queda progressiva de pressão de óleo!",
                                IsCritical = true,
                                SimEventName = ""
                            });
                        }
                    }

                    // 3. Falha na Válvula Reguladora de Pressão (Oil Pressure Relief Valve - Saúde do Óleo <= 20%)
                    if (aircraft.OilCondition <= 20.0 && !_activeFailures.Contains(FailureType.OilLeak))
                    {
                        double reliefValveChance = CalculateBreakCheckChance(aircraft.OilCondition) * 0.30;
                        if (reliefValveChance > 0.0 && _random.NextDouble() < reliefValveChance)
                        {
                            _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, 3.0);
                            _oilLeakedPercent = Math.Max(_oilLeakedPercent, 10.0);
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.OilLeak,
                                Title = "VAZAMENTO DE ÓLEO: VÁLVULA REGULADORA DE PRESSÃO TRAVADA",
                                Description = $"Borra e verniz de óleo vencido ({aircraft.OilCondition:F1}%) travaram a válvula de alívio fechada. A sobrepressão rompeu a vedação do circuito!",
                                IsCritical = true,
                                SimEventName = ""
                            });
                        }
                    }
                }

                // Cenário A: Ruptura de Linha de Combustível ou Bico Injetor Solto
                if (aircraft.EngineCondition <= 20.0 && !_activeFailures.Contains(FailureType.EngineFire))
                {
                    double fuelFireChance = CalculateFireChance(aircraft.EngineCondition, 1.0);
                    if (fuelFireChance > 0.0 && _random.NextDouble() < fuelFireChance)
                    {
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.EngineFire,
                            Title = "INCÊNDIO: RUPTURA DE LINHA DE COMBUSTÍVEL / BICO SOLTO",
                            Description = $"Vibração crônica por desgaste severo ({aircraft.EngineCondition:F1}%) soltou conexão de alta pressão. Combustível pulverizado entrou em ignição no cofre do motor. Corte combustível imediatamente!",
                            IsCritical = true,
                            SimEventName = ""
                        });
                    }
                }

                // Cenário B: Vazamento no Sistema de Escapamento (Exhaust Manifold Crack - Pistão)
                if (!isJetOrTurbo && aircraft.EngineCondition <= 20.0 && !_activeFailures.Contains(FailureType.EngineFire))
                {
                    double exhaustFireChance = CalculateFireChance(aircraft.EngineCondition, 0.8);
                    if (exhaustFireChance > 0.0 && _random.NextDouble() < exhaustFireChance)
                    {
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.EngineFire,
                            Title = "INCÊNDIO: RACHADURA NO COLETOR DE ESCAPAMENTO",
                            Description = "Fissura estrutural no escapamento soprou gases incandescentes a ~800°C sobre mangueiras de combustível e isolamento acústico do motor. Corte combustível imediatamente!",
                            IsCritical = true,
                            SimEventName = ""
                        });
                    }
                }

                // Cenário C (Sub-gatilho 2): Curto-Circuito no Chicote do Alternador
                if (aircraft.AvionicsCondition <= 20.0 && !_activeFailures.Contains(FailureType.EngineFire))
                {
                    double altHarnessChance = CalculateFireChance(aircraft.AvionicsCondition, 0.8);
                    if (altHarnessChance > 0.0 && _random.NextDouble() < altHarnessChance)
                    {
                        TriggerFailure(new FailureRecord
                        {
                            Type = FailureType.EngineFire,
                            Title = "INCÊNDIO ELÉTRICO: CURTO NO CHICOTE DO ALTERNADOR",
                            Description = $"Curto-circuito de alta corrente no chicote principal do alternador por degradação elétrica ({aircraft.AvionicsCondition:F1}%). Fiação incandescente incendiou o cofre. Corte combustível imediatamente!",
                            IsCritical = true,
                            SimEventName = ""
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Avalia falhas de partida (Erros de Procedimento no Solo):
        /// - Gatilho 1: Motor Afogado / Excesso de Prime com Backfire (Pistão)
        /// - Gatilho 2: Partida Quente / Hot Start (Turboélice)
        /// - Gatilho 3: Partida com Vento de Cauda Forte (Turboélice)
        /// Risco zero absoluto se a saúde do motor estiver acima de 20%.
        /// </summary>
        private void CheckStartupFailures(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            bool isJetOrTurbo = data.EngineType == 1.0 || data.EngineType == 5.0;

            // Gatilho 1: Motor Afogado / Excesso de Prime com Backfire (Pistão - !isJetOrTurbo)
            if (!isJetOrTurbo)
            {
                if (!data.IsEngineRunning)
                {
                    if (data.FuelPumpSwitch > 0.5 && data.MixturePercent > 80.0)
                    {
                        _primeTimerSeconds += deltaTimeHours * 3600.0;
                    }
                    else
                    {
                        _primeTimerSeconds = Math.Max(0, _primeTimerSeconds - (deltaTimeHours * 3600.0 * 0.5));
                    }

                    if (data.StarterActive > 0.5 && _primeTimerSeconds >= 6.0)
                    {
                        double backfireChance = ChanceOverInterval(CalculateFireChance(aircraft.EngineCondition, 1.2), deltaTimeHours * 3600.0);
                        if (backfireChance > 0.0 && _random.NextDouble() < backfireChance && !_activeFailures.Contains(FailureType.EngineFire))
                        {
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.EngineFire,
                                Title = "INCÊNDIO NO MOTOR (EXCESSO DE PRIME / RETORNO DE CHAMA)",
                                Description = "Combustível acumulado no coletor por bomba ligada com mistura rica causou backfire violento e incêndio no capô. Corte a mistura e desligue a bomba!",
                                IsCritical = true,
                                SimEventName = ""
                            });
                            _primeTimerSeconds = 0;
                        }
                    }
                }
                else
                {
                    _primeTimerSeconds = 0;
                }
            }
            // Gatilhos 2 e 3: Turboélice (EngineType == 5.0)
            else if (data.EngineType == 5.0)
            {
                // Gatilho 2: Partida Quente / Hot Start (Turboélice)
                if (data.StarterActive > 0.5 || (!data.IsEngineRunning && data.EngineN1 > 1.0))
                {
                    if ((data.ConditionLever > 10.0 && data.EngineN1 < 12.0) || data.TurboITT > 1000.0)
                    {
                        double hotStartChance = ChanceOverInterval(CalculateFireChance(aircraft.EngineCondition, 1.5), deltaTimeHours * 3600.0);
                        if (hotStartChance > 0.0 && _random.NextDouble() < hotStartChance && !_activeFailures.Contains(FailureType.EngineFire))
                        {
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.EngineFire,
                                Title = "INCÊNDIO DE TURBINA: HOT START SEVERO",
                                Description = $"Combustível introduzido prematuramente (N1: {data.EngineN1:F1}%) ou ITT acima de 1000°C. Falta de fluxo de ar causou fogo na seção quente. Corte a Condition Lever!",
                                IsCritical = true,
                                SimEventName = ""
                            });
                        }
                    }
                }

                // Gatilho 3: Partida com Vento de Cauda Forte (Turboélice)
                if (data.StarterActive > 0.5)
                {
                    if (data.TailwindComponentKnots > 10.0)
                    {
                        double tailwindChance = ChanceOverInterval(CalculateFireChance(aircraft.EngineCondition, 1.0), deltaTimeHours * 3600.0);
                        if (tailwindChance > 0.0 && _random.NextDouble() < tailwindChance && !_activeFailures.Contains(FailureType.EngineFire))
                        {
                            TriggerFailure(new FailureRecord
                            {
                                Type = FailureType.EngineFire,
                                Title = "INCÊNDIO POR VENTO DE CAUDA NA PARTIDA",
                                Description = $"Vento de cauda de {data.TailwindComponentKnots:F0} kts empurrou os gases de exaustão de volta para o interior da turbina, gerando incêndio interno na partida. Corte a Condition Lever!",
                                IsCritical = true,
                                SimEventName = ""
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
                        SimEventName = "" // Sem evento nativo; ALT de verdade é mantido desligado por EnforceActiveFailures
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
                        SimEventName = "" // Alavanca mantida na posição travada por EnforceActiveFailures
                    });
                }
            }

            // 6. Despressurização de Cabine (Altitude > 12.000 ft e cabine subindo)
            // Só faz sentido em aeronave pressurizada: em não pressurizada a altitude de cabine é igual à real
            if (_observedPressurization && !_activeFailures.Contains(FailureType.CabinDepressurization) && !data.IsOnGround && data.Altitude > 12000.0 && data.CabinAltitude > 10000.0)
            {
                if (aircraft.AirframeCondition < 65.0)
                {
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.CabinDepressurization,
                        Title = "DESPRESSURIZAÇÃO DA CABINE",
                        Description = $"Altitude de cabine anormal ({data.CabinAltitude:F0} ft) por falha de vedação ou válvula outflow travada. Inicie descida imediata para o FL100.",
                        IsCritical = true,
                        SimEventName = "" // Sem evento nativo; ALT de verdade é mantido desligado por EnforceActiveFailures
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
                    SimEventName = "" // Sem evento nativo de pneu/trem no SimConnect
                });
            }
        }

        /// <summary>
        /// Avalia risco de Bird Strike Severo em Baixa Altitude (Fator Ambiental Externo - Física Pura):
        /// Ingestão de ave de grande porte (> 2 kg, ex: urubu/ganso) em turboélices (EngineType == 5.0).
        /// Ocorre exclusivamente em voo em baixa altitude (< 3.500 ft) com velocidade ativa (> 75 kts).
        /// Independe da saúde do motor (a energia cinética massiva da ave rompe a carcaça e perfura linhas de combustível/óleo).
        /// Taxa probabilística calibrada (~1 evento a cada 600-800 horas de baixa altitude).
        /// </summary>
        private void CheckBirdStrikeFailure(PilotAircraft aircraft, SimTelemetry data, double deltaTimeHours)
        {
            if (_activeFailures.Contains(FailureType.EngineFire)) return;
            if (data.IsOnGround || !data.IsEngineRunning) return;
            if (data.EngineType != 5.0) return; // Exclusivo para turboélices
            if (data.Altitude > 3500.0 || data.Airspeed < 75.0) return;

            // A energia cinética da colisão escala quadraticamente com a velocidade (Ek = 1/2 * m * v^2)
            double speedFactor = Math.Pow(data.Airspeed / 100.0, 2);
            double lambda = 0.0012 * speedFactor; // ~1 evento a cada 600-800 horas de baixa altitude

            if (_random.NextDouble() < (lambda * deltaTimeHours))
            {
                TriggerFailure(new FailureRecord
                {
                    Type = FailureType.EngineFire,
                    Title = "INCÊNDIO DE TURBINA: BIRD STRIKE SEVERO (INGESTÃO)",
                    Description = $"Ingestão de ave de grande porte a {data.Airspeed:F0} kts em baixa altitude ({data.Altitude:F0} ft). O desbalanceamento dinâmico violento rompeu a carcaça do compressor e perfurou linhas de Jet-A1 e óleo, gerando incêndio imediato na nacele. Corte a Condition Lever!",
                    IsCritical = true,
                    SimEventName = ""
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
                    SimEventName = "" // Sem evento nativo de pneu/trem no SimConnect
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
                        SimEventName = "" // Sem evento nativo de pneu/trem no SimConnect
                    });
                }
            }
        }

        /// <summary>
        /// Avalia trinca de cárter ou rompimento do retentor frontal do virabrequim por Pouso Duro / Prop Strike (> 650 FPM).
        /// </summary>
        private void CheckHardLandingOilLeakFailure(PilotAircraft aircraft, double touchdownFPM)
        {
            if (_activeFailures.Contains(FailureType.OilLeak)) return;
            if (touchdownFPM < 650.0) return;

            double leakChance = Math.Min(0.85, ((touchdownFPM - 600.0) / 400.0) * 0.55);
            if (_random.NextDouble() < leakChance)
            {
                _oilLeakedPercent = Math.Max(_oilLeakedPercent, 15.0);
                _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, 3.5);
                _simConnect.SetOilLeakedPercent(_oilLeakedPercent);
                _lastTransmittedOilLeak = _oilLeakedPercent;

                TriggerFailure(new FailureRecord
                {
                    Type = FailureType.OilLeak,
                    Title = "VAZAMENTO SEVERO DE ÓLEO (POUSO DURO / TRINCA DE CÁRTER)",
                    Description = $"Impacto severo no toque ({touchdownFPM:F0} FPM) trincou o cárter / rompeu o retentor frontal do virabrequim. Vazamento rápido de óleo ativo!",
                    IsCritical = true,
                    SimEventName = ""
                });
            }
        }

        public void TriggerFailure(FailureRecord record)
        {
            if (_activeFailures.Contains(record.Type)) return;

            _activeFailures.Add(record.Type);
            _failureHistory.Add(record);

            if (record.Type == FailureType.EngineFire)
            {
                // No MSFS 2024, Fogo != Motor Apagado Automaticamente:
                // Ativa ENG ON FIRE:1 = 1 e inicia o cronômetro de 90s (60-120s) para corte de combustível pelo piloto
                _isEngineOnFire = true;
                _activeFireBurnSeconds = 0.0;
                _fireCoolingTimerSeconds = 0.0;
                _simConnect.SetEngineFire(true);
            }
            else if (record.Type == FailureType.FlapLock)
            {
                _lockedFlapsHandle = NormalizeFlapsHandle(_lastData.FlapsHandlePercent);
            }
            else if (record.Type == FailureType.OilLeak && _oilLeakedPercent > 0)
            {
                _simConnect.SetOilLeakedPercent(_oilLeakedPercent);
                _lastTransmittedOilLeak = _oilLeakedPercent;
            }

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
                case FailureType.TireBlowout:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.TireBlowout,
                        Title = "ESTOURO DE PNEU",
                        Description = "Estouro forçado via painel de testes/controle.",
                        IsCritical = true,
                        SimEventName = "" // Sem evento nativo de pneu/trem no SimConnect
                    });
                    break;
                case FailureType.GearCollapse:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.GearCollapse,
                        Title = "COLAPSO DE TREM DE POUSO",
                        Description = "Colapso mecânico forçado da perna do trem.",
                        IsCritical = true,
                        SimEventName = "" // Sem evento nativo de pneu/trem no SimConnect
                    });
                    break;
                case FailureType.AlternatorFailure:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.AlternatorFailure,
                        Title = "FALHA DE ALTERNADOR",
                        Description = "Falha elétrica forçada via painel de testes/controle.",
                        IsCritical = false,
                        SimEventName = "" // Sem evento nativo; ALT de verdade é mantido desligado por EnforceActiveFailures
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
                        SimEventName = "" // Alavanca mantida na posição travada por EnforceActiveFailures
                    });
                    break;
                case FailureType.CabinDepressurization:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.CabinDepressurization,
                        Title = "DESPRESSURIZAÇÃO DA CABINE",
                        Description = "Despressurização forçada via painel de testes/controle.",
                        IsCritical = true,
                        SimEventName = "" // Sem evento nativo; ALT de verdade é mantido desligado por EnforceActiveFailures
                    });
                    break;
                case FailureType.MagnetoFailure:
                    bool leftFailed = _random.Next(2) == 0;
                    _failedMagnetoIsLeft = leftFailed;
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.MagnetoFailure,
                        Title = $"PANE DE IGNIÇÃO: MAGNETO {(leftFailed ? "ESQUERDO" : "DIREITO")}",
                        Description = "Falha forçada de magneto/ignição via painel de testes/controle.",
                        IsCritical = false,
                        SimEventName = "" // Mantido por EnforceActiveFailures (chave não volta para BOTH)
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
                case FailureType.EngineFire:
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.EngineFire,
                        Title = "INCÊNDIO NO MOTOR (ENGINE FIRE)",
                        Description = "Fogo ativo no compartimento do motor (ENG ON FIRE:1 = 1). Corte combustível e bomba em até 90s para extinguir as chamas!",
                        IsCritical = true,
                        SimEventName = ""
                    });
                    break;
                case FailureType.OilLeak:
                    _oilLeakedPercent = Math.Max(_oilLeakedPercent, 15.0);
                    _activeOilLeakRatePctPerMin = Math.Max(_activeOilLeakRatePctPerMin, 2.5);
                    TriggerFailure(new FailureRecord
                    {
                        Type = FailureType.OilLeak,
                        Title = "VAZAMENTO PROGRESSIVO DE ÓLEO",
                        Description = "Vazamento progressivo de óleo ativo (GENERAL ENG OIL LEAKED PERCENT:1). Monitore queda de pressão e subida de temperatura.",
                        IsCritical = true,
                        SimEventName = ""
                    });
                    break;
            }
        }
    }
}
