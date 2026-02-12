import 'dart:math';
import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import '../../widgets/sidenav.dart';
import '../../models/saddler.dart';
import '../../models/horse.dart';
import '../../services/saddler_service.dart';
import '../../services/horse_service.dart';
import 'saddler_profile_screen.dart';

class FindSaddler extends StatefulWidget {
  const FindSaddler({super.key});

  @override
  State<FindSaddler> createState() => _FindSaddlerState();
}

class _FindSaddlerState extends State<FindSaddler> {
  final SaddlerService _saddlerService = SaddlerService();
  final HorseService _horseService = HorseService();
  final TextEditingController _searchController = TextEditingController();

  List<Saddler> _saddlers = [];
  List<Horse> _horses = [];
  Horse? _selectedHorse;
  bool _isLoading = true;
  String _searchQuery = '';
  bool _showOnlyFavorites = false;

  @override
  void initState() {
    super.initState();
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
      if (_selectedHorse == null && horses.isNotEmpty) {
        _selectedHorse = horses.first;
      }
      _isLoading = false;
    });
  }

  /// Haversine formula - returns distance in km between two lat/lng points.
  double _haversineKm(double lat1, double lon1, double lat2, double lon2) {
    const earthRadius = 6371.0; // km
    final dLat = _degToRad(lat2 - lat1);
    final dLon = _degToRad(lon2 - lon1);
    final a = sin(dLat / 2) * sin(dLat / 2) +
        cos(_degToRad(lat1)) * cos(_degToRad(lat2)) *
        sin(dLon / 2) * sin(dLon / 2);
    final c = 2 * atan2(sqrt(a), sqrt(1 - a));
    return earthRadius * c;
  }

  double _degToRad(double deg) => deg * (pi / 180);

  /// Returns distance in km from the selected horse's stable to the saddler,
  /// or null if coordinates are missing.
  double? _distanceToSaddler(Saddler saddler) {
    if (_selectedHorse == null ||
        !_selectedHorse!.hasStableLocation ||
        !saddler.hasLocation) {
      return null;
    }
    return _haversineKm(
      _selectedHorse!.stableLatitude!,
      _selectedHorse!.stableLongitude!,
      saddler.latitude!,
      saddler.longitude!,
    );
  }

  List<Saddler> get _sortedFilteredSaddlers {
    var filtered = _saddlers.toList();

    // Filter favorites only
    if (_showOnlyFavorites) {
      filtered = filtered.where((s) => s.isFavorite).toList();
    }

    // Filter by search query
    if (_searchQuery.isNotEmpty) {
      final query = _searchQuery.toLowerCase();
      filtered = filtered.where((s) =>
        s.name.toLowerCase().contains(query) ||
        (s.city?.toLowerCase().contains(query) ?? false)
      ).toList();
    }

    // Sort: favorites first, then by distance or alphabetically
    if (_selectedHorse != null && _selectedHorse!.hasStableLocation) {
      filtered.sort((a, b) {
        if (!_showOnlyFavorites) {
          if (a.isFavorite && !b.isFavorite) return -1;
          if (!a.isFavorite && b.isFavorite) return 1;
        }
        final distA = _distanceToSaddler(a);
        final distB = _distanceToSaddler(b);
        if (distA == null && distB == null) return a.name.compareTo(b.name);
        if (distA == null) return 1;
        if (distB == null) return -1;
        return distA.compareTo(distB);
      });
    } else {
      filtered.sort((a, b) {
        if (!_showOnlyFavorites) {
          if (a.isFavorite && !b.isFavorite) return -1;
          if (!a.isFavorite && b.isFavorite) return 1;
        }
        return a.name.compareTo(b.name);
      });
    }

    return filtered;
  }

  Future<void> _toggleFavorite(Saddler saddler) async {
    await _saddlerService.toggleFavorite(saddler.id);
    await _loadData();
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
          'Sattler finden',
          style: TextStyle(
            color: Colors.black,
            fontWeight: FontWeight.w600,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
        actions: [
          IconButton(
            icon: Icon(
              _showOnlyFavorites ? Icons.star : Icons.star_border,
              color: _showOnlyFavorites ? Colors.amber : Colors.grey[600],
            ),
            onPressed: () {
              setState(() {
                _showOnlyFavorites = !_showOnlyFavorites;
              });
            },
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                _buildHorseDropdown(),
                _buildSearchField(),
                Expanded(child: _buildSaddlerList()),
              ],
            ),
    );
  }

  Widget _buildHorseDropdown() {
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: DropdownButtonFormField<Horse>(
        value: _selectedHorse,
        decoration: InputDecoration(
          labelText: 'Pferd als Referenz',
          prefixIcon: Padding(
            padding: const EdgeInsets.all(12),
            child: SvgPicture.asset(
              'assets/icon/horseIcon.svg',
              width: 18,
              height: 18,
              colorFilter: ColorFilter.mode(Colors.grey[600]!, BlendMode.srcIn),
            ),
          ),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        ),
        isExpanded: true,
        items: _horses.map((horse) {
          final stableInfo = horse.stableCity != null
              ? ' - ${horse.stableCity}'
              : '';
          return DropdownMenuItem<Horse>(
            value: horse,
            child: Text(
              '${horse.name}$stableInfo',
              overflow: TextOverflow.ellipsis,
            ),
          );
        }).toList(),
        onChanged: (horse) {
          setState(() {
            _selectedHorse = horse;
          });
        },
      ),
    );
  }

  Widget _buildSearchField() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
      child: TextField(
        controller: _searchController,
        decoration: InputDecoration(
          hintText: 'Nach Name oder Stadt suchen...',
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
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        ),
        onChanged: (value) {
          setState(() {
            _searchQuery = value;
          });
        },
      ),
    );
  }

  Widget _buildSaddlerList() {
    final saddlers = _sortedFilteredSaddlers;

    if (saddlers.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              _showOnlyFavorites ? Icons.star_border : Icons.search_off,
              size: 64,
              color: Colors.grey[300],
            ),
            const SizedBox(height: 16),
            Text(
              _showOnlyFavorites
                  ? 'Keine Favoriten vorhanden'
                  : 'Keine Sattler gefunden',
              style: TextStyle(
                fontSize: 16,
                color: Colors.grey[600],
                fontWeight: FontWeight.w500,
              ),
            ),
            if (_showOnlyFavorites) ...[
              const SizedBox(height: 8),
              Text(
                'Tippe auf den Stern bei einem Sattler',
                style: TextStyle(fontSize: 13, color: Colors.grey[400]),
              ),
            ],
          ],
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.symmetric(vertical: 8),
      itemCount: saddlers.length,
      separatorBuilder: (_, __) => Divider(height: 1, indent: 72, color: Colors.grey[200]),
      itemBuilder: (context, index) {
        final saddler = saddlers[index];
        final distance = _distanceToSaddler(saddler);

        return ListTile(
          leading: _buildAvatar(saddler),
          title: Text(
            saddler.name,
            style: const TextStyle(fontWeight: FontWeight.w600),
          ),
          subtitle: Text(
            saddler.city ?? '',
            style: TextStyle(color: Colors.grey[600], fontSize: 13),
          ),
          trailing: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (distance != null)
                Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: Text(
                    '${distance.toStringAsFixed(1)} km',
                    style: TextStyle(
                      color: Colors.grey[600],
                      fontSize: 13,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ),
              GestureDetector(
                onTap: () => _toggleFavorite(saddler),
                child: Icon(
                  saddler.isFavorite ? Icons.star : Icons.star_border,
                  color: saddler.isFavorite ? Colors.amber : Colors.grey[400],
                  size: 24,
                ),
              ),
            ],
          ),
          onTap: () => _openProfile(saddler),
        );
      },
    );
  }

  Widget _buildAvatar(Saddler saddler) {
    if (saddler.imagePath != null) {
      return CircleAvatar(
        radius: 22,
        backgroundImage: AssetImage(saddler.imagePath!),
      );
    }

    return CircleAvatar(
      radius: 22,
      backgroundColor: Colors.green[50],
      child: Text(
        _getInitials(saddler.name),
        style: TextStyle(
          color: Colors.green[700],
          fontWeight: FontWeight.bold,
          fontSize: 13,
        ),
      ),
    );
  }

  String _getInitials(String name) {
    final parts = name.split(' ');
    if (parts.length >= 2) {
      return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    }
    return name.isNotEmpty ? name[0].toUpperCase() : '?';
  }
}
