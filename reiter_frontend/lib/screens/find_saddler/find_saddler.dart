import 'dart:ui' as ui;
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_map_cancellable_tile_provider/flutter_map_cancellable_tile_provider.dart';
import 'package:latlong2/latlong.dart' show LatLng;
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import 'package:flutter_svg/flutter_svg.dart';
import '../../widgets/sidenav.dart';
import '../../models/saddler.dart';
import '../../models/horse.dart';
import '../../services/saddler_service.dart';
import '../../services/horse_service.dart';
import 'saddler_profile_screen.dart';
import '../horses/horse_profil_screen.dart';

class FindSaddler extends StatefulWidget {
  const FindSaddler({super.key});

  @override
  State<FindSaddler> createState() => _FindSaddlerState();
}

class _FindSaddlerState extends State<FindSaddler> {
  final SaddlerService _saddlerService = SaddlerService();
  final HorseService _horseService = HorseService();
  final MapController _mapController = MapController();
  late TextEditingController _searchController;

  List<Saddler> _saddlers = [];
  List<Horse> _horses = [];
  Saddler? _selectedSaddler;
  List<Horse>? _selectedHorses;
  bool _isLoading = true;
  bool _showSearch = false;
  bool _isSatelliteView = false;
  bool _showFavoritesList = false;
  String _searchQuery = '';

  // Center on Upper Austria (Linz area)
  static const LatLng _initialCenter = LatLng(48.27, 14.20);
  static const double _initialZoom = 10.5;

  // User's home location
  static const LatLng _userHomeLocation = LatLng(48.2856, 14.2858);

  @override
  void initState() {
    super.initState();
    _searchController = TextEditingController();
    _loadData();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    final saddlers = await _saddlerService.getSaddlersWithLocation();
    final horses = await _horseService.getHorses();
    setState(() {
      _saddlers = saddlers;
      _horses = horses;
      _isLoading = false;
    });
  }

  List<Saddler> get _filteredSaddlers {
    var filtered = _saddlers;

    if (_searchQuery.isNotEmpty) {
      final query = _searchQuery.toLowerCase();
      filtered = filtered.where((s) =>
        s.name.toLowerCase().contains(query) ||
        (s.city?.toLowerCase().contains(query) ?? false)
      ).toList();
    }

    return filtered;
  }

  List<Saddler> get _favoriteSaddlers {
    return _saddlers.where((s) => s.isFavorite).toList();
  }

  // Group horses by stable location
  Map<String, List<Horse>> get _horsesGroupedByLocation {
    final Map<String, List<Horse>> grouped = {};
    for (final horse in _horses) {
      if (horse.hasStableLocation) {
        final key = '${horse.stableLatitude},${horse.stableLongitude}';
        grouped.putIfAbsent(key, () => []);
        grouped[key]!.add(horse);
      }
    }
    return grouped;
  }

  void _selectSaddler(Saddler saddler) {
    setState(() {
      _selectedSaddler = saddler;
      _selectedHorses = null;
      _showSearch = false;
    });

    if (saddler.hasLocation) {
      _mapController.move(
        LatLng(saddler.latitude!, saddler.longitude!),
        14.0,
      );
    }
  }

  void _selectHorses(List<Horse> horses) {
    setState(() {
      _selectedHorses = horses;
      _selectedSaddler = null;
      _showSearch = false;
    });

    if (horses.isNotEmpty && horses.first.hasStableLocation) {
      _mapController.move(
        LatLng(horses.first.stableLatitude!, horses.first.stableLongitude!),
        14.0,
      );
    }
  }

  void _toggleFavoritesList() {
    setState(() {
      _showFavoritesList = !_showFavoritesList;
      if (_showFavoritesList) {
        _showSearch = false;
        _selectedSaddler = null;
        _selectedHorses = null;
      }
    });
  }

  void _closeFavoritesList() {
    setState(() {
      _showFavoritesList = false;
    });
  }

  void _toggleSearch() {
    setState(() {
      _showSearch = !_showSearch;
    });
  }

  void _toggleMapType() {
    setState(() {
      _isSatelliteView = !_isSatelliteView;
    });
  }

