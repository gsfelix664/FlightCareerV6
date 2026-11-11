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
using FlightCareer.UI.ViewModels;

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
        private readonly List<GMapMarker> _airportMarkers = new List<GMapMarker>();
        private readonly List<GMapMarker> _missionClusterMarkers = new List<GMapMarker>();

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

        // Propriedades de telemetria em tempo real
        public static readonly DependencyProperty IsOnGroundProperty = DependencyProperty.Register("IsOnGround", typeof(bool), typeof(FlightMapControl), new PropertyMetadata(true, OnTelemetryChanged));
        public static readonly DependencyProperty AircraftHeadingProperty = DependencyProperty.Register("AircraftHeading", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnTelemetryChanged));
        public static readonly DependencyProperty AltitudeProperty = DependencyProperty.Register("Altitude", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnTelemetryChanged));
        public static readonly DependencyProperty GroundSpeedProperty = DependencyProperty.Register("GroundSpeed", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnTelemetryChanged));
        public static readonly DependencyProperty ShowWaypointsOverlayProperty = DependencyProperty.Register("ShowWaypointsOverlay", typeof(bool), typeof(FlightMapControl), new PropertyMetadata(true));

        // Propriedades de Clustering e Seleção de Missões pelo Mapa
        public static readonly DependencyProperty MissionsSourceProperty = DependencyProperty.Register(
            nameof(MissionsSource),
            typeof(System.Collections.IEnumerable),
            typeof(FlightMapControl),
            new PropertyMetadata(null, OnMissionsSourceChanged));

        public static readonly DependencyProperty SelectedMissionProperty = DependencyProperty.Register(
            nameof(SelectedMission),
            typeof(object),
            typeof(FlightMapControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedMissionChanged));

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
        public System.Collections.IEnumerable? MissionsSource { get => (System.Collections.IEnumerable?)GetValue(MissionsSourceProperty); set => SetValue(MissionsSourceProperty, value); }
        public object? SelectedMission { get => GetValue(SelectedMissionProperty); set => SetValue(SelectedMissionProperty, value); }

        public FlightMapControl()
        {
            InitializeComponent();
            InitMap();
        }

        private void InitMap()
        {
            // Configurações de acesso
            GMap.NET.MapProviders.GMapProvider.UserAgent = "FlightCareerV6/1.0 (WPF; Windows; https://github.com/gsfel/FlightCareerV6)";
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            // Camada Noturna Tática Escura (CartoDB Dark Matter / Esri Canvas)
            MainMap.MapProvider = CartoDbDarkMapProvider.Instance;

            MainMap.Position = new PointLatLng(0, 0);
            MainMap.MinZoom = 2;
            MainMap.MaxZoom = 16;
            MainMap.Zoom = 8;
            MainMap.ShowCenter = false;
            MainMap.CanDragMap = true;
            MainMap.DragButton = MouseButton.Left;
            MainMap.IgnoreMarkerOnMouseWheel = true;

            // Sincronizar clusters de missões ao alterar o nível de zoom
            MainMap.OnMapZoomChanged += MainMap_OnMapZoomChanged;

            // Criar marcador da aeronave
            _aircraftMarker = new GMapMarker(new PointLatLng(0, 0))
            {
                ZIndex = 200
            };
            MainMap.Markers.Add(_aircraftMarker);
        }

        private void MainMap_OnMapZoomChanged()
        {
            UpdateMissionClusters();
        }

        private void BtnRecenter_Click(object sender, RoutedEventArgs e)
        {
            if (AircraftLat != 0 || AircraftLon != 0)
            {
                MainMap.Position = new PointLatLng(AircraftLat, AircraftLon);
                MainMap.Zoom = 9;
            }
        }

        #region Callbacks de Propriedades de Dependência

        private static void OnAircraftPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                map.UpdateAircraftPosition();
            }
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

        private static void OnMissionsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldColl)
                    oldColl.CollectionChanged -= map.Missions_CollectionChanged;

                if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newColl)
                    newColl.CollectionChanged += map.Missions_CollectionChanged;

                map.UpdateMissionClusters();
            }
        }

        private void Missions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateMissionClusters();
        }

        private static void OnSelectedMissionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map && e.NewValue != null)
            {
                var coords = GetMissionCoords(e.NewValue);
                if (coords.HasValue && (coords.Value.lat != 0 || coords.Value.lon != 0))
                {
                    map.DestLat = coords.Value.lat;
                    map.DestLon = coords.Value.lon;
                    map.DestIcao = coords.Value.icao;
                    map.UpdateDestinationRoute();
                }
            }
        }

        #endregion

        #region Renderização Tática de Aeronave & Rotas

        /// <summary>
        /// Atualiza a posição da aeronave com visual limpo de cockpit:
        /// Apenas o aviãozinho vetorial girando no heading (em voo) e o radar ping de rampa (em solo).
        /// Zero caixas de texto flutuantes.
        /// </summary>
        private void UpdateAircraftPosition()
        {
            if (_aircraftMarker != null)
            {
                var pos = new PointLatLng(AircraftLat, AircraftLon);
                _aircraftMarker.Position = pos;

                if (IsOnGround)
                {
                    // === MODO SOLO (ON GROUND) ===
                    // Apenas círculo tático de rampa com radar ping
                    var groundIcon = new Grid
                    {
                        Width = 18,
                        Height = 18,
                        ToolTip = $"AERONAVE EM SOLO • {OriginIcao}\nPosição: {AircraftLat:F4}, {AircraftLon:F4}"
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

                    _aircraftMarker.ZIndex = 200;
                    _aircraftMarker.Shape = groundIcon;
                }
                else
                {
                    // === MODO EM VOO (AIRBORNE) ===
                    // APENAS a silhueta vetorial da aeronave ciano com rotação de proa em tempo real
                    var planeGrid = new Grid
                    {
                        Width = 28,
                        Height = 28,
                        ToolTip = $"EM VOO | ALT: {Altitude:N0} ft | GS: {GroundSpeed:N0} kt | HDG: {AircraftHeading:N0}°"
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

                    _aircraftMarker.ZIndex = 200;
                    _aircraftMarker.Shape = planeGrid;
                }

                // Centraliza o mapa se estiver no início
                if (MainMap.Position.Lat == 0 && MainMap.Position.Lng == 0 && (pos.Lat != 0 || pos.Lng != 0))
                {
                    MainMap.Position = pos;
                }
            }
        }

        /// <summary>
        /// Desenha a rota até o destino selecionado utilizando um arco geodésico curvo (Great-Circle)
        /// com 20 pontos intermediários interpolados esfericamente e marcador circular sutil.
        /// </summary>
        private void UpdateDestinationRoute()
        {
            if (_destinationMarker != null) MainMap.Markers.Remove(_destinationMarker);
            if (_destinationRoute != null) MainMap.Markers.Remove(_destinationRoute);

            if (DestLat == 0 || DestLon == 0) return;

            var pStart = new PointLatLng(AircraftLat, AircraftLon);
            var pEnd = new PointLatLng(DestLat, DestLon);

            // 1. Gerar arco geodésico curvo (Great-Circle) com 20 pontos intermediários
            var points = GenerateGreatCircleArc(pStart, pEnd, 20);
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

            // 2. Marcador circular sutil no destino
            var destCircle = new Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = (Brush)new BrushConverter().ConvertFromString("#00E676")!, // Verde Lucro / VFR
                Stroke = (Brush)new BrushConverter().ConvertFromString("#0B0F14")!,
                StrokeThickness = 1.5,
                ToolTip = $"DESTINO: {DestIcao}"
            };

            _destinationMarker = new GMapMarker(pEnd)
            {
                ZIndex = 110,
                Shape = destCircle
            };
            MainMap.Markers.Add(_destinationMarker);

            // 3. Transição e enquadramento suave (fly-to / pan-zoom)
            if (AircraftLat != 0 && AircraftLon != 0 && DestLat != 0 && DestLon != 0)
            {
                double minLat = points.Min(p => p.Lat);
                double maxLat = points.Max(p => p.Lat);
                double minLon = points.Min(p => p.Lng);
                double maxLon = points.Max(p => p.Lng);

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

        private GMapMarker CreateWaypointCircle(PointLatLng pos, string hexColor, string tooltip)
        {
            var circle = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = (Brush)new BrushConverter().ConvertFromString(hexColor)!,
                Stroke = (Brush)new BrushConverter().ConvertFromString("#0B0F14")!,
                StrokeThickness = 1.5,
                ToolTip = tooltip
            };

            return new GMapMarker(pos)
            {
                ZIndex = 120,
                Shape = circle
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

                    var tglMarker = CreateWaypointCircle(
                        new PointLatLng(lat0 + (dLat * 0.4), lon0 + dLon),
                        "#FFB300",
                        $"CIRCUITO DE TRÁFEGO • {leg.OriginICAO} (TGL) • {leg.DistanceNM:N0} NM");
                    _multiLegMarkers.Add(tglMarker);
                    MainMap.Markers.Add(tglMarker);

                    plottedIcaos.Add(leg.OriginICAO);
                }
                else
                {
                    // 2. Perna de Navegação entre Aeródromos Distintos com Arco Geodésico Curvo
                    var pStart = new PointLatLng(leg.OriginLat, leg.OriginLon);
                    var pEnd = new PointLatLng(leg.DestLat, leg.DestLon);

                    if (pStart.Lat == 0 && pStart.Lng == 0 && AircraftLat != 0)
                        pStart = new PointLatLng(AircraftLat, AircraftLon);

                    if (pEnd.Lat == 0 && pEnd.Lng == 0) continue;

                    var arcPoints = GenerateGreatCircleArc(pStart, pEnd, 15);
                    allRoutePoints.AddRange(arcPoints);

                    var legRoute = new GMapRoute(arcPoints)
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
                        var originCircle = CreateWaypointCircle(pStart, "#00E676", $"PARTIDA: {leg.OriginICAO}");
                        _multiLegMarkers.Add(originCircle);
                        MainMap.Markers.Add(originCircle);
                        plottedIcaos.Add(leg.OriginICAO);
                    }

                    string pinColor = isLastLeg ? "#00E676" : "#00E5FF";
                    string pinTooltip = isLastLeg 
                        ? $"DESTINO FINAL: {leg.DestinationICAO} ({leg.DistanceNM:N0} NM)"
                        : $"ESCALA / DESTINO: {leg.DestinationICAO} ({leg.DistanceNM:N0} NM)";

                    var destCircle = CreateWaypointCircle(pEnd, pinColor, pinTooltip);
                    _multiLegMarkers.Add(destCircle);
                    MainMap.Markers.Add(destCircle);
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

        #endregion

        #region Clustering Dinâmico de Missões no Mapa

        private void UpdateMissionClusters()
        {
            foreach (var m in _missionClusterMarkers)
            {
                MainMap.Markers.Remove(m);
            }
            _missionClusterMarkers.Clear();

            if (MissionsSource == null) return;

            var validMissions = new List<(object Item, PointLatLng Pos, string Icao)>();
            foreach (var item in MissionsSource)
            {
                if (item == null) continue;
                var coords = GetMissionCoords(item);
                if (coords.HasValue && (coords.Value.lat != 0 || coords.Value.lon != 0))
                {
                    validMissions.Add((item, new PointLatLng(coords.Value.lat, coords.Value.lon), coords.Value.icao));
                }
            }

            if (validMissions.Count == 0) return;

            // Agrupamento baseado em proximidade de tela (< 45px) no nível de zoom atual
            var clusters = new List<MissionCluster>();
            foreach (var vm in validMissions)
            {
                var screenPoint = MainMap.FromLatLngToLocal(vm.Pos);
                MissionCluster? nearest = null;
                double minDistance = double.MaxValue;

                foreach (var c in clusters)
                {
                    double dx = screenPoint.X - c.ScreenPos.X;
                    double dy = screenPoint.Y - c.ScreenPos.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < 45.0 && dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = c;
                    }
                }

                if (nearest != null)
                {
                    nearest.Add(vm, screenPoint);
                }
                else
                {
                    var newCluster = new MissionCluster();
                    newCluster.Add(vm, screenPoint);
                    clusters.Add(newCluster);
                }
            }

            // Renderizar marcadores dos clusters
            foreach (var cluster in clusters)
            {
                if (cluster.Missions.Count > 1)
                {
                    // Bolha circular com contagem de missões
                    var clusterBorder = new Border
                    {
                        Background = (Brush)new BrushConverter().ConvertFromString("#1F2B37")!,
                        BorderBrush = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!,
                        BorderThickness = new Thickness(1.5),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(7, 3, 7, 3),
                        Cursor = Cursors.Hand,
                        ToolTip = $"{cluster.Missions.Count} missões nesta área (Clique para aproximar)"
                    };

                    var clusterText = new TextBlock
                    {
                        Text = $"[ {cluster.Missions.Count} ]",
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        FontSize = 10.5,
                        FontFamily = new FontFamily("JetBrains Mono, Consolas, monospace"),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    clusterBorder.Child = clusterText;

                    var cCenter = cluster.Center;
                    clusterBorder.MouseLeftButtonUp += (s, e) =>
                    {
                        e.Handled = true;
                        MainMap.Position = cCenter;
                        MainMap.Zoom += 2;
                    };

                    var clusterMarker = new GMapMarker(cluster.Center)
                    {
                        ZIndex = 115,
                        Shape = clusterBorder
                    };
                    _missionClusterMarkers.Add(clusterMarker);
                    MainMap.Markers.Add(clusterMarker);
                }
                else
                {
                    // Pin tático individual com o código DestinationICAO
                    var single = cluster.Missions[0];
                    var pinBorder = new Border
                    {
                        Background = (Brush)new BrushConverter().ConvertFromString("#E60B0F14")!,
                        BorderBrush = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!,
                        BorderThickness = new Thickness(1.2),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        Cursor = Cursors.Hand,
                        ToolTip = $"Destino: {single.Icao} (Clique para selecionar missão)"
                    };

                    var pinStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    pinStack.Children.Add(new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!,
                        Margin = new Thickness(0, 0, 4, 0)
                    });
                    pinStack.Children.Add(new TextBlock
                    {
                        Text = single.Icao,
                        Foreground = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!,
                        FontWeight = FontWeights.Bold,
                        FontSize = 10,
                        FontFamily = new FontFamily("JetBrains Mono, Consolas, monospace"),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    pinBorder.Child = pinStack;

                    pinBorder.MouseLeftButtonUp += (s, e) =>
                    {
                        e.Handled = true;
                        SelectedMission = single.Item;
                        DestLat = single.Pos.Lat;
                        DestLon = single.Pos.Lng;
                        DestIcao = single.Icao;
                        UpdateDestinationRoute();
                    };

                    var pinMarker = new GMapMarker(single.Pos)
                    {
                        ZIndex = 112,
                        Shape = pinBorder
                    };
                    _missionClusterMarkers.Add(pinMarker);
                    MainMap.Markers.Add(pinMarker);
                }
            }
        }

        private class MissionCluster
        {
            public List<(object Item, PointLatLng Pos, string Icao)> Missions { get; } = new();
            public PointLatLng Center { get; private set; }
            public GPoint ScreenPos { get; private set; }

            public void Add((object Item, PointLatLng Pos, string Icao) mission, GPoint screenPos)
            {
                Missions.Add(mission);
                double avgLat = Missions.Average(m => m.Pos.Lat);
                double avgLng = Missions.Average(m => m.Pos.Lng);
                Center = new PointLatLng(avgLat, avgLng);

                long avgX = (long)Missions.Average(m => screenPos.X);
                long avgY = (long)Missions.Average(m => screenPos.Y);
                ScreenPos = new GPoint(avgX, avgY);
            }
        }

        private static (double lat, double lon, string icao)? GetMissionCoords(object item)
        {
            if (item == null) return null;

            if (item is MissionDisplayItem mdi)
            {
                return (mdi.DestLat, mdi.DestLon, mdi.DestinationICAO);
            }

            var propLat = item.GetType().GetProperty("DestLat") ?? item.GetType().GetProperty("DestinationLat");
            var propLon = item.GetType().GetProperty("DestLon") ?? item.GetType().GetProperty("DestinationLon");
            var propIcao = item.GetType().GetProperty("DestinationICAO") ?? item.GetType().GetProperty("DestIcao");

            if (propLat != null && propLon != null)
            {
                try
                {
                    double lat = Convert.ToDouble(propLat.GetValue(item));
                    double lon = Convert.ToDouble(propLon.GetValue(item));
                    string icao = propIcao?.GetValue(item)?.ToString() ?? string.Empty;
                    return (lat, lon, icao);
                }
                catch { }
            }

            return null;
        }

        #endregion

        #region Cálculo de Arco Geodésico Esférico (Great-Circle)

        /// <summary>
        /// Gera uma sequência de pontos esfericamente interpolados (Great-Circle / Slerp)
        /// entre as coordenadas de início e fim, formando um arco geodésico suave em projeções mercator.
        /// </summary>
        public static List<PointLatLng> GenerateGreatCircleArc(PointLatLng start, PointLatLng end, int segments = 20)
        {
            var points = new List<PointLatLng>();
            if (segments < 2) segments = 2;

            double lat1 = start.Lat * Math.PI / 180.0;
            double lon1 = start.Lng * Math.PI / 180.0;
            double lat2 = end.Lat * Math.PI / 180.0;
            double lon2 = end.Lng * Math.PI / 180.0;

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                       Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
            double d = 2.0 * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));

            if (d < 1e-6)
            {
                points.Add(start);
                points.Add(end);
                return points;
            }

            double sinD = Math.Sin(d);

            for (int i = 0; i <= segments; i++)
            {
                double f = (double)i / segments;
                double A = Math.Sin((1.0 - f) * d) / sinD;
                double B = Math.Sin(f * d) / sinD;

                double x = A * Math.Cos(lat1) * Math.Cos(lon1) + B * Math.Cos(lat2) * Math.Cos(lon2);
                double y = A * Math.Cos(lat1) * Math.Sin(lon1) + B * Math.Cos(lat2) * Math.Sin(lon2);
                double z = A * Math.Sin(lat1) + B * Math.Sin(lat2);

                double lat = Math.Atan2(z, Math.Sqrt(x * x + y * y)) * 180.0 / Math.PI;
                double lon = Math.Atan2(y, x) * 180.0 / Math.PI;

                points.Add(new PointLatLng(lat, lon));
            }

            return points;
        }

        #endregion

        #region Aeroportos, Waypoints Manuais e Trajetória Gravada

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
                        Width = 6,
                        Height = 6,
                        Fill = Brushes.Transparent,
                        Stroke = Brushes.White,
                        StrokeThickness = 1,
                        ToolTip = $"{airport.ICAO} - {airport.Name}"
                    }
                };
                _airportMarkers.Add(marker);
                MainMap.Markers.Add(marker);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _waypoints.Clear();
            if (_waypointRoute != null)
            {
                MainMap.Markers.Remove(_waypointRoute);
                _waypointRoute = null;
            }
            TxtDistance.Text = "0.0 NM";
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

        #endregion
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
