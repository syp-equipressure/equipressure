import 'package:flutter/material.dart';
import '../models/message.dart';

class ChatFileMessage extends StatelessWidget {
  final Message message;

  const ChatFileMessage({
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
          children: attachments.map((attachment) {
            return Container(
              margin: const EdgeInsets.only(bottom: 4),
              child: _FileAttachmentCard(
                attachment: attachment,
                isFromMe: isFromMe,
              ),
            );
          }).toList(),
        ),
      ),
    );
  }
}

class _FileAttachmentCard extends StatelessWidget {
  final MessageAttachment attachment;
  final bool isFromMe;

  const _FileAttachmentCard({
    required this.attachment,
    required this.isFromMe,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: isFromMe ? const Color(0xFFD6EAF8) : const Color(0xFFFCE4EC),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: _getFileIconColor().withValues(alpha: 0.2),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(
              _getFileIcon(),
              color: _getFileIconColor(),
              size: 24,
            ),
          ),
          const SizedBox(width: 12),
          Flexible(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  attachment.fileName,
                  style: const TextStyle(
                    fontWeight: FontWeight.w500,
                    fontSize: 14,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                if (attachment.fileSize != null)
                  Text(
                    attachment.fileSizeFormatted,
                    style: TextStyle(
                      color: Colors.grey[600],
                      fontSize: 12,
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          IconButton(
            icon: const Icon(Icons.download_rounded),
            color: Colors.grey[600],
            iconSize: 20,
            onPressed: () => _downloadFile(),
          ),
        ],
      ),
    );
  }

  IconData _getFileIcon() {
    final ext = attachment.fileName.toLowerCase();

    if (ext.endsWith('.pdf')) return Icons.picture_as_pdf;
    if (ext.endsWith('.doc') || ext.endsWith('.docx')) return Icons.description;
    if (ext.endsWith('.xls') || ext.endsWith('.xlsx')) return Icons.table_chart;
    if (ext.endsWith('.ppt') || ext.endsWith('.pptx')) return Icons.slideshow;
    if (ext.endsWith('.zip') || ext.endsWith('.rar')) return Icons.folder_zip;
    if (ext.endsWith('.txt')) return Icons.article;
    if (ext.endsWith('.mp3') || ext.endsWith('.wav')) return Icons.audio_file;
    if (ext.endsWith('.mp4') || ext.endsWith('.mov')) return Icons.video_file;

    return Icons.insert_drive_file;
  }

  Color _getFileIconColor() {
    final ext = attachment.fileName.toLowerCase();

    if (ext.endsWith('.pdf')) return Colors.red;
    if (ext.endsWith('.doc') || ext.endsWith('.docx')) return Colors.blue;
    if (ext.endsWith('.xls') || ext.endsWith('.xlsx')) return Colors.green;
    if (ext.endsWith('.ppt') || ext.endsWith('.pptx')) return Colors.orange;
    if (ext.endsWith('.zip') || ext.endsWith('.rar')) return Colors.amber;

    return Colors.grey;
  }

  void _downloadFile() {
    // TODO: Implement file download/open
  }
}
