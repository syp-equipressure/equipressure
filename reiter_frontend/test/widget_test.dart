import 'package:flutter_test/flutter_test.dart';
import 'package:reiterappfrontend/main.dart';

void main() {
  testWidgets('MyApp loads successfully', (WidgetTester tester) async {
    await tester.pumpWidget(const MyApp());
    await tester.pump();
    expect(find.byType(MyApp), findsOneWidget);
  });
}
