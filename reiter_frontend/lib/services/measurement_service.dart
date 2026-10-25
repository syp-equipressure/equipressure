import 'dart:convert';
import 'dart:math';
import 'package:http/http.dart' as http;
import 'package:reiterappfrontend/models/measurement.dart';
import 'package:reiterappfrontend/services/api_config.dart';
import 'package:reiterappfrontend/services/horse_service.dart';

/// Geworfen wenn das Backend einen erwarteten Fehlerstatus liefert.
class MeasurementApiException implements Exception {
  final int statusCode;
  final String message;

  MeasurementApiException(this.statusCode, this.message);

  bool get isNotFound => statusCode == 404;
  bool get isConflict => statusCode == 409;
  bool get isBadRequest => statusCode == 400;

  @override
  String toString() => 'MeasurementApiException($statusCode): $message';
}

/// Welcher Wert über die Messdaten berechnet werden soll.
enum MeasurementAggregate { average, min, max }

/// Spricht den MeasurementController (`api/measurementgroups`) des Backends an.
///
/// Eine Messgruppe des Backends entspricht einer [Measurement] der App,
/// die Messungen der Gruppe (Gangart + Hand) den [MeasurementSectionData].
class MeasurementService {
  static String get baseUrl => ApiConfig.baseUrl;
  static const Duration _timeout = Duration(seconds: 10);
  static const Map<String, String> _jsonHeaders = {
    'Content-Type': 'application/json',
  };

  /// Die Heatmap zeigt ein Raster aus [heatmapGridSize] x [heatmapGridSize] Sensoren.
  static const int heatmapGridSize = 20;

  // Singleton
  static final MeasurementService _instance = MeasurementService._internal();
  factory MeasurementService() => _instance;
  MeasurementService._internal();

  // Das Backend bietet keinen Endpunkt zum Speichern von Messdaten an. Damit live
  // aufgenommene Druckwerte in dieser Sitzung trotzdem angezeigt werden, werden sie
  // pro Backend-Messung zwischengespeichert. Key: "{groupId}/{measurementId}"
  static final Map<String, MeasurementSectionData> _recordedSections = {};

  static Never _throwForStatus(http.Response r, String action) {
    throw MeasurementApiException(
      r.statusCode,
      'Failed to $action: HTTP ${r.statusCode}',
    );
  }

  // --- App-Modell ----------------------------------------------------------

  /// Lädt alle Messungen aller Pferde der aktuellen Person.
  Future<List<Measurement>> getMeasurements() async {
    final horses = await HorseService().getHorses();
    final measurementsByHorse = await Future.wait(
      horses.map((horse) => getMeasurementsForHorse(horse.id)),
    );
    final result = measurementsByHorse.expand((m) => m).toList();
    result.sort((a, b) => b.date.compareTo(a.date));
    return result;
  }

  /// Lädt alle Messungen eines Pferdes, neueste zuerst. Die Druckdaten werden dabei
  /// nicht geladen (die Teilmessungen haben nur Gangart und Hand), das macht
  /// [getMeasurementById] beim Öffnen einer Messung.
  Future<List<Measurement>> getMeasurementsForHorse(String horseId) async {
    final parsedHorseId = int.tryParse(horseId);
    if (parsedHorseId == null) return [];

    final groups = await fetchGroupsOfHorse(parsedHorseId);
    final measurements = await Future.wait(
      groups.map(
        (group) =>
            _loadMeasurement(group['id'] as int, horseId, withData: false),
      ),
    );

    // Gruppen ohne Teilmessungen gelten als gelöscht (siehe deleteMeasurement)
    final result = measurements
        .whereType<Measurement>()
        .where((m) => m.sections.isNotEmpty)
        .toList();
    result.sort((a, b) => b.date.compareTo(a.date));
    return result;
  }

  /// Lädt eine Messung vollständig, also inklusive aller Aufnahmen.
  Future<Measurement?> getMeasurementById(
    String id, {
    String horseId = '',
  }) async {
    final groupId = int.tryParse(id);
    if (groupId == null) return null;
    return _loadMeasurement(groupId, horseId);
  }

