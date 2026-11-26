using System;

namespace FlightCareer.Domain.Missions
{
    public sealed record HudContractItem(string Text, string Color)
    {
        public static HudContractItem From(ContractObjectiveLine line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            string text = line.State switch
            {
                ContractObjectiveState.Done => line.Text + " ✓",
                ContractObjectiveState.Failed => "✖ " + line.Text,
                _ => line.Text
            };

            string color = line.State switch
            {
                ContractObjectiveState.Done => "#00E676",
                ContractObjectiveState.Failed => "#FF5252",
                _ => "#FBBF24"
            };

            return new HudContractItem(text, color);
        }
    }
}
