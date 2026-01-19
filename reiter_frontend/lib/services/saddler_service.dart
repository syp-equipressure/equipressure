import 'dart:convert';
import 'package:flutter/services.dart';
import '../models/saddler.dart';

class SaddlerService {
  static final SaddlerService _instance = SaddlerService._internal();
  factory SaddlerService() => _instance;
  SaddlerService._internal();

  List<Saddler>? _cachedSaddlers;

  Future<List<Saddler>> getSaddlers() async {
    if (_cachedSaddlers != null) {
      return _cachedSaddlers!;
    }

    try {
      final String response = await rootBundle.loadString('assets/data/saddlers.json');
      final Map<String, dynamic> data = json.decode(response);
      final List<dynamic> saddlersJson = data['saddlers'];
      _cachedSaddlers = saddlersJson.map((json) => Saddler.fromJson(json)).toList();
      return _cachedSaddlers!;
    } catch (e) {
      return [];
    }
  }

  void clearCache() {
    _cachedSaddlers = null;
  }
}
