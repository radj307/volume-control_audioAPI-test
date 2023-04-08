using CoreAudio;
using VolumeControl.Log;

namespace Audio
{
    /// <summary>
    /// Manages the list of <see cref="AudioSession"/> instances for an <see cref="Audio.AudioDevice"/>.
    /// </summary>
    public class AudioDeviceSessionManager
    {
        /// <summary>
        /// Creates a new <see cref="AudioDeviceSessionManager"/> instance for the given <paramref name="audioDevice"/>.
        /// </summary>
        /// <param name="audioDevice">The <see cref="AudioDevice"/> instance to attach this instance to.</param>
        public AudioDeviceSessionManager(AudioDevice audioDevice)
        {
            AudioDevice = audioDevice;
            AudioSessionManager = AudioDevice.AudioSessionManager;

            Sessions = new();

            if (AudioSessionManager.Sessions is not null)
            { // initialize the sessions list
                foreach (var audioSessionControl in AudioSessionManager.Sessions)
                {
                    CreateAndAddSessionIfUnique(audioSessionControl);
                }
            }

            AudioSessionManager.OnSessionCreated += this.AudioSessionManager_OnSessionCreated;
        }

        private void AudioSessionManager_OnSessionCreated(object sender, CoreAudio.Interfaces.IAudioSessionControl2 newSessionControl)
        {
            newSessionControl.GetSessionInstanceIdentifier(out string newSessionInstanceIdentifier);
            AudioSessionManager.RefreshSessions();
            if (AudioSessionManager.Sessions.FirstOrDefault(session => session.SessionInstanceIdentifier.Equals(newSessionInstanceIdentifier, StringComparison.Ordinal)) is AudioSessionControl2 audioSessionControl)
            {
                if (CreateAndAddSessionIfUnique(audioSessionControl) is AudioSession newAudioSession)
                {
                    Log.Debug($"New {nameof(AudioSession)} '{newAudioSession.ProcessName}' ({newAudioSession.PID}) created; successfully added it to the list.");
                }
                else
                {
                    var existingSession = FindSessionByAudioSessionControl(audioSessionControl);
                    Log.Error($"New {nameof(AudioSession)} '{existingSession.ProcessName}' ({existingSession.PID}) created; but it was already in the list!");
                }
            }
            else
            {
                Log.Error($"New {nameof(AudioSession)} created, but no new session was found!");
            }
        }

        #region Events
        /// <summary>
        /// Occurs when an <see cref="AudioSession"/> is added to the <see cref="Sessions"/> list for any reason.
        /// </summary>
        public event EventHandler<AudioSession>? SessionAddedToList;
        private void NotifySessionAddedToList(AudioSession session) => SessionAddedToList?.Invoke(this, session);
        /// <summary>
        /// Occurs when an <see cref="AudioSession"/> is removed from the <see cref="Sessions"/> list for any reason.
        /// </summary>
        public event EventHandler<AudioSession>? SessionRemovedFromList;
        private void NotifySessionRemovedFromList(AudioSession session) => SessionRemovedFromList?.Invoke(this, session);
        #endregion Events

        #region Properties
        private static LogWriter Log => FLog.Log;
        internal AudioDevice AudioDevice { get; }
        internal AudioSessionManager2 AudioSessionManager { get; }

        /// <summary>
        /// Gets the list of <see cref="AudioSession"/> instances.
        /// </summary>
        public List<AudioSession> Sessions { get; }
        #endregion Properties

        #region Methods
        #region Methods (FindSession)
        /// <summary>
        /// Gets the <see cref="AudioSession"/> instance associated with the given <paramref name="sessionInstanceIdentifier"/>. (<see cref="AudioSessionControl2.SessionInstanceIdentifier"/>)
        /// </summary>
        /// <param name="sessionInstanceIdentifier">The SessionInstanceIdentifier GUID of the target session.</param>
        /// <param name="comparisonType">The <see cref="StringComparison"/> type to use when comparing ID strings.</param>
        /// <returns>The <see cref="AudioSession"/> associated with the given <paramref name="sessionInstanceIdentifier"/> if found; otherwise <see langword="null"/>.</returns>
        public AudioSession? FindSessionBySessionInstanceIdentifier(string sessionInstanceIdentifier, StringComparison comparisonType = StringComparison.Ordinal)
            => Sessions.FirstOrDefault(session => session.AudioSessionControl.SessionInstanceIdentifier.Equals(sessionInstanceIdentifier, comparisonType));
        /// <summary>
        /// Gets the <see cref="AudioSession"/> instance associated with the given <paramref name="audioSessionControl"/> instance.
        /// </summary>
        /// <param name="audioSessionControl">The <see cref="AudioSessionControl2"/> instance associated with the target session.</param>
        /// <returns>The <see cref="AudioSession"/> associated with the given <paramref name="audioSessionControl"/> if found; otherwise <see langword="null"/>.</returns>
        public AudioSession? FindSessionByAudioSessionControl(AudioSessionControl2 audioSessionControl)
            => FindSessionBySessionInstanceIdentifier(audioSessionControl.SessionInstanceIdentifier);
        #endregion Methods (FindSession)

        /// <summary>
        /// Creates a new <see cref="AudioSession"/> from the given <paramref name="audioSessionControl"/> and adds it to the <see cref="Sessions"/> list, and triggers the <see cref="SessionAddedToList"/> event.
        /// </summary>
        /// <param name="audioSessionControl">The <see cref="AudioSessionControl2"/> instance to create a new <see cref="AudioSession"/> from.</param>
        /// <returns>The newly-created <see cref="AudioSession"/> instance if successful; otherwise <see langword="null"/> if <paramref name="audioSessionControl"/> is already associated with another session.</returns>
        private AudioSession? CreateAndAddSessionIfUnique(AudioSessionControl2 audioSessionControl)
        {
            if (FindSessionByAudioSessionControl(audioSessionControl) is null)
            {
                var newSession = new AudioSession(audioSessionControl);
                // connect important session events:
                newSession.SessionDisconnected += this.Session_SessionDisconnected;
                newSession.StateChanged += this.Session_StateChanged;
                Sessions.Add(newSession);

                NotifySessionAddedToList(newSession);

                return newSession;
            }
            else return null;
        }
        /// <summary>
        /// Removes the given <paramref name="session"/> from the <see cref="Sessions"/> list and disposes of it.
        /// </summary>
        /// <param name="session">An <see cref="AudioSession"/> instance to delete.</param>
        private void DeleteSession(AudioSession session)
        {
            // remove from Sessions list
            Sessions.Remove(session);
            // disconnect events just because we can
            session.SessionDisconnected -= this.Session_SessionDisconnected;
            session.StateChanged -= this.Session_StateChanged;

            Log.Debug($"{nameof(AudioSession)} '{session.ProcessName}' ({session.PID}) exited.");
            // notify that a session was removed
            NotifySessionRemovedFromList(session);
            // dispose of the session
            session.Dispose();
        }

        private void Session_SessionDisconnected(object sender, AudioSessionDisconnectReason disconnectReason)
            => DeleteSession((AudioSession)sender);
        private void Session_StateChanged(object? sender, AudioSessionState e)
        {
            if (e.Equals(AudioSessionState.AudioSessionStateExpired))
            {
                DeleteSession(sender as AudioSession);
            }
        }
        #endregion Methods
    }
}