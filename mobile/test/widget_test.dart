import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:carmine_people/app.dart';

void main() {
  testWidgets('App boots to splash while session resolves', (tester) async {
    await tester.pumpWidget(const ProviderScope(child: CarminePeopleApp()));
    await tester.pump();
    expect(find.byType(MaterialApp), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });
}