  /// Legt eine Messgruppe samt Teilmessungen im Backend an und liefert die
  /// gespeicherte Messung (mit der ID des Backends) zurück.
  Future<Measurement> addMeasurement(
    Measurement measurement, {
    required int personId,
    required int saddleId,
    required String deviceId,
  }) async {
    final group = await createGroup(
      name: measurement.notes,
      date: measurement.date,
      // der Name ist auf 100 Zeichen begrenzt, längere Notizen werden zusätzlich gespeichert
      notes: measurement.notes.length > 100 ? measurement.notes : null,
      personId: personId,
      horseId: int.parse(measurement.horseId),
      saddleId: saddleId,
      deviceId: deviceId,
    );
    final groupId = group['id'] as int;

    // Gangart + Hand müssen innerhalb einer Gruppe eindeutig sein, die letzte Teilmessung gewinnt
    final uniqueSections = <String, MeasurementSectionData>{
      for (final section in measurement.sections)
        '${section.gait}|${section.hand}': section,
    };
    for (final section in uniqueSections.values) {
      final created = await createMeasurement(
        groupId,
        pace: section.gait,
        hand: section.hand,
      );
      _recordedSections['$groupId/${created['id']}'] = section;
    }

    return await _loadMeasurement(groupId, measurement.horseId) ??
        _copyWithId(measurement, groupId.toString());
  }

  /// Löscht alle Teilmessungen einer Messung. Das Backend bietet keinen Endpunkt
  /// zum Löschen einer ganzen Messgruppe an, die leere Gruppe wird ausgeblendet.
  Future<void> deleteMeasurement(String id) async {
    final groupId = int.parse(id);
    final measurements = await fetchMeasurementsOfGroup(groupId);
    for (final measurement in measurements) {
      await deleteMeasurementOfGroup(groupId, measurement['id'] as int);
    }
  }

  /// [withData] false lädt nur Gruppe und Teilmessungen, ohne die Aufnahmen.
  Future<Measurement?> _loadMeasurement(
    int groupId,
    String horseId, {
    bool withData = true,
  }) async {
    final results = await Future.wait([
      fetchGroup(groupId),
      fetchMeasurementsOfGroup(groupId),
    ]);
    final group = results[0] as Map<String, dynamic>?;
    if (group == null) return null;
    final measurements = results[1] as List<Map<String, dynamic>>;

    final sections = await Future.wait(
      measurements.map((m) async {
        final measurementId = m['id'] as int;
        final gait = (m['pace'] ?? '').toString();
        final hand = (m['hand'] ?? '').toString();
        if (!withData) {
          return _recordedSections['$groupId/$measurementId'] ??
              MeasurementSectionData(gait: gait, hand: hand);
        }
        final loaded = await _loadSectionData(
          groupId,
          measurementId,
          gait,
          hand,
        );
        if (loaded.pressureData.isNotEmpty) return loaded;
        return _recordedSections['$groupId/$measurementId'] ?? loaded;
      }),
    );

    final rider =
        '${group['riderFirstName'] ?? ''} ${group['riderLastName'] ?? ''}'
            .trim();
    final name = (group['name'] ?? '').toString();
    final groupNotes = (group['notes'] ?? '').toString();
    final notes = groupNotes.isEmpty
        ? name
        : name.isEmpty || groupNotes.startsWith(name)
        ? groupNotes
        : '$name - $groupNotes';

    return Measurement(
      id: groupId.toString(),
      horseId: horseId,
      horseName: (group['horseName'] ?? '').toString(),
      date: DateTime.parse(group['date'].toString()),
      owner: rider,
      rider: rider,
      weight: '${_formatNumber(group['horseWeight'])}kg',
      height: '${_formatNumber(_toDouble(group['riderHeight']) / 100)}m',
      saddleName: (group['saddleName'] ?? '').toString(),
      notes: notes,
      images: MeasurementImages(
        normal: 'measurement_${groupId}_normal.png',
        filtered: 'measurement_${groupId}_filtered.png',
        profile: 'measurement_${groupId}_profile.png',
      ),
      sections: sections,
    );
  }

