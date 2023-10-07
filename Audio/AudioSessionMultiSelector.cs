using Audio.Interfaces;
using MockConfig;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VolumeControl.TypeExtensions;

namespace Audio
{
    /// <summary>
    /// Manages multiple "selected" audio sessions for a given <see cref="Audio.AudioSessionManager"/> instance.
    /// </summary>
    public class AudioSessionMultiSelector : IAudioMultiSelector, INotifyPropertyChanged
    {
        #region Initializer
        /// <summary>
        /// Creates a new <see cref="AudioSessionMultiSelector"/> instance for the specified <paramref name="audioSessionManager"/>.
        /// </summary>
        /// <param name="audioSessionManager">An <see cref="Audio.AudioSessionManager"/> instance to select from.</param>
        public AudioSessionMultiSelector(AudioSessionManager audioSessionManager)
        {
            AudioSessionManager = audioSessionManager;

            CurrentIndex = -1;
            _selectionStates = new();
            for (int i = 0; i < AudioSessionManager.Sessions.Count; ++i)
            {
                _selectionStates.Add(false); //< TODO: set this based on config
            }

            AudioSessionManager.AddedSessionToList += this.AudioSessionManager_SessionAddedToList;
            AudioSessionManager.RemovingSessionFromList += this.AudioSessionManager_RemovingSessionFromList; ;
        }
        #endregion Initializer

        #region Properties
        private static Config Settings => (Config.Default as Config)!;
        AudioSessionManager AudioSessionManager { get; }
        /// <inheritdoc/>
        public IReadOnlyList<bool> SelectionStates
        {
            get => _selectionStates;
            set
            {
                if (value.Count != AudioSessionManager.Sessions.Count) // enforce size limits
                    throw new ArgumentOutOfRangeException(nameof(SelectionStates), $"The length of the {nameof(SelectionStates)} array ({value.Count}) must be equal to the length of the {nameof(AudioSessionManager.Sessions)} list ({AudioSessionManager.Sessions.Count})!");

                if (LockSelection) return;

                _selectionStates = (List<bool>)value;
                NotifyPropertyChanged();
            }
        }
        private List<bool> _selectionStates;
        /// <summary>
        /// Gets or sets the list of selected AudioSession instances.
        /// </summary>
        /// <returns>An array of all selected AudioSessions in the order that they appear in the Sessions list.</returns>
        public AudioSession[] SelectedItems
        {
            get
            {
                List<AudioSession> l = new();
                for (int i = 0; i < SelectionStates.Count; ++i)
                {
                    if (SelectionStates[i])
                    {
                        l.Add(AudioSessionManager.Sessions[i]);
                    }
                }
                return l.ToArray();
            }
            set
            {
                for (int i = 0; i < SelectionStates.Count; ++i)
                {
                    _selectionStates[i] = value.Contains(AudioSessionManager.Sessions[i]);
                }
                NotifyPropertyChanged();
            }
        }
        /// <inheritdoc/>
        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                if (LockCurrentIndex) return;

