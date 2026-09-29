import 'dart:convert';
import 'package:flutter/services.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/saddler.dart';

class SaddlerService {
  static final SaddlerService _instance = SaddlerService._internal();
  factory SaddlerService() => _instance;
  SaddlerService._internal();

  List<Saddler>? _cachedSaddlers;
  Set<String> _favoriteIds = {};
  static const String _favoritesKey = 'saddler_favorites';

  Future<void> _loadFavorites() async {
    final prefs = await SharedPreferences.getInstance();
    final favoritesJson = prefs.getStringList(_favoritesKey);
    if (favoritesJson != null) {
      _favoriteIds = favoritesJson.toSet();
    }
  }

  Future<void> _saveFavorites() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setStringList(_favoritesKey, _favoriteIds.toList());
  }

  Future<List<Saddler>> getSaddlers() async {
    if (_cachedSaddlers != null) {
      return _cachedSaddlers!;
    }

    await _loadFavorites();

    try {
      final String response = await rootBundle.loadString('assets/data/saddlers.json');
      final Map<String, dynamic> data = json.decode(response);
      final List<dynamic> saddlersJson = data['saddlers'];
      _cachedSaddlers = saddlersJson.map((json) {
        final saddler = Saddler.fromJson(json);
        if (_favoriteIds.contains(saddler.id)) {
          return saddler.copyWith(isFavorite: true);
        }
        return saddler;
      }).toList();
      return _cachedSaddlers!;
    } catch (e) {
      return [];
    }
  }

  Future<List<Saddler>> getSaddlersWithLocation() async {
    final saddlers = await getSaddlers();
    return saddlers.where((s) => s.hasLocation).toList();
  }

  Future<List<Saddler>> getFavorites() async {
    final saddlers = await getSaddlers();
    return saddlers.where((s) => s.isFavorite).toList();
  }

  Future<Saddler?> getSaddlerById(String id) async {
    final saddlers = await getSaddlers();
    try {
      return saddlers.firstWhere((s) => s.id == id);
    } catch (e) {
      return null;
    }
  }

  Future<void> toggleFavorite(String saddlerId) async {
    if (_favoriteIds.contains(saddlerId)) {
      _favoriteIds.remove(saddlerId);
    } else {
      _favoriteIds.add(saddlerId);
    }
    await _saveFavorites();

    if (_cachedSaddlers != null) {
      _cachedSaddlers = _cachedSaddlers!.map((s) {
        if (s.id == saddlerId) {
          return s.copyWith(isFavorite: _favoriteIds.contains(saddlerId));
        }
        return s;
      }).toList();
    }
  }

  bool isFavorite(String saddlerId) {
    return _favoriteIds.contains(saddlerId);
  }

  void clearCache() {
    _cachedSaddlers = null;
  }
}
