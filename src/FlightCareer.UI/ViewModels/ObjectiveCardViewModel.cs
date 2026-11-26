using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FlightCareer.Domain.Missions;

namespace FlightCareer.UI.ViewModels
{
    public sealed class ObjectiveCardMissionItem
    {
        public string Title { get; }
        public string Route { get; }

        public ObjectiveCardMissionItem(string title, string route)
        {
            Title = title;
            Route = route;
        }
    }

    public sealed class ObjectiveCardObjectiveItem
    {
        public string Icon { get; }
        public string Text { get; }
        public Brush Brush { get; }

        public ObjectiveCardObjectiveItem(string icon, string text, Brush brush)
        {
            Icon = icon;
            Text = text;
            Brush = brush;
        }
    }

    public partial class ObjectiveCardViewModel : ObservableObject
    {
        private static readonly SolidColorBrush PendingBrush = CreateFrozenBrush(0xFF, 0xC4, 0x00);
        private static readonly SolidColorBrush DoneBrush = CreateFrozenBrush(0x00, 0xE6, 0x76);
        private static readonly SolidColorBrush FailedBrush = CreateFrozenBrush(0xFF, 0x52, 0x52);

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private string _lastContentKey = string.Empty;

        [ObservableProperty]
        private bool _hasFlight;

        [ObservableProperty]
        private string _emptyMessage = "Nenhum voo em andamento.";

        public ObservableCollection<ObjectiveCardMissionItem> Missions { get; } = new();
        public ObservableCollection<ObjectiveCardObjectiveItem> Objectives { get; } = new();

        public void Update(ObjectiveCardContent content)
        {
            string missionsKey = content.Missions == null || content.Missions.Count == 0
                ? string.Empty
                : string.Join(";", content.Missions.Select(m => $"{m.Title}|{m.Route}"));

            string objectivesKey = content.Objectives == null || content.Objectives.Count == 0
                ? string.Empty
                : string.Join(";", content.Objectives.Select(o => $"{o.Text}|{o.State}"));

            string newKey = $"{missionsKey}##{objectivesKey}";
            if (_lastContentKey == newKey) return;
            _lastContentKey = newKey;

            Missions.Clear();
            if (content.Missions != null)
            {
                foreach (var m in content.Missions)
                {
                    Missions.Add(new ObjectiveCardMissionItem(m.Title, m.Route));
                }
            }

            Objectives.Clear();
            if (content.Objectives != null)
            {
                foreach (var obj in content.Objectives)
                {
                    var (icon, brush) = obj.State switch
                    {
                        ContractObjectiveState.Done => ("✓", DoneBrush),
                        ContractObjectiveState.Failed => ("✖", FailedBrush),
                        _ => ("○", PendingBrush)
                    };
                    Objectives.Add(new ObjectiveCardObjectiveItem(icon, obj.Text, brush));
                }
            }

            HasFlight = Missions.Count > 0 || Objectives.Count > 0;
        }

        public void SetEmpty(string message)
        {
            if (_lastContentKey == "__EMPTY__" && EmptyMessage == message) return;
            _lastContentKey = "__EMPTY__";
            EmptyMessage = message;
            Missions.Clear();
            Objectives.Clear();
            HasFlight = false;
        }
    }
}
