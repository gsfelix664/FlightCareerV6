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
        private GMapRoute? _recordedFlightRoute;
        private GMapMarker? _originPinMarker;
        private GMapMarker? _destPinMarker;
        private readonly List<GMapRoute> _multiLegRoutes = new List<GMapRoute>();
        private readonly List<GMapMarker> _multiLegMarkers = new List<GMapMarker>();

        // Dependency Properties
        public static readonly DependencyProperty RecordedPathProperty = DependencyProperty.Register("RecordedPath", typeof(System.Collections.IEnumerable), typeof(FlightMapControl), new PropertyMetadata(null, OnRecordedPathChanged));
        public static readonly DependencyProperty RouteLegsProperty = DependencyProperty.Register("RouteLegs", typeof(System.Collections.IEnumerable), typeof(FlightMapControl), new PropertyMetadata(null, OnRouteLegsChanged));
        public static readonly DependencyProperty AircraftLatProperty = DependencyProperty.Register("AircraftLat", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnAircraftPosChanged));
        public static readonly DependencyProperty AircraftLonProperty = DependencyProperty.Register("AircraftLon", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnAircraftPosChanged));
        public static readonly DependencyProperty DestLatProperty = DependencyProperty.Register("DestLat", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnDestinationChanged));
        public static readonly DependencyProperty DestLonProperty = DependencyProperty.Register("DestLon", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnDestinationChanged));
        public static readonly DependencyProperty OriginIcaoProperty = DependencyProperty.Register("OriginIcao", typeof(string), typeof(FlightMapControl), new PropertyMetadata(string.Empty, OnIcaoChanged));
        public static readonly DependencyProperty DestIcaoProperty = DependencyProperty.Register("DestIcao", typeof(string), typeof(FlightMapControl), new PropertyMetadata(string.Empty, OnIcaoChanged));
        public static readonly DependencyProperty AirportsProperty = DependencyProperty.Register("Airports", typeof(System.Collections.ObjectModel.ObservableCollection<Airport>), typeof(FlightMapControl), new PropertyMetadata(null, OnAirportsChanged));
        
        // Novas propriedades de telemetria em tempo real
        public static readonly DependencyProperty IsOnGroundProperty = DependencyProperty.Register("IsOnGround", typeof(bool), typeof(FlightMapControl), new PropertyMetadata(true, OnTelemetryChanged));
        public static readonly DependencyProperty AircraftHeadingProperty = DependencyProperty.Register("AircraftHeading", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnTelemetryChanged));
        public static readonly DependencyProperty AltitudeProperty = DependencyProperty.Register("Altitude", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnTelemetryChanged));
        public static readonly DependencyProperty GroundSpeedProperty = DependencyProperty.Register("GroundSpeed", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnTelemetryChanged));
        public static readonly DependencyProperty ShowWaypointsOverlayProperty = DependencyProperty.Register("ShowWaypointsOverlay", typeof(bool), typeof(FlightMapControl), new PropertyMetadata(true));

        public System.Collections.IEnumerable? RecordedPath { get => (System.Collections.IEnumerable?)GetValue(RecordedPathProperty); set => SetValue(RecordedPathProperty, value); }
        public System.Collections.IEnumerable? RouteLegs { get => (System.Collections.IEnumerable?)GetValue(RouteLegsProperty); set => SetValue(RouteLegsProperty, value); }
        public double AircraftLat { get => (double)GetValue(AircraftLatProperty); set => SetValue(AircraftLatProperty, value); }
        public double AircraftLon { get => (double)GetValue(AircraftLonProperty); set => SetValue(AircraftLonProperty, value); }
        public double DestLat { get => (double)GetValue(DestLatProperty); set => SetValue(DestLatProperty, value); }
        public double DestLon { get => (double)GetValue(DestLonProperty); set => SetValue(DestLonProperty, value); }
        public string OriginIcao { get => (string)GetValue(OriginIcaoProperty); set => SetValue(OriginIcaoProperty, value); }
        public string DestIcao { get => (string)GetValue(DestIcaoProperty); set => SetValue(DestIcaoProperty, value); }
        public System.Collections.ObjectModel.ObservableCollection<Airport> Airports { get => (System.Collections.ObjectModel.ObservableCollection<Airport>)GetValue(AirportsProperty); set => SetValue(AirportsProperty, value); }
        public bool IsOnGround { get => (bool)GetValue(IsOnGroundProperty); set => SetValue(IsOnGroundProperty, value); }
        public double AircraftHeading { get => (double)GetValue(AircraftHeadingProperty); set => SetValue(AircraftHeadingProperty, value); }
        public double Altitude { get => (double)GetValue(AltitudeProperty); set => SetValue(AltitudeProperty, value); }
        public double GroundSpeed { get => (double)GetValue(GroundSpeedProperty); set => SetValue(GroundSpeedProperty, value); }
        public bool ShowWaypointsOverlay { get => (bool)GetValue(ShowWaypointsOverlayProperty); set => SetValue(ShowWaypointsOverlayProperty, value); }

        private static void OnRouteLegsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldColl)
                    oldColl.CollectionChanged -= map.RouteLegs_CollectionChanged;

                if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newColl)
                    newColl.CollectionChanged += map.RouteLegs_CollectionChanged;

                map.UpdateMultiLegRoute();
            }
        }

        private void RouteLegs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateMultiLegRoute();
        }

        private static void OnTelemetryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                map.UpdateAircraftPosition();
            }
        }

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

            // Camada Noturna Tática Escura (CartoDB Dark Matter) perfeitamente integrada ao tema Avionics Dark
            MainMap.MapProvider = CartoDbDarkMapProvider.Instance;
            
            MainMap.Position = new PointLatLng(0, 0);
            MainMap.MinZoom = 2;
            MainMap.MaxZoom = 16;
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
            }
        }

        private static void OnDestinationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                if (map.RouteLegs != null && map.RouteLegs.OfType<object>().Any())
                {
                    map.UpdateMultiLegRoute();
                }
                else
                {
                    map.UpdateDestinationRoute();
                }
            }
        }

        private void UpdateAircraftPosition()
        {
            if (_aircraftMarker != null)
            {
                var pos = new PointLatLng(AircraftLat, AircraftLon);
                _aircraftMarker.Position = pos;

                var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                if (IsOnGround)
                {
                    // === MODO SOLO (ON GROUND) ===
                    // Círculo tático de rampa com radar ping
                    var groundIcon = new Grid
                    {
                        Width = 18,
                        Height = 18,
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    groundIcon.Children.Add(new Ellipse
                    {
                        Width = 18,
                        Height = 18,
                        Fill = (Brush)new BrushConverter().ConvertFromString("#33FFB300")!,
                        Stroke = (Brush)new BrushConverter().ConvertFromString("#FFB300")!,
                        StrokeThickness = 1.5
                    });
                    groundIcon.Children.Add(new Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = (Brush)new BrushConverter().ConvertFromString("#00E676")!,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    panel.Children.Add(groundIcon);

                    string badgeText = !string.IsNullOrWhiteSpace(OriginIcao) ? $"SOLO • {OriginIcao}" : "SOLO / RAMPA";
                    if (GroundSpeed > 3.0)
                    {
                        badgeText += $" ({GroundSpeed:N0} kt)";
                    }

                    var border = new Border
                    {
                        Background = (Brush)new BrushConverter().ConvertFromString("#E6141C24")!,
                        BorderBrush = (Brush)new BrushConverter().ConvertFromString("#00E676")!,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        Child = new TextBlock
                        {
                            Text = badgeText,
                            FontSize = 9,
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)new BrushConverter().ConvertFromString("#F0F4F8")!,
                            FontFamily = new FontFamily("JetBrains Mono, Consolas, monospace")
                        }
                    };
                    panel.Children.Add(border);
                    panel.ToolTip = $"AERONAVE EM SOLO | Posição: {AircraftLat:F4}, {AircraftLon:F4}";
                }
                else
                {
                    // === MODO EM VOO (AIRBORNE) ===
                    // Silhueta vetorial de aeronave com rotação de proa em tempo real
                    var planeGrid = new Grid
                    {
                        Width = 28,
                        Height = 28,
                        Margin = new Thickness(0, 0, 6, 0)
                    };

                    var planeGeom = System.Windows.Media.Geometry.Parse("M 12,2 L 15,9 L 25,13 L 25,15 L 15,14 L 14,21 L 18,23 L 18,25 L 12,24 L 6,25 L 6,23 L 10,21 L 9,14 L -1,15 L -1,13 L 9,9 Z");
                    var planePath = new Path
                    {
                        Data = planeGeom,
                        Fill = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!, // Ciano Aviônica
                        Stroke = (Brush)new BrushConverter().ConvertFromString("#0B0F14")!,
                        StrokeThickness = 1.5,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = new RotateTransform(AircraftHeading)
                    };
                    planeGrid.Children.Add(planePath);
                    panel.Children.Add(planeGrid);

                    // Badge de Telemetria de Voo Ativo
                    var flightBorder = new Border
                    {
                        Background = (Brush)new BrushConverter().ConvertFromString("#E60B0F14")!,
                        BorderBrush = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        Child = new TextBlock
                        {
                            Text = $"✈️ {Altitude:N0} FT • {GroundSpeed:N0} KT • {AircraftHeading:N0}°",
                            FontSize = 9,
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!,
                            FontFamily = new FontFamily("JetBrains Mono, Consolas, monospace")
                        }
                    };
                    panel.Children.Add(flightBorder);
                    panel.ToolTip = $"EM VOO | ALT: {Altitude:N0} ft | GS: {GroundSpeed:N0} kt | HDG: {AircraftHeading:N0}°";
                }

                _aircraftMarker.ZIndex = 200;
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

        private void ClearMultiLegVisuals()
        {
            foreach (var r in _multiLegRoutes) MainMap.Markers.Remove(r);
            _multiLegRoutes.Clear();
            foreach (var m in _multiLegMarkers) MainMap.Markers.Remove(m);
            _multiLegMarkers.Clear();
        }

        private GMapMarker CreateWaypointBadge(PointLatLng pos, string text, string hexColor, string tooltip)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            panel.Children.Add(new Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = (Brush)new BrushConverter().ConvertFromString(hexColor)!,
                Stroke = (Brush)new BrushConverter().ConvertFromString("#0B0F14")!,
                StrokeThickness = 1.5,
                Margin = new Thickness(0, 0, 4, 0)
            });

            var border = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#E6141C24")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString(hexColor)!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 2, 5, 2),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#F0F4F8")!,
                    FontFamily = new FontFamily("JetBrains Mono, Consolas, monospace")
                }
            };
            panel.Children.Add(border);
            panel.ToolTip = tooltip;

            return new GMapMarker(pos)
            {
                ZIndex = 120,
                Shape = panel
            };
        }

        public void UpdateMultiLegRoute()
        {
            ClearMultiLegVisuals();

            if (RouteLegs == null)
            {
                UpdateDestinationRoute();
                return;
            }

            var legs = RouteLegs.OfType<ViewModels.FlightLegModel>().ToList();
            if (!legs.Any())
            {
                UpdateDestinationRoute();
                return;
            }

            // Ocultar rota simples de destino único se estiver em modo multi-pernas
            if (_destinationRoute != null) MainMap.Markers.Remove(_destinationRoute);
            if (_destinationMarker != null) MainMap.Markers.Remove(_destinationMarker);

            var allRoutePoints = new List<PointLatLng>();
            if (AircraftLat != 0 && AircraftLon != 0)
            {
                allRoutePoints.Add(new PointLatLng(AircraftLat, AircraftLon));
            }

            var plottedIcaos = new HashSet<string>();

            for (int i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                bool isFirstLeg = (i == 0);
                bool isLastLeg = (i == legs.Count - 1);

                // 1. Circuito de Tráfego / Toque e Arremetida (TGL)
                if (leg.IsTglOrLocal || leg.OriginICAO == leg.DestinationICAO)
                {
                    double lat0 = leg.OriginLat != 0 ? leg.OriginLat : AircraftLat;
                    double lon0 = leg.OriginLon != 0 ? leg.OriginLon : AircraftLon;

                    if (lat0 == 0 && lon0 == 0) continue;

                    // Circuito retangular padrão de tráfego aéreo (~1.2 NM de largura/comprimento)
                    double dLat = 0.018; // ~1.1 NM
                    double dLon = 0.024; // ~1.2 NM

                    var pRwy = new PointLatLng(lat0, lon0);
                    var pUpwind = new PointLatLng(lat0 - dLat, lon0);
                    var pCrosswind = new PointLatLng(lat0 - dLat, lon0 + dLon);
                    var pDownwind = new PointLatLng(lat0 + dLat, lon0 + dLon);
                    var pBase = new PointLatLng(lat0 + dLat, lon0);

                    var circuitPoints = new List<PointLatLng> { pRwy, pUpwind, pCrosswind, pDownwind, pBase, pRwy };
                    allRoutePoints.AddRange(circuitPoints);

                    var tglRoute = new GMapRoute(circuitPoints)
                    {
                        Shape = new Path
                        {
                            Stroke = (Brush)new BrushConverter().ConvertFromString("#FFB300")!, // Âmbar para Instrução/TGL
                            StrokeThickness = 2.8,
                            StrokeDashArray = new DoubleCollection { 3, 2 },
                            Opacity = 0.95
                        }
                    };
                    _multiLegRoutes.Add(tglRoute);
                    MainMap.Markers.Add(tglRoute);

                    var tglBadge = CreateWaypointBadge(
                        new PointLatLng(lat0 + (dLat * 0.4), lon0 + dLon),
                        $"LEG {leg.LegNumber} • {leg.OriginICAO} (TGL)",
                        "#FFB300",
                        $"CIRCUITO DE TRÁFEGO • TOQUE E ARREMETIDA EM {leg.OriginICAO} ({leg.DistanceNM:N0} NM)");
                    _multiLegMarkers.Add(tglBadge);
                    MainMap.Markers.Add(tglBadge);

                    plottedIcaos.Add(leg.OriginICAO);
                }
                else
                {
                    // 2. Perna de Navegação entre Aeródromos Distintos
                    var pStart = new PointLatLng(leg.OriginLat, leg.OriginLon);
                    var pEnd = new PointLatLng(leg.DestLat, leg.DestLon);

                    if (pStart.Lat == 0 && pStart.Lng == 0 && AircraftLat != 0)
                        pStart = new PointLatLng(AircraftLat, AircraftLon);

                    if (pEnd.Lat == 0 && pEnd.Lng == 0) continue;

                    allRoutePoints.Add(pStart);
                    allRoutePoints.Add(pEnd);

                    var legRoute = new GMapRoute(new List<PointLatLng> { pStart, pEnd })
                    {
                        Shape = new Path
                        {
                            Stroke = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!, // Ciano Aviônica
                            StrokeThickness = 3,
                            StrokeDashArray = new DoubleCollection { 4, 2 },
                            Opacity = 0.95
                        }
                    };
                    _multiLegRoutes.Add(legRoute);
                    MainMap.Markers.Add(legRoute);

                    if (isFirstLeg && !plottedIcaos.Contains(leg.OriginICAO))
                    {
                        var originBadge = CreateWaypointBadge(pStart, $"PARTIDA • {leg.OriginICAO}", "#00E676", $"AERÓDROMO DE PARTIDA: {leg.OriginICAO}");
                        _multiLegMarkers.Add(originBadge);
                        MainMap.Markers.Add(originBadge);
                        plottedIcaos.Add(leg.OriginICAO);
                    }

                    string pinColor = isLastLeg ? "#00E676" : "#00E5FF";
                    string pinLabel = isLastLeg 
                        ? $"LEG {leg.LegNumber} • {leg.DestinationICAO} (FINAL)" 
                        : $"LEG {leg.LegNumber} • {leg.DestinationICAO}";

                    var destBadge = CreateWaypointBadge(pEnd, pinLabel, pinColor, $"ESCALA / DESTINO: {leg.DestinationICAO} ({leg.DistanceNM:N0} NM)");
                    _multiLegMarkers.Add(destBadge);
                    MainMap.Markers.Add(destBadge);
                    plottedIcaos.Add(leg.DestinationICAO);
                }
            }

            // Enquadramento automático cobrindo todas as pernas do voo
            if (allRoutePoints.Count >= 2)
            {
                double minLat = allRoutePoints.Min(p => p.Lat);
                double maxLat = allRoutePoints.Max(p => p.Lat);
                double minLon = allRoutePoints.Min(p => p.Lng);
                double maxLon = allRoutePoints.Max(p => p.Lng);

                double latSpan = Math.Max(0.06, maxLat - minLat);
                double lonSpan = Math.Max(0.06, maxLon - minLon);

                var rect = RectLatLng.FromLTRB(
                    minLon - (lonSpan * 0.30),
                    maxLat + (latSpan * 0.30),
                    maxLon + (lonSpan * 0.30),
                    minLat - (latSpan * 0.30)
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

        private static void OnRecordedPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldColl)
                    oldColl.CollectionChanged -= map.RecordedPath_CollectionChanged;

                if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newColl)
                    newColl.CollectionChanged += map.RecordedPath_CollectionChanged;

                map.UpdateRecordedPath();
            }
        }

        private void RecordedPath_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateRecordedPath();
        }

        public void UpdateRecordedPath()
        {
            if (_recordedFlightRoute != null)
            {
                MainMap.Markers.Remove(_recordedFlightRoute);
                _recordedFlightRoute = null;
            }
            if (_originPinMarker != null)
            {
                MainMap.Markers.Remove(_originPinMarker);
                _originPinMarker = null;
            }
            if (_destPinMarker != null)
            {
                MainMap.Markers.Remove(_destPinMarker);
                _destPinMarker = null;
            }

            var points = (RecordedPath as System.Collections.IEnumerable)?.OfType<PointLatLng>().ToList();
            if (points == null || points.Count == 0) return;

            // 1. Rota Ciano Neon de Trajetória Real
            _recordedFlightRoute = new GMapRoute(points)
            {
                Shape = new Path
                {
                    Stroke = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!,
                    StrokeThickness = 3.5,
                    Opacity = 0.95
                },
                ZIndex = 80
            };
            MainMap.Markers.Add(_recordedFlightRoute);

            // 2. Pin de Decolagem / Origem (Verde)
            var pStart = points.First();
            _originPinMarker = new GMapMarker(pStart)
            {
                ZIndex = 110,
                Shape = CreatePinBadge(!string.IsNullOrWhiteSpace(OriginIcao) ? OriginIcao : "DECOLAGEM", "#00E676")
            };
            MainMap.Markers.Add(_originPinMarker);

            // 3. Pin de Pouso / Destino (Amarelo Ouro)
            var pEnd = points.Last();
            _destPinMarker = new GMapMarker(pEnd)
            {
                ZIndex = 110,
                Shape = CreatePinBadge(!string.IsNullOrWhiteSpace(DestIcao) ? DestIcao : "POUSO", "#FFB300")
            };
            MainMap.Markers.Add(_destPinMarker);

            // 4. Enquadramento e Zoom Automático da Rota
            try
            {
                double minLat = points.Min(p => p.Lat);
                double maxLat = points.Max(p => p.Lat);
                double minLon = points.Min(p => p.Lng);
                double maxLon = points.Max(p => p.Lng);

                double latSpan = Math.Max(0.04, maxLat - minLat);
                double lonSpan = Math.Max(0.04, maxLon - minLon);

                var rect = RectLatLng.FromLTRB(
                    minLon - (lonSpan * 0.25),
                    maxLat + (latSpan * 0.25),
                    maxLon + (lonSpan * 0.25),
                    minLat - (latSpan * 0.25)
                );

                MainMap.SetZoomToFitRect(rect);
            }
            catch { }
        }

        private FrameworkElement CreatePinBadge(string text, string colorHex)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            panel.Children.Add(new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = (Brush)new BrushConverter().ConvertFromString(colorHex)!,
                Stroke = (Brush)new BrushConverter().ConvertFromString("#0B0F14")!,
                StrokeThickness = 2,
                Margin = new Thickness(0, 0, 4, 0)
            });

            var border = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#E6141C24")!,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString(colorHex)!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 2, 5, 2),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)new BrushConverter().ConvertFromString("#F0F4F8")!,
                    FontFamily = new FontFamily("JetBrains Mono, Consolas, monospace")
                }
            };
            panel.Children.Add(border);
            return panel;
        }
    }

    /// <summary>
    /// Provedor de Camada de Referência e Rótulos Noturnos (Cidades, Fronteiras) sobreposta ao mapa escuro.
    /// </summary>
    public class EsriDarkReferenceMapProvider : GMapProvider
    {
        public static readonly EsriDarkReferenceMapProvider Instance;
        private readonly Guid _id = new Guid("E5A91D32-1234-4C5E-9876-0ABCDEF12345");
        public override Guid Id => _id;

        private readonly string _name = "Esri_Dark_Reference";
        public override string Name => _name;

        private GMapProvider[]? _overlays;
        public override GMapProvider[] Overlays
        {
            get
            {
                if (_overlays == null)
                {
                    _overlays = new GMapProvider[] { this };
                }
                return _overlays;
            }
        }

        public override PureProjection Projection => GMap.NET.Projections.MercatorProjection.Instance;

        static EsriDarkReferenceMapProvider()
        {
            Instance = new EsriDarkReferenceMapProvider();
        }

        public override PureImage? GetTileImage(GPoint pos, int zoom)
        {
            try
            {
                string url = $"https://server.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Dark_Gray_Reference/MapServer/tile/{zoom}/{pos.Y}/{pos.X}";
                return GetTileImageUsingHttp(url);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Provedor de Mapa Noturno / Dark Mode Tático Avionics baseado em ESRI World Dark Gray Canvas.
    /// Tile server de altíssima velocidade, 100% livre de marcas d'água / API keys e paleta ultra-escura (#0B0F14 / #141C24).
    /// </summary>
    public class CartoDbDarkMapProvider : GMapProvider
    {
        public static readonly CartoDbDarkMapProvider Instance;

        // Novo GUID e Nome para forçar limpeza de cache de tiles antigos com marca d'água
        private readonly Guid _id = new Guid("B7E20F45-8899-4A1C-99BB-27F389A8E2E5");
        public override Guid Id => _id;

        private readonly string _name = "Esri_Dark_Canvas_Avionics";
        public override string Name => _name;

        private GMapProvider[]? _overlays;
        public override GMapProvider[] Overlays
        {
            get
            {
                if (_overlays == null)
                {
                    _overlays = new GMapProvider[] { this, EsriDarkReferenceMapProvider.Instance };
                }
                return _overlays;
            }
        }

        public override PureProjection Projection => GMap.NET.Projections.MercatorProjection.Instance;

        static CartoDbDarkMapProvider()
        {
            Instance = new CartoDbDarkMapProvider();
        }

        public override PureImage? GetTileImage(GPoint pos, int zoom)
        {
            try
            {
                string url = $"https://server.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Dark_Gray_Base/MapServer/tile/{zoom}/{pos.Y}/{pos.X}";
                return GetTileImageUsingHttp(url);
            }
            catch
            {
                return null;
            }
        }
    }
}