                _currentIndex = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(CurrentItem));
            }
        }
        private int _currentIndex;
        /// <inheritdoc/>
        public bool LockCurrentIndex
        {
            get => _lockCurrentIndex;
            set
            {
                _lockCurrentIndex = value;
                NotifyPropertyChanged();
            }
        }
        private bool _lockCurrentIndex;
        /// <summary>
        /// Gets or sets the item that the selector is currently at.
        /// </summary>
        public AudioSession? CurrentItem
        {
            get => CurrentIndex == -1 ? null : AudioSessionManager.Sessions[CurrentIndex];
            set
            {
                if (LockCurrentIndex) return;

                if (value == null)
                {
                    CurrentIndex = -1;
                }
                else // value is a valid session
                {
                    CurrentIndex = AudioSessionManager.Sessions.IndexOf(value);
                }
            }
        }
        /// <inheritdoc/>
        public bool LockSelection
        {
            get => _lockSelection;
            set
            {
                _lockSelection = value;
                NotifyPropertyChanged();
            }
        }
        private bool _lockSelection;
        #endregion Properties

        #region Events
        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        /// <summary>
        /// Occurs when a session is selected for any reason.
        /// </summary>
        public event EventHandler<AudioSession>? SessionSelected;
        private void NotifySessionSelected(AudioSession audioSession) => SessionSelected?.Invoke(this, audioSession);
        /// <summary>
        /// Occurs when a session is deselected for any reason.
        /// </summary>
        public event EventHandler<AudioSession>? SessionDeselected;
        private void NotifySessionDeselected(AudioSession audioSession) => SessionDeselected?.Invoke(this, audioSession);
        #endregion Events

        #region Methods

        #region Get/Set SessionSelectionState
        /// <summary>
        /// Gets the selection state of the specified <paramref name="audioSession"/>.
        /// </summary>
        /// <param name="audioSession">An <see cref="AudioSession"/> instance.</param>
        /// <returns><see langword="true"/> when the <paramref name="audioSession"/> is selected; otherwise <see langword="false"/>.</returns>
        public bool GetSessionSelectionState(AudioSession audioSession)
        {
            var index = AudioSessionManager.Sessions.IndexOf(audioSession);
            if (index == -1) return false;
            return SelectionStates[index];
        }
        /// <summary>
        /// Sets the selection state of the specified <paramref name="audioSession"/>.
        /// </summary>
        /// <param name="audioSession">An <see cref="AudioSession"/> instance.</param>
        /// <param name="isSelected"><see langword="true"/> selects the specified <paramref name="audioSession"/>; <see langword="false"/> deselects the specified <paramref name="audioSession"/>.</param>
        /// <returns><see langword="true"/> when the selection state was successfully changed; otherwise <see langword="false"/>.</returns>
        public bool SetSessionSelectionState(AudioSession audioSession, bool isSelected)
        {
            var index = AudioSessionManager.Sessions.IndexOf(audioSession);
            if (index == -1) return false;

            if (_selectionStates[index] = isSelected)
            {
                NotifySessionSelected(audioSession);
            }
            else
            {
                NotifySessionDeselected(audioSession);
            }
            return true;
        }
        #endregion Get/Set SessionSelectionState

        #region Select/Deselect/ToggleSelect CurrentItem
        /// <summary>
        /// Selects the CurrentItem.
        /// </summary>
        /// <remarks>
        /// Does nothing when <see cref="LockSelection"/> is <see langword="true"/> or <see cref="CurrentIndex"/> is -1.
        /// </remarks>
        public void SelectCurrentItem()
        {
            if (LockSelection || CurrentIndex == -1) return;

            _selectionStates[CurrentIndex] = true;
            NotifySessionSelected(CurrentItem!);
        }
        /// <summary>
        /// Deselects the CurrentItem.
        /// </summary>
        /// <remarks>
        /// Does nothing when <see cref="LockSelection"/> is <see langword="true"/> or <see cref="CurrentIndex"/> is -1.
        /// </remarks>
        public void DeselectCurrentItem()
        {
            if (LockSelection || CurrentIndex == -1) return;

            _selectionStates[CurrentIndex] = false;
            NotifySessionDeselected(CurrentItem!);
        }
        /// <summary>
        /// Toggles whether the CurrentItem is selected.
        /// </summary>
        /// <remarks>
        /// Does nothing when <see cref="LockSelection"/> is <see langword="true"/> or <see cref="CurrentIndex"/> is -1.
        /// </remarks>
        public void ToggleSelectCurrentItem()
        {
            if (LockSelection || CurrentIndex == -1) return;

            if (_selectionStates[CurrentIndex] = !SelectionStates[CurrentIndex])
            {
                NotifySessionSelected(CurrentItem!);
            }
            else
            {
                NotifySessionDeselected(CurrentItem!);
            }
        }
        #endregion Select/Deselect/ToggleSelect CurrentItem

        #region Increment/Decrement/Unset CurrentIndex
        /// <summary>
        /// Increments the CurrentIndex by 1, looping back around to 0 when it exceeds the length of the Sessions list.
        /// </summary>
        /// <remarks>
        /// Does nothing when <see cref="LockCurrentIndex"/> is <see langword="true"/>.
        /// </remarks>
        public void IncrementCurrentIndex()
        {
            if (LockCurrentIndex) return;

            if (CurrentIndex + 1 < SelectionStates.Count)
                ++CurrentIndex;
            else
            { // loopback:
                CurrentIndex = 0;
            }
        }
        /// <summary>
        /// Decrements the CurrentIndex by 1, looping back around to the length of the Sessions list when it goes past 0.
        /// </summary>
        /// <remarks>
        /// Does nothing when <see cref="LockCurrentIndex"/> is <see langword="true"/>.
        /// </remarks>
        public void DecrementCurrentIndex()
        {
            if (LockCurrentIndex) return;

            if (CurrentIndex > 0)
                --CurrentIndex;
            else
            { // loopback:
                CurrentIndex = SelectionStates.Count;
            }
        }
        /// <summary>
        /// Sets the CurrentIndex to 0.
        /// </summary>
        /// <remarks>
        /// Does nothing when <see cref="LockCurrentIndex"/> is <see langword="true"/>.
        /// </remarks>
        public void UnsetCurrentIndex()
        {
            if (LockCurrentIndex) return;

            CurrentIndex = -1;
        }
        #endregion Increment/Decrement/Unset CurrentIndex

        #endregion Methods

        #region EventHandlers

        #region AudioSessionManager
        private void AudioSessionManager_SessionAddedToList(object? sender, AudioSession e)
        {
            var index = AudioSessionManager.Sessions.IndexOf(e);
            var isSelected = Settings.TargetSessions.Any(targetInfo
                => targetInfo.PID.Equals(e.PID) //< PID can match when sessions are hidden/unhidden
                || targetInfo.ProcessName.Equals(e.ProcessName, StringComparison.Ordinal));
            _selectionStates.Insert(index, isSelected);
            if (isSelected)
            {
                NotifySessionSelected(e);
            }
        }
        private void AudioSessionManager_RemovingSessionFromList(object? sender, AudioSession e)
        {
            var index = AudioSessionManager.Sessions.IndexOf(e); //< we can get the index here because the session hasn't been removed yet
            var wasSelected = _selectionStates[index];
            _selectionStates.RemoveAt(index);
            if (wasSelected)
            {
                NotifySessionDeselected(e);
            }
        }
        #endregion AudioSessionManager

        #endregion EventHandlers
    }
}
