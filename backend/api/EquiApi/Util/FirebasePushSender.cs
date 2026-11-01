using EquiApi.Core.Services;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace EquiApi.Util;

/// <summary>
///     Sends push notifications over Firebase Cloud Messaging
/// </summary>
/// <param name="credentialFile">The path of the service account key, never commit this file</param>
internal sealed class FirebasePushSender(string credentialFile) : IPushSender
{
    // loaded with the first notification, a broken key must not stop the API from starting
    private readonly Lazy<FirebaseMessaging> _messaging = new(() =>
    {
        var credential = CredentialFactory.FromFile<ServiceAccountCredential>(credentialFile).ToGoogleCredential();
        var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions { Credential = credential });

        return FirebaseMessaging.GetMessaging(app);
    });

    public async ValueTask<IReadOnlyCollection<string>> SendAsync(IReadOnlyCollection<string> tokens,
                                                                  PushMessage message)
    {
        var devices = tokens.ToList();
        var response = await _messaging.Value.SendEachForMulticastAsync(new MulticastMessage
        {
            // the apps get a registration token from FirebaseMessaging.getToken, not an installation id
#pragma warning disable CS0618
            Tokens = devices,
#pragma warning restore CS0618
            Notification = new Notification { Title = message.Title, Body = message.Body },
            Data = message.Data,
            Android = new AndroidConfig { Priority = Priority.High }
        });

        return response.Responses
                       .Select((result, index) => (result, token: devices[index]))
                       .Where(item => item.result.Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered)
                       .Select(item => item.token)
                       .ToList();
    }
}