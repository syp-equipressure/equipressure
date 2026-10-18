import 'dart:math' as math;
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
  bool _showListView = true;
  Horse? _referenceHorse;
  String _searchQuery = '';
  Set<String> _selectedBrands = {};

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
      // If a previously selected reference horse no longer exists, drop it.
      if (_referenceHorse != null && !horses.any((h) => h.id == _referenceHorse!.id)) {
        _referenceHorse = null;
      }
      // Default to the first horse with a stable location so distances can
      // be calculated as soon as the screen opens.
      if (_referenceHorse == null) {
        for (final h in horses) {
          if (h.hasStableLocation) {
            _referenceHorse = h;
            break;
          }
        }
      }
      _isLoading = false;
    });
  }

  List<String> get _allBrands {
    final brands = <String>{};
    for (final s in _saddlers) {
      brands.addAll(s.brands);
    }
    final sorted = brands.toList()..sort();
    return sorted;
  }

  List<Saddler> get _filteredSaddlers {
    var filtered = _saddlers.toList();

    if (_selectedBrands.isNotEmpty) {
      filtered = filtered.where((s) =>
        s.allBrands || s.brands.any((b) => _selectedBrands.contains(b))
      ).toList();
    }

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

  List<Horse> get _filteredHorses {
    if (_searchQuery.isEmpty) return [];
    
    final query = _searchQuery.toLowerCase();
    return _horses.where((h) =>
      h.name.toLowerCase().contains(query) ||
      (h.stableCity?.toLowerCase().contains(query) ?? false)
    ).toList();
  }

  // Saddlers filtered + sorted by distance from the reference horse's stable.
  List<Saddler> get _saddlersByDistance {
    final list = _filteredSaddlers.where((s) => s.hasLocation).toList();
    list.sort((a, b) {
      final da = _distanceFromReferenceHorse(a);
      final db = _distanceFromReferenceHorse(b);
      return da.compareTo(db);
    });
    return list;
  }

  LatLng? get _referenceLocation {
    final h = _referenceHorse;
    if (h == null || !h.hasStableLocation) return null;
    return LatLng(h.stableLatitude!, h.stableLongitude!);
  }

  double _distanceFromReferenceHorse(Saddler s) {
    final ref = _referenceLocation;
    if (ref == null || !s.hasLocation) return double.infinity;
    return _haversineKm(ref, LatLng(s.latitude!, s.longitude!));
  }

  double _haversineKm(LatLng a, LatLng b) {
    const earthRadiusKm = 6371.0;
    final dLat = _degToRad(b.latitude - a.latitude);
    final dLon = _degToRad(b.longitude - a.longitude);
    final lat1 = _degToRad(a.latitude);
    final lat2 = _degToRad(b.latitude);

    final h = math.sin(dLat / 2) * math.sin(dLat / 2) +
        math.sin(dLon / 2) * math.sin(dLon / 2) * math.cos(lat1) * math.cos(lat2);
    final c = 2 * math.atan2(math.sqrt(h), math.sqrt(1 - h));
    return earthRadiusKm * c;
  }

  double _degToRad(double deg) => deg * (math.pi / 180.0);

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

  void _setViewMode(bool showList) {
    if (_showListView == showList) return;
    setState(() {
      _showListView = showList;
      _showSearch = false;
      if (showList) {
        _selectedSaddler = null;
        _selectedHorses = null;
      }
    });
  }

  void _setReferenceHorse(Horse? horse) {
    setState(() {
      _referenceHorse = horse;
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

  void _openHorseProfile(Horse horse) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => HorseProfileScreen(horse: horse),
      ),
    ).then((_) => _loadData());
  }

  void _showBrandFilterSheet() {
    String brandSearchQuery = '';
    
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setSheetState) {
            final allBrands = _allBrands;
            final filteredBrands = allBrands
                .where((brand) => brand.toLowerCase().contains(brandSearchQuery.toLowerCase()))
                .toList();
            
            return Padding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Nach Sattelmarke filtern',
                    style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 12),
                  Container(
                    constraints: const BoxConstraints(maxHeight: 40),
                    child: TextField(
                      decoration: InputDecoration(
                        hintText: 'Marke suchen...',
                        prefixIcon: const Icon(Icons.search, size: 18),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(6),
                          borderSide: BorderSide(color: Colors.grey[300]!),
                        ),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                        isDense: true,
                      ),
                      style: const TextStyle(fontSize: 13),
                      onChanged: (value) {
                        setSheetState(() {
                          brandSearchQuery = value;
                        });
                      },
                    ),
                  ),
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 6,
                    runSpacing: 6,
                    children: [
                      _buildChip(
                        'Alle',
                        _selectedBrands.isEmpty,
                        onTap: () {
                          setSheetState(() => _selectedBrands = {});
                          setState(() => _selectedBrands = {});
                        },
                      ),
                      ...filteredBrands.map((brand) => _buildChip(
                        brand,
                        _selectedBrands.contains(brand),
                        onTap: () {
                          setSheetState(() {
                            if (_selectedBrands.contains(brand)) {
                              _selectedBrands.remove(brand);
                            } else {
                              _selectedBrands.add(brand);
                            }
                          });
                          setState(() {});
                        },
                      )),
                    ],
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildChip(String label, bool isActive, {VoidCallback? onTap}) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
        decoration: BoxDecoration(
          color: isActive ? const Color(0xFF6B4C9A) : Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: isActive ? const Color(0xFF6B4C9A) : Colors.grey[300]!,
            width: 1.5,
          ),
        ),
        child: Text(
          isActive ? '✓ $label' : label,
          style: TextStyle(
            fontSize: 13,
            color: isActive ? Colors.white : Colors.grey[700],
            fontWeight: isActive ? FontWeight.w600 : FontWeight.normal,
          ),
        ),
      ),
    );
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
          : Column(
              children: [
                _buildViewToggle(),
                Expanded(
                  child: Stack(
                    children: [
                      if (_showListView) _buildListView() else _buildMap(),
                      _buildSearchHeader(),
                      if (!_showListView && _searchQuery.isNotEmpty) _buildSearchDropdown(),
                      _buildTopRightButtons(),
                      if (!_showListView) _buildBottomLeftButtons(),
                      if (!_showListView && _selectedSaddler != null) _buildSaddlerPopup(),
                      if (!_showListView && _selectedHorses != null) _buildHorsesPopup(),
                      if (_showFavoritesList) _buildFavoritesList(),
                    ],
                  ),
                ),
              ],
            ),
    );
  }

  Widget _buildViewToggle() {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
      child: Container(
        decoration: BoxDecoration(
          color: Colors.grey[200],
          borderRadius: BorderRadius.circular(10),
        ),
        padding: const EdgeInsets.all(4),
        child: Row(
          children: [
            Expanded(child: _buildViewToggleTab(label: 'Liste', icon: Icons.view_list, selected: _showListView, onTap: () => _setViewMode(true))),
            Expanded(child: _buildViewToggleTab(label: 'Karte', icon: Icons.map, selected: !_showListView, onTap: () => _setViewMode(false))),
          ],
        ),
      ),
    );
  }

  Widget _buildViewToggleTab({
    required String label,
    required IconData icon,
    required bool selected,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      behavior: HitTestBehavior.opaque,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 180),
        padding: const EdgeInsets.symmetric(vertical: 8),
        decoration: BoxDecoration(
          color: selected ? Colors.white : Colors.transparent,
          borderRadius: BorderRadius.circular(8),
          boxShadow: selected
              ? [
                  BoxShadow(
                    color: Colors.black.withValues(alpha: 0.08),
                    blurRadius: 4,
                    offset: const Offset(0, 1),
                  ),
                ]
              : null,
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              icon,
              size: 18,
              color: selected ? const Color(0xFF6B4C9A) : Colors.grey[600],
            ),
            const SizedBox(width: 6),
            Text(
              label,
              style: TextStyle(
                fontSize: 14,
                fontWeight: selected ? FontWeight.w600 : FontWeight.w500,
                color: selected ? const Color(0xFF6B4C9A) : Colors.grey[600],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildListView() {
    final saddlers = _saddlersByDistance;
    final horsesWithStable = _horses.where((h) => h.hasStableLocation).toList();

    return Container(
      color: Colors.grey[50],
      child: Column(
        children: [
          // Top spacer so the floating search header (positioned at top: 16,
          // height ~48) does not overlap the horse picker.
          const SizedBox(height: 72),
          _buildHorsePicker(horsesWithStable),
          Expanded(
            child: saddlers.isEmpty
                ? _buildEmptyListPlaceholder()
                : ListView.separated(
                    padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
                    itemCount: saddlers.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 10),
                    itemBuilder: (context, index) {
                      final saddler = saddlers[index];
                      return _buildSaddlerListCard(saddler);
                    },
                  ),
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyListPlaceholder() {
    final hasReference = _referenceLocation != null;
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            hasReference ? Icons.search_off : Icons.pets,
            size: 48,
            color: Colors.grey[400],
          ),
          const SizedBox(height: 12),
          Text(
            hasReference
                ? 'Keine Sattler*innen gefunden'
                : 'Wähle ein Pferd mit Stalladresse,\num Sattler*innen in der Nähe zu sehen',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Colors.grey[600],
              fontSize: 15,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHorsePicker(List<Horse> horsesWithStable) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: Colors.grey[200]!),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.04),
              blurRadius: 4,
              offset: const Offset(0, 1),
            ),
          ],
        ),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        child: Row(
          children: [
            Container(
              width: 32,
              height: 32,
              decoration: BoxDecoration(
                color: const Color(0xFF6B4C9A).withValues(alpha: 0.1),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                FontAwesomeIcons.horseHead,
                size: 16,
                color: Color(0xFF6B4C9A),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    'Entfernung ab',
                    style: TextStyle(
                      fontSize: 11,
                      color: Colors.grey[500],
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  const SizedBox(height: 2),
                  if (horsesWithStable.isEmpty)
                    Text(
                      'Kein Pferd mit Stalladresse',
                      style: TextStyle(
                        fontSize: 14,
                        color: Colors.grey[600],
                        fontStyle: FontStyle.italic,
                      ),
                    )
                  else
                    DropdownButtonHideUnderline(
                      child: DropdownButton<String>(
                        isDense: true,
                        value: _referenceHorse?.id,
                        icon: Icon(Icons.arrow_drop_down, color: Colors.grey[600]),
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w600,
                          color: Colors.black,
                        ),
                        items: horsesWithStable.map((h) {
                          return DropdownMenuItem<String>(
                            value: h.id,
                            child: Text(
                              h.stableCity != null
                                  ? '${h.name} · ${h.stableCity}'
                                  : h.name,
                              overflow: TextOverflow.ellipsis,
                            ),
                          );
                        }).toList(),
                        onChanged: (id) {
                          if (id == null) return;
                          final picked = horsesWithStable.firstWhere((h) => h.id == id);
                          _setReferenceHorse(picked);
                        },
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSaddlerListCard(Saddler saddler) {
    final distance = _distanceFromReferenceHorse(saddler);
    final distanceLabel = distance.isFinite
        ? (distance < 10 ? '${distance.toStringAsFixed(1)} km' : '${distance.round()} km')
        : '—';

    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(12),
      elevation: 1,
      shadowColor: Colors.black.withValues(alpha: 0.1),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => _openProfile(saddler),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _buildAvatar(saddler, radius: 26),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            saddler.name,
                            style: const TextStyle(
                              fontWeight: FontWeight.w600,
                              fontSize: 15,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        GestureDetector(
                          onTap: () => _toggleFavorite(saddler),
                          behavior: HitTestBehavior.opaque,
                          child: Padding(
                            padding: const EdgeInsets.only(left: 6),
                            child: Icon(
                              saddler.isFavorite ? Icons.favorite : Icons.favorite_border,
                              color: saddler.isFavorite ? Colors.red : Colors.grey[400],
                              size: 22,
                            ),
                          ),
                        ),
                      ],
                    ),
                    if (saddler.city != null) ...[
                      const SizedBox(height: 2),
                      Row(
                        children: [
                          Icon(Icons.place, size: 13, color: Colors.grey[500]),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              saddler.city!,
                              style: TextStyle(
                                color: Colors.grey[600],
                                fontSize: 13,
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                    ],
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                          decoration: BoxDecoration(
                            color: const Color(0xFF6B4C9A).withValues(alpha: 0.1),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              const Icon(
                                Icons.near_me,
                                size: 12,
                                color: Color(0xFF6B4C9A),
                              ),
                              const SizedBox(width: 4),
                              Text(
                                distanceLabel,
                                style: const TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w600,
                                  color: Color(0xFF6B4C9A),
                                ),
                              ),
                            ],
                          ),
                        ),
                        if (saddler.allBrands || saddler.brands.isNotEmpty) ...[
                          const SizedBox(width: 6),
                          Flexible(
                            child: Text(
                              saddler.allBrands
                                  ? 'Alle Marken'
                                  : saddler.brands.take(2).join(' · '),
                              style: TextStyle(
                                fontSize: 12,
                                color: Colors.grey[600],
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
              Icon(Icons.chevron_right, color: Colors.grey[400], size: 20),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSearchHeader() {
    final hintText = _showListView 
        ? 'Sattler*in suchen ...'
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
          _buildSmallButton(
            icon: _showFavoritesList ? Icons.favorite : Icons.favorite_border,
            color: _showFavoritesList ? Colors.red : Colors.grey[700]!,
            onTap: _toggleFavoritesList,
            isActive: _showFavoritesList,
          ),
          const SizedBox(height: 8),
          if (!_showListView) ...[
            _buildSmallButton(
              icon: _isSatelliteView ? Icons.map : Icons.satellite_alt,
              onTap: _toggleMapType,
            ),
            const SizedBox(height: 8),
          ],
          Stack(
            clipBehavior: Clip.none,
            children: [
              _buildSmallButton(
                icon: Icons.filter_list,
                onTap: _showBrandFilterSheet,
                isActive: _selectedBrands.isNotEmpty,
              ),
              if (_selectedBrands.isNotEmpty)
                Positioned(
                  top: -4,
                  right: -4,
                  child: Container(
                    width: 16,
                    height: 16,
                    decoration: const BoxDecoration(
                      color: Color(0xFF6B4C9A),
                      shape: BoxShape.circle,
                    ),
                    child: Center(
                      child: Text(
                        '${_selectedBrands.length}',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ),
                ),
            ],
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
          _buildCircleButton(
            icon: Icons.home,
            onTap: _goToUserHome,
            tooltip: 'Mein Zuhause',
          ),
          const SizedBox(height: 12),
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
            _buildHomeMarker(),
            ..._buildHorseMarkers(),
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
