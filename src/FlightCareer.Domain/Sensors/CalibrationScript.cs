using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightCareer.Domain.Parts;

namespace FlightCareer.Domain.Sensors
{
    /// <summary>
    /// Passo individual do roteiro de calibração operacional de aeronave.
    /// </summary>
    public sealed record CalibrationStep(
        string Id,
        string Text,
        double MinSeconds,
        PartAppliesTo AppliesTo);

    /// <summary>
    /// Roteiro guiado de calibração no solo para determinação empírica de circuitos e barramentos da aeronave.
    /// Lógica pura de Domain, sem EF, sem UI e sem SimConnect.
    /// </summary>
    public sealed class CalibrationScript
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public IReadOnlyList<CalibrationStep> AllSteps { get; }
        public IReadOnlyList<CalibrationStep> ActiveSteps { get; }

        public int CurrentIndex { get; private set; }
        public double ElapsedInCurrentStep { get; private set; }

        public CalibrationStep? CurrentStep => CurrentIndex < ActiveSteps.Count ? ActiveSteps[CurrentIndex] : null;
        public bool IsCompleted => CurrentIndex >= ActiveSteps.Count || (CurrentStep != null && CurrentStep.Id == "end");
        public bool CanAdvance => CurrentStep != null && ElapsedInCurrentStep >= CurrentStep.MinSeconds;
        public double TimeRemaining => CurrentStep != null ? Math.Max(0.0, CurrentStep.MinSeconds - ElapsedInCurrentStep) : 0.0;

        public CalibrationScript(IEnumerable<CalibrationStep> steps, IReadOnlyCollection<string>? features = null)
        {
            AllSteps = steps?.ToList() ?? new List<CalibrationStep>();
            var feat = features ?? Array.Empty<string>();
            ActiveSteps = AllSteps.Where(s => s.AppliesTo == null || s.AppliesTo.Matches(feat)).ToList();
            CurrentIndex = 0;
            ElapsedInCurrentStep = 0.0;
        }

        public void Tick(double dtSeconds)
        {
            if (CurrentStep != null && !IsCompleted)
            {
                ElapsedInCurrentStep += Math.Max(0.0, dtSeconds);
            }
        }

        public bool TryAdvance(out CalibrationStep? nextStep)
        {
            if (!CanAdvance)
            {
                nextStep = CurrentStep;
                return false;
            }

            CurrentIndex++;
            ElapsedInCurrentStep = 0.0;
            nextStep = CurrentStep;
            return true;
        }

        public CalibrationStep? Skip()
        {
            if (CurrentIndex < ActiveSteps.Count)
            {
                CurrentIndex++;
                ElapsedInCurrentStep = 0.0;
            }
            return CurrentStep;
        }

        public void Reset()
        {
            CurrentIndex = 0;
            ElapsedInCurrentStep = 0.0;
        }

        public CalibrationScript WithFeatures(IReadOnlyCollection<string>? features)
        {
            return new CalibrationScript(AllSteps, features);
        }

        public static CalibrationScript FromJson(string json, IReadOnlyCollection<string>? features = null)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("JSON do roteiro de calibração não pode ser vazio.");

            var dto = JsonSerializer.Deserialize<CalibrationScriptDto>(json, JsonOptions)
                ?? throw new InvalidDataException("Roteiro de calibração vazio ou inválido.");

            var steps = (dto.Steps ?? Enumerable.Empty<CalibrationStepDto>())
                .Select(s => new CalibrationStep(
                    s.Id ?? string.Empty,
                    s.Text ?? string.Empty,
                    s.MinSeconds,
                    s.AppliesTo != null
                        ? new PartAppliesTo(
                            s.AppliesTo.All != null ? s.AppliesTo.All : Array.Empty<string>(),
                            s.AppliesTo.Any != null ? s.AppliesTo.Any : Array.Empty<string>(),
                            s.AppliesTo.None != null ? s.AppliesTo.None : Array.Empty<string>())
                        : new PartAppliesTo()))
                .ToList();

            return new CalibrationScript(steps, features);
        }

        public static CalibrationScript LoadFromFile(string path, IReadOnlyCollection<string>? features = null)
        {
            return FromJson(File.ReadAllText(path), features);
        }

        public static CalibrationScript LoadDefault(string fileName = "calibration_script.json", IReadOnlyCollection<string>? features = null)
        {
            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "Data", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "FlightCareer.Infrastructure", "Data", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "FlightCareer.Infrastructure", "Data", fileName)
            };

            var path = candidates.FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException($"Arquivo '{fileName}' não encontrado nas pastas Data candidatas.");

            return LoadFromFile(path, features);
        }

        private sealed class CalibrationScriptDto
        {
            public List<CalibrationStepDto>? Steps { get; set; }
        }

        private sealed class CalibrationStepDto
        {
            public string? Id { get; set; }
            public string? Text { get; set; }
            public double MinSeconds { get; set; }
            public AppliesToDto? AppliesTo { get; set; }
        }

        private sealed class AppliesToDto
        {
            public List<string>? All { get; set; }
            public List<string>? Any { get; set; }
            public List<string>? None { get; set; }
        }
    }
}
