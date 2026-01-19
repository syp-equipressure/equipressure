import 'dart:io';
import 'package:flutter/material.dart';
import '../models/message.dart';

class ChatImageMessage extends StatelessWidget {
  final Message message;

  const ChatImageMessage({
    super.key,
    required this.message,
  });

  @override
  Widget build(BuildContext context) {
    final attachments = message.attachments ?? [];
    final isFromMe = message.isFromMe;

    return Align(
      alignment: isFromMe ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.symmetric(vertical: 4),
        constraints: BoxConstraints(
          maxWidth: MediaQuery.of(context).size.width * 0.75,
        ),
        child: Column(
          crossAxisAlignment:
              isFromMe ? CrossAxisAlignment.end : CrossAxisAlignment.start,
          children: [
            // Images grid
            ClipRRect(
              borderRadius: BorderRadius.circular(12),
              child: attachments.length == 1
                  ? _buildSingleImage(attachments.first)
                  : _buildImageGrid(attachments),
            ),
            // Optional caption
            if (message.content.isNotEmpty)
              Container(
                margin: const EdgeInsets.only(top: 4),
                padding:
                    const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: isFromMe
                      ? const Color(0xFFD6EAF8)
                      : const Color(0xFFFCE4EC),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  message.content,
                  style: const TextStyle(fontSize: 14),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildSingleImage(MessageAttachment attachment) {
    return GestureDetector(
      onTap: () => _openImageViewer(attachment),
      child: Container(
        constraints: const BoxConstraints(maxHeight: 250),
        child: _buildImageWidget(attachment),
      ),
    );
  }

  Widget _buildImageGrid(List<MessageAttachment> attachments) {
    if (attachments.length == 2) {
      return Row(
        children: attachments.map((a) {
          return Expanded(
            child: Padding(
              padding: const EdgeInsets.all(1),
              child: AspectRatio(
                aspectRatio: 1,
                child: GestureDetector(
                  onTap: () => _openImageViewer(a),
                  child: _buildImageWidget(a),
                ),
              ),
            ),
          );
        }).toList(),
      );
    }

    // For 3+ images, show grid
    return Wrap(
      spacing: 2,
      runSpacing: 2,
      children: attachments.take(4).map((a) {
        return SizedBox(
          width: 120,
          height: 120,
          child: GestureDetector(
            onTap: () => _openImageViewer(a),
            child: _buildImageWidget(a),
          ),
        );
      }).toList(),
    );
  }

  Widget _buildImageWidget(MessageAttachment attachment) {
    // Check if it's a local file or network URL
    if (attachment.filePath.startsWith('http')) {
      return Image.network(
        attachment.filePath,
        fit: BoxFit.cover,
        loadingBuilder: (context, child, loadingProgress) {
          if (loadingProgress == null) return child;
          return Container(
            color: Colors.grey[200],
            child: const Center(
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
          );
        },
        errorBuilder: (context, error, stackTrace) {
          return Container(
            color: Colors.grey[200],
            child: const Icon(Icons.broken_image, color: Colors.grey),
          );
        },
      );
    }

    // Local file
    final file = File(attachment.filePath);
    if (file.existsSync()) {
      return Image.file(
        file,
        fit: BoxFit.cover,
        errorBuilder: (context, error, stackTrace) {
          return Container(
            color: Colors.grey[200],
            child: const Icon(Icons.broken_image, color: Colors.grey),
          );
        },
      );
    }

    return Container(
      color: Colors.grey[200],
      child: const Icon(Icons.image, color: Colors.grey),
    );
  }

  void _openImageViewer(MessageAttachment attachment) {
    // TODO: Implement full-screen image viewer
  }
}
