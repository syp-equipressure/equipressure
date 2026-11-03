import 'package:flutter/material.dart';
import 'package:reiterappfrontend/services/backend_person_context_service.dart';
import 'package:reiterappfrontend/services/chat_service.dart';
import 'package:reiterappfrontend/services/person_service.dart';
import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:reiterappfrontend/widgets/person_avatar.dart';

/// Eine Sattler*in, der etwas geschickt werden kann
class _Saddler {
  final String id;
  final String name;
  final String? imageUpdatedAt;

  const _Saddler(this.id, this.name, this.imageUpdatedAt);
}

/// Lässt eine Sattler*in auswählen und schickt ihr über den Chat, was [share] sendet.
Future<void> shareWithSaddler(
  BuildContext context, {
  required String title,
  required Future<void> Function(String contactId) share,
}) async {
  final saddler = await showModalBottomSheet<_Saddler>(
    context: context,
    backgroundColor: AppColors.surface,
    builder: (context) => _ShareSheet(title: title),
  );
  if (saddler == null || !context.mounted) return;

  final messenger = ScaffoldMessenger.of(context);
  try {
    await share(saddler.id);
    messenger.showSnackBar(SnackBar(content: Text('An ${saddler.name} gesendet')));
  } catch (_) {
    messenger.showSnackBar(
      const SnackBar(content: Text('Senden hat nicht funktioniert. Bitte versuche es noch einmal.')),
    );
  }
}

class _ShareSheet extends StatefulWidget {
  final String title;

  const _ShareSheet({required this.title});

  @override
  State<_ShareSheet> createState() => _ShareSheetState();
}

class _ShareSheetState extends State<_ShareSheet> {
  late final Future<List<_Saddler>> _saddlers = _load();

  /// Kontakte und alle, mit denen es schon einen Chat gibt
  Future<List<_Saddler>> _load() async {
    final saddlers = <String, _Saddler>{};
    for (final chat in ChatService().chats) {
      saddlers[chat.contactId] = _Saddler(chat.contactId, chat.contactName, chat.contactImageUpdatedAt);
    }

    final personId = await BackendPersonContextService.loadPersonId();
    if (personId != null) {
      for (final contact in await PersonService.loadPersonContacts(personId)) {
        final id = contact['id'].toString();
        final name = '${contact['firstName'] ?? ''} ${contact['lastName'] ?? ''}'.trim();
        saddlers[id] = _Saddler(id, name, contact['imageUpdatedAt']?.toString());
      }
    }
    return saddlers.values.toList();
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: FutureBuilder<List<_Saddler>>(
        future: _saddlers,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const SizedBox(height: 160, child: Center(child: CircularProgressIndicator()));
          }

          final saddlers = snapshot.data ?? const [];
          return ListView(
            shrinkWrap: true,
            children: [
              Padding(
                padding: const EdgeInsets.all(16),
                child: Text(
                  widget.title,
                  style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
                ),
              ),
              if (snapshot.hasError || saddlers.isEmpty)
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                  child: Text(
                    snapshot.hasError
                        ? 'Deine Sattler*innen konnten nicht geladen werden.'
                        : 'Du hast noch keine Sattler*innen. Schreib zuerst jemandem über „Sattler finden“.',
                    style: const TextStyle(color: AppColors.textMuted),
                  ),
                ),
              for (final saddler in saddlers)
                ListTile(
                  leading: PersonAvatar(
                    personId: saddler.id,
                    imageUpdatedAt: saddler.imageUpdatedAt,
                    name: saddler.name,
                    radius: 20,
                  ),
                  title: Text(saddler.name),
                  onTap: () => Navigator.pop(context, saddler),
                ),
            ],
          );
        },
      ),
    );
  }
}
