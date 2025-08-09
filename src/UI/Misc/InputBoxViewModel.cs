using eft_dma_radar.Misc;
using System.Windows.Input;

namespace eft_dma_radar.UI.Misc
{
    public sealed class InputBoxViewModel : INotifyPropertyChanged
    {
        public string Title { get; }
        public string Prompt { get; }
        private string _inputText;
        public string InputText
        {
            get => _inputText;
            set
            {
                if (value == _inputText) return;
                _inputText = value;
                OnPropertyChanged(nameof(InputText));
            }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        // Fires when either command runs; subscriber should close the window
        public event EventHandler<CloseRequestedEventArgs> CloseRequested;

        public InputBoxViewModel(string title, string prompt)
        {
            Title = title;
            Prompt = prompt;
            OkCommand = new SimpleCommand(() => OnCloseRequested(true));
            CancelCommand = new SimpleCommand(() => OnCloseRequested(false));
        }

        private void OnCloseRequested(bool result)
            => CloseRequested?.Invoke(this, new CloseRequestedEventArgs(result));

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public class CloseRequestedEventArgs : EventArgs
        {
            public bool DialogResult { get; }
            public CloseRequestedEventArgs(bool dialogResult) => DialogResult = dialogResult;
        }
    }
}
