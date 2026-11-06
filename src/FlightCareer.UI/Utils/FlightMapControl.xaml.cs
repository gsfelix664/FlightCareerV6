using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FlightCareer.Domain.Entities;
using FlightCareer.Domain.Utils;

namespace FlightCareer.UI.Utils
{
    public partial class FlightMapControl : UserControl
    {
        private readonly List<PointLatLng> _waypoints = new List<PointLatLng>();
        private GMapMarker? _aircraftMarker;
        private GMapMarker? _destinationMarker;
        private GMapRoute? _destinationRoute;
        private GMapRoute? _waypointRoute;

        // Dependency Properties
        public static readonly DependencyProperty AircraftLatProperty = DependencyProperty.Register("AircraftLat", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnAircraftPosChanged));
        public static readonly DependencyProperty AircraftLonProperty = DependencyProperty.Register("AircraftLon", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnAircraftPosChanged));
        public static readonly DependencyProperty DestLatProperty = DependencyProperty.Register("DestLat", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnDestinationChanged));
        public static readonly DependencyProperty DestLonProperty = DependencyProperty.Register("DestLon", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnDestinationChanged));
        public static readonly DependencyProperty OriginIcaoProperty = DependencyProperty.Register("OriginIcao", typeof(string), typeof(FlightMapControl), new PropertyMetadata(string.Empty, OnIcaoChanged));
        public static readonly DependencyProperty DestIcaoProperty = DependencyProperty.Register("DestIcao", typeof(string), typeof(FlightMapControl), new PropertyMetadata(string.Empty, OnIcaoChanged));
        public static readonly DependencyProperty AirportsProperty = DependencyProperty.Register("Airports", typeof(System.Collections.ObjectModel.ObservableCollection<Airport>), typeof(FlightMapControl), new PropertyMetadata(null, OnAirportsChanged));

        public double AircraftLat { get => (double)GetValue(AircraftLatProperty); set => SetValue(AircraftLatProperty, value); }
        public double AircraftLon { get => (double)GetValue(AircraftLonProperty); set => SetValue(AircraftLonProperty, value); }
        public double DestLat { get => (double)GetValue(DestLatProperty); set => SetValue(DestLatProperty, value); }
        public double DestLon { get => (double)GetValue(DestLonProperty); set => SetValue(DestLonProperty, value); }
        public string OriginIcao { get => (string)GetValue(OriginIcaoProperty); set => SetValue(OriginIcaoProperty, value); }
        public string DestIcao { get => (string)GetValue(DestIcaoProperty); set => SetValue(DestIcaoProperty, value); }
        public System.Collections.ObjectModel.ObservableCollection<Airport> Airports { get => (System.Collections.ObjectModel.ObservableCollection<Airport>)GetValue(AirportsProperty); set => SetValue(AirportsProperty, value); }

        private static void OnIcaoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                map.UpdateAircraftPosition();
                map.UpdateDestinationRoute();
            }
        }

        private readonly List<GMapMarker> _airportMarkers = new List<GMapMarker>();

        public FlightMapControl()
        {
            InitializeComponent();
            InitMap();
        }

        private static void OnAirportsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldColl)
                    oldColl.CollectionChanged -= map.Airports_CollectionChanged;
                
                if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newColl)
                    newColl.CollectionChanged += map.Airports_CollectionChanged;

                map.UpdateAirportMarkers();
            }
        }

        private void Airports_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateAirportMarkers();
        }

        private void UpdateAirportMarkers()
        {
            foreach (var m in _airportMarkers) MainMap.Markers.Remove(m);
            _airportMarkers.Clear();

            if (Airports == null) return;

            foreach (var airport in Airports)
            {
                var marker = new GMapMarker(new PointLatLng(airport.Latitude, airport.Longitude))
                {
                    ZIndex = 10,
                    Shape = new Ellipse
                    {
                        Width = 6, Height = 6, Fill = Brushes.Transparent, 
                        Stroke = Brushes.White, StrokeThickness = 1,
                        ToolTip = $"{airport.ICAO} - {airport.Name}"
                    }
                };
                _airportMarkers.Add(marker);
                MainMap.Markers.Add(marker);
            }
        }

        private void InitMap()
        {
            // Configurações de acesso
            GMap.NET.MapProviders.GMapProvider.UserAgent = "FlightCareerV6/1.0 (WPF; Windows; https://github.com/gsfel/FlightCareerV6)";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            // Trocando para ArcGIS (Baseado em OSM mas com servidores ESRI mais estáveis)
            MainMap.MapProvider = GMapProviders.ArcGIS_World_Street_Map;
            
            MainMap.Position = new PointLatLng(0, 0);
            MainMap.MinZoom = 2;
            MainMap.MaxZoom = 18;
            MainMap.Zoom = 8;
            MainMap.ShowCenter = false;
            MainMap.CanDragMap = true;
            MainMap.DragButton = MouseButton.Left;

            // Criar marcador da aeronave
            _aircraftMarker = new GMapMarker(new PointLatLng(0, 0))
            {
                ZIndex = 100,
                Shape = new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = Brushes.DeepSkyBlue,
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    ToolTip = "AERONAVE ATUAL"
                }
            };
            MainMap.Markers.Add(_aircraftMarker);
        }

        private static void OnAircraftPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                map.UpdateAircraftPosition();
                map.UpdateDestinationRoute();
            }
        }

        private static void OnDestinationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map) map.UpdateDestinationRoute();
        }

        private void UpdateAircraftPosition()
        {
            if (_aircraftMarker != null)
            {
                var pos = new PointLatLng(AircraftLat, AircraftLon);
                _aircraftMarker.Position = pos;

                // Tactical Origin Badge
                var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                panel.Children.Add(new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!, // Ciano Aviônica
                    Stroke = (Brush)new BrushConverter().ConvertFromString("#0B0F14")!,
                    StrokeThickness = 2,
                    Margin = new Thickness(0, 0, 4, 0)
                });

                if (!string.IsNullOrWhiteSpace(OriginIcao))
                {
                    var border = new Border
                    {
                        Background = (Brush)new BrushConverter().ConvertFromString("#E6141C24")!,
                        BorderBrush = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(5, 2, 5, 2),
                        Child = new TextBlock
                        {
                            Text = OriginIcao,
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)new BrushConverter().ConvertFromString("#F0F4F8")!,
                            FontFamily = new FontFamily("JetBrains Mono, Consolas, monospace")
                        }
                    };
                    panel.Children.Add(border);
                }

                panel.ToolTip = $"ORIGEM: {OriginIcao}";
                _aircraftMarker.Shape = panel;
                
                // Centraliza o mapa se estiver no início
                if (MainMap.Position.Lat == 0 && MainMap.Position.Lng == 0 && (pos.Lat != 0 || pos.Lng != 0))
                {
                    MainMap.Position = pos;
                }
            }
        }

        private void UpdateDestinationRoute()
        {
            if (_destinationMarker != null) MainMap.Markers.Remove(_destinationMarker);
            if (_destinationRoute != null) MainMap.Markers.Remove(_destinationRoute);

            if (DestLat == 0 || DestLon == 0) return;

            // Criar rota geodésica pontilhada em Ciano Aviônica (#00E5FF)
            var points = new List<PointLatLng> { new PointLatLng(AircraftLat, AircraftLon), new PointLatLng(DestLat, DestLon) };
            _destinationRoute = new GMapRoute(points)
            {
                Shape = new Path
                {
                    Stroke = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!, // Ciano Aviônica
                    StrokeThickness = 3,
                    StrokeDashArray = new DoubleCollection { 3, 2 },
                    Opacity = 0.95
                }
            };
            MainMap.Markers.Add(_destinationRoute);

            // Criar marcador tático de destino com badge ICAO em Verde Lucro/VFR (#00E676)
            var destPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            destPanel.Children.Add(new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = (Brush)new BrushConverter().ConvertFromString("#00E676")!, // Verde Lucro / VFR
                Stroke = (Brush)new BrushConverter().ConvertFromString("#0B0F14")!,
                StrokeThickness = 2,
                Margin = new Thickness(0, 0, 4, 0)
            });

            if (!string.IsNullOrWhiteSpace(DestIcao))
            {
                var border = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFromString("#E6141C24")!,
                    BorderBrush = (Brush)new BrushConverter().ConvertFromString("#00E676")!,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 2, 5, 2),
                    Child = new TextBlock
                    {
                        Text = DestIcao,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = (Brush)new BrushConverter().ConvertFromString("#F0F4F8")!,
                        FontFamily = new FontFamily("JetBrains Mono, Consolas, monospace")
                    }
                };
                destPanel.Children.Add(border);
            }

            destPanel.ToolTip = $"DESTINO: {DestIcao}";

            _destinationMarker = new GMapMarker(new PointLatLng(DestLat, DestLon))
            {
                ZIndex = 110,
                Shape = destPanel
            };
            MainMap.Markers.Add(_destinationMarker);

            // Transição e enquadramento suave (fly-to / pan-zoom)
            if (AircraftLat != 0 && AircraftLon != 0 && DestLat != 0 && DestLon != 0)
            {
                double minLat = Math.Min(AircraftLat, DestLat);
                double maxLat = Math.Max(AircraftLat, DestLat);
                double minLon = Math.Min(AircraftLon, DestLon);
                double maxLon = Math.Max(AircraftLon, DestLon);

                double latSpan = Math.Max(0.08, maxLat - minLat);
                double lonSpan = Math.Max(0.08, maxLon - minLon);

                var rect = RectLatLng.FromLTRB(
                    minLon - (lonSpan * 0.35),
                    maxLat + (latSpan * 0.35),
                    maxLon + (lonSpan * 0.35),
                    minLat - (latSpan * 0.35)
                );

                MainMap.SetZoomToFitRect(rect);
            }
        }

        private void MainMap_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(MainMap);
            var pos = MainMap.FromLocalToLatLng((int)p.X, (int)p.Y);
            
            _waypoints.Add(pos);
            UpdateWaypointMarkers();
            UpdateWaypointRoute();
        }

        private void UpdateWaypointMarkers()
        {
            // Limpar marcadores de waypoint antigos (mantendo aeronave e rota de destino)
            var toRemove = MainMap.Markers.Where(m => m != _aircraftMarker && m != _destinationRoute && m != _waypointRoute).ToList();
            foreach (var m in toRemove) MainMap.Markers.Remove(m);

            foreach (var wp in _waypoints)
            {
                var marker = new GMapMarker(wp)
                {
                    Shape = new Ellipse { Width = 8, Height = 8, Fill = Brushes.Orange, Stroke = Brushes.Black, StrokeThickness = 1 }
                };
                MainMap.Markers.Add(marker);
            }
        }

        private void UpdateWaypointRoute()
        {
            if (_waypointRoute != null) MainMap.Markers.Remove(_waypointRoute);

            if (_waypoints.Count < 2)
            {
                TxtDistance.Text = "0.0 NM";
                return;
            }

            _waypointRoute = new GMapRoute(_waypoints)
            {
                Shape = new Path { Stroke = Brushes.Orange, StrokeThickness = 2, Opacity = 0.8 }
            };
            MainMap.Markers.Add(_waypointRoute);

            // Calcular Distância NM
            double totalDist = 0;
            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                totalDist += LocationUtils.CalculateDistance(_waypoints[i].Lat, _waypoints[i].Lng, _waypoints[i + 1].Lat, _waypoints[i + 1].Lng);
            }
            TxtDistance.Text = $"{totalDist:F1} NM";
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _waypoints.Clear();
            UpdateWaypointMarkers();
            UpdateWaypointRoute();
        }
    }
}
