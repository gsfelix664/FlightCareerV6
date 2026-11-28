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
using FlightCareer.Domain.Missions;
using FlightCareer.Domain.Utils;
using FlightCareer.UI.ViewModels;

namespace FlightCareer.UI.Utils
{
    public partial class FlightMapControl : UserControl
    {
        private GMapMarker? _aircraftMarker;
        private GMapMarker? _originMarker;
        private GMapMarker? _destinationMarker;
        private GMapRoute? _destinationRoute;
        private GMapRoute? _recordedFlightRoute;
        private GMapMarker? _originPinMarker;
        private GMapMarker? _destPinMarker;
        private readonly List<GMapRoute> _multiLegRoutes = new List<GMapRoute>();
        private readonly List<GMapMarker> _multiLegMarkers = new List<GMapMarker>();
        private readonly List<GMapMarker> _airportMarkers = new List<GMapMarker>();
        private readonly List<GMapMarker> _missionClusterMarkers = new List<GMapMarker>();
        private readonly List<GMapMarker> _contractMarkers = new List<GMapMarker>();
        private readonly List<GMapRoute> _contractRoutes = new List<GMapRoute>();
        private readonly List<PointLatLng> _routeFramingPoints = new List<PointLatLng>();
        private readonly List<PointLatLng> _contractFramingPoints = new List<PointLatLng>();