  void _goToUserHome() {
    _mapController.move(_userHomeLocation, 14.0);
    setState(() {
      _selectedSaddler = null;
      _selectedHorses = null;
      _showSearch = false;
    });
  }

  Future<void> _toggleFavorite(Saddler saddler) async {
    await _saddlerService.toggleFavorite(saddler.id);
    await _loadData();

    if (_selectedSaddler?.id == saddler.id) {
      final updated = await _saddlerService.getSaddlerById(saddler.id);
      setState(() {
        _selectedSaddler = updated;
      });
    }
  }

  void _closePopup() {
    setState(() {
      _selectedSaddler = null;
      _selectedHorses = null;
    });
  }

  void _closeSearch() {
    setState(() {
      _showSearch = false;
    });
  }

  void _openProfile(Saddler saddler) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => SaddlerProfileScreen(saddler: saddler),
      ),
    ).then((_) => _loadData());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      drawer: const SideNav(),
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 3,
        shadowColor: Colors.black.withValues(alpha: 0.3),
        leading: Builder(
          builder: (context) => IconButton(
            icon: const Icon(Icons.menu, color: Colors.black),
            onPressed: () => Scaffold.of(context).openDrawer(),
          ),
        ),
        title: const Text(
          'EquiPressure',
          style: TextStyle(
            color: Colors.black,
            fontWeight: FontWeight.w600,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : Stack(
              children: [
                _buildMap(),
                _buildSearchHeader(),
                if (_showSearch) _buildSearchDropdown(),
                _buildTopRightButtons(),
                _buildBottomLeftButtons(),
                if (_selectedSaddler != null) _buildSaddlerPopup(),
                if (_selectedHorses != null) _buildHorsesPopup(),
                if (_showFavoritesList) _buildFavoritesList(),
              ],
            ),
    );
  }

  Widget _buildSearchHeader() {
    final hintText = _showListView 
        ? 'Sattler*innen suchen ...'
        : 'Sattler*innen oder Pferde suchen ...';

    return Positioned(
      top: 16,
      left: 16,
      right: 16,
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.15),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: TextField(
          controller: _searchController,
          decoration: InputDecoration(
            hintText: hintText,
            prefixIcon: const Icon(Icons.search),
            suffixIcon: _searchQuery.isNotEmpty
                ? IconButton(
                    icon: const Icon(Icons.clear),
                    onPressed: () {
                      _searchController.clear();
                      setState(() {
                        _searchQuery = '';
                      });
                    },
                  )
                : null,
            border: InputBorder.none,
            contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
          ),
          onChanged: (value) {
            setState(() {
              _searchQuery = value;
            });
          },
        ),
      ),
    );
  }

  Widget _buildSearchDropdown() {
    final filteredHorses = _filteredHorses;
    final filteredSaddlers = _filteredSaddlers;
    final totalResults = filteredHorses.length + filteredSaddlers.length;

    return Positioned(
      top: 70,
      left: 16,
      right: 16,
      child: Container(
        constraints: const BoxConstraints(maxHeight: 300),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.15),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: totalResults == 0
            ? Padding(
                padding: const EdgeInsets.symmetric(vertical: 16),
                child: Center(
                  child: Text(
                    'Keine Treffer gefunden',
                    style: TextStyle(
                      color: Colors.grey[600],
                      fontSize: 14,
                    ),
                  ),
                ),
              )
            : ListView(
                shrinkWrap: true,
                padding: EdgeInsets.zero,
                children: [
                  // Horses section
                  if (filteredHorses.isNotEmpty) ...[
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
                      child: Text(
                        'Pferde',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: Colors.grey[600],
                        ),
                      ),
                    ),
                    ...filteredHorses.map((horse) => ListTile(
                      leading: SvgPicture.asset(
                        'assets/icon/horseIcon.svg',
                        width: 24,
                        height: 24,
                        colorFilter: const ColorFilter.mode(Colors.black, BlendMode.srcIn),
                      ),
                      title: Text(horse.name),
                      subtitle: Text(horse.stableCity ?? 'Stall'),
                      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                      onTap: () {
                        // Find all horses at the same location
                        final groupKey = '${horse.stableLatitude},${horse.stableLongitude}';
                        final horsesAtLocation = _horsesGroupedByLocation[groupKey] ?? [horse];
                        _selectHorses(horsesAtLocation);
                        _closeSearch();
                      },
                    )),
                  ],
                  // Saddlers section
                  if (filteredSaddlers.isNotEmpty) ...[
                    if (filteredHorses.isNotEmpty)
                      const Divider(height: 1),
                    Padding(
                      padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
                      child: Text(
                        'Sattler*innen',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: Colors.grey[600],
                        ),
                      ),
                    ),
                    ...filteredSaddlers.map((saddler) => ListTile(
                      leading: _buildSmallAvatar(saddler),
                      title: Text(saddler.name),
                      subtitle: Text(saddler.city ?? ''),
                      trailing: saddler.isFavorite
                          ? const Icon(Icons.favorite, color: Colors.red, size: 20)
                          : null,
                      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                      onTap: () {
                        _selectSaddler(saddler);
                        _closeSearch();
                      },
                    )),
                  ],
                ],
              ),
      ),
    );
  }

  Widget _buildTopRightButtons() {
    return Positioned(
      top: 72,
      right: 16,
      child: Column(
        children: [
          // Favorites list toggle
          _buildSmallButton(
            icon: _showFavoritesList ? Icons.favorite : Icons.favorite_border,
            color: _showFavoritesList ? Colors.red : Colors.grey[700]!,
            onTap: _toggleFavoritesList,
            isActive: _showFavoritesList,
          ),
          const SizedBox(height: 8),
          // Map type toggle
          _buildSmallButton(
            icon: _isSatelliteView ? Icons.map : Icons.satellite_alt,
            onTap: _toggleMapType,
          ),
        ],
      ),
    );
  }

  Widget _buildSmallButton({
    required IconData icon,
    required VoidCallback onTap,
    Color? color,
    bool isActive = false,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 40,
        height: 40,
        decoration: BoxDecoration(
          color: isActive ? const Color(0xFF6B4C9A) : Colors.white,
          borderRadius: BorderRadius.circular(8),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.15),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Icon(
          icon,
          color: isActive ? Colors.white : (color ?? Colors.grey[700]),
          size: 22,
        ),
      ),
    );
  }

  Widget _buildBottomLeftButtons() {
    final bottomOffset = (_selectedSaddler != null || _selectedHorses != null) ? 160.0 : 24.0;

    return Positioned(
      left: 16,
      bottom: bottomOffset,
      child: Column(
        children: [
          // Home button
          _buildCircleButton(
            icon: Icons.home,
            onTap: _goToUserHome,
            tooltip: 'Mein Zuhause',
          ),
          const SizedBox(height: 12),
          // My location button
          _buildCircleButton(
            icon: Icons.my_location,
            onTap: () {
              _mapController.move(_initialCenter, _initialZoom);
              _closePopup();
            },
            tooltip: 'Übersicht',
          ),
        ],
      ),
    );
  }

  Widget _buildCircleButton({
    required IconData icon,
    required VoidCallback onTap,
    String? tooltip,
  }) {
    final button = GestureDetector(
      onTap: onTap,
      child: Container(
        width: 48,
        height: 48,
        decoration: BoxDecoration(
          color: Colors.white,
          shape: BoxShape.circle,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.2),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Icon(
          icon,
          color: Colors.grey[700],
          size: 24,
        ),
      ),
    );

    if (tooltip != null) {
      return Tooltip(message: tooltip, child: button);
    }
    return button;
  }

  Widget _buildMap() {
    final tileUrl = _isSatelliteView
        ? 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}'
        : 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';

    return FlutterMap(
      mapController: _mapController,
      options: MapOptions(
        initialCenter: _initialCenter,
        initialZoom: _initialZoom,
        onTap: (_, __) {
          _closePopup();
          _closeSearch();
        },
      ),
      children: [
        TileLayer(
          urlTemplate: tileUrl,
          userAgentPackageName: 'com.equipressure.app',
          tileProvider: CancellableNetworkTileProvider(),
        ),
        MarkerLayer(
          markers: [
            // User home marker
            _buildHomeMarker(),
            // Horse stable markers
            ..._buildHorseMarkers(),
            // Saddler markers
            ..._filteredSaddlers.map((saddler) => _buildSaddlerMarker(saddler)),
          ],
        ),
      ],
    );
  }

  Marker _buildHomeMarker() {
    return Marker(
      point: _userHomeLocation,
      width: 44,
      height: 54,
      child: GestureDetector(
        onTap: _goToUserHome,
        child: Column(
          children: [
            Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                color: const Color(0xFF2196F3),
                shape: BoxShape.circle,
                border: Border.all(color: Colors.white, width: 3),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.3),
                    blurRadius: 6,
                    offset: const Offset(0, 3),
                  ),
                ],
              ),
              child: const Icon(
                Icons.home,
                size: 22,
                color: Colors.white,
              ),
            ),
            CustomPaint(
              size: const Size(14, 10),
              painter: _MarkerTrianglePainter(color: const Color(0xFF2196F3)),
            ),
          ],
        ),
      ),
    );
  }

  List<Marker> _buildHorseMarkers() {
    final markers = <Marker>[];

    _horsesGroupedByLocation.forEach((key, horses) {
      final first = horses.first;
      markers.add(
        Marker(
          point: LatLng(first.stableLatitude!, first.stableLongitude!),
          width: 44,
          height: 54,
          child: GestureDetector(
            onTap: () => _selectHorses(horses),
            child: Column(
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: const Color(0xFF8BC34A),
                    shape: BoxShape.circle,
                    border: Border.all(color: Colors.white, width: 3),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: 0.3),
                        blurRadius: 6,
                        offset: const Offset(0, 3),
                      ),
                    ],
                  ),
                  child: Stack(
                    alignment: Alignment.center,
                    children: [
                      const Icon(
                        FontAwesomeIcons.horseHead,
                        size: 18,
                        color: Colors.white,
                      ),
                      if (horses.length > 1)
                        Positioned(
                          right: 0,
                          top: 0,
                          child: Container(
                            padding: const EdgeInsets.all(4),
                            decoration: const BoxDecoration(
                              color: Colors.red,
                              shape: BoxShape.circle,
                            ),
                            child: Text(
                              '${horses.length}',
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 10,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                        ),
                    ],
                  ),
                ),
                CustomPaint(
                  size: const Size(14, 10),
                  painter: _MarkerTrianglePainter(color: const Color(0xFF8BC34A)),
                ),
              ],
            ),
          ),
        ),
      );
    });

    return markers;
  }

  Marker _buildSaddlerMarker(Saddler saddler) {
    final isSelected = _selectedSaddler?.id == saddler.id;
    final isFavorite = saddler.isFavorite;

    return Marker(
      point: LatLng(saddler.latitude!, saddler.longitude!),
      width: 44,
      height: 54,
      child: GestureDetector(
        onTap: () => _selectSaddler(saddler),
        child: Column(
          children: [
            Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                color: isSelected
                    ? const Color(0xFF6B4C9A)
                    : isFavorite
                        ? const Color(0xFFE53935)
                        : Colors.white,
                shape: BoxShape.circle,
                border: Border.all(
                  color: isSelected
                      ? const Color(0xFF6B4C9A)
                      : isFavorite
                          ? const Color(0xFFE53935)
                          : const Color(0xFF6B4C9A),
                  width: 3,
                ),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.3),
                    blurRadius: 6,
                    offset: const Offset(0, 3),
                  ),
                ],
              ),
              child: Icon(
                Icons.person,
                size: 22,
                color: isSelected || isFavorite
                    ? Colors.white
                    : const Color(0xFF6B4C9A),
              ),
            ),
            CustomPaint(
              size: const Size(14, 10),
              painter: _MarkerTrianglePainter(
                color: isSelected
                    ? const Color(0xFF6B4C9A)
                    : isFavorite
                        ? const Color(0xFFE53935)
                        : const Color(0xFF6B4C9A),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSaddlerPopup() {
    final saddler = _selectedSaddler!;

    return Positioned(
      bottom: 24,
      left: 16,
      right: 16,
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.2),
              blurRadius: 12,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  _buildAvatar(saddler),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          saddler.name,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          saddler.shortAddress,
                          style: TextStyle(
                            fontSize: 14,
                            color: Colors.grey[600],
                          ),
                        ),
                      ],
                    ),
                  ),
                  GestureDetector(
                    onTap: () => _toggleFavorite(saddler),
                    child: Container(
                      padding: const EdgeInsets.all(8),
                      child: Icon(
                        saddler.isFavorite ? Icons.favorite : Icons.favorite_border,
                        color: saddler.isFavorite ? Colors.red : Colors.grey[400],
                        size: 28,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            InkWell(
              onTap: () => _openProfile(saddler),
              borderRadius: const BorderRadius.only(
                bottomLeft: Radius.circular(16),
                bottomRight: Radius.circular(16),
              ),
              child: Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(vertical: 14),
                decoration: const BoxDecoration(
                  color: Color(0xFF6B4C9A),
                  borderRadius: BorderRadius.only(
                    bottomLeft: Radius.circular(16),
                    bottomRight: Radius.circular(16),
                  ),
                ),
                child: const Text(
                  'Zum Profil',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 16,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHorsesPopup() {
    final horses = _selectedHorses!;
    final first = horses.first;
    
    // Build full address
    final streetWithNumber = first.stableHouseNumber != null
        ? '${first.stableStreet ?? 'Stall'} ${first.stableHouseNumber}'
        : (first.stableStreet ?? 'Stall');
    final stableLocation = '${first.stablePostalCode ?? ''} ${first.stableCity ?? ''}'.trim();

    return Positioned(
      bottom: 24,
      left: 16,
      right: 16,
      child: Container(
        constraints: const BoxConstraints(maxHeight: 300),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.2),
              blurRadius: 12,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Header
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFF8BC34A).withValues(alpha: 0.1),
                borderRadius: const BorderRadius.only(
                  topLeft: Radius.circular(16),
                  topRight: Radius.circular(16),
                ),
              ),
              child: Row(
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(
                      color: const Color(0xFF8BC34A),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Icon(
                      FontAwesomeIcons.horseHead,
                      color: Colors.white,
                      size: 22,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          streetWithNumber,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        if (stableLocation.isNotEmpty)
                          Text(
                            stableLocation,
                            style: TextStyle(
                              fontSize: 13,
                              color: Colors.grey[600],
                            ),
                          ),
                      ],
                    ),
                  ),
                  GestureDetector(
                    onTap: _closePopup,
                    child: Icon(Icons.close, color: Colors.grey[600]),
                  ),
                ],
              ),
            ),

            // Horse list
            Flexible(
              child: ListView.separated(
                shrinkWrap: true,
                padding: const EdgeInsets.symmetric(vertical: 8),
                itemCount: horses.length,
                separatorBuilder: (_, __) => const Divider(height: 1, indent: 72),
                itemBuilder: (context, index) {
                  final horse = horses[index];
                  return _buildHorseListItem(horse);
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildFavoritesList() {
    return Positioned(
      top: 130,
      right: 16,
      child: Container(
        width: 280,
        constraints: const BoxConstraints(maxHeight: 400),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.2),
              blurRadius: 12,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Header
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.amber.withValues(alpha: 0.1),
                borderRadius: const BorderRadius.only(
                  topLeft: Radius.circular(16),
                  topRight: Radius.circular(16),
                ),
              ),
              child: Row(
                children: [
                  const Icon(Icons.favorite, color: Colors.red, size: 24),
                  const SizedBox(width: 12),
                  const Expanded(
                    child: Text(
                      'Meine Favoriten',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                  GestureDetector(
                    onTap: _closeFavoritesList,
                    child: Icon(Icons.close, color: Colors.grey[600]),
                  ),
                ],
              ),
            ),

            // Favorites list
            if (_favoriteSaddlers.isEmpty)
              Padding(
                padding: const EdgeInsets.all(32),
                child: Column(
                  children: [
                    Icon(Icons.favorite_border, size: 48, color: Colors.grey[300]),
                    const SizedBox(height: 12),
                    Text(
                      'Keine Favoriten',
                      style: TextStyle(
                        color: Colors.grey[600],
                        fontSize: 16,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'Tippe auf das Herz bei einem\nSattler, um ihn hinzuzufügen',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: Colors.grey[400],
                        fontSize: 13,
                      ),
                    ),
                  ],
                ),
              )
            else
              Flexible(
                child: ListView.separated(
                  shrinkWrap: true,
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  itemCount: _favoriteSaddlers.length,
                  separatorBuilder: (_, __) => Divider(height: 1, indent: 72, color: Colors.grey[200]),
                  itemBuilder: (context, index) {
                    final saddler = _favoriteSaddlers[index];
                    return _buildFavoriteListItem(saddler);
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildFavoriteListItem(Saddler saddler) {
    return InkWell(
      onTap: () {
        _closeFavoritesList();
        _selectSaddler(saddler);
      },
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            _buildSmallAvatar(saddler),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    saddler.name,
                    style: const TextStyle(
                      fontWeight: FontWeight.w600,
                      fontSize: 15,
                    ),
                  ),
                  if (saddler.city != null)
                    Text(
                      saddler.city!,
                      style: TextStyle(
                        color: Colors.grey[500],
                        fontSize: 13,
                      ),
                    ),
                ],
              ),
            ),
            Icon(Icons.chevron_right, color: Colors.grey[400], size: 20),
          ],
        ),
      ),
    );
  }

  Widget _buildHorseListItem(Horse horse) {
    return InkWell(
      onTap: () => _openHorseProfile(horse),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
        children: [
          // Horse avatar
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              color: Colors.brown[100],
              borderRadius: BorderRadius.circular(12),
            ),
            child: horse.imagePath != null
                ? ClipRRect(
                    borderRadius: BorderRadius.circular(12),
                    child: Image.asset(horse.imagePath!, fit: BoxFit.cover),
                  )
                : Icon(
                    FontAwesomeIcons.horseHead,
                    color: Colors.brown[400],
                    size: 24,
                  ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  horse.name,
                  style: const TextStyle(
                    fontWeight: FontWeight.w600,
                    fontSize: 15,
                  ),
                ),
                Text(
                  '${horse.breed} - ${horse.age}',
                  style: TextStyle(
                    color: Colors.grey[600],
                    fontSize: 13,
                  ),
                ),
              ],
            ),
          ),
          Icon(
            horse.sex == 'male' ? Icons.male : Icons.female,
            color: horse.sex == 'male' ? Colors.blue : Colors.pink,
            size: 20,
          ),
        ],
      ),
      ),
    );
  }

  Widget _buildAvatar(Saddler saddler, {double radius = 28}) {
    if (saddler.imagePath != null) {
      return CircleAvatar(
        radius: radius,
        backgroundImage: AssetImage(saddler.imagePath!),
      );
    }

    return CircleAvatar(
      radius: radius,
      backgroundColor: Colors.green[50],
      child: Text(
        _getInitials(saddler.name),
        style: TextStyle(
          color: Colors.green[700],
          fontWeight: FontWeight.bold,
          fontSize: radius * 0.6,
        ),
      ),
    );
  }

  Widget _buildSmallAvatar(Saddler saddler) {
    return _buildAvatar(saddler, radius: 22);
  }

  String _getInitials(String name) {
    final parts = name.split(' ');
    if (parts.length >= 2) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return name.isNotEmpty ? name[0].toUpperCase() : '?';
  }
}

class _MarkerTrianglePainter extends CustomPainter {
  final Color color;

  _MarkerTrianglePainter({required this.color});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.fill;

    final path = ui.Path()
      ..moveTo(size.width / 2, size.height)
      ..lineTo(0, 0)
      ..lineTo(size.width, 0)
      ..close();

    canvas.drawPath(path, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