  /// Lädt alle Messdaten einer Teilmessung. Ist jeder Eintrag ein volles Raster
  /// (eine Aufnahme der Sensormatte), bleiben die Aufnahmen erhalten und die Dauer
  /// ergibt sich aus den Zeitstempeln. Sonst werden die Werte aneinandergehängt und
  /// auf das Heatmap-Raster gebracht.
  Future<MeasurementSectionData> _loadSectionData(
    int groupId,
    int measurementId,
    String gait,
    String hand,
  ) async {
    final data = await fetchMeasurementData(groupId, measurementId);
    final entries = <List<double>>[];
    final timestamps = <DateTime>[];
    for (final entry in data) {
      final values = parseDataValues((entry['data'] ?? '').toString());
      if (values.isEmpty) continue;
      entries.add(values);
      final timestamp = DateTime.tryParse(
        (entry['timestamp'] ?? '').toString(),
      );
      if (timestamp != null) timestamps.add(timestamp);
    }
    if (entries.isEmpty) {
      return MeasurementSectionData(gait: gait, hand: hand);
    }

    final duration = timestamps.length >= 2
        ? timestamps
              .reduce((a, b) => a.isAfter(b) ? a : b)
              .difference(timestamps.reduce((a, b) => a.isBefore(b) ? a : b))
        : null;

    const cells = heatmapGridSize * heatmapGridSize;
    if (entries.every((values) => values.length == cells)) {
      return MeasurementSectionData(
        gait: gait,
        hand: hand,
        pressureData: averageFrames(entries),
        frames: entries,
        duration: duration,
      );
    }
    return MeasurementSectionData(
      gait: gait,
      hand: hand,
      pressureData: toHeatmapGrid(entries.expand((values) => values).toList()),
      duration: duration,
    );
  }

  /// Mittelt mehrere gleich große Aufnahmen Wert für Wert.
  static List<double> averageFrames(List<List<double>> frames) {
    if (frames.isEmpty) return const [];
    return List<double>.generate(frames.first.length, (i) {
      var sum = 0.0;
      for (final frame in frames) {
        sum += frame[i];
      }
      return sum / frames.length;
    });
  }

  /// Ein Messdaten-Eintrag ist entweder eine einzelne Zahl oder ein JSON-Array
  /// von Zahlen (z.B. eine Druckkurve).
  static List<double> parseDataValues(String data) {
    final trimmed = data.trim();
    if (trimmed.isEmpty) return const [];
    if (trimmed.startsWith('[')) {
      final decoded = jsonDecode(trimmed);
      if (decoded is! List) return const [];
      return decoded.whereType<num>().map((e) => e.toDouble()).toList();
    }
    final value = double.tryParse(trimmed);
    return value == null ? const [] : [value];
  }

  /// Die Heatmap braucht [heatmapGridSize]² Werte. Liefert das Backend weniger
  /// (z.B. nur eine Druckkurve), wird daraus ein 2D-Raster gebildet: die Kurve wird
  /// auf eine Zeile/Spalte gestreckt und mit sich selbst multipliziert.
  static List<double> toHeatmapGrid(List<double> values) {
    const cells = heatmapGridSize * heatmapGridSize;
    if (values.isEmpty || values.length >= cells) return values;

    final profile = List<double>.generate(heatmapGridSize, (i) {
      if (values.length == 1) return values.first;
      final pos = i * (values.length - 1) / (heatmapGridSize - 1);
      final lower = pos.floor();
      final upper = min(lower + 1, values.length - 1);
      final t = pos - lower;
      return values[lower] * (1 - t) + values[upper] * t;
    });
    final peak = profile.reduce(max);
    if (peak <= 0) return List<double>.filled(cells, 0);

    return List<double>.generate(cells, (i) {
      final row = i ~/ heatmapGridSize;
      final col = i % heatmapGridSize;
      return profile[row] * profile[col] / peak;
    });
  }

  static double _toDouble(dynamic value) =>
      value is num ? value.toDouble() : double.tryParse('$value') ?? 0;

  static String _formatNumber(dynamic value) {
    final number = _toDouble(value);
    return number == number.roundToDouble()
        ? number.toInt().toString()
        : number.toStringAsFixed(2).replaceAll(RegExp(r'0+$'), '');
  }

  static Measurement _copyWithId(Measurement m, String id) => Measurement(
    id: id,
    horseId: m.horseId,
    horseName: m.horseName,
    date: m.date,
    owner: m.owner,
    rider: m.rider,
    weight: m.weight,
    height: m.height,
    saddleName: m.saddleName,
    notes: m.notes,
    images: m.images,
    sections: m.sections,
  );

  // --- Messgruppen -----------------------------------------------------------