        // Dependency Properties
        public static readonly DependencyProperty RecordedPathProperty = DependencyProperty.Register("RecordedPath", typeof(System.Collections.IEnumerable), typeof(FlightMapControl), new PropertyMetadata(null, OnRecordedPathChanged));
        public static readonly DependencyProperty RouteLegsProperty = DependencyProperty.Register("RouteLegs", typeof(System.Collections.IEnumerable), typeof(FlightMapControl), new PropertyMetadata(null, OnRouteLegsChanged));
        /// <summary>
        /// Contratos a desenhar (áreas, pontos, zonas de salto, faixa de varredura). Aceita uma
        /// <see cref="Mission"/>, um <see cref="MissionDisplayItem"/> ou uma coleção deles; o próprio
        /// controle monta os visuais com <see cref="ContractMarkerBuilder"/> (quadro, briefing e HUD iguais).
        /// </summary>
        public static readonly DependencyProperty ContractMissionsProperty = DependencyProperty.Register(nameof(ContractMissions), typeof(object), typeof(FlightMapControl), new PropertyMetadata(null, OnContractMissionsChanged));
        public static readonly DependencyProperty ShowContractLabelsProperty = DependencyProperty.Register(nameof(ShowContractLabels), typeof(bool), typeof(FlightMapControl), new PropertyMetadata(true, OnShowContractLabelsChanged));
        public static readonly DependencyProperty AircraftLatProperty = DependencyProperty.Register("AircraftLat", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnAircraftPosChanged));
        public static readonly DependencyProperty AircraftLonProperty = DependencyProperty.Register("AircraftLon", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnAircraftPosChanged));
        public static readonly DependencyProperty OriginLatProperty = DependencyProperty.Register("OriginLat", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnOriginChanged));
        public static readonly DependencyProperty OriginLonProperty = DependencyProperty.Register("OriginLon", typeof(double), typeof(FlightMapControl), new PropertyMetadata(0.0, OnOriginChanged));
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
        public double OriginLat { get => (double)GetValue(OriginLatProperty); set => SetValue(OriginLatProperty, value); }
        public double OriginLon { get => (double)GetValue(OriginLonProperty); set => SetValue(OriginLonProperty, value); }
        public double DestLat { get => (double)GetValue(DestLatProperty); set => SetValue(DestLatProperty, value); }
        public double DestLon { get => (double)GetValue(DestLonProperty); set => SetValue(DestLonProperty, value); }
        public string OriginIcao { get => (string)GetValue(OriginIcaoProperty); set => SetValue(OriginIcaoProperty, value); }
        public string DestIcao { get => (string)GetValue(DestIcaoProperty); set => SetValue(DestIcaoProperty, value); }
        public System.Collections.ObjectModel.ObservableCollection<Airport> Airports { get => (System.Collections.ObjectModel.ObservableCollection<Airport>)GetValue(AirportsProperty); set => SetValue(AirportsProperty, value); }
        public bool IsOnGround { get => (bool)GetValue(IsOnGroundProperty); set => SetValue(IsOnGroundProperty, value); }
        public double AircraftHeading { get => (double)GetValue(AircraftHeadingProperty); set => SetValue(AircraftHeadingProperty, value); }
        public double Altitude { get => (double)GetValue(AltitudeProperty); set => SetValue(AltitudeProperty, value); }
        public double GroundSpeed { get => (double)GetValue(GroundSpeedProperty); set => SetValue(GroundSpeedProperty, value); }
        public System.Collections.IEnumerable? MissionsSource { get => (System.Collections.IEnumerable?)GetValue(MissionsSourceProperty); set => SetValue(MissionsSourceProperty, value); }
        public object? SelectedMission { get => GetValue(SelectedMissionProperty); set => SetValue(SelectedMissionProperty, value); }
        public object? ContractMissions { get => GetValue(ContractMissionsProperty); set => SetValue(ContractMissionsProperty, value); }
        public bool ShowContractLabels { get => (bool)GetValue(ShowContractLabelsProperty); set => SetValue(ShowContractLabelsProperty, value); }

        private const double AircraftAnchorToleranceDeg = 0.05; // ~3 NM
        private ContractMapVisuals _contractVisuals = new();
        private GeoPoint? _lastDrawnOriginAnchor;
        private GeoPoint? _lastDrawnDestAnchor;
        private bool _originAnchoredByAircraft;
        private object? _aircraftAnchorContractMissions;
        private string? _aircraftAnchorOriginIcao;

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
                map.RefreshContractAnchors();
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
                map.RefreshContractAnchors();
            }
        }

        private static void OnOriginChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                map.UpdateDestinationRoute();
                map.RefreshContractAnchors();
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
                map.RefreshContractAnchors();
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

        private static void OnContractMissionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldColl)
                    oldColl.CollectionChanged -= map.ContractMissions_CollectionChanged;

                if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newColl)
                    newColl.CollectionChanged += map.ContractMissions_CollectionChanged;

                map.RebuildContractVisuals();
            }
        }

        private static void OnShowContractLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlightMapControl map)
            {
                map.UpdateContractVisuals();
            }
        }

        private void ContractMissions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RebuildContractVisuals();
        }

        private void RebuildContractVisuals()
        {
            var orig = ResolveAirport(OriginIcao);
            var dest = ResolveAirport(DestIcao);

            _lastDrawnOriginAnchor = orig;
            _lastDrawnDestAnchor = dest;
            _aircraftAnchorContractMissions = ContractMissions;
            _aircraftAnchorOriginIcao = OriginIcao;
            _originAnchoredByAircraft = (OriginLat == 0 && OriginLon == 0 && orig != null);

            _contractVisuals = ContractMarkerBuilder.Build(ExtractMissions(ContractMissions), ResolveAirport);
            UpdateContractVisuals();
        }

        private GeoPoint? ResolveAirport(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return null;

            if (string.Equals(icao, OriginIcao, StringComparison.OrdinalIgnoreCase))
            {
                if (OriginLat != 0 || OriginLon != 0)
                {
                    return new GeoPoint(OriginLat, OriginLon);
                }

                var cached = _originAnchoredByAircraft &&
                    ReferenceEquals(ContractMissions, _aircraftAnchorContractMissions) &&
                    string.Equals(OriginIcao, _aircraftAnchorOriginIcao, StringComparison.OrdinalIgnoreCase)
                        ? _lastDrawnOriginAnchor
                        : null;

                // Nunca ancorar a origem num avião em voo: fica a última posição em solo
                if (!IsOnGround || (AircraftLat == 0 && AircraftLon == 0)) return cached;

                // Táxi não redesenha o mapa; salto grande (troca de aeroporto, lat antes de lon) atualiza
                if (cached != null &&
                    Math.Abs(cached.Lat - AircraftLat) < AircraftAnchorToleranceDeg &&
                    Math.Abs(cached.Lon - AircraftLon) < AircraftAnchorToleranceDeg)
                {
                    return cached;
                }

                return new GeoPoint(AircraftLat, AircraftLon);
            }

            if (string.Equals(icao, DestIcao, StringComparison.OrdinalIgnoreCase))
            {
                if (DestLat != 0 || DestLon != 0)
                {
                    return new GeoPoint(DestLat, DestLon);
                }
                return null;
            }

            return null;
        }

        private void RefreshContractAnchors()
        {
            // Sem contrato não há linha a redesenhar (e o reenquadramento atrapalharia a rota)
            if (!ExtractMissions(ContractMissions).Any()) return;

            var currentOrigin = ResolveAirport(OriginIcao);
            var currentDest = ResolveAirport(DestIcao);

            bool originChanged = !AreAnchorsEqual(_lastDrawnOriginAnchor, currentOrigin);
            bool destChanged = !AreAnchorsEqual(_lastDrawnDestAnchor, currentDest);

            if (originChanged || destChanged)
            {
                RebuildContractVisuals();
            }
        }

        private static bool AreAnchorsEqual(GeoPoint? a, GeoPoint? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return Math.Abs(a.Lat - b.Lat) < 1e-7 && Math.Abs(a.Lon - b.Lon) < 1e-7;
        }

        private static IEnumerable<Mission> ExtractMissions(object? source)
        {
            switch (source)
            {
                case null: yield break;
                case Mission m: yield return m; yield break;
                case MissionDisplayItem item: yield return item.Mission; yield break;
                case System.Collections.IEnumerable list:
                    foreach (var o in list)
                    {
                        if (o is Mission lm) yield return lm;
                        else if (o is MissionDisplayItem li) yield return li.Mission;
                    }
                    yield break;
            }
        }

        private static List<PointLatLng> GenerateCirclePoints(double centerLat, double centerLon, double radiusNm, int segments = 36)
        {
            var points = new List<PointLatLng>(segments + 1);
            double latRad = centerLat * Math.PI / 180.0;
            double lonRad = centerLon * Math.PI / 180.0;
            double dR = radiusNm / 3440.065;

            for (int i = 0; i <= segments; i++)
            {
                double bearing = (i * 360.0 / segments) * Math.PI / 180.0;
                double pLat = Math.Asin(Math.Sin(latRad) * Math.Cos(dR) + Math.Cos(latRad) * Math.Sin(dR) * Math.Cos(bearing));
                double pLon = lonRad + Math.Atan2(Math.Sin(bearing) * Math.Sin(dR) * Math.Cos(latRad), Math.Cos(dR) - Math.Sin(latRad) * Math.Sin(pLat));
                points.Add(new PointLatLng(pLat * 180.0 / Math.PI, pLon * 180.0 / Math.PI));
            }
            return points;
        }

        private void UpdateContractVisuals()
        {
            foreach (var m in _contractMarkers) MainMap.Markers.Remove(m);
            _contractMarkers.Clear();
            foreach (var r in _contractRoutes) MainMap.Markers.Remove(r);
            _contractRoutes.Clear();
            _contractFramingPoints.Clear();

            var surveyArea = _contractVisuals.SurveyArea;
            // Sem contrato desenhado, o enquadramento fica com a rota (não muda o zoom)
            if (_contractVisuals.Markers.Count == 0 && surveyArea == null && _contractVisuals.Routes.Count == 0)
            {
                ApplyCombinedFraming();
                return;
            }

            if (OriginLat != 0 && OriginLon != 0)
            {
                _contractFramingPoints.Add(new PointLatLng(OriginLat, OriginLon));
            }
            else if (AircraftLat != 0 && AircraftLon != 0)
            {
                // Quadro de missões: a origem chega como posição da aeronave
                _contractFramingPoints.Add(new PointLatLng(AircraftLat, AircraftLon));
            }

            if (DestLat != 0 && DestLon != 0)
            {
                _contractFramingPoints.Add(new PointLatLng(DestLat, DestLon));
            }

            {
                foreach (var marker in _contractVisuals.Markers)
                {
                    {
                        var pos = new PointLatLng(marker.Lat, marker.Lon);
                        _contractFramingPoints.Add(pos);

                        switch (marker.Kind)
                        {
                            case ContractMarkerKind.Point:
                            {
                                var gMarker = CreateContractMarker(pos, marker.Label, "#FFD600", 120);
                                _contractMarkers.Add(gMarker);
                                MainMap.Markers.Add(gMarker);
                                break;
                            }

                            case ContractMarkerKind.Area:
                            {
                                double r = Math.Max(0.5, marker.RadiusNm);
                                var circlePts = GenerateCirclePoints(marker.Lat, marker.Lon, r);
                                _contractFramingPoints.AddRange(circlePts);

                                var circleRoute = new GMapRoute(circlePts)
                                {
                                    ZIndex = 95,
                                    Shape = new Path
                                    {
                                        Stroke = (Brush)new BrushConverter().ConvertFromString("#FFD600")!,
                                        StrokeThickness = 1.5,
                                        StrokeDashArray = new DoubleCollection { 4, 3 },
                                        Opacity = 0.9
                                    }
                                };
                                _contractRoutes.Add(circleRoute);
                                MainMap.Markers.Add(circleRoute);

                                var gMarker = CreateContractMarker(pos, marker.Label, "#FFD600", 120);
                                _contractMarkers.Add(gMarker);
                                MainMap.Markers.Add(gMarker);
                                break;
                            }

                            case ContractMarkerKind.DropZone:
                            {
                                double r = Math.Max(0.5, marker.RadiusNm);
                                var dzPts = GenerateCirclePoints(marker.Lat, marker.Lon, r);
                                _contractFramingPoints.AddRange(dzPts);

                                var dzRoute = new GMapRoute(dzPts)
                                {
                                    ZIndex = 95,
                                    Shape = new Path
                                    {
                                        Stroke = (Brush)new BrushConverter().ConvertFromString("#FF9100")!,
                                        StrokeThickness = 2.0,
                                        Opacity = 0.95
                                    }
                                };
                                _contractRoutes.Add(dzRoute);
                                MainMap.Markers.Add(dzRoute);

                                var gMarker = CreateContractMarker(pos, marker.Label, "#FF9100", 120);
                                _contractMarkers.Add(gMarker);
                                MainMap.Markers.Add(gMarker);
                                break;
                            }

                            case ContractMarkerKind.Stop:
                            {
                                var gMarker = CreateContractMarker(pos, marker.Label, "#B388FF", 121);
                                _contractMarkers.Add(gMarker);
                                MainMap.Markers.Add(gMarker);
                                break;
                            }
                        }
                    }
                }
            }

            if (surveyArea != null)
            {
                if (surveyArea.Corners != null && surveyArea.Corners.Count >= 3)
                {
                    var rectPts = surveyArea.Corners.Select(c => new PointLatLng(c.Lat, c.Lon)).ToList();
                    rectPts.Add(rectPts[0]); // close loop
                    _contractFramingPoints.AddRange(rectPts);

                    var surveyRect = new GMapRoute(rectPts)
                    {
                        ZIndex = 96,
                        Shape = new Path
                        {
                            Stroke = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!,
                            StrokeThickness = 1.8,
                            Opacity = 0.95
                        }
                    };
                    _contractRoutes.Add(surveyRect);
                    MainMap.Markers.Add(surveyRect);
                }

                if (surveyArea.Legs != null)
                {
                    foreach (var leg in surveyArea.Legs)
                    {
                        if (leg == null || leg.Count < 2) continue;
                        var legPts = new List<PointLatLng>
                        {
                            new PointLatLng(leg[0].Lat, leg[0].Lon),
                            new PointLatLng(leg[1].Lat, leg[1].Lon)
                        };
                        _contractFramingPoints.AddRange(legPts);

                        var legRoute = new GMapRoute(legPts)
                        {
                            ZIndex = 94,
                            Shape = new Path
                            {
                                Stroke = (Brush)new BrushConverter().ConvertFromString("#8000E5FF")!,
                                StrokeThickness = 1.0,
                                StrokeDashArray = new DoubleCollection { 2, 2 }
                            }
                        };
                        _contractRoutes.Add(legRoute);
                        MainMap.Markers.Add(legRoute);
                    }
                }
            }

            if (_contractVisuals.Routes.Count > 0)
            {
                foreach (var route in _contractVisuals.Routes)
                {
                    if (route.Points == null || route.Points.Count < 2) continue;

                    var routePoints = new List<PointLatLng>();
                    for (int i = 0; i < route.Points.Count - 1; i++)
                    {
                        var pStart = new PointLatLng(route.Points[i].Lat, route.Points[i].Lon);
                        var pEnd = new PointLatLng(route.Points[i + 1].Lat, route.Points[i + 1].Lon);
                        var arc = GenerateGreatCircleArc(pStart, pEnd, 15);
                        if (routePoints.Count > 0 && arc.Count > 0)
                        {
                            routePoints.AddRange(arc.Skip(1));
                        }
                        else
                        {
                            routePoints.AddRange(arc);
                        }
                    }

                    if (routePoints.Count >= 2)
                    {
                        _contractFramingPoints.AddRange(routePoints);

                        var gRoute = new GMapRoute(routePoints)
                        {
                            ZIndex = 90,
                            Shape = new Path
                            {
                                Stroke = (Brush)new BrushConverter().ConvertFromString("#B388FF")!,
                                StrokeThickness = 1.8,
                                StrokeDashArray = new DoubleCollection { 2, 2 },
                                Opacity = 0.9
                            }
                        };
                        _contractRoutes.Add(gRoute);
                        MainMap.Markers.Add(gRoute);
                    }
                }
            }

            ApplyCombinedFraming();
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
                    // A rota acabou de reenquadrar: volta a incluir a área/pontos do contrato
                    map.UpdateContractVisuals();
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
            if (_originMarker != null) MainMap.Markers.Remove(_originMarker);
            if (_destinationMarker != null) MainMap.Markers.Remove(_destinationMarker);
            if (_destinationRoute != null) MainMap.Markers.Remove(_destinationRoute);

            bool hasRouteLegs = RouteLegs != null && RouteLegs.OfType<object>().Any();
            if (!hasRouteLegs)
            {
                _routeFramingPoints.Clear();
            }

            // 1. Marcador circular sutil no ponto de partida (mesmo estilo do destino, ciano neon com tooltip ICAO)
            if (OriginLat != 0 || OriginLon != 0)
            {
                var pOrigin = new PointLatLng(OriginLat, OriginLon);
                if (!hasRouteLegs)
                {
                    _routeFramingPoints.Add(pOrigin);
                }

                var originCircle = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = (Brush)new BrushConverter().ConvertFromString("#00E5FF")!, // Ciano Aviônica
                    Stroke = (Brush)new BrushConverter().ConvertFromString("#0B0F14")!,
                    StrokeThickness = 1.5,
                    ToolTip = !string.IsNullOrWhiteSpace(OriginIcao) ? $"ORIGEM: {OriginIcao}" : "ORIGEM"
                };

                _originMarker = new GMapMarker(pOrigin)
                {
                    ZIndex = 110,
                    Shape = originCircle,
                    Offset = new Point(-4.5, -4.5)
                };
                MainMap.Markers.Add(_originMarker);
            }

            if (DestLat == 0 && DestLon == 0)
            {
                if (_contractFramingPoints.Count > 0)
                {
                    ApplyCombinedFraming();
                }
                return;
            }

            var pStart = (OriginLat != 0 || OriginLon != 0)
                ? new PointLatLng(OriginLat, OriginLon)
                : new PointLatLng(AircraftLat, AircraftLon);
            var pEnd = new PointLatLng(DestLat, DestLon);

            if (!hasRouteLegs)
            {
                _routeFramingPoints.Add(pEnd);
            }

            // 2. Gerar arco geodésico curvo (Great-Circle) com 20 pontos intermediários quando há origem válida
            if (pStart.Lat != 0 || pStart.Lng != 0)
            {
                var points = GenerateGreatCircleArc(pStart, pEnd, 20);
                if (!hasRouteLegs)
                {
                    _routeFramingPoints.AddRange(points);
                }

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
            }

            // 3. Marcador circular sutil no destino
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
                Shape = destCircle,
                Offset = new Point(-4.5, -4.5)
            };
            MainMap.Markers.Add(_destinationMarker);

            // 4. Transição e enquadramento suave (fly-to / pan-zoom)
            if (_contractFramingPoints.Count > 0)
            {
                ApplyCombinedFraming();
            }
            else if (_routeFramingPoints.Count >= 2)
            {
                double minLat = _routeFramingPoints.Min(p => p.Lat);
                double maxLat = _routeFramingPoints.Max(p => p.Lat);
                double minLon = _routeFramingPoints.Min(p => p.Lng);
                double maxLon = _routeFramingPoints.Max(p => p.Lng);

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

        private GMapMarker CreateRoutePointCircle(PointLatLng pos, string hexColor, string tooltip)
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
            _routeFramingPoints.Clear();
            ClearMultiLegVisuals();

            if (RouteLegs == null)
            {
                UpdateDestinationRoute();
                ApplyCombinedFraming();
                return;
            }

            var legs = RouteLegs.OfType<ViewModels.FlightLegModel>().ToList();
            if (!legs.Any())
            {
                UpdateDestinationRoute();
                ApplyCombinedFraming();
                return;
            }

            // Ocultar rota simples de destino único se estiver em modo multi-pernas
            if (_destinationRoute != null) MainMap.Markers.Remove(_destinationRoute);
            if (_destinationMarker != null) MainMap.Markers.Remove(_destinationMarker);
            if (_originMarker != null) MainMap.Markers.Remove(_originMarker);

            if (AircraftLat != 0 && AircraftLon != 0)
            {
                _routeFramingPoints.Add(new PointLatLng(AircraftLat, AircraftLon));
            }

            var plottedIcaos = new HashSet<string>();

            for (int i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                bool isFirstLeg = (i == 0);
                bool isLastLeg = (i == legs.Count - 1);

                // 1. Circuito de Tráfego / Toque e Arremetida (TGL)
                if (leg.IsTglOrLocal)
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
                    _routeFramingPoints.AddRange(circuitPoints);

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

                    var tglMarker = CreateRoutePointCircle(
                        new PointLatLng(lat0 + (dLat * 0.4), lon0 + dLon),
                        "#FFB300",
                        $"CIRCUITO DE TRÁFEGO • {leg.OriginICAO} (TGL) • {leg.DistanceNM:N0} NM");
                    _multiLegMarkers.Add(tglMarker);
                    MainMap.Markers.Add(tglMarker);

                    plottedIcaos.Add(leg.OriginICAO);
                }
                else if (leg.OriginICAO == leg.DestinationICAO)
                {
                    // Perna com origem = destino e !IsTglOrLocal (local): só o marcador de partida ("BASE: {icao}") se ainda não plotado, e o ponto entra no enquadramento; sem circuito.
                    double lat0 = leg.OriginLat != 0 ? leg.OriginLat : AircraftLat;
                    double lon0 = leg.OriginLon != 0 ? leg.OriginLon : AircraftLon;

                    if (lat0 != 0 || lon0 != 0)
                    {
                        var pBase = new PointLatLng(lat0, lon0);
                        _routeFramingPoints.Add(pBase);

                        if (!plottedIcaos.Contains(leg.OriginICAO))
                        {
                            var baseCircle = CreateRoutePointCircle(pBase, "#00E676", $"BASE: {leg.OriginICAO}");
                            _multiLegMarkers.Add(baseCircle);
                            MainMap.Markers.Add(baseCircle);
                            plottedIcaos.Add(leg.OriginICAO);
                        }
                    }
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
                    _routeFramingPoints.AddRange(arcPoints);

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
                        var originCircle = CreateRoutePointCircle(pStart, "#00E676", $"PARTIDA: {leg.OriginICAO}");
                        _multiLegMarkers.Add(originCircle);
                        MainMap.Markers.Add(originCircle);
                        plottedIcaos.Add(leg.OriginICAO);
                    }

                    string pinColor = isLastLeg ? "#00E676" : "#00E5FF";
                    string pinTooltip = isLastLeg 
                        ? $"DESTINO FINAL: {leg.DestinationICAO} ({leg.DistanceNM:N0} NM)"
                        : $"ESCALA / DESTINO: {leg.DestinationICAO} ({leg.DistanceNM:N0} NM)";

                    var destCircle = CreateRoutePointCircle(pEnd, pinColor, pinTooltip);
                    _multiLegMarkers.Add(destCircle);
                    MainMap.Markers.Add(destCircle);
                    plottedIcaos.Add(leg.DestinationICAO);
                }
            }

            ApplyCombinedFraming();
        }

        private void ApplyCombinedFraming()
        {
            try
            {
                var allPoints = new List<PointLatLng>(_routeFramingPoints.Count + _contractFramingPoints.Count);
                allPoints.AddRange(_routeFramingPoints);
                allPoints.AddRange(_contractFramingPoints);

                if (allPoints.Count < 2) return;

                double minLat = allPoints.Min(p => p.Lat);
                double maxLat = allPoints.Max(p => p.Lat);
                double minLon = allPoints.Min(p => p.Lng);
                double maxLon = allPoints.Max(p => p.Lng);

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FlightMapControl] Erro ao enquadrar mapa: {ex.Message}");
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

        #region Aeroportos e Trajetória Gravada

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

        private GMapMarker CreateContractMarker(PointLatLng pos, string label, string colorHex, int zIndex = 120)
        {
            if (!ShowContractLabels)
            {
                return new GMapMarker(pos)
                {
                    ZIndex = zIndex,
                    Shape = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = (Brush)new BrushConverter().ConvertFromString(colorHex)!,
                        Stroke = (Brush)new BrushConverter().ConvertFromString("#0B0F14")!,
                        StrokeThickness = 2
                    },
                    Offset = new Point(-5, -5)
                };
            }

            return new GMapMarker(pos)
            {
                ZIndex = zIndex,
                Shape = CreatePinBadge(label, colorHex),
                Offset = new Point(-5, -12)
            };
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
