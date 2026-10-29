import 'package:reiterappfrontend/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:reiterappfrontend/models/user_data.dart';

class PersonalDataScreen extends StatelessWidget {
  final UserData userData;

  const PersonalDataScreen({
    super.key,
    required this.userData,
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.purple300,
      appBar: AppBar(
        backgroundColor: AppColors.purple300,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: AppColors.textStrong),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.edit_outlined, color: AppColors.textStrong),
            onPressed: () {},
          ),
        ],
      ),
      body: Column(
        children: [
          // Header mit Avatar und Name
          Container(
            width: double.infinity,
            color: AppColors.purple300,
            padding: const EdgeInsets.only(bottom: 24),
            child: Column(
              children: [
                _buildAvatar(userData.imageUrl),
                const SizedBox(height: 16),
                Text(
                  userData.name,
                  style: const TextStyle(
                    fontSize: 26,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textStrong,
                  ),
                ),
              ],
            ),
          ),

          // Weißer Container mit Daten
          Expanded(
            child: Container(
              margin: const EdgeInsets.symmetric(horizontal: 16),
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.only(
                  topLeft: Radius.circular(30),
                  topRight: Radius.circular(30),
                ),
                border: Border.all(
                  color: AppColors.borderSoft,
                  width: 2,
                ),
              ),
              padding: const EdgeInsets.all(32),
              child: ListView(
                children: [
                  _buildDataRow('Geburtsdatum:', userData.birthDate),
                  Divider(height: 32, color: AppColors.border),
                  _buildDataRow('Adresse:', userData.address),
                  Divider(height: 32, color: AppColors.border),
                  _buildDataRow('Email:', userData.email),
                  Divider(height: 32, color: AppColors.border),
                  _buildDataRow('Größe:', userData.height),
                  Divider(height: 32, color: AppColors.border),
                  _buildDataRow('Gewicht:', userData.weight),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAvatar(String? imageUrl) {
    return Container(
      width: 120,
      height: 120,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: AppColors.surface, width: 5),
      ),
      child: ClipOval(
        child: imageUrl != null && imageUrl.isNotEmpty
            ? Image.network(
                imageUrl,
                fit: BoxFit.cover,
                errorBuilder: (context, error, stackTrace) {
                  return Container(
                    color: AppColors.surface,
                    child: const Icon(
                      Icons.person,
                      size: 60,
                      color: AppColors.textMuted,
                    ),
                  );
                },
              )
            : Container(
                color: AppColors.surface,
                child: const Icon(
                  Icons.person,
                  size: 60,
                  color: AppColors.textMuted,
                ),
              ),
      ),
    );
  }

  Widget _buildDataRow(String label, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontWeight: FontWeight.w700,
            fontSize: 15,
            color: AppColors.textStrong,
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(
              fontSize: 15,
              color: AppColors.textStrong,
              fontWeight: FontWeight.w400,
            ),
          ),
        ),
      ],
    );
  }
}