  /// GET /api/measurementgroups/horse/{horseId} -> { "groups": [MeasurementGroupSummaryDto] }
  Future<List<Map<String, dynamic>>> fetchGroupsOfHorse(int horseId) async {
    final url = Uri.parse('$baseUrl/measurementgroups/horse/$horseId');
    final response = await http.get(url).timeout(_timeout);
    if (response.statusCode == 404) return [];
    if (response.statusCode != 200) {
      _throwForStatus(response, 'load measurement groups of horse $horseId');
    }
    return _extractList(jsonDecode(response.body), 'groups');
  }

  /// GET /api/measurementgroups/{groupId} -> MeasurementGroupDetailDto
  Future<Map<String, dynamic>?> fetchGroup(int groupId) async {
    final url = Uri.parse('$baseUrl/measurementgroups/$groupId');
    final response = await http.get(url).timeout(_timeout);
    if (response.statusCode == 404) return null;
    if (response.statusCode != 200) {
      _throwForStatus(response, 'load measurement group $groupId');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  /// POST /api/measurementgroups (AddMeasurementGroupRequest) -> MeasurementGroupDetailDto
  Future<Map<String, dynamic>> createGroup({
    required String name,
    required DateTime date,
    String? notes,
    required int personId,
    required int horseId,
    required int saddleId,
    required String deviceId,
  }) async {
    final url = Uri.parse('$baseUrl/measurementgroups');
    final body = jsonEncode({
      'name': name.length > 100 ? name.substring(0, 100) : name,
      'date': _formatLocalDate(date),
      'notes': notes != null && notes.length > 500
          ? notes.substring(0, 500)
          : notes,
      'personId': personId,
      'horseId': horseId,
      'saddleId': saddleId,
      'deviceId': deviceId,
    });
    final response = await http
        .post(url, headers: _jsonHeaders, body: body)
        .timeout(_timeout);
    if (response.statusCode != 201 && response.statusCode != 200) {
      _throwForStatus(response, 'create measurement group');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  // --- Messungen einer Gruppe -----------------------------------------------

  /// GET /api/measurementgroups/{groupId}/measurements -> { "measurements": [MeasurementDto] }
  Future<List<Map<String, dynamic>>> fetchMeasurementsOfGroup(
    int groupId,
  ) async {
    final url = Uri.parse('$baseUrl/measurementgroups/$groupId/measurements');
    final response = await http.get(url).timeout(_timeout);
    if (response.statusCode == 404) return [];
    if (response.statusCode != 200) {
      _throwForStatus(response, 'load measurements of group $groupId');
    }
    return _extractList(jsonDecode(response.body), 'measurements');
  }

  /// GET /api/measurementgroups/{groupId}/measurements/filter?pace=&hand= -> MeasurementDto
  Future<Map<String, dynamic>?> fetchMeasurementByFilter(
    int groupId, {
    String? pace,
    String? hand,
  }) async {
    final url = Uri.parse(
      '$baseUrl/measurementgroups/$groupId/measurements/filter',
    ).replace(queryParameters: _filterParameters(pace, hand));
    final response = await http.get(url).timeout(_timeout);
    if (response.statusCode == 404) return null;
    if (response.statusCode != 200) {
      _throwForStatus(response, 'filter measurements of group $groupId');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  /// GET /api/measurementgroups/{groupId}/measurements/{average|min|max}?pace=&hand=
  Future<double?> fetchGroupAggregate(
    int groupId,
    MeasurementAggregate aggregate, {
    String? pace,
    String? hand,
  }) async {
    final url = Uri.parse(
      '$baseUrl/measurementgroups/$groupId/measurements/${aggregate.name}',
    ).replace(queryParameters: _filterParameters(pace, hand));
    return _fetchAggregate(url, 'load ${aggregate.name} of group $groupId');
  }

  /// GET /api/measurementgroups/{groupId}/measurements/{measurementId} -> MeasurementDto
  Future<Map<String, dynamic>?> fetchMeasurement(
    int groupId,
    int measurementId,
  ) async {
    final url = Uri.parse(
      '$baseUrl/measurementgroups/$groupId/measurements/$measurementId',
    );
    final response = await http.get(url).timeout(_timeout);
    if (response.statusCode == 404) return null;
    if (response.statusCode != 200) {
      _throwForStatus(response, 'load measurement $measurementId');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  /// GET /api/measurementgroups/{groupId}/measurements/{measurementId}/{average|min|max}
  Future<double?> fetchMeasurementAggregate(
    int groupId,
    int measurementId,
    MeasurementAggregate aggregate,
  ) async {
    final url = Uri.parse(
      '$baseUrl/measurementgroups/$groupId/measurements/$measurementId/${aggregate.name}',
    );
    return _fetchAggregate(
      url,
      'load ${aggregate.name} of measurement $measurementId',
    );
  }

  /// POST /api/measurementgroups/{groupId}/measurements (AddMeasurementRequest) -> MeasurementDto
  Future<Map<String, dynamic>> createMeasurement(
    int groupId, {
    required String pace,
    required String hand,
    String? description,
  }) async {
    final url = Uri.parse('$baseUrl/measurementgroups/$groupId/measurements');
    final body = jsonEncode({
      'pace': pace,
      'hand': hand,
      'description': description,
    });
    final response = await http
        .post(url, headers: _jsonHeaders, body: body)
        .timeout(_timeout);
    if (response.statusCode != 201 && response.statusCode != 200) {
      _throwForStatus(response, 'create measurement in group $groupId');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  /// DELETE /api/measurementgroups/{groupId}/measurements/{measurementId} -> 204
  Future<void> deleteMeasurementOfGroup(int groupId, int measurementId) async {
    final url = Uri.parse(
      '$baseUrl/measurementgroups/$groupId/measurements/$measurementId',
    );
    final response = await http.delete(url).timeout(_timeout);
    if (response.statusCode != 204 && response.statusCode != 200) {
      _throwForStatus(response, 'delete measurement $measurementId');
    }
    _recordedSections.remove('$groupId/$measurementId');
  }

  // --- Messdaten -------------------------------------------------------------

  /// GET /api/measurementgroups/{groupId}/measurements/{measurementId}/measurementData
  /// -> { "data": [MeasurementDataDto] }, nach Zeitstempel sortiert
  Future<List<Map<String, dynamic>>> fetchMeasurementData(
    int groupId,
    int measurementId,
  ) async {
    final url = Uri.parse(
      '$baseUrl/measurementgroups/$groupId/measurements/$measurementId/measurementData',
    );
    final response = await http.get(url).timeout(_timeout);
    if (response.statusCode == 404) return [];
    if (response.statusCode != 200) {
      _throwForStatus(response, 'load data of measurement $measurementId');
    }
    return _extractList(jsonDecode(response.body), 'data');
  }

  /// GET /api/measurementgroups/{groupId}/measurements/{measurementId}/measurementData/{dataId}
  Future<Map<String, dynamic>?> fetchMeasurementDataById(
    int groupId,
    int measurementId,
    int dataId,
  ) async {
    final url = Uri.parse(
      '$baseUrl/measurementgroups/$groupId/measurements/$measurementId/measurementData/$dataId',
    );
    final response = await http.get(url).timeout(_timeout);
    if (response.statusCode == 404) return null;
    if (response.statusCode != 200) {
      _throwForStatus(response, 'load data entry $dataId');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  // --- helpers -------------------------------------------------------------

  Future<double?> _fetchAggregate(Uri url, String action) async {
    final response = await http.get(url).timeout(_timeout);
    if (response.statusCode == 404) return null;
    if (response.statusCode != 200) _throwForStatus(response, action);
    final decoded = jsonDecode(response.body) as Map<String, dynamic>;
    return (decoded['value'] as num?)?.toDouble();
  }

  static Map<String, String>? _filterParameters(String? pace, String? hand) {
    final parameters = <String, String>{
      if (pace != null) 'pace': pace,
      if (hand != null) 'hand': hand,
    };
    return parameters.isEmpty ? null : parameters;
  }

  static String _formatLocalDate(DateTime date) =>
      '${date.year.toString().padLeft(4, '0')}-'
      '${date.month.toString().padLeft(2, '0')}-'
      '${date.day.toString().padLeft(2, '0')}';

  static List<Map<String, dynamic>> _extractList(dynamic decoded, String key) {
    if (decoded is List) {
      return decoded.cast<Map<String, dynamic>>();
    }
    if (decoded is Map<String, dynamic> && decoded[key] is List) {
      return (decoded[key] as List).cast<Map<String, dynamic>>();
    }
    return [];
  }
}
