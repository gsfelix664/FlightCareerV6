using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FlightCareer.UI.ViewModels
{
    public partial class LogbookViewModel : ObservableObject
    {
        private readonly IFlightRepository _flightRepository;
        private Guid _pilotId;

        [ObservableProperty] private ObservableCollection<Flight> _flights = new();
        [ObservableProperty] private bool _isLoading;

        public LogbookViewModel(IFlightRepository flightRepository)
        {
            _flightRepository = flightRepository;
        }

        public async Task InitializeAsync(Guid pilotId)
        {
            _pilotId = pilotId;
            await LoadFlightsAsync();
        }

        public async Task LoadFlightsAsync()
        {
            IsLoading = true;
            try
            {
                var flights = await _flightRepository.GetByPilotIdAsync(_pilotId);
                Flights = new ObservableCollection<Flight>(flights);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
