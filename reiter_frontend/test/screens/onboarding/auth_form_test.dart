import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:reiterappfrontend/screens/onboarding/widgets/auth_form.dart';

void main() {
  Widget buildButtons({required bool enabled, required ValueChanged<String> onSelected}) {
    return MaterialApp(
      home: Scaffold(
        body: AuthProviderButtons(
          action: 'registrieren',
          enabled: enabled,
          onSelected: onSelected,
        ),
      ),
    );
  }

  testWidgets('meldet den gewählten Anbieter', (WidgetTester tester) async {
    final selected = <String>[];
    await tester.pumpWidget(buildButtons(enabled: true, onSelected: selected.add));

    await tester.tap(find.text('Google'));
    await tester.tap(find.text('Microsoft'));
    await tester.tap(find.text('GitHub'));

    expect(selected, ['google', 'microsoft', 'github']);
    expect(find.byTooltip('Mit Google registrieren'), findsOneWidget);
  });

  testWidgets('reagiert während dem Laden nicht', (WidgetTester tester) async {
    final selected = <String>[];
    await tester.pumpWidget(buildButtons(enabled: false, onSelected: selected.add));

    await tester.tap(find.text('Google'));

    expect(selected, isEmpty);
  });
}
